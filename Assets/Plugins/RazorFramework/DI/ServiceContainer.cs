using System;
using System.Collections.Generic;
using System.Reflection;

namespace RazorFramework.DI
{
    public sealed class ServiceContainer : IServiceResolver, IDisposable
    {
        private readonly ContainerBuildModel _model;
        private readonly DiagnosticDispatcher _diagnostics;
        private readonly LifetimeOwner _rootOwner = new LifetimeOwner();
        private bool _disposed;

        internal ServiceContainer(
            ContainerBuildModel model,
            ContainerOptions options)
        {
            _model = model;
            _diagnostics = new DiagnosticDispatcher(options.DiagnosticSink);
            _diagnostics.Write(new DiDiagnosticEvent(DiDiagnosticKind.ContainerBuilt));
        }

        public object Resolve(Type serviceType)
        {
            return ResolveForScope(null, serviceType);
        }

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            EnsureNotDisposed();
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (!_model.DefaultRegistrations.ContainsKey(serviceType))
            {
                service = null;
                return false;
            }

            service = Resolve(serviceType);
            return true;
        }

        public IReadOnlyList<T> ResolveAll<T>() where T : class
        {
            EnsureNotDisposed();
            return Array.Empty<T>();
        }

        public ServiceScope CreateScope<TScope>()
        {
            return CreateChildScope(null, typeof(TScope));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _diagnostics.Write(new DiDiagnosticEvent(DiDiagnosticKind.ContainerDisposed));
        }

        private object ResolveRegistration(
            ServiceRegistration registration,
            ServiceScope scope,
            IList<Type> path)
        {
            if (registration.IsExternal)
            {
                return registration.ExternalInstance;
            }

            var requiredScope = _model.Plans[registration.Id].RequiredScopeType;
            if (requiredScope != null &&
                (scope == null || scope.FindAncestor(requiredScope) == null))
            {
                throw new DependencyInjectionException(
                    DependencyErrorCode.ScopeMismatch,
                    "The service requires a scope that is not active.",
                    registration.ServiceType,
                    registration.ImplementationType);
            }

            switch (registration.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return _rootOwner.GetOrCreate(
                        registration.Id,
                        () => CreateInstance(registration, null, path));
                case ServiceLifetime.Transient:
                    return CreateInstance(registration, scope, path);
                case ServiceLifetime.Scoped:
                    var anchor = scope?.FindAncestor(registration.ScopeType);
                    if (anchor == null)
                    {
                        throw new DependencyInjectionException(
                            DependencyErrorCode.ScopeMismatch,
                            "A scoped service requires a matching scope.",
                            registration.ServiceType,
                            registration.ImplementationType);
                    }

                    return anchor.Owner.GetOrCreate(
                        registration.Id,
                        () => CreateInstance(registration, anchor, path));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private object CreateInstance(
            ServiceRegistration registration,
            ServiceScope scope,
            IList<Type> path)
        {
            var plan = _model.Plans[registration.Id];
            path.Add(registration.ImplementationType);
            var arguments = new object[plan.ParameterTypes.Count];
            for (var index = 0; index < plan.ParameterTypes.Count; index++)
            {
                var dependencyType = plan.ParameterTypes[index];
                var dependency = _model.DefaultRegistrations[dependencyType];
                arguments[index] = ResolveRegistration(dependency, scope, path);
            }

            try
            {
                return plan.Constructor.Invoke(arguments);
            }
            catch (TargetInvocationException error)
            {
                var inner = error.InnerException ?? error;
                throw new DependencyInjectionException(
                    DependencyErrorCode.ActivationFailed,
                    "A service constructor threw an exception.",
                    registration.ServiceType,
                    registration.ImplementationType,
                    path,
                    inner);
            }
            finally
            {
                path.RemoveAt(path.Count - 1);
            }
        }

        internal ServiceScope CreateChildScope(
            ServiceScope parent,
            Type scopeType)
        {
            EnsureNotDisposed();
            if (scopeType == null)
            {
                throw new ArgumentNullException(nameof(scopeType));
            }

            if (!_model.ScopeParents.TryGetValue(scopeType, out var expectedParent))
            {
                throw new DependencyInjectionException(
                    DependencyErrorCode.InvalidScopeDefinition,
                    "The requested scope marker is not defined.",
                    scopeType);
            }

            var actualParent = parent?.ScopeType;
            if (expectedParent != actualParent)
            {
                throw new DependencyInjectionException(
                    DependencyErrorCode.ScopeMismatch,
                    "The scope must be created from its declared direct parent.",
                    scopeType);
            }

            var scope = new ServiceScope(this, parent, scopeType);
            _diagnostics.Write(new DiDiagnosticEvent(
                DiDiagnosticKind.ScopeCreated,
                scopeType: scopeType));
            return scope;
        }

        internal object ResolveFromScope(
            ServiceScope scope,
            Type serviceType)
        {
            return ResolveForScope(scope, serviceType);
        }

        internal bool TryResolveFromScope(
            ServiceScope scope,
            Type serviceType,
            out object service)
        {
            EnsureNotDisposed();
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (!_model.DefaultRegistrations.ContainsKey(serviceType))
            {
                service = null;
                return false;
            }

            service = ResolveForScope(scope, serviceType);
            return true;
        }

        internal IReadOnlyList<T> ResolveAllFromScope<T>(
            ServiceScope scope)
            where T : class
        {
            EnsureNotDisposed();
            return Array.Empty<T>();
        }

        private object ResolveForScope(
            ServiceScope scope,
            Type serviceType)
        {
            EnsureNotDisposed();
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (!_model.DefaultRegistrations.TryGetValue(
                    serviceType,
                    out var registration))
            {
                throw new DependencyInjectionException(
                    DependencyErrorCode.MissingDependency,
                    "The requested service is not registered.",
                    serviceType,
                    dependencyPath: new[] { serviceType });
            }

            return ResolveRegistration(
                registration,
                scope,
                new List<Type>());
        }


        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new DependencyInjectionException(
                    DependencyErrorCode.ContainerDisposed,
                    "The service container has been disposed.");
            }
        }
    }
}
