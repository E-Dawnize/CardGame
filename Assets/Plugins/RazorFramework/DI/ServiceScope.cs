using System;
using System.Collections.Generic;

namespace RazorFramework.DI
{
    public sealed class ServiceScope : IServiceResolver, IDisposable
    {
        private readonly ServiceContainer _container;
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
            EnsureNotDisposed();
            return _container.ResolveFromScope(this, serviceType);
        }

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            EnsureNotDisposed();
            return _container.TryResolveFromScope(this, serviceType, out service);
        }

        public IReadOnlyList<T> ResolveAll<T>() where T : class
        {
            EnsureNotDisposed();
            return _container.ResolveAllFromScope<T>(this);
        }

        public ServiceScope CreateScope<TScope>()
        {
            EnsureNotDisposed();
            return _container.CreateChildScope(this, typeof(TScope));
        }

        public void Dispose()
        {
            _disposed = true;
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
