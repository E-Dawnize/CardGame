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
            var collectionRegistrations =
                BuildCollectionRegistrations(registrations);
            var scopeParents = BuildScopeTree(sourceScopeDefinitions);
            var plans = BuildActivationPlans(registrations, scopeParents);
            ValidateDependencyGraph(
                registrations,
                defaultRegistrations,
                collectionRegistrations,
                plans);
            ValidateLifetimes(
                registrations,
                defaultRegistrations,
                collectionRegistrations,
                plans,
                scopeParents);

            return new ContainerBuildModel(
                registrations,
                defaultRegistrations,
                collectionRegistrations,
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

        private static IReadOnlyDictionary<
            Type,
            IReadOnlyList<ServiceRegistration>> BuildCollectionRegistrations(
                IReadOnlyList<ServiceRegistration> registrations)
        {
            var grouped =
                new Dictionary<Type, List<ServiceRegistration>>();
            foreach (var registration in registrations)
            {
                if (!registration.IsCollection)
                {
                    continue;
                }

                if (!grouped.TryGetValue(
                        registration.ServiceType,
                        out var entries))
                {
                    entries = new List<ServiceRegistration>();
                    grouped.Add(registration.ServiceType, entries);
                }

                entries.Add(registration);
            }

            var result =
                new Dictionary<Type, IReadOnlyList<ServiceRegistration>>();
            foreach (var pair in grouped)
            {
                result.Add(pair.Key, pair.Value.ToArray());
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
                var dependencies = constructor
                    .GetParameters()
                    .Select(parameter => BuildDependencyPlan(parameter.ParameterType))
                    .ToArray();
                plans.Add(
                    registration.Id,
                    new ActivationPlan(registration, constructor, dependencies));
            }

            return plans;
        }

        private static DependencyPlan BuildDependencyPlan(Type parameterType)
        {
            if (parameterType.IsGenericType &&
                parameterType.GetGenericTypeDefinition() ==
                typeof(IReadOnlyList<>))
            {
                return new DependencyPlan(
                    parameterType,
                    parameterType.GetGenericArguments()[0],
                    true);
            }

            return new DependencyPlan(parameterType, parameterType, false);
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
            IReadOnlyDictionary<Type, IReadOnlyList<ServiceRegistration>>
                collections,
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
                Visit(registration, defaults, collections, plans, states, path);
            }
        }

        private static void Visit(
            ServiceRegistration registration,
            IReadOnlyDictionary<Type, ServiceRegistration> defaults,
            IReadOnlyDictionary<Type, IReadOnlyList<ServiceRegistration>>
                collections,
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
                foreach (var dependencyPlan in
                         plans[registration.Id].Dependencies)
                {
                    if (dependencyPlan.IsCollection)
                    {
                        if (collections.TryGetValue(
                                dependencyPlan.ServiceType,
                                out var entries))
                        {
                            foreach (var entry in entries)
                            {
                                Visit(
                                    entry,
                                    defaults,
                                    collections,
                                    plans,
                                    states,
                                    path);
                            }
                        }

                        continue;
                    }

                    if (!defaults.TryGetValue(
                            dependencyPlan.ServiceType,
                            out var dependency))
                    {
                        var missingPath = new List<Type>(path)
                        {
                            dependencyPlan.ServiceType
                        };
                        throw Error(
                            DependencyErrorCode.MissingDependency,
                            "A constructor dependency is not registered.",
                            dependencyPlan.ServiceType,
                            registration.ImplementationType,
                            missingPath);
                    }

                    Visit(
                        dependency,
                        defaults,
                        collections,
                        plans,
                        states,
                        path);
                }
            }

            path.RemoveAt(path.Count - 1);
            states[registration.Id] = VisitState.Visited;
        }

        private static void ValidateLifetimes(
            IReadOnlyList<ServiceRegistration> registrations,
            IReadOnlyDictionary<Type, ServiceRegistration> defaults,
            IReadOnlyDictionary<Type, IReadOnlyList<ServiceRegistration>>
                collections,
            IReadOnlyDictionary<int, ActivationPlan> plans,
            IReadOnlyDictionary<Type, Type> scopeParents)
        {
            var requirements = new Dictionary<int, ScopeRequirement>();
            foreach (var registration in registrations)
            {
                DetermineRequiredScope(
                    registration,
                    defaults,
                    collections,
                    plans,
                    scopeParents,
                    requirements);
            }
        }

        private static ScopeRequirement DetermineRequiredScope(
            ServiceRegistration registration,
            IReadOnlyDictionary<Type, ServiceRegistration> defaults,
            IReadOnlyDictionary<Type, IReadOnlyList<ServiceRegistration>>
                collections,
            IReadOnlyDictionary<int, ActivationPlan> plans,
            IReadOnlyDictionary<Type, Type> scopeParents,
            IDictionary<int, ScopeRequirement> requirements)
        {
            if (registration.IsExternal)
            {
                return null;
            }

            if (requirements.TryGetValue(registration.Id, out var cached))
            {
                return cached;
            }

            ScopeRequirement dependencyRequirement = null;
            var plan = plans[registration.Id];
            foreach (var dependencyPlan in plan.Dependencies)
            {
                if (dependencyPlan.IsCollection)
                {
                    if (collections.TryGetValue(
                            dependencyPlan.ServiceType,
                            out var entries))
                    {
                        foreach (var entry in entries)
                        {
                            dependencyRequirement =
                                MergeDependencyRequirement(
                                    dependencyRequirement,
                                    entry,
                                    registration,
                                    defaults,
                                    collections,
                                    plans,
                                    scopeParents,
                                    requirements);
                        }
                    }

                    continue;
                }

                dependencyRequirement = MergeDependencyRequirement(
                    dependencyRequirement,
                    defaults[dependencyPlan.ServiceType],
                    registration,
                    defaults,
                    collections,
                    plans,
                    scopeParents,
                    requirements);
            }

            ScopeRequirement result;
            switch (registration.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    if (dependencyRequirement != null)
                    {
                        throw Error(
                            DependencyErrorCode.CaptiveDependency,
                            "A singleton cannot capture a scoped dependency.",
                            registration.ServiceType,
                            registration.ImplementationType,
                            PrefixPath(registration, dependencyRequirement.Path));
                    }

                    result = null;
                    break;
                case ServiceLifetime.Scoped:
                    if (dependencyRequirement != null &&
                        !IsAncestorOrSame(
                            dependencyRequirement.ScopeType,
                            registration.ScopeType,
                            scopeParents))
                    {
                        throw Error(
                            DependencyErrorCode.CaptiveDependency,
                            "A scoped service cannot depend on a descendant scope.",
                            registration.ServiceType,
                            registration.ImplementationType,
                            PrefixPath(registration, dependencyRequirement.Path));
                    }

                    result = new ScopeRequirement(
                        registration.ScopeType,
                        new[]
                        {
                            registration.ImplementationType ??
                            registration.ServiceType
                        });
                    break;
                case ServiceLifetime.Transient:
                    result = dependencyRequirement == null
                        ? null
                        : new ScopeRequirement(
                            dependencyRequirement.ScopeType,
                            PrefixPath(registration, dependencyRequirement.Path));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            plan.RequiredScopeType = result?.ScopeType;
            plan.RequiredScopePath = result?.Path;
            requirements[registration.Id] = result;
            return result;
        }

        private static ScopeRequirement MergeDependencyRequirement(
            ScopeRequirement currentRequirement,
            ServiceRegistration dependency,
            ServiceRegistration consumer,
            IReadOnlyDictionary<Type, ServiceRegistration> defaults,
            IReadOnlyDictionary<Type, IReadOnlyList<ServiceRegistration>>
                collections,
            IReadOnlyDictionary<int, ActivationPlan> plans,
            IReadOnlyDictionary<Type, Type> scopeParents,
            IDictionary<int, ScopeRequirement> requirements)
        {
            var requirement = DetermineRequiredScope(
                dependency,
                defaults,
                collections,
                plans,
                scopeParents,
                requirements);
            return MergeRequirements(
                currentRequirement,
                requirement,
                consumer,
                scopeParents);
        }

        private static ScopeRequirement MergeRequirements(
            ScopeRequirement left,
            ScopeRequirement right,
            ServiceRegistration registration,
            IReadOnlyDictionary<Type, Type> scopeParents)
        {
            if (left == null)
            {
                return right;
            }

            if (right == null)
            {
                return left;
            }

            if (IsAncestorOrSame(
                    left.ScopeType,
                    right.ScopeType,
                    scopeParents))
            {
                return right;
            }

            if (IsAncestorOrSame(
                    right.ScopeType,
                    left.ScopeType,
                    scopeParents))
            {
                return left;
            }

            throw Error(
                DependencyErrorCode.ScopeMismatch,
                "A service requires incompatible sibling scopes.",
                registration.ServiceType,
                registration.ImplementationType,
                MergeConflictPath(registration, left.Path, right.Path));
        }

        private static IReadOnlyList<Type> PrefixPath(
            ServiceRegistration registration,
            IEnumerable<Type> dependencyPath)
        {
            var result = new List<Type>
            {
                registration.ImplementationType ?? registration.ServiceType
            };
            result.AddRange(dependencyPath);
            return result;
        }

        private static IReadOnlyList<Type> MergeConflictPath(
            ServiceRegistration registration,
            IEnumerable<Type> left,
            IEnumerable<Type> right)
        {
            var result = new List<Type>
            {
                registration.ImplementationType ?? registration.ServiceType
            };
            result.AddRange(left);
            result.AddRange(right);
            return result;
        }

        private static bool IsAncestorOrSame(
            Type ancestor,
            Type descendant,
            IReadOnlyDictionary<Type, Type> scopeParents)
        {
            var current = descendant;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                scopeParents.TryGetValue(current, out current);
            }

            return false;
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

        private sealed class ScopeRequirement
        {
            public ScopeRequirement(
                Type scopeType,
                IReadOnlyList<Type> path)
            {
                ScopeType = scopeType;
                Path = path;
            }

            public Type ScopeType { get; }
            public IReadOnlyList<Type> Path { get; }
        }
    }
}
