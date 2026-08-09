# 旧 RazorFramework 历史审计快照

**快照来源：** `01ade590338cd683785fb3604d9fb22270b6c266:DESIGN-REVIEW.md`
**原审查日期：** 2026-07-19
**迁存日期：** 2026-08-10
**状态：** 历史重构输入，等待 feat-003 逐项复核

## 使用说明

本文恢复 DI V2 分支基线中被覆盖的旧框架风险清单。它描述的是从 Unsaid Goodbye 提取的旧 `RazorFramework`，不是 CardGame 当前运行时代码的验收报告。

截至迁存时：

- CardGame 已有 Unity `6000.3.10f1` 项目宿主，旧文中的“缺少 Unity 宿主”已不成立；
- feat-002 已用 `Assets/Plugins/RazorFramework/DI/` 的 DI V2 替换旧根 DI，DI 结论以当前 `DESIGN-REVIEW.md` 为准；
- 根目录 `Boot/`、`Events/`、`Input/`、`Lifecycle/`、`MVVM/` 仍是未迁入 Unity 编译范围的旧源码；
- 旧审计中“已在提取版本修复”的描述只代表历史动作，不能证明 CardGame 当前实现正确；
- feat-003 必须重新检查真实源码、建立引用图并用 Unity 编译和测试验证。

## 历史总体评价

旧框架包含自研 DI、生命周期管理、MVVM 绑定和强类型事件总线，工程意识明显强于一般原型项目；同时，程序集隔离、框架与玩法耦合、静态全局状态和 Unity 依赖倒灌等问题会降低复用性与可测试性。以下风险按原审计严重度保存。

## 严重风险

### 1. 缺少 Assembly Definition，模块边界无法由编译器强制

**历史现象：** 旧框架 80 余个 C# 文件进入默认 `Assembly-CSharp.dll`，没有 asmdef。

**影响：** 纯 C# ViewModel/Command 可以无意引用 Unity；模块依赖方向只存在于文档；任意脚本改动扩大增量编译范围；测试难以只引用最小依赖。

**历史建议拆分：**

```text
RazorFramework.DI          -> BCL only
RazorFramework.Lifecycle   -> DI + Unity
RazorFramework.Events      -> Lifecycle
RazorFramework.MVVM        -> Events + DI；Unity 仅限 view/binding
RazorFramework.Input       -> Lifecycle + Unity Input System
RazorFramework.Boot        -> 上述模块 + Unity/Addressables
CardGame.Gameplay          -> RazorFramework
```

**当前处理：** DI V2 已有独立 asmdef；其余模块仍待 feat-003 逐个迁入和验证，不能一次性把全部旧源码暴露给 Unity 编译。

### 2. ProjectContext 与游戏特定代码耦合

**历史现象：** `Core.Boot.ProjectContext.SetupGlobalViews()` 直接引用 `EndingDirector`、`InteractionPromptView`、`InterstitialScreen`、`GameFlowView` 等 Gameplay 类型，并发布 `GameReadyEvent`。

**影响：** Boot 层不能独立复用，玩法类型会反向污染框架程序集。

**历史建议：** 把游戏初始化改为项目侧 installer 或显式扩展点，例如 `OnGlobalViewsReady()`、`OnBootComplete()`。

**待复核：** feat-003 检查真实 `Boot/` 源码，决定继承钩子、组合式 installer 或启动管线事件，并用 asmdef 固定依赖方向。

### 3. 旧 DIContainer 无条件输出日志

**历史现象：** `DIContainer.Register()` 每次注册调用 `Debug.Log`，导致 Core 依赖 Unity 且日志不可关闭。

**当前处理：** 旧根 DI 已删除；DI V2 使用可选 `IDiDiagnosticSink`，诊断异常不会改变容器行为。本条仅作为不回归背景。

## 中等风险

### 4. LifecycleRegistry 使用全静态状态

**历史现象：** 生命周期注册表由静态字段和方法组成，仅由 `ProjectBootstrap.ResetStatics()` 在域重载时清理。

