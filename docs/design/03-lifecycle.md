# Lifecycle — 生命周期引擎

> 旧实现：根目录 `Lifecycle/`（非编译域，引用已删除的 DI V1 API）
> 目标程序集：`RazorFramework.Lifecycle`（纯 C#）＋ `RazorFramework.Unity.Lifecycle`（Unity，refs DI/Lifecycle/Unity.DI）
> 状态：⏳ 迁移中（feat-003 Task 3 未开始） · 重写输入而非机械迁移

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
