using System;
using System.Collections.Generic;
using System.Reflection;

namespace RazorFramework.DI
{
    internal readonly struct DependencyPlan
    {
        public DependencyPlan(
            Type parameterType,
            Type serviceType,
            bool isCollection)
        {
            ParameterType = parameterType;
            ServiceType = serviceType;
            IsCollection = isCollection;
        }

        public Type ParameterType { get; }
        public Type ServiceType { get; }
        public bool IsCollection { get; }
    }

    internal sealed class ActivationPlan
    {
        public ActivationPlan(
            ServiceRegistration registration,
            ConstructorInfo constructor,
            IReadOnlyList<DependencyPlan> dependencies)
        {
            Registration = registration;
            Constructor = constructor;
            Dependencies = dependencies;
        }

        public ServiceRegistration Registration { get; }
        public ConstructorInfo Constructor { get; }
        public IReadOnlyList<DependencyPlan> Dependencies { get; }
        public Type RequiredScopeType { get; set; }
    }

    internal sealed class ContainerBuildModel
    {
        public ContainerBuildModel(
            IReadOnlyList<ServiceRegistration> registrations,
            IReadOnlyDictionary<Type, ServiceRegistration> defaultRegistrations,
            IReadOnlyDictionary<Type, IReadOnlyList<ServiceRegistration>>
                collectionRegistrations,
            IReadOnlyDictionary<int, ActivationPlan> plans,
            IReadOnlyDictionary<Type, Type> scopeParents)
        {
            Registrations = registrations;
            DefaultRegistrations = defaultRegistrations;
            CollectionRegistrations = collectionRegistrations;
            Plans = plans;
            ScopeParents = scopeParents;
        }

        public IReadOnlyList<ServiceRegistration> Registrations { get; }
        public IReadOnlyDictionary<Type, ServiceRegistration>
            DefaultRegistrations { get; }
        public IReadOnlyDictionary<Type, IReadOnlyList<ServiceRegistration>>
            CollectionRegistrations { get; }
        public IReadOnlyDictionary<int, ActivationPlan> Plans { get; }
        public IReadOnlyDictionary<Type, Type> ScopeParents { get; }
    }
}
