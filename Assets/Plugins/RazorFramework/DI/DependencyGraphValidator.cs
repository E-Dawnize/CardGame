using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RazorFramework.DI
{
    internal static class DependencyGraphValidator
    {
        public static ContainerBuildModel Build(
            IReadOnlyList<ServiceRegistration> sourceRegistrations,
            IReadOnlyList<ScopeDefinition> sourceScopeDefinitions)
        {
            var registrations = sourceRegistrations.ToArray();
            var defaultRegistrations = BuildDefaultRegistrations(registrations);
            var scopeParents = BuildScopeTree(sourceScopeDefinitions);
            var plans = BuildActivationPlans(registrations, scopeParents);
            ValidateDependencyGraph(registrations, defaultRegistrations, plans);

            return new ContainerBuildModel(
                registrations,
                defaultRegistrations,
                plans,
                scopeParents);
        }

        private static IReadOnlyDictionary<Type, ServiceRegistration>
            BuildDefaultRegistrations(IReadOnlyList<ServiceRegistration> registrations)
        {
            var result = new Dictionary<Type, ServiceRegistration>();
            foreach (var registration in registrations)
            {
                if (registration.IsCollection)
                {
                    continue;
                }

                if (result.ContainsKey(registration.ServiceType))
                {
                    throw Error(
                        DependencyErrorCode.DuplicateRegistration,
                        "A default service may only be registered once.",
                        registration.ServiceType,
                        registration.ImplementationType);
                }

                result.Add(registration.ServiceType, registration);
            }

            return result;
        }

        private static IReadOnlyDictionary<Type, Type> BuildScopeTree(
            IReadOnlyList<ScopeDefinition> definitions)
        {
            var parents = new Dictionary<Type, Type>();
            foreach (var definition in definitions)
            {
                if (parents.ContainsKey(definition.ScopeType))
                {
                    throw Error(
                        DependencyErrorCode.InvalidScopeDefinition,
                        "A scope marker may only be defined once.",
                        definition.ScopeType);
                }

                parents.Add(definition.ScopeType, definition.ParentScopeType);
            }

            foreach (var definition in definitions)
            {
                if (definition.ParentScopeType != null &&
                    !parents.ContainsKey(definition.ParentScopeType))
                {
                    throw Error(
                        DependencyErrorCode.InvalidScopeDefinition,
                        "The parent scope marker is not defined.",
                        definition.ScopeType);
                }

                var seen = new HashSet<Type>();
                var current = definition.ScopeType;
                while (current != null)
                {
                    if (!seen.Add(current))
                    {
                        throw Error(
                            DependencyErrorCode.InvalidScopeDefinition,
                            "The scope definition graph contains a cycle.",
                            definition.ScopeType);
                    }

                    parents.TryGetValue(current, out current);
                }
            }

            return parents;
        }

        private static IReadOnlyDictionary<int, ActivationPlan> BuildActivationPlans(
            IReadOnlyList<ServiceRegistration> registrations,
            IReadOnlyDictionary<Type, Type> scopeParents)
        {
            var plans = new Dictionary<int, ActivationPlan>();
            foreach (var registration in registrations)
            {
                if (registration.IsExternal)
                {
                    if (registration.ExternalInstance == null)
                    {
                        throw Error(
                            DependencyErrorCode.InvalidImplementation,
                            "An external service instance cannot be null.",
                            registration.ServiceType);
                    }

                    continue;
                }

                var implementationType = registration.ImplementationType;
                if (!registration.ServiceType.IsAssignableFrom(implementationType) ||
                    implementationType.IsAbstract ||
                    implementationType.IsInterface ||
                    implementationType.ContainsGenericParameters)
                {
                    throw Error(
                        DependencyErrorCode.InvalidImplementation,
                        "The implementation must be a closed concrete type assignable to the service.",
                        registration.ServiceType,
                        implementationType);
                }

                if (registration.Lifetime == ServiceLifetime.Scoped &&
                    (registration.ScopeType == null ||
                     !scopeParents.ContainsKey(registration.ScopeType)))
                {
                    throw Error(
                        DependencyErrorCode.InvalidScopeDefinition,
                        "A scoped registration must use a defined scope marker.",
                        registration.ServiceType,
                        implementationType);
                }

                var constructor = SelectConstructor(registration);
                var parameterTypes = constructor
                    .GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray();
                plans.Add(
                    registration.Id,
                    new ActivationPlan(registration, constructor, parameterTypes));
            }

            return plans;
        }

        private static ConstructorInfo SelectConstructor(
            ServiceRegistration registration)
        {
            var implementationType = registration.ImplementationType;
            var allConstructors = implementationType.GetConstructors(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            var markedNonPublic = allConstructors.Any(
                constructor =>
                    !constructor.IsPublic &&
                    constructor.IsDefined(typeof(InjectConstructorAttribute), false));
            var publicConstructors = implementationType.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public);
            var markedPublic = publicConstructors
                .Where(constructor =>
                    constructor.IsDefined(typeof(InjectConstructorAttribute), false))
                .ToArray();

            if (markedNonPublic ||
                publicConstructors.Length == 0 ||
                markedPublic.Length > 1 ||
                (publicConstructors.Length > 1 && markedPublic.Length != 1))
            {
                throw Error(
                    DependencyErrorCode.AmbiguousConstructor,
                    "The implementation must have one public constructor or one marked public constructor.",
                    registration.ServiceType,
                    implementationType);
            }

            return markedPublic.Length == 1
                ? markedPublic[0]
                : publicConstructors[0];
        }

        private static void ValidateDependencyGraph(
            IReadOnlyList<ServiceRegistration> registrations,
            IReadOnlyDictionary<Type, ServiceRegistration> defaults,
            IReadOnlyDictionary<int, ActivationPlan> plans)
        {
            foreach (var registration in registrations)
            {
                if (registration.IsExternal)
                {
                    continue;
                }

                var states = new Dictionary<int, VisitState>();
                var path = new List<Type>();
                Visit(registration, defaults, plans, states, path);
            }
        }

        private static void Visit(
            ServiceRegistration registration,
            IReadOnlyDictionary<Type, ServiceRegistration> defaults,
            IReadOnlyDictionary<int, ActivationPlan> plans,
            IDictionary<int, VisitState> states,
            IList<Type> path)
        {
            if (states.TryGetValue(registration.Id, out var state))
            {
                if (state == VisitState.Visiting)
                {
                    var cyclePath = new List<Type>(path)
                    {
                        registration.ImplementationType ?? registration.ServiceType
                    };
                    throw Error(
                        DependencyErrorCode.CircularDependency,
                        "The dependency graph contains a cycle.",
                        registration.ServiceType,
                        registration.ImplementationType,
                        cyclePath);
                }

                return;
            }

            states[registration.Id] = VisitState.Visiting;
            path.Add(registration.ImplementationType ?? registration.ServiceType);

            if (!registration.IsExternal)
            {
                foreach (var parameterType in plans[registration.Id].ParameterTypes)
                {
                    if (!defaults.TryGetValue(parameterType, out var dependency))
                    {
                        var missingPath = new List<Type>(path)
                        {
                            parameterType
                        };
                        throw Error(
                            DependencyErrorCode.MissingDependency,
                            "A constructor dependency is not registered.",
                            parameterType,
                            registration.ImplementationType,
                            missingPath);
                    }

                    Visit(dependency, defaults, plans, states, path);
                }
            }

            path.RemoveAt(path.Count - 1);
            states[registration.Id] = VisitState.Visited;
        }

        private static DependencyInjectionException Error(
            DependencyErrorCode code,
            string message,
            Type serviceType,
            Type implementationType = null,
            IEnumerable<Type> path = null)
        {
            return new DependencyInjectionException(
                code,
                message,
                serviceType,
                implementationType,
                path);
        }

        private enum VisitState
        {
            Visiting,
            Visited
        }
    }
}
