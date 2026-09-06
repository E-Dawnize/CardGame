using System;
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
    }
}
