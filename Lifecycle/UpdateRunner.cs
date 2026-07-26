using System.Collections.Generic;
using UnityEngine;

namespace RazorFramework.Lifecycle
{
    /// <summary>
    /// UpdateRunner — 驱动所有 ITickable 组件的 MonoBehaviour。
    /// 挂载在 ProjectContext GameObject 上，由 ProjectContext 自动创建。
    /// </summary>
    public class UpdateRunner : MonoBehaviour
    {
        private readonly List<ITickable> _tickables = new();
        private readonly object _lock = new();

        private void Awake()
        {
            LifecycleRegistry.OnTickableRegistered += OnTickableRegistered;
            LifecycleRegistry.OnTickableUnregistered += OnTickableUnregistered;
        }

        private void OnTickableRegistered(ITickable t) => Register(t);
        private void OnTickableUnregistered(ITickable t) => Unregister(t);

        public void Register(ITickable tickable)
        {
            lock (_lock) { if (!_tickables.Contains(tickable)) _tickables.Add(tickable); }
        }

        public void Unregister(ITickable tickable)
        {
            lock (_lock) { _tickables.Remove(tickable); }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            ITickable[] snapshot;
            lock (_lock) { snapshot = _tickables.ToArray(); }

            foreach (var t in snapshot)
            {
                try { t.Tick(dt); }
                catch (System.Exception ex) { Debug.LogError($"[UpdateRunner] Tick error in {t.GetType().Name}: {ex.Message}"); }
            }
        }

        private void OnDestroy()
        {
            LifecycleRegistry.OnTickableRegistered -= OnTickableRegistered;
            LifecycleRegistry.OnTickableUnregistered -= OnTickableUnregistered;
            lock (_lock) _tickables.Clear();
        }
    }
}
