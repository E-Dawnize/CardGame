using UnityEngine;
using UnityEngine.InputSystem;

namespace RazorFramework.Input
{
    /// <summary>
    /// 玩家输入管理器 — 轮询式输入（非事件驱动）。
    /// 使用 Unity Input System，通过 IPlayerInput 接口暴露每帧状态。
    /// </summary>
    public class PlayerInputManager : IPlayerInput
    {
        private readonly PlayerInput _input;

        public Vector2 MoveDirection { get; private set; }
        public Vector2 MousePosition { get; private set; }
        public bool IsClickTriggered { get; private set; }
        public bool BackpackToggleTriggered { get; private set; }

        public PlayerInputManager()
        {
            _input = new PlayerInput();
        }

        public void Enable()
        {
            _input.Enable();
            _input.Player.Move.performed += OnMove;
            _input.Player.Move.canceled += OnMoveCanceled;
            _input.Player.Click.performed += OnClick;
            _input.Player.MousePosition.performed += OnMousePosition;
            _input.Player.BackpackToggle.performed += OnBackpackToggle;
        }

        public void Disable()
        {
            _input.Player.Move.performed -= OnMove;
            _input.Player.Move.canceled -= OnMoveCanceled;
            _input.Player.Click.performed -= OnClick;
            _input.Player.MousePosition.performed -= OnMousePosition;
            _input.Player.BackpackToggle.performed -= OnBackpackToggle;
            _input.Disable();
        }

        /// <summary>每帧在 Tick 末尾调用，重置帧级标志</summary>
        public void ResetFrameFlags()
        {
            IsClickTriggered = false;
            BackpackToggleTriggered = false;
        }

        private void OnMove(InputAction.CallbackContext ctx) => MoveDirection = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => MoveDirection = Vector2.zero;
        private void OnClick(InputAction.CallbackContext ctx) => IsClickTriggered = true;
        private void OnMousePosition(InputAction.CallbackContext ctx) => MousePosition = ctx.ReadValue<Vector2>();
        private void OnBackpackToggle(InputAction.CallbackContext ctx) => BackpackToggleTriggered = true;
    }
}
