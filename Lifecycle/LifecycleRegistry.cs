using System;
using System.Collections.Generic;
using System.Linq;
using RazorFramework.DI;

namespace RazorFramework.Lifecycle
{
    /// <summary>
    /// 生命周期注册表 — 统一管理所有 IInitializable / IStartable / ITickable 组件。
    /// 核心保证：所有 Initialize 完成后 → 所有 OnStart 开始。
    /// 
    /// 设计注意：这是静态注册表。如需多容器场景，可改为实例模式注入 DI。
    /// 当前设计适合单一项目上下文（ProjectContext）的全局管理。
    /// </summary>
    public static class LifecycleRegistry
    {
        private static readonly List<IInitializable> _initializables = new();
        private static readonly List<IStartable> _startables = new();
        private static readonly List<ITickable> _tickables = new();
        private static readonly List<object> _pendingInjection = new();

        private static bool _isInitializing;
        private static bool _isStarting;
        private static bool _initializationComplete;
        private static bool _startComplete;

        private static readonly Queue<IInitializable> _pendingInitializables = new();
        private static readonly Queue<IStartable> _pendingStartables = new();

        private static DIContainer _container;
        private static IScope _projectScope;

        public static event Action<ITickable> OnTickableRegistered;
        public static event Action<ITickable> OnTickableUnregistered;

        public static bool IsInitializationComplete => _initializationComplete;
        public static bool IsStartComplete => _startComplete;
        public static int InitializableCount { get { lock (_initializables) return _initializables.Count; } }
        public static int StartableCount { get { lock (_initializables) return _startables.Count; } }
        public static int TickableCount { get { lock (_initializables) return _tickables.Count; } }

        public static IReadOnlyList<ITickable> GetTickables()
        {
            lock (_initializables) return _tickables.ToList();
        }

        /// <summary>设置 DI 容器引用，由 ProjectContext 调用</summary>
        public static void SetContainer(DIContainer container, IScope projectScope)
        {
            _container = container;
            _projectScope = projectScope;
        }

        /// <summary>注册组件到生命周期系统。线程安全。</summary>
        public static void Register(object component)
        {
            if (component == null) return;

            lock (_initializables)
            {
                TryInjectDependencies(component);

                if (component is IInitializable initializable && !_initializables.Contains(initializable))
                {
                    if (_isInitializing || _initializationComplete)
                        _pendingInitializables.Enqueue(initializable);
                    else
                        _initializables.Add(initializable);
                }

                if (component is IStartable startable && !_startables.Contains(startable))
                {
                    if (_isStarting || _startComplete)
                        _pendingStartables.Enqueue(startable);
                    else
                        _startables.Add(startable);
                }

                if (component is ITickable tickable && !_tickables.Contains(tickable))
                    _tickables.Add(tickable);

                HandleDynamicComponent(component);
            }
        }

        /// <summary>注销组件</summary>
        public static void Unregister(object component)
        {
            if (component == null) return;
            lock (_initializables)
            {
                if (component is IInitializable init) _initializables.Remove(init);
                if (component is IStartable start) _startables.Remove(start);
                if (component is ITickable tick && _tickables.Remove(tick))
                    OnTickableUnregistered?.Invoke(tick);
                _pendingInjection.Remove(component);
            }
        }

        /// <summary>执行 Initialize 阶段</summary>
        public static void InitializeAll()
        {
            if (_isInitializing) return;
            _isInitializing = true;
            _initializationComplete = false;

            try
            {
                ProcessDelayedInjection();
                var components = new List<IInitializable>(_initializables);
                foreach (var c in components)
                {
                    try { c.Initialize(); }
                    catch (Exception ex) { UnityEngine.Debug.LogError($"[Lifecycle] Initialize FAILED: {c.GetType().Name}: {ex.Message}"); }
                }
                ProcessPendingRegistrations();
                _initializationComplete = true;
            }
            finally { _isInitializing = false; }
        }

        /// <summary>执行 OnStart 阶段（保证初始化完成）</summary>
        public static void StartAll()
        {
            if (_isStarting) return;
            if (!_initializationComplete) InitializeAll();

            _isStarting = true;
            _startComplete = false;
            try
            {
                foreach (var c in new List<IStartable>(_startables))
                {
                    try { c.OnStart(); } catch (Exception ex) { UnityEngine.Debug.LogError($"[Lifecycle] OnStart FAILED: {c.GetType().Name}: {ex.Message}"); }
                }
                ProcessPendingStartables();
                _startComplete = true;
            }
            finally { _isStarting = false; }
        }

        /// <summary>清理所有注册（场景/域重载时调用）</summary>
        public static void Clear()
        {
            lock (_initializables)
            {
                _initializables.Clear();
                _startables.Clear();
                _tickables.Clear();
                _pendingInjection.Clear();
                _pendingInitializables.Clear();
                _pendingStartables.Clear();
                _initializationComplete = false;
                _startComplete = false;
            }
        }

        public static void DumpState()
        {
            UnityEngine.Debug.Log($"=== LifecycleRegistry ===");
            UnityEngine.Debug.Log($"  InitializeComplete: {_initializationComplete}, StartComplete: {_startComplete}");
            UnityEngine.Debug.Log($"  IInitializable: {_initializables.Count}, IStartable: {_startables.Count}, ITickable: {_tickables.Count}");
            UnityEngine.Debug.Log($"  Pending: injection={_pendingInjection.Count}, init={_pendingInitializables.Count}, start={_pendingStartables.Count}");
            foreach (var c in _initializables) UnityEngine.Debug.Log($"    [{c.GetType().FullName}]");
        }

        #region 内部实现

        private static void TryInjectDependencies(object component)
        {
            if (_container == null) { _pendingInjection.Add(component); return; }
            try { _container.Inject(component, _projectScope); }
            catch (Exception) { _pendingInjection.Add(component); }
        }

        private static void ProcessDelayedInjection()
        {
            if (_container == null) return;
            foreach (var c in _pendingInjection.ToList())
            {
                try { _container.Inject(c, _projectScope); _pendingInjection.Remove(c); }
                catch (Exception ex) { UnityEngine.Debug.LogError($"[Lifecycle] Injection failed: {c.GetType().Name}: {ex.Message}"); }
            }
        }

        private static void ProcessPendingRegistrations()
        {
            while (_pendingInitializables.Count > 0)
            {
                var c = _pendingInitializables.Dequeue();
                if (!_initializables.Contains(c)) { _initializables.Add(c); try { c.Initialize(); } catch { } }
            }
        }

        private static void ProcessPendingStartables()
        {
            while (_pendingStartables.Count > 0)
            {
                var c = _pendingStartables.Dequeue();
                if (!_startables.Contains(c)) { _startables.Add(c); try { c.OnStart(); } catch { } }
            }
        }

        private static void HandleDynamicComponent(object component)
        {
            if (_initializationComplete && component is IInitializable init)
                try { init.Initialize(); } catch { }
            if (_startComplete && component is IStartable start)
                try { start.OnStart(); } catch { }
            if (_startComplete && component is ITickable tick)
                OnTickableRegistered?.Invoke(tick);
        }

        #endregion
    }
}
