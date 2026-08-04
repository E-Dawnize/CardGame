using System;
using System.Collections.Generic;
using System.Reflection;

namespace RazorFramework.DI
{
    internal sealed class ActivationPlan
    {
        public ActivationPlan(
            ServiceRegistration registration,
            ConstructorInfo constructor,
            IReadOnlyList<Type> parameterTypes)
        {
            Registration = registration;
            Constructor = constructor;
            ParameterTypes = parameterTypes;
        }

        public ServiceRegistration Registration { get; }
        public ConstructorInfo Constructor { get; }
        public IReadOnlyList<Type> ParameterTypes { get; }
        public Type RequiredScopeType { get; set; }
    }

    internal sealed class ContainerBuildModel
    {
        public ContainerBuildModel(
            IReadOnlyList<ServiceRegistration> registrations,
            IReadOnlyDictionary<Type, ServiceRegistration> defaultRegistrations,
            IReadOnlyDictionary<int, ActivationPlan> plans,
            IReadOnlyDictionary<Type, Type> scopeParents)
        {
            Registrations = registrations;
            DefaultRegistrations = defaultRegistrations;
            Plans = plans;
            ScopeParents = scopeParents;
        }

        public IReadOnlyList<ServiceRegistration> Registrations { get; }
        public IReadOnlyDictionary<Type, ServiceRegistration> DefaultRegistrations { get; }
        public IReadOnlyDictionary<int, ActivationPlan> Plans { get; }
        public IReadOnlyDictionary<Type, Type> ScopeParents { get; }
    }
}
