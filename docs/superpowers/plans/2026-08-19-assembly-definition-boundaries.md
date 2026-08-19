# feat-003 Assembly Definition Boundaries 实施计划

> **面向智能代理执行者：** 必须使用 `superpowers:subagent-driven-development`（推荐）或 `superpowers:executing-plans` 逐项执行本计划。所有步骤用 `- [ ]` 复选框追踪。

**目标：** 把根目录旧 `Events`、`Lifecycle`、`MVVM`、`Input`、`Boot` 重写并迁入 `Assets/Plugins/RazorFramework/` 或 `Assets/CardGame/`，为每个框架模块建立 asmdef，让 Unity 编译依赖图且纯 C# 模块不含 Unity 引用。

**架构：** 依赖方向自底向上为 `DI → Events → Lifecycle → MVVM → Unity.* → Boot → CardGame.Runtime`。纯 C# 模块只依赖 BCL 或更底层纯 C# 模块；Unity 适配与 CardGame 才引用 Unity。旧模块引用已删除的 DI V1 API，因此是重写而非机械迁移。

**技术栈：** Unity `6000.3.10f1`、C#、Node.js `>=18`、PowerShell、Git。

## 全局约束

- 目标 Unity 版本固定为 `6000.3.10f1`。
- 纯 C# asmdef 使用 `noEngineReferences: true` 且 `references` 只指向更底层纯 C# asmdef。
- 不修改 DI V2 的 `noEngineReferences` 与唯一实现门禁。
- 根目录旧 `Events/`、`Lifecycle/`、`MVVM/`、`Input/`、`Boot/` 最终不得残留进入 Unity 编译范围的 C#。
- 新建或实质修改的文档使用简体中文；类型、路径、配置键保持英文。
- 任一时刻最多一个功能处于 `in-progress`。
- 每步先写失败测试，再写最小实现，并运行 Node 契约测试与便携 Harness。

---

## 文件结构与职责

```text
Assets/Plugins/RazorFramework/
├─ Events/                          # 纯 C#：EventManager + IEventCenter
│  ├─ IEventCenter.cs
│  ├─ EventManager.cs
│  └─ RazorFramework.Events.asmdef
├─ Lifecycle/                       # 纯 C#：接口 + LifecycleEngine
│  ├─ IInitializable.cs
│  ├─ IStartable.cs
│  ├─ ITickable.cs
│  ├─ IInstaller.cs
│  ├─ ILifecycleLog.cs
│  ├─ LifecycleEngine.cs
│  └─ RazorFramework.Lifecycle.asmdef
├─ MVVM/                            # 纯 C#：ViewModelBase + Commands + 接口
│  ├─ IBinding.cs
│  ├─ RelayCommand.cs
│  ├─ AsyncCommand.cs
│  ├─ ViewModelBase.cs
│  └─ RazorFramework.MVVM.asmdef
├─ Unity/
│  ├─ Lifecycle/                    # Unity runner
│  │  ├─ StrictLifecycleMonoBehaviour.cs
│  │  ├─ UpdateRunner.cs
│  │  └─ RazorFramework.Unity.Lifecycle.asmdef
│  ├─ MVVM/                         # Unity binding
│  │  ├─ BindingManager.cs
│  │  └─ RazorFramework.Unity.MVVM.asmdef
│  └─ Boot/                         # 通用启动组合
│     ├─ InstallerConfig.cs
│     ├─ InstallerAsset.cs
│     ├─ SceneScopeRunner.cs
│     ├─ ProjectContext.cs
│     ├─ ProjectBootstrap.cs
│     └─ RazorFramework.Unity.Boot.asmdef
Assets/CardGame/Runtime/
└─ Input/                           # 游戏专属输入
   ├─ IPlayerInput.cs
   ├─ PlayerInput.cs
   ├─ PlayerInputManager.cs
   └─ CardGame.Runtime.asmdef
```

---

### Task 1：激活 feat-003 并固定程序集边界契约

**文件：**
- 修改：`feature_list.json`
- 修改：`progress.md`
- 修改：`session-handoff.md`
- 创建：`scripts/harness/tests/assembly-boundaries.test.mjs`