**影响：** 测试共享状态；Domain Reload 配置变化时容易残留；无法自然支持多个容器或隔离 session；生命周期所有权不清晰。

**待复核建议：** 先为当前顺序建立测试，再评估改为容器持有的实例服务。若保留静态方案，必须定义 reset 契约和测试隔离。

### 5. RelayCommand 把 Unity 日志依赖带入纯 C# Command

**历史现象：** `MVVM.Commands.RelayCommand` 调用 `UnityEngine.Debug.LogError`。

**影响：** ViewModel/Command 无法保持纯 C#。

**历史建议：** 抛出含上下文的异常，或注入纯 C# 错误报告接口，由 View/宿主决定如何记录。feat-003 必须扫描真实文件并用编译门禁验证，不能只信历史“已修复”备注。

### 6. EventManager.Initialize() 是空实现

**历史现象：** `EventManager` 实现 `IInitializable`，但 `Initialize()` 无行为，只为适配 installer 的多接口注册。

**影响：** 生命周期接口语义不清，维护者无法判断空实现是占位、遗漏还是必要适配。

**待复核建议：** 若无需初始化则移除接口；若为统一编排保留，增加清晰注释和契约测试。

## 轻微风险

### 7. ProjectBootstrap 硬编码相机和美术参数

**历史现象：** `FixCameraFor1080p()` 固定 `orthoSize = 5.4f` 和背景色。

**影响与建议：** 游戏美术配置不应进入通用 Boot；迁入时放入 CardGame 配置资产或项目侧组件。

### 8. 游戏事件定义混入框架事件模块

**历史现象：** `EventStructs.cs` 包含 `InteractionEvent`、`DialogueEndedEvent`、`StoryBeatCompletedEvent` 等业务事件。

**影响与建议：** 框架只提供发布/订阅机制；地图、战斗、叙事和 UI 事件放在 CardGame 对应功能程序集。

### 9. CoreInstaller 注册游戏特定服务

**历史现象：** `CoreInstaller` 注册 `IPlayerInput -> PlayerInputManager` 和 `IViewModelFactory -> ViewModelFactory`。

**影响与建议：** 基础设施安装器不应知道项目级输入和视图工厂；拆分框架 installer 与 CardGame installer。

## 历史亮点与保留候选

| 能力 | 保留理由 | feat-003 复核问题 |
|---|---|---|
| 严格生命周期顺序 | 降低 Unity 隐式回调混乱 | 是否仍依赖静态注册表，异常策略是否明确 |
| 强类型值类型事件 | 编译时约束清晰，可控制 GC | 是否把玩法事件和框架机制分开 |
| 代码式 MVVM 绑定 | 比 Inspector 字符串更可重构 | ViewModel/Command 是否真正纯 C# |
| Installer 编排 | 有利于组合不同宿主 | 是否存在框架到 Gameplay 的反向依赖 |
| 明确协作规则 | 适合 Harness 持久化 | 是否有自动化验证，而非只靠文档 |

DI 容器不再是保留候选：feat-002 已以 DI V2 重新实现并验证，后续只使用当前规格。

## feat-003 复核清单

1. 枚举根 `Boot/`、`Events/`、`Input/`、`Lifecycle/`、`MVVM/` 的真实文件和 namespace；
2. 画出当前引用图，区分 BCL、Unity、Input System、Addressables 和 Gameplay；
3. 为每个纯 C# 边界先写可失败的 Harness fixture；
4. 决定迁移顺序和每步可独立编译的 asmdef；
5. 先迁最底层模块，每步运行 Unity EditMode；
6. 对每条风险标记“仍存在、已由证据关闭、设计已改变”；
7. 不因迁移方便降低 DI V2 的 `noEngineReferences` 或唯一实现门禁；
8. 把新结论写回 `DESIGN-REVIEW.md`，本文继续作为不可覆盖的历史来源。

## 当前结论

这份审计保存已发现的问题和重构方向，不证明旧框架可直接迁入。feat-003 必须以当前源码、编译结果和测试为准，任何历史“已修复”声明都需要新证据。
