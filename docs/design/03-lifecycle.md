# Lifecycle — 生命周期引擎

> **状态：🚫 已退役（2026-09-02）** —— 生命周期引擎不实施；本文件转为历史存档
> 决策评估见 [2026-09-02 决策规格](../../superpowers/specs/2026-09-02-lifecycle-boot-simplification-design.md) 问题 1/2：
> 两阶段引擎本质上是针对 DI V1「注入时机不定」弱点的补偿结构；DI V2 构造注入消解根因后，
> 初始化/启动语义由「构造即初始化 + 显式启动点」承载（composition root：Build → Resolve → Start，
> 已落地为 GameComposition / GameFlow）。若将来 ≥2 个真实服务需要「全图构造完、零副作用窗口」，
> 按决策规格中方案 B（约 30 行轻量两阶段层）升级——A 是 B 的子集，升级平滑。
> UpdateRunner 不迁移：「替代 Unity 原生 Update」的四条原始理由经逐条核验均不成立；
> 替代路径 = UITK scheduler（每帧动效）/ 战斗引擎动作队列（连贯节奏）/ 逻辑步进（无头模拟）。
> 处置：根目录旧 `Lifecycle/` 源码已于 2026-09-02 删除；不建 Lifecycle 相关程序集。
>
> 以下为退役前的历史内容（旧实现与已废弃目标形态），仅存档不再维护。

## 旧实现（迁移输入）

```text
根目录 Lifecycle/（非编译域）
├─ 接口          IInitializable / IStartable / ITickable / IInstaller / IScopeProvider
├─ LifecycleRegistry（静态单例注册表）
│  ├─ 顺序保证    InitializeAll → StartAll：所有 Initialize 完成后才开始 OnStart
│  ├─ 动态注册    init/start 进行中或完成后注册的组件进入 pending 队列补跑
│  ├─ 依赖注入    DI V1 Container.Inject（TryInjectDependencies / ProcessDelayedInjection）
│  ├─ 错误隔离    单组件失败记 UnityEngine.Debug.LogError，不阻断其余组件
│  └─ 诊断        DumpState / 计数属性 / Tickable 注册事件
├─ StrictLifecycleMonoBehaviour
│  ├─ 封存 Awake()/Start() 为 private；子类只重写 OnInitialize / OnStartExternal / Tick / OnShutdown
│  └─ Awake 注册 → OnDestroy 先 OnShutdown 再注销
├─ UpdateRunner   快照式逐帧 Tick + 错误隔离；订阅注册表的 Tickable 注册事件
└─ ScopeProvider  当前场景 Scope 追踪
```

## 目标形态（feat-003 D2）

```text
RazorFramework.Lifecycle（纯 C#，实例化）
├─ 生命周期接口迁入（IInitializable / IStartable / ITickable / IInstaller）
├─ LifecycleEngine（替代静态注册表）
│  ├─ 顺序保证（同旧语义）
│  ├─ ILifecycleLog 日志槽       替换 UnityEngine.Debug 耦合
│  └─ Action<object> 注入缝      替换 DI V1 Container.Inject
└─ 去静态：支持多容器/多引擎场景

RazorFramework.Unity.Lifecycle
├─ StrictLifecycleMonoBehaviour   接入注入缝
└─ UpdateRunner
```

## 保留不变量（迁移必须保留）

- 两阶段顺序：全部 Initialize 完成 → 开始 OnStart。
- 动态注册补跑（pending 队列语义）。
- 单组件失败隔离，不阻断全局流程。
- Tick 快照遍历：注册表变更不打断当帧。

## 已知限制

- 旧实现为静态单例注册表，仅适配单一 ProjectContext；目标改为实例引擎（D2）。
- 旧实现失败处理为吞异常记日志；目标形态的错误策略（是否抛/聚合）需在迁移时定案。
- 旧实现强耦合 DI V1 注入与 `UnityEngine.Debug`，与纯 C# 目标冲突，必须重写。
