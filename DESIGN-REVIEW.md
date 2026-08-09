# CardGame 架构审查与已知限制

**更新日期：** 2026-08-09
**当前已完成功能：** feat-002 Pure C# DI Boundary

本文只记录 CardGame 当前已验证的架构结论与尚未解决的风险。旧 RazorFramework 审计是重构输入，不可被当作现有运行时的验收结论。

## 已关闭：旧 DI 边界债务

旧根目录 `DI/DIContainer.cs` 已删除。CardGame 当前唯一 DI 实现为：

- `Assets/Plugins/RazorFramework/DI/`：`RazorFramework.DI`，纯 C#，程序集启用 `noEngineReferences: true`；
- `Assets/Plugins/RazorFramework/Unity/DI/`：`RazorFramework.Unity.DI`，把 Unity 对象成员注入隔离为显式适配器。

DI V2 在 `Build()` 时冻结注册并验证依赖图；支持 Singleton、具层级关系的 Scoped、Transient、逆序释放、集合解析和非侵入式诊断。Harness 会拒绝旧根目录 DI 实现、DI 核心中的 `UnityEngine` 记号或外来命名空间，并在完整模式实际编译并执行 EditMode 测试。

因此，原先“DI 核心中通过 `UnityEngine.Object` 作空值判断”的编译边界债务已关闭，不再是 Harness 警告项。

## 当前可接受的边界

| 边界 | 现状 | 维护要求 |
|---|---|---|
| DI 核心 | BCL 与 `RazorFramework.DI` 自身 | 不向核心添加 Unity 类型、MonoBehaviour、ScriptableObject 或游戏玩法规则 |
| Unity 注入 | 反射式字段/属性赋值 | 仅在 owner thread 调用；通过 `[Inject]` 与 `[InjectOptional]` 表达成员依赖 |
| 生命周期 | 容器、作用域和可释放对象 | 用 `using` 或等价的确定性释放管理容器与作用域；不依赖 GC 管理业务资源 |
| 游戏玩法 | 尚未接入 DI V2 | 卡牌、伤害结算、地图和叙事系统在独立功能中设计，不能把玩法规则加入 RazorFramework |

## 当前已知限制

1. Unity 注入器记录的是其构造时的托管线程（owner thread），不是通用的主线程调度机制。调用方负责在 Unity 线程创建并调用它。
2. Unity 注入仅覆盖字段和可写的非索引实例属性；静态、只读、常量、索引属性、无 setter 属性，以及同时标记必需和可选注入的成员都会失败。
3. 反射注入已由 Unity `6000.3.10f1` 的 EditMode 覆盖，但 **IL2CPP、linker stripping 与 AOT 兼容性尚未证明**。在任何目标平台发布前，必须添加并审查 `link.xml`/保留策略、执行对应平台构建验证；若该维护成本不可接受，应采用生成式或显式注入替代反射。
4. feat-003 尚未开始。`Boot`、`Events`、`Input`、`Lifecycle`、`MVVM` 仍在仓库根目录，未进入 Unity 编译范围；它们的程序集依赖边界需要在 feat-003 单独设计与验证。
5. 当前验证证明 Editor 编译和 EditMode 行为，不证明场景运行时流程、玩家输入、存档、网络或实际肉鸽玩法。

## 下一阶段审查重点

feat-003 应先进行 brainstorming，确认其程序集拆分图、允许的依赖方向、测试程序集引用与迁移顺序，再创建实施计划。它不得以降低 DI 纯 C# 门禁为代价换取迁移便利。
