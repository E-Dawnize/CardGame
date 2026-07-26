# 设计审查报告 — Unsaid Goodbye 项目框架分析

> 审查日期：2026-07-19  
> 审查范围：`Assets/Core/`、`Assets/MVVM/`、`Assets/Input/`  
> 原则依据：Harness Engineering 五子系统模型 + Superpowers 方法论

---

## 一、总体评价

该项目拥有**自制的高水平工程框架**（DI 容器、生命周期管理、MVVM 绑定、事件总线），代码质量和架构设计远超一般 Unity 项目。CLAUDE.md 中的约束规则清晰，可作为团队协作的可靠基线。

但仍有若干设计问题值得关注，按严重性排列如下。

---

## 二、设计问题（按严重性排序）

### 🔴 严重

#### 1. 无 Assembly Definition 文件 — 无程序集隔离

**现象**：所有 80+ .cs 文件编译到默认 `Assembly-CSharp.dll`，无任何 `.asmdef`。

**影响**：
- 无法防止模块间的意外引用（例如 ViewModel 引用 `UnityEngine.UI`）
- 编译时无依赖边界检查，耦合会随时间逐步累积
- 大项目增量编译慢（修改任意脚本重编译全部）

**建议**：按命名空间拆分为：
```
RazorFramework.DI.asmdef        → Core.DI（零依赖）
RazorFramework.Lifecycle.asmdef → Core.Architecture + Interfaces（依赖 DI）
RazorFramework.Events.asmdef    → Core.Events（依赖 Lifecycle）
RazorFramework.MVVM.asmdef      → MVVM.*（依赖 Events）
RazorFramework.Input.asmdef     → Input.*（依赖 Lifecycle）
RazorFramework.Boot.asmdef      → Core.Boot（依赖以上全部 + Addressables）
Gameplay.asmdef                 → Gameplay.*（依赖 Framework）
```

#### 2. ProjectContext 与游戏特定代码耦合

**现象**：`Core.Boot.ProjectContext` 的 `SetupGlobalViews()` 方法直接引用了 4 个 Gameplay 命名空间的类型（`EndingDirector`, `InteractionPromptView`, `InterstitialScreen`, `GameFlowView`），并通过 `PublishGameReady()` 发布游戏特定事件 `GameReadyEvent`。

**影响**：启动引导系统无法跨项目复用，每次新项目需要修改 Core 代码。

**建议**：将游戏特定初始化提取为 **protected virtual 钩子方法**（已在提取版本中修复）：
```csharp
protected virtual void OnGlobalViewsReady() { }  // 项目在此创建全局 View
protected virtual void OnBootComplete() { }       // 项目在此发布 GameReady
```
项目侧创建 `GameProjectContext : ProjectContext` 重写这些钩子。

#### 3. DIContainer 无条件 Debug.Log

**现象**：`DIContainer.Register()` 第 93 行无条件执行 `Debug.Log(descriptor.ServiceType.FullName + " is registered")`。

**影响**：每次注册都产生日志噪音；发布版本中无法关闭。

**建议**：通过可选的 `Action<string>` 委托暴露日志接口（已在提取版本中修复）。

### 🟡 中等

#### 4. LifecycleRegistry 是纯静态类 — 不支持测试和多容器

**现象**：`LifecycleRegistry` 全部使用 `static` 字段和方法。

**影响**：
- 单元测试中无法隔离不同测试用例的状态
- 域重载（Domain Reload）时必须手动 `Clear()`
- 不支持多个独立 DI 容器实例的场景

**缓解**：当前项目通过 `ProjectBootstrap.ResetStatics()` 在域重载时清理，基本可用。
**建议**：长期考虑将 LifecycleRegistry 改为实例模式并注册到 DI 容器。

#### 5. RelayCommand 使用 UnityEngine.Debug.LogError

**现象**：`MVVM.Commands.RelayCommand` 第 24 行调用 `Debug.LogError`。

**影响**：CLAUDE.md 声明 "ViewModel 不引用 `UnityEngine`"，但 RelayCommand 是 ViewModel 的 Command 基类，它引用了 Unity。

**建议**：移除 Debug.LogError，改为抛出包含完整上下文的异常，由调用方（View 层）处理日志（已在提取版本中修复）。

