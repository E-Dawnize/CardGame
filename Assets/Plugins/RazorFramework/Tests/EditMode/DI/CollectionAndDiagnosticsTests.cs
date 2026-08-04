using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RazorFramework.DI.Tests
{
    public sealed class CollectionAndDiagnosticsTests
    {
        [Test]
        public void Collection_UsesOnlyExplicitEntriesInRegistrationOrder()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<IPlugin, DefaultPlugin>();
            builder.AddCollectionTransient<IPlugin, FirstPlugin>();
            builder.AddCollectionTransient<IPlugin, SecondPlugin>();
            using var container = builder.Build();

            var plugins = container.ResolveAll<IPlugin>();

            Assert.That(
                plugins.Select(plugin => plugin.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(FirstPlugin),
                    typeof(SecondPlugin)
                }));
            Assert.That(container.Resolve<IPlugin>(), Is.TypeOf<DefaultPlugin>());
        }

        [Test]
        public void MissingCollection_ResolvesAndInjectsAsEmptyReadOnlyList()
        {
            var builder = new ContainerBuilder();
            builder.AddTransient<PluginConsumer>();
            using var container = builder.Build();

            Assert.That(container.ResolveAll<IPlugin>(), Is.Empty);
            Assert.That(container.Resolve<PluginConsumer>().Plugins, Is.Empty);
        }

        [Test]
        public void CollectionEntries_RespectTheirOwnLifetimes()
        {
            var builder = new ContainerBuilder();
            builder.AddCollectionSingleton<IPlugin, FirstPlugin>();
            builder.AddCollectionTransient<IPlugin, SecondPlugin>();
            using var container = builder.Build();

            var first = container.ResolveAll<IPlugin>();
            var second = container.ResolveAll<IPlugin>();

            Assert.That(first[0], Is.SameAs(second[0]));
            Assert.That(first[1], Is.Not.SameAs(second[1]));
        }

        [Test]
        public void ScopedCollection_RequiresAndReusesItsDeclaredScope()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.AddCollectionScoped<IPlugin, FirstPlugin, RunTag>();
            using var container = builder.Build();

            var error = Assert.Throws<DependencyInjectionException>(
                () => container.ResolveAll<IPlugin>());
            using var run = container.CreateScope<RunTag>();
            var first = run.ResolveAll<IPlugin>();
            var second = run.ResolveAll<IPlugin>();

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.ScopeMismatch));
            Assert.That(first[0], Is.SameAs(second[0]));
        }

        [Test]
        public void SingletonConsumer_CannotCaptureScopedCollection()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.AddCollectionScoped<IPlugin, FirstPlugin, RunTag>();
            builder.AddSingleton<PluginConsumer>();

            var error = Assert.Throws<DependencyInjectionException>(
                () => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.CaptiveDependency));
        }

        [Test]
        public void Consumer_CannotRequireCollectionsFromSiblingScopes()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.DefineScope<RewardTag>();
            builder.AddCollectionScoped<IPlugin, FirstPlugin, RunTag>();
            builder.AddCollectionScoped<IPlugin, SecondPlugin, RewardTag>();
            builder.AddTransient<PluginConsumer>();

            var error = Assert.Throws<DependencyInjectionException>(
                () => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.ScopeMismatch));
        }

        [Test]
        public void Diagnostics_ReportStableLifecycleAndResolutionFields()
        {
            var sink = new RecordingDiagnosticSink();
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.AddSingleton<IService, Service>();
            builder.AddScoped<ScopedDiagnosticService, RunTag>();
            var container = builder.Build(new ContainerOptions
            {
                DiagnosticSink = sink
            });
            var run = container.CreateScope<RunTag>();

            run.Resolve<ScopedDiagnosticService>();
            var error = Assert.Throws<DependencyInjectionException>(
                () => run.Resolve<IMissingService>());
            run.Dispose();
            container.Dispose();

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.MissingDependency));
            Assert.That(
                sink.Events.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    DiDiagnosticKind.ContainerBuilt,
                    DiDiagnosticKind.ScopeCreated,
                    DiDiagnosticKind.InstanceCreated,
                    DiDiagnosticKind.InstanceCreated,
                    DiDiagnosticKind.ResolutionFailed,
                    DiDiagnosticKind.ScopeDisposed,
                    DiDiagnosticKind.ContainerDisposed
                }));

            var singletonCreated = sink.Events[2];
            Assert.That(singletonCreated.ServiceType, Is.EqualTo(typeof(IService)));
            Assert.That(
                singletonCreated.ImplementationType,
                Is.EqualTo(typeof(Service)));
            Assert.That(singletonCreated.ScopeType, Is.Null);
            Assert.That(singletonCreated.ErrorCode, Is.Null);

            var scopedCreated = sink.Events[3];
            Assert.That(
                scopedCreated.ServiceType,
                Is.EqualTo(typeof(ScopedDiagnosticService)));
            Assert.That(scopedCreated.ScopeType, Is.EqualTo(typeof(RunTag)));

            var resolutionFailed = sink.Events[4];
            Assert.That(
                resolutionFailed.ServiceType,
                Is.EqualTo(typeof(IMissingService)));
            Assert.That(
                resolutionFailed.ErrorCode,
                Is.EqualTo(DependencyErrorCode.MissingDependency));
            Assert.That(resolutionFailed.ScopeType, Is.EqualTo(typeof(RunTag)));
        }

        [Test]
        public void ActivationFailure_ProducesOneResolutionFailedEvent()
        {
            var sink = new RecordingDiagnosticSink();
            var builder = new ContainerBuilder();
            builder.AddTransient<ThrowingService>();
            using var container = builder.Build(new ContainerOptions
            {
                DiagnosticSink = sink
            });

            var error = Assert.Throws<DependencyInjectionException>(
                () => container.Resolve<ThrowingService>());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.ActivationFailed));
            Assert.That(
                sink.Events.Count(
                    item => item.Kind == DiDiagnosticKind.ResolutionFailed),
                Is.EqualTo(1));
            Assert.That(
                sink.Events.Single(
                    item => item.Kind == DiDiagnosticKind.ResolutionFailed)
                    .ErrorCode,
                Is.EqualTo(DependencyErrorCode.ActivationFailed));
        }

        [Test]
        public void DiagnosticSinkFailure_DoesNotChangeContainerBehavior()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.AddSingleton<IService, Service>();
            var container = builder.Build(new ContainerOptions
            {
                DiagnosticSink = new ThrowingDiagnosticSink()
            });

            Assert.That(container.Resolve<IService>(), Is.Not.Null);
            var scope = container.CreateScope<RunTag>();
            Assert.DoesNotThrow(scope.Dispose);
            Assert.DoesNotThrow(container.Dispose);
        }

        private interface IPlugin
        {
        }

        private sealed class DefaultPlugin : IPlugin
        {
        }

        private sealed class FirstPlugin : IPlugin
        {
        }

        private sealed class SecondPlugin : IPlugin
        {
        }

        private sealed class PluginConsumer
        {
            public PluginConsumer(IReadOnlyList<IPlugin> plugins)
            {
                Plugins = plugins;
            }

            public IReadOnlyList<IPlugin> Plugins { get; }
        }

        private interface IService
        {
        }

        private sealed class Service : IService
        {
        }

        private sealed class ScopedDiagnosticService
        {
            public ScopedDiagnosticService(IService service)
            {
            }
        }

        private sealed class ThrowingService
        {
            public ThrowingService()
            {
                throw new ExpectedException();
            }
        }

        private interface IMissingService
        {
        }

        private sealed class RunTag
        {
        }

        private sealed class RewardTag
        {
        }

        private sealed class RecordingDiagnosticSink : IDiDiagnosticSink
        {
            public List<DiDiagnosticEvent> Events { get; } =
                new List<DiDiagnosticEvent>();

            public void Write(DiDiagnosticEvent diagnosticEvent)
            {
                Events.Add(diagnosticEvent);
            }
        }

        private sealed class ThrowingDiagnosticSink : IDiDiagnosticSink
        {
            public void Write(DiDiagnosticEvent diagnosticEvent)
            {
                throw new ExpectedException();
            }
        }

        private sealed class ExpectedException : Exception
        {
        }
    }
}
