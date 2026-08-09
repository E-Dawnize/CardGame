using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using RazorFramework.DI;
using RazorFramework.Unity.DI;
using UnityEngine;

namespace RazorFramework.Unity.DI.Tests
{
    public sealed class UnityObjectInjectorTests
    {
        [SetUp]
        public void SetUp()
        {
            UnityMainThread.InitializeForTests();
        }

        [Test]
        public void Inject_AssignsRequiredBaseFieldAndOptionalProperty()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<IService, Service>();
            using var container = builder.Build();
            var gameObject = new GameObject("injection-test");
            var target = gameObject.AddComponent<DerivedTarget>();

            try
            {
                new UnityObjectInjector(container).Inject(target);

                Assert.That(
                    target.BaseService,
                    Is.SameAs(container.Resolve<IService>()));
                Assert.That(target.Optional, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Inject_NullAndDestroyedObjects_AreNoOpOnOwnerThread()
        {
            using var container = new ContainerBuilder().Build();
            var injector = new UnityObjectInjector(container);
            var gameObject = new GameObject("destroyed-target");
            var target = gameObject.AddComponent<RequiredTarget>();
            UnityEngine.Object.DestroyImmediate(gameObject);

            Assert.DoesNotThrow(() => injector.Inject(null));
            Assert.DoesNotThrow(() => injector.Inject(target));
        }

        [Test]
        public void Inject_FromAnotherThread_IsRejectedBeforeUnityAccess()
        {
            using var container = new ContainerBuilder().Build();
            var injector = new UnityObjectInjector(container);

            var captured = Task.Run(() =>
            {
                try
                {
                    injector.Inject(null);
                    return null;
                }
                catch (UnityInjectionException error)
                {
                    return error;
                }
            }).GetAwaiter().GetResult();

            Assert.That(captured, Is.Not.Null);
            Assert.That(
                captured.Code,
                Is.EqualTo(UnityInjectionErrorCode.WrongThread));
        }

        [Test]
        public void ConstructingOnWorkerThread_IsRejectedEvenWhenUsedOnThatThread()
        {
            using var container = new ContainerBuilder().Build();

            var captured = Task.Run(() =>
            {
                try
                {
                    var injector = new UnityObjectInjector(container);
                    injector.Inject(null);
                    return null;
                }
                catch (UnityInjectionException error)
                {
                    return error;
                }
            }).GetAwaiter().GetResult();

            Assert.That(captured, Is.Not.Null);
            Assert.That(
                captured.Code,
                Is.EqualTo(UnityInjectionErrorCode.WrongThread));
        }

        [Test]
        public void MissingRequiredDependency_ReportsStructuredMemberContext()
        {
            using var container = new ContainerBuilder().Build();
            var gameObject = new GameObject("missing-dependency");
            var target = gameObject.AddComponent<MissingRequiredTarget>();

            try
            {
                var error = Assert.Throws<UnityInjectionException>(
                    () => new UnityObjectInjector(container).Inject(target));

                Assert.That(
                    error.Code,
                    Is.EqualTo(UnityInjectionErrorCode.MissingDependency));
                Assert.That(
                    error.TargetType,
                    Is.EqualTo(typeof(MissingRequiredTarget)));
                Assert.That(error.MemberName, Is.EqualTo("Missing"));
                Assert.That(
                    error.ServiceType,
                    Is.EqualTo(typeof(IMissingService)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Inject_UsesBaseToDerivedMetadataOrder()
        {
            var resolver = new RecordingResolver();
            var gameObject = new GameObject("ordered-injection");
            var target = gameObject.AddComponent<OrderedDerivedTarget>();

            try
            {
                new UnityObjectInjector(resolver).Inject(target);

                Assert.That(
                    resolver.Requests,
                    Is.EqualTo(new[]
                    {
                        typeof(IFirstService),
                        typeof(ISecondService),
                        typeof(IThirdService)
                    }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [TestCase(typeof(StaticFieldTarget), "Service")]
        [TestCase(typeof(ReadonlyFieldTarget), "Service")]
        [TestCase(typeof(StaticPropertyTarget), "Service")]
        [TestCase(typeof(GetOnlyPropertyTarget), "Service")]
        [TestCase(typeof(IndexerTarget), "Item")]
        [TestCase(typeof(ConflictingAttributeTarget), "Service")]
        public void InvalidMemberShape_IsRejected(Type targetType, string memberName)
        {
            var gameObject = new GameObject("invalid-member");
            var target = (UnityEngine.Object)gameObject.AddComponent(targetType);
            using var container = new ContainerBuilder().Build();

            try
            {
                var error = Assert.Throws<UnityInjectionException>(
                    () => new UnityObjectInjector(container).Inject(target));

                Assert.That(
                    error.Code,
                    Is.EqualTo(UnityInjectionErrorCode.InvalidMember));
                Assert.That(error.TargetType, Is.EqualTo(targetType));
                Assert.That(error.MemberName, Is.EqualTo(memberName));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AssignmentTypeMismatch_IsReportedWithInnerException()
        {
            var gameObject = new GameObject("assignment-failure");
            var target = gameObject.AddComponent<RequiredTarget>();

            try
            {
                var error = Assert.Throws<UnityInjectionException>(
                    () => new UnityObjectInjector(new WrongTypeResolver())
                        .Inject(target));

                Assert.That(
                    error.Code,
                    Is.EqualTo(UnityInjectionErrorCode.AssignmentFailed));
                Assert.That(error.TargetType, Is.EqualTo(typeof(RequiredTarget)));
                Assert.That(error.MemberName, Is.EqualTo("Service"));
                Assert.That(error.ServiceType, Is.EqualTo(typeof(IService)));
                Assert.That(error.InnerException, Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NullResolver_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(
                () => new UnityObjectInjector(null));
        }
    }

    public interface IService
    {
    }

    public sealed class Service : IService
    {
    }

    public interface IMissingService
    {
    }

    public interface IOptionalService
    {
    }

    public abstract class BaseTarget : MonoBehaviour
    {
        [Inject]
        private IService _baseService;

        public IService BaseService => _baseService;
    }

    public sealed class DerivedTarget : BaseTarget
    {
        [InjectOptional]
        public IOptionalService Optional { get; private set; }
    }

    public sealed class RequiredTarget : MonoBehaviour
    {
        [Inject]
        public IService Service;
    }

    public sealed class MissingRequiredTarget : MonoBehaviour
    {
        [Inject]
        public IMissingService Missing;
    }

    public interface IFirstService
    {
    }

    public interface ISecondService
    {
    }

    public interface IThirdService
    {
    }

    public sealed class FirstService : IFirstService
    {
    }

    public sealed class SecondService : ISecondService
    {
    }

    public sealed class ThirdService : IThirdService
    {
    }

    public abstract class OrderedBaseTarget : MonoBehaviour
    {
        [Inject]
        public IFirstService First;

        [Inject]
        public ISecondService Second;
    }

    public sealed class OrderedDerivedTarget : OrderedBaseTarget
    {
        [Inject]
        public IThirdService Third;
    }

    public sealed class StaticFieldTarget : MonoBehaviour
    {
        [Inject]
        public static IService Service;
    }

    public sealed class ReadonlyFieldTarget : MonoBehaviour
    {
        [Inject]
        public readonly IService Service;
    }

    public sealed class StaticPropertyTarget : MonoBehaviour
    {
        [Inject]
        public static IService Service { get; set; }
    }

    public sealed class GetOnlyPropertyTarget : MonoBehaviour
    {
        [Inject]
        public IService Service => null;
    }

    public sealed class IndexerTarget : MonoBehaviour
    {
        [Inject]
        public IService this[int index]
        {
            get => null;
            set { }
        }
    }

    public sealed class ConflictingAttributeTarget : MonoBehaviour
    {
        [Inject]
        [InjectOptional]
        public IService Service;
    }

    internal sealed class RecordingResolver : IServiceResolver
    {
        private readonly Dictionary<Type, object> _services =
            new Dictionary<Type, object>
            {
                { typeof(IFirstService), new FirstService() },
                { typeof(ISecondService), new SecondService() },
                { typeof(IThirdService), new ThirdService() }
            };

        public List<Type> Requests { get; } = new List<Type>();

        public object Resolve(Type serviceType)
        {
            if (!TryResolve(serviceType, out var service))
            {
                throw new InvalidOperationException();
            }

            return service;
        }

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            Requests.Add(serviceType);
            return _services.TryGetValue(serviceType, out service);
        }

        public IReadOnlyList<T> ResolveAll<T>() where T : class
        {
            return Array.Empty<T>();
        }
    }

    internal sealed class WrongTypeResolver : IServiceResolver
    {
        public object Resolve(Type serviceType)
        {
            return "wrong-type";
        }

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            service = "wrong-type";
            return true;
        }

        public IReadOnlyList<T> ResolveAll<T>() where T : class
        {
            return Array.Empty<T>();
        }
    }
}