**接口：**
- 输入：已完成的 feat-002
- 输出：唯一 `in-progress` 的 feat-003，以及先失败的 asmdef 边界 Node 测试

- [ ] **Step 1：记录基线**

运行 `node scripts/harness/verify.mjs` 与 `git status --short --branch`，预期便携 25 passed、0 failure、1 warning。

- [ ] **Step 2：把 feat-003 置为 in-progress**

在 `feature_list.json` 中把 `feat-003.status` 改为 `in-progress`，并按本规格第 8 节更新 `doneCriteria`。

- [ ] **Step 3：先写失败的 asmdef 边界测试**

在 `scripts/harness/tests/assembly-boundaries.test.mjs` 中，用临时 fixture 断言每个纯 C# asmdef 的 `name`、`rootNamespace`、`noEngineReferences: true` 与空 `references`；断言 `RazorFramework.Unity.*.asmdef` 显式引用其依赖；断言根目录 `Events/Lifecycle/MVVM/Input/Boot` 下存在 C# 时 Harness 失败。

- [ ] **Step 4：运行并确认失败**

运行 `node --test scripts/harness/tests/assembly-boundaries.test.mjs`，预期缺少实现导致失败。

- [ ] **Step 5：提交功能激活**

提交 `feature_list.json`、`progress.md`、`session-handoff.md` 与新增测试。

---

### Task 2：迁移 Events 为纯 C# 零依赖程序集

**文件：**
- 创建：`Assets/Plugins/RazorFramework/Events/IEventCenter.cs`
- 创建：`Assets/Plugins/RazorFramework/Events/EventManager.cs`
- 创建：`Assets/Plugins/RazorFramework/Events/RazorFramework.Events.asmdef`
- 删除：`Events/IEventCenter.cs`、`Events/EventManager.cs`、`Events/EventStructs.cs`
- 修改：`scripts/harness/verify.mjs`（递归拒绝根 Events C#，校验新 asmdef）

**接口：**
- `IEventCenter` 提供 `Subscribe<T>/Unsubscribe<T>/Publish<T>`，`where T : struct`，不再继承 `IInitializable`。
- `EventManager` 实现 `IEventCenter, IDisposable`，订阅/取消/发布线程安全。

- [ ] **Step 1：先写 Unity EditMode 失败测试**

新增 `Assets/Plugins/RazorFramework/Tests/EditMode/Events/EventManagerTests.cs`，覆盖订阅、多订阅、取消、发布空订阅不抛、struct 约束；先运行并确认编译失败（旧代码仍引用已删除 DI）。

- [ ] **Step 2：创建 Events asmdef 与纯 C# 实现**

创建 asmdef 与两个纯 C# 文件，`IEventCenter` 去掉 `IInitializable`。

- [ ] **Step 3：删除根 Events 旧源码**

删除 `Events/` 下旧文件，保持目录可为空。

- [ ] **Step 4：更新 Harness 边界检查**

让 `checkCSharpBoundaries` 递归拒绝根 `Events/` 任意 C#，并校验 `RazorFramework.Events.asmdef`。

- [ ] **Step 5：运行 Node 与便携 Harness**

运行 `node --test scripts/harness/tests/` 与 `node scripts/harness/verify.mjs`，确认变绿。

- [ ] **Step 6：提交 Events 迁移**

---

### Task 3：迁移 Lifecycle 为纯核心 + Unity runner

**文件：**
- 创建：`Assets/Plugins/RazorFramework/Lifecycle/` 下 `IInitializable.cs`、`IStartable.cs`、`ITickable.cs`、`IInstaller.cs`、`ILifecycleLog.cs`、`LifecycleEngine.cs`、`RazorFramework.Lifecycle.asmdef`
- 创建：`Assets/Plugins/RazorFramework/Unity/Lifecycle/` 下 `StrictLifecycleMonoBehaviour.cs`、`UpdateRunner.cs`、`RazorFramework.Unity.Lifecycle.asmdef`
- 删除：`Lifecycle/` 旧文件

