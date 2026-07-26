// PlayerInput.cs — Unity Input System 自动生成文件的替代。
// 如果项目使用 Unity Input System 的 "Generate C# Class" 功能，
// 请用生成的版本替换此模板。
// 
// 此文件定义输入 Action 映射结构。键位绑定在对应的 .inputactions 资产中。
//
// 生成方式：选中 .inputactions 资产 → Inspector → Generate C# Class → 保存到此路径。

using UnityEngine;
using UnityEngine.InputSystem;

namespace RazorFramework.Input
{
    public partial class PlayerInput : IInputActionCollection2
    {
        // 由 InputSystem 自动生成时替换。此处为占位。
        private InputActionAsset _asset;
        public InputActionAsset asset => _asset;

        public PlayerInput()
        {
            // 尝试从 Resources 加载默认 inputactions
            _asset = Resources.Load<InputActionAsset>("PlayerInput");
            if (_asset == null)
                Debug.LogWarning("[RazorFramework.Input] PlayerInput.inputactions not found in Resources. Create one via Create > Input Actions.");
        }

        public void Enable() => _asset?.Enable();
        public void Disable() => _asset?.Disable();
        public InputBinding? bindingMask { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public System.ReadOnlyArray<InputDevice>? devices { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public System.ReadOnlyArray<InputControlScheme> controlSchemes => throw new System.NotImplementedException();

        public bool Contains(InputAction action) => _asset != null && _asset.Contains(action);
        public System.Collections.Generic.IEnumerator<InputAction> GetEnumerator() => _asset?.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public void RegisterCallbacks() { }
        public void UnregisterCallbacks() { }
        public void SetCallbacks() { }

        public class PlayerActions
        {
            // 子 Actions 由 InputSystem 生成
        }

        // 此类型和成员名需与 .inputactions 资产中的定义一致
        // 如果使用自动生成的 C# 类，直接替换整个文件。
    }
}
