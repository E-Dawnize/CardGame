using System;
using CardGame.Domain;
using CardGame.Runtime;
using NUnit.Framework;
using RazorFramework.DI;

namespace CardGame.Tests.EditMode
{
    /// <summary>
    /// Composition root 行为：作用域层级、数据注入、显式启动与释放语义
    /// （2026-09-02 决策问题 1/3/5 的落地：构造即初始化 + 显式启动点 + 流程驱动作用域）。
    /// </summary>
    public sealed class GameCompositionTests
    {
        private static DataRepository SampleRepository()
        {
            return new GameDataLoader(new JsonUtilityGameDataSerializer())
                .Load(SampleContentTests.LoadSampleAssets());
        }

        [Test]
        public void Build_RegistersDataAndFlowAsSingletons()
        {
            using (var composition = new GameComposition(SampleRepository()))
            {
                var first = composition.Container.Resolve<DataRepository>();
                var second = composition.Container.Resolve<DataRepository>();
                Assert.That(first, Is.SameAs(second));
                Assert.That(composition.Flow, Is.SameAs(composition.Container.Resolve<GameFlow>()));
            }
        }

        [Test]
        public void ScopeHierarchy_RunThenEncounter_ResolvesAncestorServices()
        {
            using (var composition = new GameComposition(SampleRepository()))
            using (var run = composition.Container.CreateScope<RunScope>())
            {
                var rootData = composition.Container.Resolve<DataRepository>();
                Assert.That(run.Resolve<DataRepository>(), Is.SameAs(rootData));
                using (var encounter = run.CreateScope<EncounterScope>())
                {
                    Assert.That(encounter.Resolve<GameFlow>(), Is.Not.Null);
                }
            }
        }

        [Test]
        public void StartFlow_SetsWorldTitleOnlyAfterExplicitStart()
        {
            using (var composition = new GameComposition(SampleRepository()))
            {
                Assert.That(composition.Flow.WorldTitle, Is.Null); // Start 前：未开始交互（屏障语义）
                composition.StartFlow();
                Assert.That(composition.Flow.WorldTitle, Is.EqualTo("永冻之心"));
            }
        }

        [Test]
        public void Resolve_AfterDispose_ThrowsContainerDisposed()
        {
            var composition = new GameComposition(SampleRepository());
            composition.Dispose();
            var ex = Assert.Throws<DependencyInjectionException>(
                () => composition.Container.Resolve<GameFlow>());
            Assert.That(ex.Code, Is.EqualTo(DependencyErrorCode.ContainerDisposed));
        }
    }
}
