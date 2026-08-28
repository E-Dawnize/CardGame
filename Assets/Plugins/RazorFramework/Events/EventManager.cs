using System;
using System.Collections.Generic;

namespace RazorFramework.Events
{
    /// <summary>
    /// 强类型事件总线实现。事件必须是 struct，基于 Delegate 字典分发。
    /// 线程安全：所有操作对 _eventHandlers 加锁保护。
    /// </summary>
    public sealed class EventManager : IEventCenter, IDisposable
    {
        private readonly Dictionary<Type, Delegate> _eventHandlers = new();
        private readonly object _lock = new();

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(T);
            lock (_lock)
            {
                if (_eventHandlers.TryGetValue(type, out var existing))
                    _eventHandlers[type] = Delegate.Combine(existing, handler);
                else
                    _eventHandlers[type] = handler;
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            var type = typeof(T);
            lock (_lock)
            {
                if (!_eventHandlers.TryGetValue(type, out var existing)) return;
                var next = Delegate.Remove(existing, handler);
                if (next == null)
                    _eventHandlers.Remove(type);
                else
                    _eventHandlers[type] = next;
            }
        }

        public void Publish<T>(T evt) where T : struct
        {
            Delegate handler;
            lock (_lock)
            {
                _eventHandlers.TryGetValue(typeof(T), out handler);
            }
            (handler as Action<T>)?.Invoke(evt);
        }

        /// <summary>当前订阅数（诊断用）</summary>
        public int SubscriptionCount
        {
            get { lock (_lock) return _eventHandlers.Count; }
        }

        public void Dispose()
        {
            lock (_lock) _eventHandlers.Clear();
        }
    }
}
