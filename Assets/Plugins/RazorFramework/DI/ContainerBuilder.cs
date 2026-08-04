using System;
using System.Collections.Generic;

namespace RazorFramework.DI
{
    public sealed class ContainerBuilder
    {
        private readonly List<ServiceRegistration> _registrations =
            new List<ServiceRegistration>();
        private readonly List<ScopeDefinition> _scopeDefinitions =
            new List<ScopeDefinition>();
        private int _nextRegistrationId;
        private bool _consumed;

        public ContainerBuilder AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return AddType(
                typeof(TService),
                typeof(TImplementation),
                ServiceLifetime.Singleton,
                null);
        }

        public ContainerBuilder AddSingleton<TImplementation>()
            where TImplementation : class
        {
            return AddSingleton<TImplementation, TImplementation>();
        }

        public ContainerBuilder AddSingleton<TService>(TService instance)
            where TService : class
        {
            EnsureMutable();
            _registrations.Add(new ServiceRegistration(
                _nextRegistrationId++,
                typeof(TService),
                null,
                ServiceLifetime.Singleton,
                null,
                instance,
                false));
            return this;
        }

        public ContainerBuilder AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return AddType(
                typeof(TService),
                typeof(TImplementation),
                ServiceLifetime.Transient,
                null);
        }

        public ContainerBuilder AddTransient<TImplementation>()
            where TImplementation : class
        {
            return AddTransient<TImplementation, TImplementation>();
        }

        public ContainerBuilder AddScoped<TService, TImplementation, TScope>()
            where TService : class
            where TImplementation : class, TService
        {
            return AddType(
                typeof(TService),
                typeof(TImplementation),
                ServiceLifetime.Scoped,
                typeof(TScope));
        }

        public ContainerBuilder AddScoped<TImplementation, TScope>()
            where TImplementation : class
        {
            return AddScoped<TImplementation, TImplementation, TScope>();
        }

        public ContainerBuilder AddCollectionSingleton<
            TService,
            TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return AddType(
                typeof(TService),
                typeof(TImplementation),
                ServiceLifetime.Singleton,
                null,
                true);
        }

        public ContainerBuilder AddCollectionTransient<
            TService,
            TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return AddType(
                typeof(TService),
                typeof(TImplementation),
                ServiceLifetime.Transient,
                null,
                true);
        }

        public ContainerBuilder AddCollectionScoped<
            TService,
            TImplementation,
            TScope>()
            where TService : class
            where TImplementation : class, TService
        {
            return AddType(
                typeof(TService),
                typeof(TImplementation),
                ServiceLifetime.Scoped,
                typeof(TScope),
                true);
        }

        public ContainerBuilder DefineScope<TScope>()
        {
            return DefineScope(typeof(TScope), null);
        }

        public ContainerBuilder DefineScope<TScope, TParentScope>()
        {
            return DefineScope(typeof(TScope), typeof(TParentScope));
        }

        public ServiceContainer Build(ContainerOptions options = null)
        {
            EnsureMutable();
            var model = DependencyGraphValidator.Build(
                _registrations,
                _scopeDefinitions);
            var container = new ServiceContainer(model, options ?? new ContainerOptions());
            _consumed = true;
            return container;
        }

        private ContainerBuilder AddType(
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime,
            Type scopeType,
            bool isCollection = false)
        {
            EnsureMutable();
            _registrations.Add(new ServiceRegistration(
                _nextRegistrationId++,
                serviceType,
                implementationType,
                lifetime,
                scopeType,
                null,
                isCollection));
            return this;
        }

        private ContainerBuilder DefineScope(Type scopeType, Type parentScopeType)
        {
            EnsureMutable();
            _scopeDefinitions.Add(new ScopeDefinition(scopeType, parentScopeType));
            return this;
        }

        private void EnsureMutable()
        {
            if (_consumed)
            {
                throw new InvalidOperationException(
                    "ContainerBuilder has already produced a container.");
            }
        }
    }
}
