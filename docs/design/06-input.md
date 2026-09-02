# Input — 玩家输入

> **状态：🚫 旧框架输入层已删除（2026-09-02）** —— 输入属游戏细节，归 `CardGame.Runtime`；本文件转为方向记录
> 决策见 [2026-09-02 决策规格](../../superpowers/specs/2026-09-02-lifecycle-boot-simplification-design.md) 问题 7：
> 根目录旧 `Input/` 抽象层（IPlayerInput / PlayerInput / PlayerInputManager）已删除——
> 该抽象面向多设备/重映射的动作游戏，InputSystem 已内建该能力；本游戏为鼠标驱动的 UI 交互，
> 指针/键盘事件直接走 UITK。

## 现状与按需路径

- 输入资产：`Assets/CardGame/Settings/InputSystem_Actions.inputactions`（游戏输入的唯一来源）。
- 类型化包装类（Generate C# Class）：**首个输入消费者出现时**再从 `.inputactions` 生成进 `CardGame.Runtime`。
  模板动作集对当前游戏无语义，提前生成即占位代码——与 UpdateRunner 同一「按需」原则
  （见 [03-lifecycle.md](03-lifecycle.md) 退役说明与决策规格问题 2）。
- 若将来做重映射 UI：直接用 InputSystem 内建的 `InputActionRebinding`，不自建抽象层。
- 归属边界：任何输入代码只进 `Assets/CardGame/`，不回框架。
