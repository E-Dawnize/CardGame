using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RazorFramework.MVVM
{
    /// <summary>
    /// 绑定管理器 — 管理所有 IBinding 的生命周期。
    /// 支持按 context（ViewModel）/GameObject 索引绑定，支持场景卸载自动清理。
    /// </summary>
    public class BindingManager : IBindingManager
    {
        private readonly List<IBinding> _bindings = new();
        private readonly Dictionary<object, List<IBinding>> _bindingsByContext = new();
        private readonly Dictionary<GameObject, List<IBinding>> _bindingsByGameObject = new();
        private readonly object _lock = new();

        public void RegisterBinding(IBinding binding, object context = null)
        {
            lock (_lock)
            {
                _bindings.Add(binding);
                if (context != null)
                {
                    if (!_bindingsByContext.TryGetValue(context, out var list))
                        _bindingsByContext[context] = list = new List<IBinding>();
                    list.Add(binding);
                }
                if (binding is MonoBehaviour mono)
                {
                    if (!_bindingsByGameObject.TryGetValue(mono.gameObject, out var goList))
                        _bindingsByGameObject[mono.gameObject] = goList = new List<IBinding>();
                    goList.Add(binding);
                }
            }
        }

        public void UnregisterBinding(IBinding binding, object context = null)
        {
            lock (_lock)
            {
                _bindings.Remove(binding);
                foreach (var kv in _bindingsByContext) kv.Value.Remove(binding);
                foreach (var kv in _bindingsByGameObject) kv.Value.Remove(binding);
                CleanupEmpty();
            }
        }

        public void BindAllInContext(object context)
        {
            if (context == null || !_bindingsByContext.TryGetValue(context, out var bindings)) return;
            foreach (var b in bindings) { try { b.Bind(); } catch (Exception ex) { Debug.LogError($"[Binding] Bind failed: {b.GetType().Name}: {ex.Message}"); } }
        }

        public void UnbindAllInContext(object context)
        {
            if (context == null || !_bindingsByContext.TryGetValue(context, out var bindings)) return;
            foreach (var b in bindings) { try { b.UnBind(); } catch (Exception ex) { Debug.LogError($"[Binding] Unbind failed: {b.GetType().Name}: {ex.Message}"); } }
        }

        public void BindAll()
        {
            lock (_lock) { foreach (var b in _bindings.ToArray()) { try { b.Bind(); } catch { } } }
        }

        public void UnbindAll()
        {
            lock (_lock) { foreach (var b in _bindings.ToArray()) { try { b.UnBind(); } catch { } } }
        }

        public void CleanupDestroyedGameObjects()
        {
            lock (_lock)
            {
                var destroyed = _bindingsByGameObject.Where(kv => kv.Key == null).Select(kv => kv.Key).ToList();
                foreach (var go in destroyed) _bindingsByGameObject.Remove(go);
                CleanupEmpty();
            }
        }

        private void CleanupEmpty()
        {
            var emptyCtx = _bindingsByContext.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
            foreach (var k in emptyCtx) _bindingsByContext.Remove(k);
            var emptyGo = _bindingsByGameObject.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
            foreach (var k in emptyGo) _bindingsByGameObject.Remove(k);
        }

        public int GetBindingCount() { lock (_lock) return _bindings.Count; }
    }
}