**接口：**
- `LifecycleEngine` 实例类，维护 `IInitializable/IStartable/ITickable` 顺序；`ILifecycleLog` 提供 `LogError(string)`；`Action<object> Injector` 为注入缝。
- `StrictLifecycleMonoBehaviour` 与 `UpdateRunner` 迁入 Unity 层，接入 `LifecycleEngine`。

- [ ] **Step 1：先写 Lifecycle 顺序与失败隔离 EditMode 测试**
- [ ] **Step 2：实现纯 C# LifecycleEngine**
- [ ] **Step 3：实现 Unity runner**
- [ ] **Step 4：删除根 Lifecycle 旧源码并更新 Harness**
- [ ] **Step 5：运行 Node 与便携 Harness**
- [ ] **Step 6：提交 Lifecycle 迁移**

---

### Task 4：迁移 MVVM 为纯 ViewModel + Unity binding

**文件：**
- 创建：`Assets/Plugins/RazorFramework/MVVM/` 下 `IBinding.cs`、`RelayCommand.cs`、`AsyncCommand.cs`、`ViewModelBase.cs`、`RazorFramework.MVVM.asmdef`
- 创建：`Assets/Plugins/RazorFramework/Unity/MVVM/BindingManager.cs`、`RazorFramework.Unity.MVVM.asmdef`
- 删除：`MVVM/` 旧文件

**接口：**
- `ViewModelBase` 构造注入 `IEventCenter`，去掉 `[Inject]` 字段与 DI 依赖。
- `BindingManager` 只保留 Unity 相关实现。

- [ ] **Step 1：先写 ViewModelBase 构造注入与 Command 测试**
- [ ] **Step 2：实现纯 C# ViewModel/Command**
- [ ] **Step 3：实现 Unity BindingManager**
- [ ] **Step 4：删除根 MVVM 旧源码并更新 Harness**
- [ ] **Step 5：运行 Node 与便携 Harness**
- [ ] **Step 6：提交 MVVM 迁移**

---

### Task 5：迁移 Installer/Boot 到 Unity.Boot 并迁出游戏代码

**文件：**
- 创建：`Assets/Plugins/RazorFramework/Unity/Boot/` 下 `InstallerConfig.cs`、`InstallerAsset.cs`、`SceneScopeRunner.cs`、`ProjectContext.cs`、`ProjectBootstrap.cs`、`RazorFramework.Unity.Boot.asmdef`
- 创建：`Assets/CardGame/Runtime/Input/` 下 `IPlayerInput.cs`、`PlayerInput.cs`、`PlayerInputManager.cs`、`CardGame.Runtime.asmdef`
- 删除：`Boot/`、`Input/` 旧文件

**接口：**
- `IInstaller.Register(ContainerBuilder builder)`。
- `ProjectContext` 保留通用组合与 `virtual` 钩子，游戏加载遮罩与输入修复移入 `CardGame.Runtime`。

- [ ] **Step 1：先写 Boot 组合与 Installer 构建测试**
- [ ] **Step 2：实现 Installer 改接 ContainerBuilder**
- [ ] **Step 3：实现 Unity.Boot 通用组合**
- [ ] **Step 4：把 Input 与游戏细节迁到 CardGame.Runtime**
- [ ] **Step 5：删除根 Boot/Input 旧源码并更新 Harness**
- [ ] **Step 6：运行 Node 与便携 Harness**
- [ ] **Step 7：提交 Boot/Input 迁移**

---

### Task 6：完整验证并关闭 feat-003

**文件：**
- 修改：`feature_list.json`、`progress.md`、`session-handoff.md`、`README.md`、`DESIGN-REVIEW.md`、`docs/HARNESS.md`

- [ ] **Step 1：运行 Node 契约测试与便携 Harness**
- [ ] **Step 2：运行真实 Unity 完整 Harness**

```powershell
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
node scripts/harness/verify.mjs --full
```

- [ ] **Step 3：确认根目录旧框架模块无残留 C# 进入编译范围**
- [ ] **Step 4：把 feat-003 标记 done 并回填实际证据**
- [ ] **Step 5：更新中文文档与交接**
- [ ] **Step 6：提交并推送**
