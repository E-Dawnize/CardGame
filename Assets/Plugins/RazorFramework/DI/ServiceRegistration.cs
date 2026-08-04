using System;

namespace RazorFramework.DI
{
    internal sealed class ServiceRegistration
    {
        public ServiceRegistration(
            int id,
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime,
            Type scopeType,
            object externalInstance,
            bool isCollection)
        {
            Id = id;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            ScopeType = scopeType;
            ExternalInstance = externalInstance;
            IsCollection = isCollection;
        }

        public int Id { get; }
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
        public ServiceLifetime Lifetime { get; }
        public Type ScopeType { get; }
        public object ExternalInstance { get; }
        public bool IsCollection { get; }
        public bool IsExternal => ImplementationType == null;
    }

    internal readonly struct ScopeDefinition
    {
        public ScopeDefinition(Type scopeType, Type parentScopeType)
        {
            ScopeType = scopeType;
            ParentScopeType = parentScopeType;
        }

        public Type ScopeType { get; }
        public Type ParentScopeType { get; }
    }
}
