using System;
using NUnit.Framework;

namespace RazorFramework.DI.Tests
{
    public sealed class BuilderValidationTests
    {
        [Test]
        public void Build_RejectsDuplicateDefaultRegistration()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<IService, Service>();
            builder.AddTransient<IService, AlternateService>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.DuplicateRegistration));
            Assert.That(error.ServiceType, Is.EqualTo(typeof(IService)));
        }

        [Test]
        public void Build_RejectsMissingConstructorDependency()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<NeedsMissingDependency>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.MissingDependency));
            Assert.That(error.DependencyPath, Is.EqualTo(new[]
            {
                typeof(NeedsMissingDependency),
                typeof(IMissingDependency)
            }));
        }

        [Test]
        public void Build_RejectsIndirectCycle()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<CycleA>();
            builder.AddTransient<CycleB>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.CircularDependency));
            Assert.That(error.DependencyPath[0], Is.EqualTo(typeof(CycleA)));
            Assert.That(
                error.DependencyPath[error.DependencyPath.Count - 1],
                Is.EqualTo(typeof(CycleA)));
        }

        [Test]
        public void Build_UsesTheSingleMarkedPublicConstructor()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<KnownDependency>();
            builder.AddTransient<SelectedConstructor>();

            using var container = builder.Build();

            Assert.That(
                container.Resolve<SelectedConstructor>().Selected,
                Is.EqualTo("marked"));
        }

        [Test]
        public void Build_RejectsAbstractImplementation()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<IService, AbstractService>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.InvalidImplementation));
        }

        [Test]
        public void Build_RejectsUnmarkedAmbiguousConstructors()
        {
            AssertAmbiguousConstructor<AmbiguousConstructors>();
        }

        [Test]
        public void Build_RejectsMultipleMarkedConstructors()
        {
            AssertAmbiguousConstructor<MultipleMarkedConstructors>();
        }

        [Test]
        public void Build_RejectsMarkedPrivateConstructor()
        {
            AssertAmbiguousConstructor<PrivateMarkedConstructor>();
        }

        private static void AssertAmbiguousConstructor<T>() where T : class
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<T>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.AmbiguousConstructor));
            Assert.That(error.ImplementationType, Is.EqualTo(typeof(T)));
        }

        [Test]
        public void FailedBuild_DoesNotConsumeBuilder()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<NeedsMissingDependency>();
            Assert.Throws<DependencyInjectionException>(() => builder.Build());

            builder.AddTransient<IMissingDependency, MissingDependency>();

            using var container = builder.Build();
            Assert.That(container.Resolve<NeedsMissingDependency>(), Is.Not.Null);
        }

        [Test]
        public void SuccessfulBuild_ConsumesBuilder()
        {
            var builder = new ContainerBuilder();
            using var container = builder.Build();

            Assert.Throws<InvalidOperationException>(() => builder.AddTransient<Service>());
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        private interface IService
        {
        }

        private sealed class Service : IService
        {
        }

        private sealed class AlternateService : IService
        {
        }

        private abstract class AbstractService : IService
        {
        }

        private interface IMissingDependency
        {
        }

        private sealed class MissingDependency : IMissingDependency
        {
        }

        private sealed class NeedsMissingDependency
        {
            public NeedsMissingDependency(IMissingDependency dependency)
            {
                Dependency = dependency;
            }

            public IMissingDependency Dependency { get; }
        }

        private sealed class CycleA
        {
            public CycleA(CycleB dependency)
            {
            }
        }

        private sealed class CycleB
        {
            public CycleB(CycleA dependency)
            {
            }
        }

        private sealed class KnownDependency
        {
        }

        private sealed class SelectedConstructor
        {
            public SelectedConstructor()
            {
                Selected = "unmarked";
            }

            [InjectConstructor]
            public SelectedConstructor(KnownDependency dependency)
            {
                Selected = "marked";
            }

            public string Selected { get; }
        }

        private sealed class AmbiguousConstructors
        {
            public AmbiguousConstructors()
            {
            }

            public AmbiguousConstructors(KnownDependency dependency)
            {
            }
        }

        private sealed class MultipleMarkedConstructors
        {
            [InjectConstructor]
            public MultipleMarkedConstructors()
            {
            }

            [InjectConstructor]
            public MultipleMarkedConstructors(KnownDependency dependency)
            {
            }
        }

        private sealed class PrivateMarkedConstructor
        {
            public PrivateMarkedConstructor()
            {
            }

            [InjectConstructor]
            private PrivateMarkedConstructor(KnownDependency dependency)
            {
            }
        }
    }
}
