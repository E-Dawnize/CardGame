# Input — 玩家输入

> 旧实现：根目录 `Input/`（非编译域）
> 目标位置：`Assets/CardGame/Runtime/Input/`（`CardGame.Runtime` 程序集）
> 状态：⏳ 迁出中（feat-003 Task 5） · 属游戏专属代码，不属于框架

## 旧实现（迁移输入）

```text
根目录 Input/（非编译域）
├─ IPlayerInput   轮询式接口
│  ├─ MoveDirection / MousePosition
│  ├─ IsClickTriggered / BackpackToggleTriggered
│  └─ Enable() / Disable()
├─ PlayerInput    InputSystem 生成类占位
│  ├─ IInputActionCollection2 stub（未实现成员抛 NotImplementedException）
│  ├─ 构造时 Resources.Load<InputActionAsset>("PlayerInput")
│  └─ PlayerActions 占位类（真实成员由 InputSystem 生成）
└─ PlayerInputManager : IPlayerInput
   ├─ 事件回调 → 帧状态（Move / Click / MousePosition / BackpackToggle）
   └─ ResetFrameFlags()   每帧 Tick 末尾重置帧级标志
```

## 目标形态（feat-003 D5）

```text
Assets/CardGame/Runtime/Input/（CardGame.Runtime）
├─ IPlayerInput / PlayerInputManager 迁入
└─ PlayerInput 用真实 .inputactions 生成版本替换
   （现有 InputSystem_Actions.inputactions 位于 Assets/CardGame/Settings/）
```

## 关键决策

- 输入属游戏业务（Move/Click/Backpack 是游戏语义），不进框架（D5）。
- 轮询模型：View 每帧读 `IPlayerInput` 状态；帧级标志由管理器在 Tick 末尾重置。

## 已知限制

- `PlayerInput.cs` 为占位：成员与 `.inputactions` 资产未对齐，不可直接使用，需生成替换。
- 未接入 Boot 的输入系统修复（FixEventSystemInputModules）验证。
- 触摸支持（EnhancedTouch）在旧 Boot 中启用，迁移时需确认归属（游戏侧）。
