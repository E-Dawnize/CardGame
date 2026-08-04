using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RazorFramework.DI
{
    public enum DependencyErrorCode
    {
        DuplicateRegistration,
        InvalidImplementation,
        AmbiguousConstructor,
        MissingDependency,
        CircularDependency,
        InvalidScopeDefinition,
        ScopeMismatch,
        CaptiveDependency,
        ContainerDisposed,
        ActivationFailed
    }

    public sealed class DependencyInjectionException : InvalidOperationException
    {
        public DependencyInjectionException(
            DependencyErrorCode code,
            string message,
            Type serviceType = null,
            Type implementationType = null,
            IEnumerable<Type> dependencyPath = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            Code = code;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            var path = dependencyPath == null
                ? new List<Type>()
                : new List<Type>(dependencyPath);
            DependencyPath = new ReadOnlyCollection<Type>(path);
        }

        public DependencyErrorCode Code { get; }
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
        public IReadOnlyList<Type> DependencyPath { get; }
    }
}
