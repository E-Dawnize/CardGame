using System;
using System.Reflection;
using CardGame.Runtime;
using NUnit.Framework;
using RazorFramework.DI;
using RazorFramework.Unity.DI;
using UnityEngine;

namespace CardGame.Tests.EditMode.Bootstrap
{
    /// <summary>场景 [Inject] 注入驱动测试（规格：docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md）。</summary>
    public sealed class SceneInjectionTests
    {
        private interface ISceneService { }

        private sealed class SceneService : ISceneService { }

        private interface INeverRegistered { }

        private sealed class InjectableComponent : MonoBehaviour
        {
            [Inject] public ISceneService Service;
            [InjectOptional] public INeverRegistered Optional;
        }

        private sealed class RunState { }

        private sealed class MissingRequiredComponent : MonoBehaviour
        {
            [Inject] public ISceneService Service;
        }

        private sealed class ScopedWantingComponent : MonoBehaviour
        {
            [Inject] public RunState State;
        }

        private sealed class ScopeSeamComponent : MonoBehaviour
        {
            [Inject] public ISceneService Service;
            [Inject] public RunState State;
        }

        [SetUp]
        public void SetUp()
        {
            UnityMainThread.InitializeForTests();
        }

        [Test]
        public void Injector_FromGameTestAssembly_InjectsComponent()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<ISceneService, SceneService>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-smoke");
            var component = gameObject.AddComponent<InjectableComponent>();
            try
            {
                new UnityObjectInjector(container).Inject(component);

                Assert.That(component.Service, Is.SameAs(container.Resolve<ISceneService>()));
                Assert.That(component.Optional, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_InjectsActiveComponentFromSceneScan()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<ISceneService, SceneService>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-active");
            gameObject.AddComponent<InjectableComponent>();
            try
            {
                SceneInjection.InjectScene(container);

                var component = gameObject.GetComponent<InjectableComponent>();
                Assert.That(component.Service, Is.SameAs(container.Resolve<ISceneService>()));
                Assert.That(component.Optional, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_InjectsInactiveComponent()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<ISceneService, SceneService>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-inactive");
            gameObject.SetActive(false);
            gameObject.AddComponent<InjectableComponent>();
            try
            {
                SceneInjection.InjectScene(container);

                var component = gameObject.GetComponent<InjectableComponent>();
                Assert.That(component.Service, Is.SameAs(container.Resolve<ISceneService>()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_ThrowsMissingDependencyWhenRequiredServiceNotRegistered()
        {
            var builder = new ContainerBuilder();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-missing");
            gameObject.AddComponent<MissingRequiredComponent>();
            try
            {
                var exception = Assert.Throws<UnityInjectionException>(
                    () => SceneInjection.InjectScene(container));

                Assert.That(exception.Code, Is.EqualTo(UnityInjectionErrorCode.MissingDependency));
                Assert.That(exception.TargetType, Is.EqualTo(typeof(MissingRequiredComponent)));
                Assert.That(exception.MemberName, Is.EqualTo("Service"));
                Assert.That(exception.ServiceType, Is.EqualTo(typeof(ISceneService)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_ThrowsScopeMismatchWhenSceneComponentWantsScopedService()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunScope>();
            builder.AddScoped<RunState, RunScope>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-scoped");
            gameObject.AddComponent<ScopedWantingComponent>();
            try
            {
                var exception = Assert.Throws<DependencyInjectionException>(
                    () => SceneInjection.InjectScene(container));

                Assert.That(exception.Code, Is.EqualTo(DependencyErrorCode.ScopeMismatch));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_LeavesOptionalMemberUnsetWhenNotRegistered()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<ISceneService, SceneService>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-optional");
            gameObject.AddComponent<InjectableComponent>();
            try
            {
                SceneInjection.InjectScene(container);

                var component = gameObject.GetComponent<InjectableComponent>();
                Assert.That(component.Optional, Is.Null);
                Assert.That(component.Service, Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GameBootstrap_RunsBeforeAllOtherSceneScripts()
        {
            // Unity 6 将特性类型命名为 UnityEngine.DefaultExecutionOrder（无 Attribute 后缀），
            // C# 特性用法 [DefaultExecutionOrder(N)] 不变。
            var attribute = typeof(GameBootstrap)
                .GetCustomAttribute<DefaultExecutionOrder>();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.order, Is.LessThanOrEqualTo(-30000));
        }

        [Test]
        public void Injector_FromRunScope_ResolvesScopedAndSingletonMembers()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunScope>();
            builder.AddSingleton<ISceneService, SceneService>();
            builder.AddScoped<RunState, RunScope>();
            using var container = builder.Build();
            using var runScope = container.CreateScope<RunScope>();
            var gameObject = new GameObject("scope-seam");
            var component = gameObject.AddComponent<ScopeSeamComponent>();
            try
            {
                new UnityObjectInjector(runScope).Inject(component);

                Assert.That(component.Service, Is.SameAs(container.Resolve<ISceneService>()));
                Assert.That(component.State, Is.SameAs(runScope.Resolve<RunState>()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
