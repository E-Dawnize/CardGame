using System;
using NUnit.Framework;

namespace RazorFramework.DI.Tests
{
    public sealed class ResolutionLifetimeTests
    {
        [Test]
        public void Singleton_IsSharedAcrossRootResolutions()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<IService, Service>();
            using var container = builder.Build();

            Assert.That(
                container.Resolve<IService>(),
                Is.SameAs(container.Resolve<IService>()));
        }

        [Test]
        public void Transient_IsCreatedForEveryResolution()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<IService, Service>();
            using var container = builder.Build();

            Assert.That(
                container.Resolve<IService>(),
                Is.Not.SameAs(container.Resolve<IService>()));
        }

        [Test]
        public void ExternalInstance_IsReturnedWithoutConstruction()
        {
            var instance = new Service();
            var builder = new ContainerBuilder();
            builder.AddSingleton<IService>(instance);
            using var container = builder.Build();

            Assert.That(container.Resolve<IService>(), Is.SameAs(instance));
        }

        [Test]
        public void ConstructorDependencies_AreResolved()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<Dependency>();
            builder.AddTransient<Consumer>();
            using var container = builder.Build();

            var consumer = container.Resolve<Consumer>();

            Assert.That(consumer.Dependency, Is.Not.Null);
        }

        [Test]
        public void ResolveUnknownService_ProvidesStructuredFailure()
        {
            using var container = new ContainerBuilder().Build();

            var error = Assert.Throws<DependencyInjectionException>(
                () => container.Resolve<IService>());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.MissingDependency));
            Assert.That(error.ServiceType, Is.EqualTo(typeof(IService)));
            Assert.That(error.DependencyPath, Is.EqualTo(new[] { typeof(IService) }));
        }

        [Test]
        public void TryResolveUnknownService_ReturnsFalse()
        {
            using var container = new ContainerBuilder().Build();

            var found = container.TryResolve(typeof(IService), out var service);

            Assert.That(found, Is.False);
            Assert.That(service, Is.Null);
        }

        [Test]
        public void TryResolve_DoesNotHideActivationFailure()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<ThrowingService>();
            using var container = builder.Build();

            var error = Assert.Throws<DependencyInjectionException>(
                () => container.TryResolve(typeof(ThrowingService), out _));

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.ActivationFailed));
            Assert.That(error.InnerException, Is.TypeOf<ExpectedConstructionException>());
            Assert.That(
                error.DependencyPath,
                Is.EqualTo(new[] { typeof(ThrowingService) }));
        }

        private interface IService
        {
        }

        private sealed class Service : IService
        {
        }

        private sealed class Dependency
        {
        }

        private sealed class Consumer
        {
            public Consumer(Dependency dependency)
            {
                Dependency = dependency;
            }

            public Dependency Dependency { get; }
        }

        private sealed class ThrowingService
        {
            public ThrowingService()
            {
                throw new ExpectedConstructionException();
            }
        }

        private sealed class ExpectedConstructionException : Exception
        {
        }
    }
}
