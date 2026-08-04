using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RazorFramework.DI.Tests
{
    public sealed class OwnershipAndConcurrencyTests
    {
        [Test]
        public void Scope_DisposesDependentsBeforeDependencies()
        {
            var order = new List<string>();
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.AddSingleton(order);
            builder.AddScoped<DisposableDependency, RunTag>();
            builder.AddScoped<DisposableService, RunTag>();
            using var container = builder.Build();
            var scope = container.CreateScope<RunTag>();
            scope.Resolve<DisposableService>();

            scope.Dispose();

            Assert.That(order, Is.EqualTo(new[] { "service", "dependency" }));
        }

        [Test]
        public void ParentScope_DisposesChildrenBeforeParentInstances()
        {
            var order = new List<string>();
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.DefineScope<EncounterTag, RunTag>();
            builder.AddSingleton(order);
            builder.AddScoped<RunDisposable, RunTag>();
            builder.AddScoped<EncounterDisposable, EncounterTag>();
            using var container = builder.Build();
            var run = container.CreateScope<RunTag>();
            var encounter = run.CreateScope<EncounterTag>();
            run.Resolve<RunDisposable>();
            encounter.Resolve<EncounterDisposable>();

            run.Dispose();

            Assert.That(order, Is.EqualTo(new[] { "encounter", "run" }));
        }

        [Test]
        public void Container_DisposesActiveScopesBeforeRootInstances()
        {
            var order = new List<string>();
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.AddSingleton(order);
            builder.AddSingleton<RootDisposable>();
            builder.AddScoped<RunDisposable, RunTag>();
            var container = builder.Build();
            var run = container.CreateScope<RunTag>();
            container.Resolve<RootDisposable>();
            run.Resolve<RunDisposable>();

            container.Dispose();

            Assert.That(order, Is.EqualTo(new[] { "run", "root" }));
        }

        [Test]
        public void ExternalDisposableInstance_IsNotDisposedByContainer()
        {
            var instance = new DisposableProbe();
            var builder = new ContainerBuilder();
            builder.AddSingleton(instance);
            var container = builder.Build();

            container.Dispose();

            Assert.That(instance.DisposeCount, Is.Zero);
        }

        [Test]
        public void Transients_AreOwnedByTheResolvingScope()
        {
            var probe = new DisposeCounter();
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.AddSingleton(probe);
            builder.AddTransient<CountingDisposable>();
            using var container = builder.Build();
            var run = container.CreateScope<RunTag>();
            run.Resolve<CountingDisposable>();
            run.Resolve<CountingDisposable>();

            run.Dispose();

            Assert.That(probe.Count, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_ContinuesAfterFailureAndAggregatesErrors()
        {
            var second = new DisposeCounter();
            var builder = new ContainerBuilder();
            builder.AddSingleton(second);
            builder.AddTransient<ThrowingDisposable>();
            builder.AddTransient<CountingDisposable>();
            var container = builder.Build();
            container.Resolve<ThrowingDisposable>();
            container.Resolve<CountingDisposable>();

            var error = Assert.Throws<AggregateException>(() => container.Dispose());

            Assert.That(error.InnerExceptions, Has.Count.EqualTo(1));
            Assert.That(second.Count, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var probe = new DisposeCounter();
            var builder = new ContainerBuilder();
            builder.AddSingleton(probe);
            builder.AddSingleton<CountingDisposable>();
            var container = builder.Build();
            container.Resolve<CountingDisposable>();

            container.Dispose();
            container.Dispose();

            Assert.That(probe.Count, Is.EqualTo(1));
        }

        [Test]
        public void DisposedScope_RejectsResolutionAndChildCreation()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.DefineScope<EncounterTag, RunTag>();
            builder.AddSingleton(new List<string>());
            builder.AddScoped<RunDisposable, RunTag>();
            using var container = builder.Build();
            var run = container.CreateScope<RunTag>();
            run.Dispose();

            Assert.That(
                Assert.Throws<DependencyInjectionException>(
                    () => run.Resolve<RunDisposable>()).Code,
                Is.EqualTo(DependencyErrorCode.ContainerDisposed));
            Assert.That(
                Assert.Throws<DependencyInjectionException>(
                    () => run.CreateScope<EncounterTag>()).Code,
                Is.EqualTo(DependencyErrorCode.ContainerDisposed));
        }

        [Test]
        public void ConcurrentSingletonResolution_ConstructsExactlyOnce()
        {
            var counter = new ConstructionCounter();
            var builder = new ContainerBuilder();
            builder.AddSingleton(counter);
            builder.AddSingleton<ConcurrentSingleton>();
            using var container = builder.Build();

            var instances = ResolveConcurrently(
                () => container.Resolve<ConcurrentSingleton>());

            Assert.That(instances.Distinct().Count(), Is.EqualTo(1));
            Assert.That(counter.Count, Is.EqualTo(1));
        }

        [Test]
        public void ConcurrentScopedResolution_ConstructsOncePerScope()
        {
            var counter = new ConstructionCounter();
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.AddSingleton(counter);
            builder.AddScoped<ConcurrentScoped, RunTag>();
            using var container = builder.Build();
            using var first = container.CreateScope<RunTag>();
            using var second = container.CreateScope<RunTag>();

            var firstInstances = ResolveConcurrently(
                () => first.Resolve<ConcurrentScoped>());
            var secondInstances = ResolveConcurrently(
                () => second.Resolve<ConcurrentScoped>());

            Assert.That(firstInstances.Distinct().Count(), Is.EqualTo(1));
            Assert.That(secondInstances.Distinct().Count(), Is.EqualTo(1));
            Assert.That(firstInstances[0], Is.Not.SameAs(secondInstances[0]));
            Assert.That(counter.Count, Is.EqualTo(2));
        }

        [Test]
        public void FaultedSingleton_DoesNotRetryConstruction()
        {
            var counter = new ConstructionCounter();
            var builder = new ContainerBuilder();
            builder.AddSingleton(counter);
            builder.AddSingleton<AlwaysThrowingSingleton>();
            using var container = builder.Build();

            Assert.Throws<DependencyInjectionException>(
                () => container.Resolve<AlwaysThrowingSingleton>());
            Assert.Throws<DependencyInjectionException>(
                () => container.Resolve<AlwaysThrowingSingleton>());

            Assert.That(counter.Count, Is.EqualTo(1));
        }

        private static T[] ResolveConcurrently<T>(Func<T> resolve)
        {
            using var start = new ManualResetEventSlim(false);
            var tasks = Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() =>
                {
                    start.Wait();
                    return resolve();
                }))
                .ToArray();
            start.Set();
            Task.WaitAll(tasks);
            return tasks.Select(task => task.Result).ToArray();
        }

        private sealed class RunTag
        {
        }

        private sealed class EncounterTag
        {
        }

        private sealed class DisposableDependency : IDisposable
        {
            private readonly List<string> _order;

            public DisposableDependency(List<string> order)
            {
                _order = order;
            }

            public void Dispose()
            {
                _order.Add("dependency");
            }
        }

        private sealed class DisposableService : IDisposable
        {
            private readonly List<string> _order;

            public DisposableService(
                DisposableDependency dependency,
                List<string> order)
            {
                _order = order;
            }

            public void Dispose()
            {
                _order.Add("service");
            }
        }

        private sealed class RunDisposable : IDisposable
        {
            private readonly List<string> _order;

            public RunDisposable(List<string> order)
            {
                _order = order;
            }

            public void Dispose()
            {
                _order.Add("run");
            }
        }

        private sealed class EncounterDisposable : IDisposable
        {
            private readonly List<string> _order;

            public EncounterDisposable(List<string> order)
            {
                _order = order;
            }

            public void Dispose()
            {
                _order.Add("encounter");
            }
        }

        private sealed class RootDisposable : IDisposable
        {
            private readonly List<string> _order;

            public RootDisposable(List<string> order)
            {
                _order = order;
            }

            public void Dispose()
            {
                _order.Add("root");
            }
        }

        private sealed class DisposableProbe : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose()
            {
                DisposeCount++;
            }
        }

        private sealed class DisposeCounter
        {
            public int Count;
        }

        private sealed class CountingDisposable : IDisposable
        {
            private readonly DisposeCounter _counter;

            public CountingDisposable(DisposeCounter counter)
            {
                _counter = counter;
            }

            public void Dispose()
            {
                Interlocked.Increment(ref _counter.Count);
            }
        }

        private sealed class ThrowingDisposable : IDisposable
        {
            public void Dispose()
            {
                throw new ExpectedDisposeException();
            }
        }

        private sealed class ConstructionCounter
        {
            public int Count;
        }

        private sealed class ConcurrentSingleton
        {
            public ConcurrentSingleton(ConstructionCounter counter)
            {
                Interlocked.Increment(ref counter.Count);
            }
        }

        private sealed class ConcurrentScoped
        {
            public ConcurrentScoped(ConstructionCounter counter)
            {
                Interlocked.Increment(ref counter.Count);
            }
        }

        private sealed class AlwaysThrowingSingleton
        {
            public AlwaysThrowingSingleton(ConstructionCounter counter)
            {
                Interlocked.Increment(ref counter.Count);
                throw new ExpectedConstructionException();
            }
        }

        private sealed class ExpectedDisposeException : Exception
        {
        }

        private sealed class ExpectedConstructionException : Exception
        {
        }
    }
}
