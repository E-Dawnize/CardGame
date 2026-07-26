using UnityEngine;

namespace RazorFramework.Input
{
    /// <summary>
    /// 玩家输入接口 — 基于轮询（polling），供 View 每帧 Tick 读取。
    /// 使用 UnityEngine.InputSystem 实现。
    /// </summary>
    public interface IPlayerInput
    {
        Vector2 MoveDirection { get; }
        Vector2 MousePosition { get; }
        bool IsClickTriggered { get; }
        bool BackpackToggleTriggered { get; }
        void Enable();
        void Disable();
    }
}
