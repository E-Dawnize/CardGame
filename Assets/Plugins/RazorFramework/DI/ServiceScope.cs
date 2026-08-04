using System;
using System.Collections.Generic;

namespace RazorFramework.DI
{
    public sealed class ServiceScope : IServiceResolver, IDisposable
    {
        private readonly object _lifecycleGate = new object();
        private readonly ServiceContainer _container;
        private readonly List<ServiceScope> _children =
            new List<ServiceScope>();
        private bool _disposed;

        internal ServiceScope(
            ServiceContainer container,
            ServiceScope parent,
            Type scopeType)
        {
            _container = container;
            Parent = parent;
            ScopeType = scopeType;
            Owner = new LifetimeOwner();
        }

        internal ServiceScope Parent { get; }
        internal Type ScopeType { get; }
        internal LifetimeOwner Owner { get; }

        public object Resolve(Type serviceType)
        {
            lock (_lifecycleGate)
            {
                EnsureNotDisposed();
                return _container.ResolveFromScope(this, serviceType);
            }
        }

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            lock (_lifecycleGate)
            {
                EnsureNotDisposed();
                return _container.TryResolveFromScope(
                    this,
                    serviceType,
                    out service);
            }
        }

        public IReadOnlyList<T> ResolveAll<T>() where T : class
        {
            lock (_lifecycleGate)
            {
                EnsureNotDisposed();
                return _container.ResolveAllFromScope<T>(this);
            }
        }

        public ServiceScope CreateScope<TScope>()
        {
            lock (_lifecycleGate)
            {
                EnsureNotDisposed();
                return _container.CreateChildScope(this, typeof(TScope));
            }
        }

        public void Dispose()
        {
            List<ServiceScope> children;
            lock (_lifecycleGate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                children = new List<ServiceScope>(_children);
                _children.Clear();
            }

            var errors = new List<Exception>();
            for (var index = children.Count - 1; index >= 0; index--)
            {
                DisposalExceptionCollector.Capture(
                    errors,
                    children[index].Dispose);
            }

            DisposalExceptionCollector.Capture(errors, Owner.Dispose);
            _container.NotifyScopeDisposed(this);
            DisposalExceptionCollector.ThrowIfAny(errors);
        }

        internal ServiceScope FindAncestor(Type scopeType)
        {
            for (var current = this; current != null; current = current.Parent)
            {
                if (current.ScopeType == scopeType)
                {
                    return current;
                }
            }

            return null;
        }

        internal void RegisterChild(ServiceScope child)
        {
            lock (_lifecycleGate)
            {
                EnsureNotDisposed();
                _children.Add(child);
            }
        }

        internal void RemoveChild(ServiceScope child)
        {
            lock (_lifecycleGate)
            {
                _children.Remove(child);
            }
        }

        internal void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new DependencyInjectionException(
                    DependencyErrorCode.ContainerDisposed,
                    "The service scope has been disposed.");
            }
        }
    }
}
