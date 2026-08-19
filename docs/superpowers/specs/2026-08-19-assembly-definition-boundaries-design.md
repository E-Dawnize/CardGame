# feat-003 Assembly Definition Boundaries 设计规格

**状态：** 待批准
**日期：** 2026-08-19
**功能：** feat-003 Assembly Definition Boundaries
**适用范围：** 仅服务 CardGame，不作为通用包发布

## 1. 背景与目标

feat-002 已用 `Assets/Plugins/RazorFramework/DI/` 的 DI V2 替换旧根目录 `DI/DIContainer.cs`。DI V2 使用 `ContainerBuilder` + 构建后不可变的 `ServiceContainer`，纯核心只做构造注入，成员注入隔离到 `RazorFramework.Unity.DI`。

剩余根目录模块 `Events/`、`Lifecycle/`、`MVVM/`、`Input/`、`Boot/` 仍是旧源码，并大量引用已被删除的旧 `DIContainer`、`IScope`、`Register`、`Inject`、`Validate`、`ResolveAll(scope)`、`OnInstanceCreated` 等 API。因此它们当前不在 Unity 编译范围，也不能直接迁入。

本设计的目标是：

- 重写并迁入这些模块到 `Assets/Plugins/RazorFramework/`（框架）或 `Assets/CardGame/`（游戏）；
- 每个框架模块建立 Assembly Definition；
- 用 asmdef 强制依赖方向，Unity 能编译完整依赖图；
- 纯 C# 模块不含任何 `UnityEngine` 编译时引用；
- 用 Node Harness fixture 与 Unity EditMode 测试共同守住边界。

## 2. 非目标

- 不实现卡牌、战斗、地图、叙事、存档、文案或数值玩法；
- 不修改 DI V2 的 `noEngineReferences` 或唯一实现门禁；
- 不宣称 IL2CPP、托管代码剥离、AOT 或目标平台构建已验证；
- 不一次性把全部旧源码暴露给 Unity 编译，逐模块自底向上迁移。

## 3. 目标程序集与依赖图

```text
RazorFramework.DI                 [BCL only]                      已有
RazorFramework.Events             [BCL only]                      新增
RazorFramework.Lifecycle          [BCL only]                      新增
RazorFramework.MVVM               [BCL only, refs Events]         新增
RazorFramework.Unity.DI           [Unity, refs DI]                已有
RazorFramework.Unity.Lifecycle    [Unity, refs DI/Lifecycle/Unity.DI]
RazorFramework.Unity.MVVM         [Unity, refs MVVM]              新增
RazorFramework.Unity.Boot         [Unity, refs 上述框架程序集]      新增
CardGame.Runtime                  [Unity, refs 框架]              游戏专属
```

依赖方向：纯 C# 模块只依赖 BCL 或更底层的纯 C# 模块；Unity 模块只被 Unity 层与 CardGame 依赖；框架不引用 CardGame。

## 4. 关键决策

- **D1 Events 解耦 Lifecycle：** `IEventCenter` 不再继承 `IInitializable`（空实现删除），Events 保持 BCL only 且零框架依赖。
- **D2 Lifecycle 拆分与去静态 DI/Unity：** 保留顺序保证的注册表迁入纯 C# `RazorFramework.Lifecycle`，移除 `UnityEngine.Debug` 与旧 `DIContainer.Inject` 耦合，改为可注入的 `Action<string>` 日志槽与 `Action<object>` 注入缝；`StrictLifecycleMonoBehaviour`、`UpdateRunner` 迁入 `RazorFramework.Unity.Lifecycle` 并接线注入缝。
- **D3 MVVM 拆分与构造注入：** `ViewModelBase` 改为构造注入 `IEventCenter`（去掉 `[Inject]` 字段与 DI 引用），与 `RelayCommand`、`AsyncCommand`、`IBinding` 接口一起迁入纯 C# `RazorFramework.MVVM`；`BindingManager` 迁入 `RazorFramework.Unity.MVVM`。
- **D4 Installer 改接 ContainerBuilder：** `IInstaller` 从 `Register(DIContainer)` 改为 `Register(ContainerBuilder)`；`InstallerAsset` 迁入 Unity.Boot。
- **D5 游戏代码外迁：** `Input/`（`IPlayerInput`/`PlayerInput`/`PlayerInputManager`，含 Move/Click/Backpack 业务输入）与 `Boot/ProjectContext` 的加载遮罩、Addressables `BootConfig`、输入系统修复迁到 `Assets/CardGame/Runtime/`；框架 Boot 保留通用组合钩子。

## 5. 迁移顺序（自底向上）

1. `Events`：纯 C#，零依赖。
2. `Lifecycle`：纯核心 + Unity runner 拆分。
3. `MVVM`：纯 ViewModel/Command + Unity binding 拆分。
4. `Installer` 与 `Boot`：改接 DI V2 并建立 Unity.Boot。
5. `Input`：迁到 CardGame。
6. Harness、文档与状态同步。

每步都以先失败的 Node Harness fixture 或 Unity EditMode 测试驱动，并在迁移下一层前验证当前层可编译。

## 6. 验证策略

- Node Harness：递归拒绝根目录旧 `Events/`、`Lifecycle/`、`MVVM/`、`Input/`、`Boot/` 下的 C# 进入 Unity 编译范围；固定各 asmdef 的 `name`、`rootNamespace`、`references`、`noEngineReferences`（纯模块）与 `autoReferenced`。
- Unity EditMode：依赖图可编译；纯 C# asmdef 无 Unity 引用；Events 订阅/发布、Lifecycle 顺序与注入缝、MVVM ViewModel 构造注入、Unity 适配器行为覆盖。
- 便携与完整 Harness 均须通过。

## 7. 风险

- 旧模块与 DI V2 不兼容，属于重写而非机械迁移，范围较大。
- IL2CPP、托管代码剥离与 AOT 仍未验证，发布前需独立功能处理。
- 游戏输入与项目启动细节的框架/游戏边界需在实现中以测试确认，不因迁移便利降低 DI V2 门禁。

## 8. 验收标准（拟修订）

1. 每个框架模块（Events、Lifecycle、MVVM、Unity.* 适配、Boot）都有 asmdef。
2. Unity 编译依赖图，纯 C# 模块不含 Unity 引用。
3. 根目录旧框架模块不再有残留 C# 进入 Unity 编译范围。
4. 便携与完整 Harness 通过，含真实 Unity EditMode 证据。
5. README、`DESIGN-REVIEW.md`、`docs/HARNESS.md`、状态与交接文档同步为中文。
