using NUnit.Framework;

namespace RazorFramework.DI.Tests
{
    public sealed class HierarchicalScopeTests
    {
        [Test]
        public void EncounterScopes_ShareRunServiceButNotEncounterService()
        {
            var builder = CreateScopedBuilder();
            using var container = builder.Build();
            using var run = container.CreateScope<RunTag>();
            using var first = run.CreateScope<EncounterTag>();
            using var second = run.CreateScope<EncounterTag>();

            Assert.That(first.Resolve<RunState>(), Is.SameAs(second.Resolve<RunState>()));
            Assert.That(
                first.Resolve<EncounterState>(),
                Is.Not.SameAs(second.Resolve<EncounterState>()));
        }

        [Test]
        public void SeparateRunScopes_DoNotShareRunService()
        {
            var builder = CreateScopedBuilder();
            using var container = builder.Build();
            using var first = container.CreateScope<RunTag>();
            using var second = container.CreateScope<RunTag>();

            Assert.That(first.Resolve<RunState>(), Is.Not.SameAs(second.Resolve<RunState>()));
        }

        [Test]
        public void ChildScope_CanResolveParentScopedDependency()
        {
            var builder = CreateScopedBuilder();
            builder.AddTransient<NeedsRunState>();
            using var container = builder.Build();
            using var run = container.CreateScope<RunTag>();
            using var encounter = run.CreateScope<EncounterTag>();

            Assert.That(
                encounter.Resolve<NeedsRunState>().RunState,
                Is.SameAs(run.Resolve<RunState>()));
        }

        [Test]
        public void CreateScope_RejectsWrongDirectParent()
        {
            var builder = CreateScopedBuilder();
            using var container = builder.Build();

            var error = Assert.Throws<DependencyInjectionException>(
                () => container.CreateScope<EncounterTag>());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.ScopeMismatch));
        }

        [Test]
        public void Resolve_RejectsScopedServiceOutsideRequiredScope()
        {
            var builder = CreateScopedBuilder();
            using var container = builder.Build();

            var error = Assert.Throws<DependencyInjectionException>(
                () => container.Resolve<RunState>());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.ScopeMismatch));
            Assert.That(error.ServiceType, Is.EqualTo(typeof(RunState)));
        }

        [Test]
        public void ParentScope_CannotResolveChildScopedService()
        {
            var builder = CreateScopedBuilder();
            using var container = builder.Build();
            using var run = container.CreateScope<RunTag>();

            var error = Assert.Throws<DependencyInjectionException>(
                () => run.Resolve<EncounterState>());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.ScopeMismatch));
        }

        [Test]
        public void Build_RejectsSingletonCapturingRunScopeThroughTransient()
        {
            var builder = CreateScopedBuilder();
            builder.AddTransient<TransientNeedsRun>();
            builder.AddSingleton<SingletonNeedsTransient>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.CaptiveDependency));
            Assert.That(error.ServiceType, Is.EqualTo(typeof(SingletonNeedsTransient)));
        }

        [Test]
        public void Build_RejectsParentScopedServiceDependingOnChildScope()
        {
            var builder = CreateScopedBuilder();
            builder.AddScoped<RunNeedsEncounter, RunTag>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.CaptiveDependency));
        }

        [Test]
        public void Build_RejectsServiceRequiringSiblingScopes()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<BattleTag>();
            builder.DefineScope<ShopTag>();
            builder.AddScoped<BattleState, BattleTag>();
            builder.AddScoped<ShopState, ShopTag>();
            builder.AddTransient<NeedsBattleAndShop>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.ScopeMismatch));
        }

        [Test]
        public void TransientWithRunDependency_ResolvesOnlyInsideRunDescendants()
        {
            var builder = CreateScopedBuilder();
            builder.AddTransient<TransientNeedsRun>();
            using var container = builder.Build();
            using var run = container.CreateScope<RunTag>();
            using var encounter = run.CreateScope<EncounterTag>();

            Assert.That(encounter.Resolve<TransientNeedsRun>().RunState, Is.Not.Null);
            Assert.That(
                Assert.Throws<DependencyInjectionException>(
                    () => container.Resolve<TransientNeedsRun>()).Code,
                Is.EqualTo(DependencyErrorCode.ScopeMismatch));
        }

        [Test]
        public void Build_RejectsDuplicateScopeDefinition()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.DefineScope<RunTag>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.InvalidScopeDefinition));
        }

        [Test]
        public void Build_RejectsScopeDefinitionCycle()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag, EncounterTag>();
            builder.DefineScope<EncounterTag, RunTag>();

            var error = Assert.Throws<DependencyInjectionException>(() => builder.Build());

            Assert.That(error.Code, Is.EqualTo(DependencyErrorCode.InvalidScopeDefinition));
        }

        private static ContainerBuilder CreateScopedBuilder()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunTag>();
            builder.DefineScope<EncounterTag, RunTag>();
            builder.AddScoped<RunState, RunTag>();
            builder.AddScoped<EncounterState, EncounterTag>();
            return builder;
        }

        private sealed class RunTag
        {
        }

        private sealed class EncounterTag
        {
        }

        private sealed class BattleTag
        {
        }

        private sealed class ShopTag
        {
        }

        private sealed class RunState
        {
        }

        private sealed class EncounterState
        {
        }

        private sealed class BattleState
        {
        }

        private sealed class ShopState
        {
        }

        private sealed class NeedsRunState
        {
            public NeedsRunState(RunState runState)
            {
                RunState = runState;
            }

            public RunState RunState { get; }
        }

        private sealed class TransientNeedsRun
        {
            public TransientNeedsRun(RunState runState)
            {
                RunState = runState;
            }

            public RunState RunState { get; }
        }

        private sealed class SingletonNeedsTransient
        {
            public SingletonNeedsTransient(TransientNeedsRun dependency)
            {
            }
        }

        private sealed class RunNeedsEncounter
        {
            public RunNeedsEncounter(EncounterState dependency)
            {
            }
        }

        private sealed class NeedsBattleAndShop
        {
            public NeedsBattleAndShop(BattleState battle, ShopState shop)
            {
            }
        }
    }
}
