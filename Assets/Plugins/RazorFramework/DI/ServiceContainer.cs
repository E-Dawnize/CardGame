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

            return ResolveRegistration(registration, new List<Type>());
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
            IList<Type> path)
        {
            if (registration.IsExternal)
            {
                return registration.ExternalInstance;
            }

            switch (registration.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return _rootOwner.GetOrCreate(
                        registration.Id,
                        () => CreateInstance(registration, path));
                case ServiceLifetime.Transient:
                    return CreateInstance(registration, path);
                case ServiceLifetime.Scoped:
                    throw new DependencyInjectionException(
                        DependencyErrorCode.ScopeMismatch,
                        "A scoped service requires a matching scope.",
                        registration.ServiceType,
                        registration.ImplementationType);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private object CreateInstance(
            ServiceRegistration registration,
            IList<Type> path)
        {
            var plan = _model.Plans[registration.Id];
            path.Add(registration.ImplementationType);
            var arguments = new object[plan.ParameterTypes.Count];
            for (var index = 0; index < plan.ParameterTypes.Count; index++)
            {
                var dependencyType = plan.ParameterTypes[index];
                var dependency = _model.DefaultRegistrations[dependencyType];
                arguments[index] = ResolveRegistration(dependency, path);
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
