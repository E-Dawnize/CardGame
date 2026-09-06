# 场景 [Inject] 成员注入驱动设计

**状态：** 已决策并执行（2026-09-06）
**日期：** 2026-09-06
**关联：** [di-internals.md](../../design/di-internals.md) §13（Unity 适配层）· [01-di.md](../../design/01-di.md)（不切实边界）· [09-ui-resources.md](../../design/09-ui-resources.md)（战斗屏 = EncounterScope）
**决策结果：** 注入上下文按「对象生命周期层」对齐；本切片 = 根驱动落地 + scope 接缝测试背书（用户确认方案 A）

---

## 0. 问题

`RazorFramework.Unity.DI` 的 `UnityObjectInjector` + `[Inject]` / `[InjectOptional]` 机制已完备且有行为测试，但**无驱动**：标记只是元数据，没有任何游戏代码调用 `Inject()`，场景脚本的 `[Inject]` 成员永远不会被赋值（di-internals §13.3「当前无消费者」）。容器外对象（Unity 实例化的 MonoBehaviour）无法走构造注入，其消费 DI 服务的唯一通道当前是断的。

## 1. 核心决策：注入上下文按对象生命周期层对齐

**法则：对象的生命周期必须 ≤ 它被注入的 resolver 的生命周期。**

| 对象 | 注入上下文 | 可拿到的服务 |
|---|---|---|
| App 级常驻场景对象 | 根容器（本切片落地） | 仅 Singleton |
| 单局屏（地图/商店） | RunScope 实例（未来流程状态机驱动） | Run Scoped + Singleton |
| 战斗屏组件 | EncounterScope 实例（未来） | Encounter/Run Scoped + Singleton |

与 `09-ui-resources` 既定对齐咬合：**战斗屏 = EncounterScope，屏的生命周期就是 scope 的生命周期**——scope 拥有的屏必须先于 scope 销毁（驱动方负责）。

机制事实（零框架行为改动的前提）：`ServiceScope : IServiceResolver`，故 `new UnityObjectInjector(scope)` 天然获得作用域解析语义；根护栏现成——根 resolver 上解析 Scoped 服务抛 `ScopeMismatch`。

**不做的（YAGNI）**：scope → 屏工厂雏形（战斗流程状态机未立项，无真实消费者）；运行时动态创建对象的自动注入（纪律 = 谁创建谁注入，文档记录）。

## 2. 组件与改动清单

```text
新增  Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/SceneInjection.cs
      public static class SceneInjection
      {
          public static void InjectScene(IServiceResolver resolver)
          // new UnityObjectInjector(resolver) +
          // FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,
          //                                  FindObjectsSortMode.None) 逐个 Inject
          // 任一必需依赖缺失 → 原样抛 UnityInjectionException（fail-fast）
      }

修改  Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/GameBootstrap.cs
      + [DefaultExecutionOrder(-32000)]   // 全场景最先 Awake，注入先于其他脚本 Awake/OnEnable
      Awake：LoadRepository → new GameComposition(data)
             → SceneInjection.InjectScene(composition.Container) → StartFlow()

修改  Assets/CardGame/Runtime/CardGame.Runtime/CardGame.Runtime.asmdef
      references += "RazorFramework.Unity.DI"

修改  Assets/CardGame/Tests/EditMode/CardGame.Tests.EditMode.asmdef
      references += "RazorFramework.Unity.DI"

修改  Assets/Plugins/RazorFramework/Unity/DI/AssemblyInfo.cs（一行）
      += [assembly: InternalsVisibleTo("CardGame.Tests.EditMode")]
      // UnityMainThread.InitializeForTests 为 internal，游戏侧 EditMode 测试需建主线程身份；
      // 不改 public，保持「仅供测试」的既定设计（di-internals §13.2）
```

框架行为（注入器、线程守护、异常模型）一律不动。

## 3. 时序保证

1. `[DefaultExecutionOrder(-32000)]` 使 GameBootstrap.Awake 为全场景第一个 Awake；
2. 此时其他脚本 Awake/OnEnable 尚未运行 → 扫描注入完成后它们可安全使用 `[Inject]` 成员；
3. inactive 对象一并注入（字段先就绪，激活时其 Awake 才运行）；
4. 注入先于 `StartFlow()`（显式启动屏障语义保持：全部注入完成后才开始交互）。

## 4. 错误处理

- 必需依赖未注册：`UnityInjectionException(MissingDependency)`（TargetType/MemberName/ServiceType 齐全）从 Awake 原样抛出——注入错误全部暴露在启动期，不静默跳过（对齐 Build() 哲学）。
- 根扫描注入 Scoped 服务：`ScopeMismatch` 原样抛出（现成护栏，防「常驻对象捕获局状态」的视图版 captive）。
- 主线程守卫、null/已销毁对象 no-op、可选成员跳过：沿用框架既有语义。

## 5. 测试计划（TDD，先失败后实现）

新文件 `Assets/CardGame/Tests/EditMode/Bootstrap/SceneInjectionTests.cs`，`[SetUp]` 调 `UnityMainThread.InitializeForTests()`：

| # | 测试 | 背书 |
|---|---|---|
| T0（装配线冒烟） | `Injector_FromGameTestAssembly_InjectsComponent`：最小容器 + `UnityObjectInjector` 直接注入本地 `[Inject]` 组件 | 游戏测试程序集可驱动注入器（asmdef 引用 + InternalsVisibleTo 装配线可见性） |
| T1 | 场景扫描注入 active 对象 `[Inject]` 字段，实例与容器解析同一 | 根驱动生效 |
| T2 | inactive 对象也被注入 | Include（含 inactive） |
| T3 | `[InjectOptional]` 未注册 → 跳过不抛 | 可选语义 |
| T4 | 必需未注册 → `MissingDependency` 且三件套字段齐全 | fail-fast |
| T5 | 根扫描注入 Scoped 服务 → `ScopeMismatch` | 对齐法则护栏 |
| T6 | RunScope 内 `new UnityObjectInjector(runScope).Inject(comp)`：Run 级 Scoped + Singleton 均可达 | scope 接缝模式（「跟着 scope 走」） |
| T7 | 反射断言 `GameBootstrap` 带 `DefaultExecutionOrder` 且 ≤ -30000 | 时序保证 |

## 6. 文档同步

- `di-internals.md` §13.3：「当前无消费者」→ 根驱动 + scope 接缝 + 对齐法则；
- `docs/design/README.md` 全局风险 3（注入器无消费者）→ 关闭/更新；
- GameBootstrap / SceneInjection 代码注释承载对齐法则与「谁创建谁注入」纪律；
- `progress.md` / `session-handoff.md` 按完成条件更新（含 Unity EditMode 实际运行证据或记录限制）。

## 7. 不变量

- RazorFramework.DI / RazorFramework.Events / Unity.DI 的公开面与行为零变化（仅 +1 行 InternalsVisibleTo）；
- 视图仍不进容器注册表（不建视图级 DI 图，01-di「不切实边界」不变）；
- 所有注入错误在启动期全量暴露，无静默降级。