#### 6. EventManager.Initialize() 是空方法

**现象**：`EventManager` 实现 `IInitializable` 接口但 `Initialize()` 为空。

**影响**：设计气味 — 如果不需要初始化，不应实现该接口。当前仅因为 `CoreInstaller` 将同一个实例注册为 `IEventCenter` 和 `IInitializable` 两个接口。

**建议**：保留接口实现（为将来扩展预留），但添加注释说明当前为空操作。

### 🟢 轻微

#### 7. ProjectBootstrap 硬编码 orthoSize 和背景色

**现象**：`FixCameraFor1080p()` 中 `orthoSize = 5.4f` 和 `backgroundColor = (0.85, 0.72, 0.55)` 是游戏特定的美术配置。

**影响**：框架模板中不应该有美术数据。

**建议**：将这些参数移到 ScriptableObject 配置中，或由项目侧重写（已在提取版本中移除该方法）。

#### 8. 事件定义与游戏逻辑混合

**现象**：`EventStructs.cs` 中的事件定义全部是游戏特定事件（`InteractionEvent`, `DialogueEndedEvent`, `StoryBeatCompletedEvent` 等）。

**影响**：事件系统框架文件混入了业务事件定义。

**建议**：框架提供空的事件定义模板文件，游戏事件定义放在 `Gameplay/Events/` 下。

#### 9. CoreInstaller 注册了游戏特定服务

**现象**：`CoreInstaller` 注册了 `IPlayerInput → PlayerInputManager` 和 `IViewModelFactory → ViewModelFactory`，这些是游戏层关注点。

**建议**：拆分为 `CoreInstaller`（仅注册 DI 基础设施）和 `GamePlayInstaller`（注册游戏服务）。

---

## 三、架构亮点（值得保留）

| 设计 | 说明 |
|------|------|
| **自研 DI 容器** | 轻量、无第三方依赖、支持 Singleton/Scoped/Transient、循环依赖检测、依赖图验证 |
| **严格生命周期** | 封存 Awake/Start/Update，强制 OnInitialize → OnStartExternal → Tick → OnShutdown |
| **Installer 模式** | ScriptableObject Installer，通过 BootConfig.asset 编排执行顺序 |
| **强类型事件总线** | 事件必须是 struct（值类型避免 GC），编译时类型安全 |
| **MVVM 代码绑定** | ViewModel 纯 C# 可单元测试，View 通过代码绑定而非 Inspector 字符串 |
| **CLAUDE.md 规则约束** | 详细的开发约定和禁止事项清单，AI 协作友好 |

---

## 四、提取框架变更总结

提取后的 `RazorFramework` 相比原项目做了以下改进：

| 变更 | 说明 |
|------|------|
| ✅ 命名空间重组 | `Core.DI` → `RazorFramework.DI`，统一命名前缀 |
| ✅ 解除 DI→Lifecycle 耦合 | 用 `OnInstanceCreated` 回调替代直接调用 `LifecycleRegistry.Register` |
| ✅ 解除 ProjectContext→Gameplay 耦合 | 用 `virtual` 钩子方法替代硬编码的游戏逻辑 |
| ✅ 移除 RelayCommand 的 Unity 依赖 | `Debug.LogError` → 异常抛出 |
| ✅ DI 日志可配置 | `LogInfo/LogWarning/LogError` 委托替代硬编码 `Debug.Log` |
| ✅ EventManager 线程安全加强 | 发布操作加锁保护 |
| ✅ 事件模板分离 | 游戏事件定义移到独立文件，框架提供空模板 |
| ✅ 启动引导移除美术硬编码 | `FixCameraFor1080p` 等游戏特定配置已移除 |

---

## 五、纳入建议

将提取的 `RazorFramework` 纳入新项目时：

1. 将整个 `extracted-framework/` 复制到新项目的 `Assets/Plugins/RazorFramework/`
2. 创建 `.asmdef` 文件（按第二.2 节的建议结构）
3. 创建项目侧 `GameProjectContext : ProjectContext` 重写钩子
4. 创建 `GameCoreInstaller`（参考原项目的 `CoreInstaller`）
5. 在 Resources 下创建 `PlayerInput.inputactions` 资产
