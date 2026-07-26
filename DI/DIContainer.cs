using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace RazorFramework.DI
{
    public enum ServiceLifetime
    {
        Transient,
        Scoped,
        Singleton
    }

    public interface IScope : IDisposable
    {
        IServiceProvider ServiceProvider { get; }
    }

    /// <summary>
    /// DI 解析出的实例创建回调。用于 Boot 层将实例注册到生命周期系统等外部基础设施。
    /// </summary>
    public delegate void InstanceCreatedCallback(object instance);

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InjectOptionalAttribute : Attribute { }

    public class ServiceDescriptor
    {
        public Type ServiceType;
        public int Id;
        public ServiceLifetime Lifetime;
        public int Order;
        public Type ImplementationType;
        public Func<IServiceProvider, object> ImplementationFactory;

        private ServiceDescriptor(int id, Type serviceType, ServiceLifetime lifetime, int order = 0)
        {
            Id = id;
            ServiceType = serviceType;
            Order = order;
            Lifetime = lifetime;
        }

        public ServiceDescriptor(int id, Type serviceType, Type implementationType, ServiceLifetime lifetime, int order = 0)
            : this(id, serviceType, lifetime, order)
        {
            ImplementationType = implementationType;
        }

        public ServiceDescriptor(int id, Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime, int order = 0)
            : this(id, serviceType, lifetime, order)
        {
            ImplementationFactory = implementationFactory;
        }
    }

    public partial class DIContainer : IServiceProvider, IDisposable
    {
        private readonly ConcurrentDictionary<Type, List<ServiceDescriptor>> _serviceDescriptors = new();
        private readonly ConcurrentDictionary<Type, object> _singletonInstances = new();
        private readonly ConcurrentDictionary<int, object> _singletonFactoryResults = new();
        private readonly ConcurrentBag<IDisposable> _disposables = new();
        private readonly ThreadLocal<Stack<Type>> _resolveStack = new(() => new Stack<Type>());
        private bool _disposed;
        private static int _globalServiceId;
        private DIContainer _parentContainer;

        // === 扩展点 ===
        /// <summary>实例创建后回调 — 用于注册到生命周期系统等外部基础设施（可选）</summary>
        public InstanceCreatedCallback OnInstanceCreated;

        /// <summary>可选的日志接口。设置后替换所有内部日志输出。</summary>
        public Action<string> LogInfo;
        public Action<string> LogWarning;
        public Action<string> LogError;

        /// <summary>开启后打印每个实例创建的完整链路</summary>
        public bool VerboseDebug;

        private int NextId() => Interlocked.Increment(ref _globalServiceId);

        public DIContainer CreateChildContainer()
        {
            return new DIContainer { _parentContainer = this };
        }

        public object GetRequiredService<T>(IScope scope = null) => GetRequiredService(typeof(T), scope);

        private object GetRequiredService(Type t, IScope scope = null)
        {
            var obj = ResolveService(t, scope as Scope);
            if (obj == null) throw new InvalidOperationException($"Service not registered: {t}");
            return obj;
        }

        #region 注册方法

        private void Register(ServiceDescriptor descriptor)
        {
            var list = _serviceDescriptors.GetOrAdd(descriptor.ServiceType, _ => new List<ServiceDescriptor>());
            list.Add(descriptor);
            LogInfo?.Invoke($"[DI] {descriptor.ServiceType.FullName} registered");
        }

        public void RegisterTransient<TService, TImplementation>() where TImplementation : TService
        {
            ValidateConcreteType(typeof(TImplementation));
            Register(new ServiceDescriptor(NextId(), typeof(TService), typeof(TImplementation), ServiceLifetime.Transient));
        }

        public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService
        {
            ValidateConcreteType(typeof(TImplementation));
            Register(new ServiceDescriptor(NextId(), typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton));
        }

        public void RegisterScoped<TService, TImplementation>() where TImplementation : TService
        {
            ValidateConcreteType(typeof(TImplementation));
            Register(new ServiceDescriptor(NextId(), typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped));
        }

        private static void ValidateConcreteType(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
                throw new InvalidOperationException(
                    $"DI requires concrete implementation type, but {type.Name} is {(type.IsInterface ? "an interface" : "abstract")}. " +
                    "Use RegisterSingleton<T>(instance) or RegisterSingleton<T>(factory) instead.");
        }

        /// <summary>注册预构建实例为单例</summary>
        public void RegisterSingleton<TService>(TService implementationInstance) where TService : class
        {
            var concreteType = implementationInstance.GetType();
            var isNewConcreteInstance = !_singletonInstances.ContainsKey(concreteType);
            _singletonInstances[concreteType] = implementationInstance;
            if (isNewConcreteInstance && implementationInstance is IDisposable d) _disposables.Add(d);
            OnInstanceCreated?.Invoke(implementationInstance);
            Register(new ServiceDescriptor(NextId(), typeof(TService), concreteType, ServiceLifetime.Singleton));
        }

        public void RegisterTransient<TService>(Func<IServiceProvider, object> implementationFactory) where TService : class
            => Register(new ServiceDescriptor(NextId(), typeof(TService), sp => implementationFactory(sp)!, ServiceLifetime.Transient));

        public void RegisterSingleton<TService>(Func<IServiceProvider, object> implementationFactory) where TService : class
            => Register(new ServiceDescriptor(NextId(), typeof(TService), sp => implementationFactory(sp)!, ServiceLifetime.Singleton));

        public void RegisterScoped<TService>(Func<IServiceProvider, object> implementationFactory) where TService : class
            => Register(new ServiceDescriptor(NextId(), typeof(TService), sp => implementationFactory(sp)!, ServiceLifetime.Scoped));

        #endregion

        #region 服务解析

        public T? GetService<T>(IScope scope = null) => (T?)ResolveService(typeof(T), scope as Scope);
        public object GetService(Type serviceType) => ResolveService(serviceType, null);

        public IEnumerable<T> ResolveAll<T>(IScope scope = null)
        {
            return ResolveAll(typeof(T), scope as Scope).Cast<T>();
        }

        private IEnumerable<object> ResolveAll(Type type, Scope scope)
        {
            var results = new List<object>();
            if (_serviceDescriptors.TryGetValue(type, out var descriptors))
            {
                foreach (var descriptor in descriptors)
                {
                    var obj = ResolveService(descriptor.ServiceType, scope);
                    if (obj != null) results.Add(obj);
                }
            }
            if (_parentContainer != null)
                results.AddRange(_parentContainer.ResolveAll(type, scope));
            return results;
        }

        private object ResolveService(Type serviceType, Scope scope)
        {
            if (_resolveStack.Value!.Contains(serviceType))
                throw new InvalidOperationException($"Circular dependency: {string.Join(" -> ", _resolveStack.Value.Reverse())} -> {serviceType}");

            _resolveStack.Value.Push(serviceType);
            try { return ResolveCore(serviceType, scope); }
            finally { _resolveStack.Value.Pop(); }
        }

        private object ResolveCore(Type serviceType, Scope scope)
        {
            if (_serviceDescriptors.TryGetValue(serviceType, out var descriptors))
                return ResolveDescriptor(descriptors, scope);

            if (_parentContainer != null)
                return _parentContainer.ResolveService(serviceType, scope);

            return null;
        }

        private object ResolveDescriptor(List<ServiceDescriptor> descriptors, Scope scope)
        {
            var descriptor = SelectDescriptor(descriptors);
            return descriptor.Lifetime switch
            {
                ServiceLifetime.Singleton => ResolveSingleton(descriptor),
                ServiceLifetime.Scoped => ResolveScoped(descriptor, scope),
                ServiceLifetime.Transient => ResolveTransient(descriptor, scope),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private ServiceDescriptor SelectDescriptor(List<ServiceDescriptor> descriptors) => descriptors.First();

        private object ResolveSingleton(ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationType != null)
            {
                if (_singletonInstances.TryGetValue(descriptor.ImplementationType, out var instance))
                    return instance;

                lock (_singletonInstances)
                {
                    if (_singletonInstances.TryGetValue(descriptor.ImplementationType, out instance))
                        return instance;
                    instance = CreateInstance(descriptor, null);
                    _singletonInstances[descriptor.ImplementationType] = instance;
                    if (instance is IDisposable disposable) _disposables.Add(disposable);
                    return instance;
                }
            }

            if (_singletonFactoryResults.TryGetValue(descriptor.Id, out var factoryInstance))
                return factoryInstance;

            lock (_singletonFactoryResults)
            {
                if (_singletonFactoryResults.TryGetValue(descriptor.Id, out factoryInstance))
                    return factoryInstance;
                factoryInstance = CreateInstance(descriptor, null);
                _singletonFactoryResults[descriptor.Id] = factoryInstance;
                if (factoryInstance is IDisposable disposable) _disposables.Add(disposable);
                return factoryInstance;
            }
        }

        private object ResolveScoped(ServiceDescriptor descriptor, Scope scope)
        {
            if (scope == null) throw new InvalidOperationException("Scoped service requires a scope.");

            if (descriptor.ImplementationType != null)
            {
                Scope currentScope = scope;
                while (currentScope != null)
                {
                    if (currentScope.ScopedInstances.TryGetValue(descriptor.ImplementationType, out var instance))
                        return instance;
                    currentScope = currentScope._parent;
                }
                var newInstance = CreateInstance(descriptor, scope);
                scope.ScopedInstances[descriptor.ImplementationType] = newInstance;
                if (newInstance is IDisposable disposable) scope.Disposables.Add(disposable);
                return newInstance;
            }

            var factoryInstance = CreateInstance(descriptor, scope);
            if (factoryInstance is IDisposable d) scope.Disposables.Add(d);
            return factoryInstance;
        }

        private object ResolveTransient(ServiceDescriptor descriptor, Scope scope)
        {
            var instance = CreateInstance(descriptor, scope);
            if (instance is IDisposable disposable)
            {
                if (scope != null) scope.Disposables.Add(disposable);
                else _disposables.Add(disposable);
            }
            return instance;
        }

        #endregion

        #region 实例创建 + 注入

        private readonly ConcurrentDictionary<Type, ConstructorInfo> _constructorsCache = new();
        private readonly ConcurrentDictionary<Type, string> _ctorSignatureCache = new();

        private object CreateInstance(ServiceDescriptor descriptor, Scope scope)
        {
            if (descriptor.ImplementationFactory != null)
            {
                var factoryInstance = descriptor.ImplementationFactory(scope != null ? scope.ServiceProvider : this);
                if (VerboseDebug) LogInfo?.Invoke($"[DI] Factory → {descriptor.ServiceType.Name} ({factoryInstance?.GetType().Name ?? "null"})");
                return factoryInstance;
            }

            var implementationType = descriptor.ImplementationType;
            var ctor = _constructorsCache.GetOrAdd(implementationType, type =>
            {
                var constructors = type.GetConstructors();
                if (constructors.Length == 0)
                    throw new InvalidOperationException(
                        $"DI cannot instantiate '{type.FullName}': no public constructors found. " +
                        "This may indicate the type was stripped by IL2CPP. Add it to link.xml.");
                return constructors.FirstOrDefault(c => c.GetCustomAttributes(typeof(InjectAttribute), false).Length != 0)
                       ?? constructors.OrderByDescending(c => c.GetParameters().Length).First();
            });

            var ctorSig = _ctorSignatureCache.GetOrAdd(implementationType,
                _ => $"{implementationType.Name}({string.Join(", ", ctor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");

            if (VerboseDebug) LogInfo?.Invoke($"[DI] Creating {ctorSig}  (lifetime: {descriptor.Lifetime})");

            var parameters = ctor.GetParameters();
            var paramValues = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                try
                {
                    var val = TryResolveEnumerable(paramType, scope, out var enumerableValue)
                        ? enumerableValue
                        : ResolveService(paramType, scope);
                    val ??= parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                    if (val == null)
                        throw new InvalidOperationException(
                            $"Missing dependency: {paramType.Name} {parameters[i].Name} while creating {implementationType.Name}");
                    paramValues[i] = val;
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    throw new InvalidOperationException(
                        $"Failed to resolve parameter '{parameters[i].Name}' ({paramType.Name}) " +
                        $"while creating {implementationType.Name}: {ex.Message}", ex);
                }
            }

            object instance;
            try
            {
                instance = ctor.Invoke(paramValues);
                if (VerboseDebug) LogInfo?.Invoke($"[DI] ✓ Created {implementationType.Name}");
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                throw new InvalidOperationException(
                    $"Constructor threw in {implementationType.Name}: {inner.Message}\n  Constructor: {ctorSig}", inner);
            }

            try
            {
                Inject(instance, scope);
                if (VerboseDebug) LogInfo?.Invoke($"[DI] ✓ Injected {implementationType.Name}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Field/property injection failed for {implementationType.Name}: {ex.Message}", ex);
            }

            // 通过回调通知外部系统（而非直接依赖 LifecycleRegistry）
            OnInstanceCreated?.Invoke(instance);

            return instance;
        }

        private bool TryResolveEnumerable(Type paramType, Scope scope, out object value)
        {
            value = null;

            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elemType = paramType.GetGenericArguments()[0];
                value = BuildList(elemType, ResolveAll(elemType, scope));
                return true;
            }

            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elemType = paramType.GetGenericArguments()[0];
                value = BuildList(elemType, ResolveAll(elemType, scope));
                return true;
            }

            if (paramType.IsArray)
            {
                var elemType = paramType.GetElementType();
                value = BuildArray(elemType, ResolveAll(elemType, scope));
                return true;
            }

            return false;
        }

        private object BuildList(Type elemType, IEnumerable<object> items)
        {
            var listType = typeof(List<>).MakeGenericType(elemType);
            var list = (System.Collections.IList)Activator.CreateInstance(listType);
            foreach (var it in items) list.Add(it);
            return list;
        }

        private object BuildArray(Type elemType, IEnumerable<object> items)
        {
            var array = items.ToArray();
            var result = Array.CreateInstance(elemType, array.Length);
            for (int i = 0; i < array.Length; i++) result.SetValue(array[i], i);
            return result;
        }

        #endregion

        #region 属性注入

        private static readonly BindingFlags InjectFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly ConcurrentDictionary<Type, InjectMember[]> _injectMembersCache = new();

        private readonly struct InjectMember
        {
            public readonly Type MemberType;
            public readonly string Name;
            public readonly bool Optional;
            public readonly Action<object, object> Setter;

            public InjectMember(Type memberType, string name, bool optional, Action<object, object> setter)
            {
                MemberType = memberType;
                Name = name;
                Optional = optional;
                Setter = setter;
            }
        }

        public void Inject(object target, IScope scope = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target is UnityEngine.Object uo && uo == null) return;

            var provider = scope?.ServiceProvider ?? (IServiceProvider)this;
            var targetType = target.GetType();
            var members = _injectMembersCache.GetOrAdd(targetType, BuildInjectMembers);

            foreach (var m in members)
            {
                var dep = provider.GetService(m.MemberType);
                if (dep == null)
                {
                    if (m.Optional) continue;
                    throw new InvalidOperationException(
                        $"Missing dependency: {m.MemberType.Name} {m.Name} for {targetType.Name}");
                }
                m.Setter(target, dep);
            }
        }

        private static InjectMember[] BuildInjectMembers(Type type)
        {
            var list = new List<InjectMember>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var f in t.GetFields(InjectFlags))
                {
                    if (!f.IsDefined(typeof(InjectAttribute), true) && !f.IsDefined(typeof(InjectOptionalAttribute), true)) continue;
                    if (f.IsInitOnly) continue;
                    var optional = f.IsDefined(typeof(InjectOptionalAttribute), true);
                    list.Add(new InjectMember(f.FieldType, f.Name, optional, (obj, val) => f.SetValue(obj, val)));
                }

                foreach (var p in t.GetProperties(InjectFlags))
                {
                    if (!p.IsDefined(typeof(InjectAttribute), true) && !p.IsDefined(typeof(InjectOptionalAttribute), true)) continue;
                    if (!p.CanWrite) continue;
                    if (p.GetIndexParameters().Length != 0) continue;
                    var set = p.GetSetMethod(true);
                    if (set == null) continue;
                    var optional = p.IsDefined(typeof(InjectOptionalAttribute), true);
                    list.Add(new InjectMember(p.PropertyType, p.Name, optional, (obj, val) => p.SetValue(obj, val)));
                }
            }
            return list.ToArray();
        }

        #endregion

        #region 依赖图验证

        /// <summary>验证所有 Singleton 服务依赖图完整性，Boot 阶段调用</summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            foreach (var kv in _serviceDescriptors)
            {
                var serviceType = kv.Key;
                if (serviceType.IsGenericTypeDefinition) continue;

                foreach (var desc in kv.Value)
                {
                    if (desc.ImplementationFactory != null) continue;
                    if (desc.ImplementationType == null) continue;
                    if (desc.Lifetime != ServiceLifetime.Singleton) continue;

                    try
                    {
                        ResolveService(serviceType, null);
                        result.CheckedCount++;
                        LogInfo?.Invoke($"[DI Validate] OK: {serviceType.Name}");
                    }
                    catch (Exception ex)
                    {
                        var msg = $"{serviceType.Name} → {desc.ImplementationType.Name}: {Unwrap(ex).Message}";
                        result.Errors.Add(msg);
                        LogError?.Invoke($"[DI Validate] FAIL: {msg}");
                    }
                }
            }

            if (result.IsValid)
                LogInfo?.Invoke($"[DI Validate] All {result.CheckedCount} services passed");
            else
                LogError?.Invoke($"[DI Validate] {result.Errors.Count}/{result.CheckedCount + result.Errors.Count} failed");

            return result;
        }

        private static Exception Unwrap(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        public class ValidationResult
        {
            public readonly List<string> Errors = new();
            public int CheckedCount;
            public bool IsValid => Errors.Count == 0;
        }

        #endregion

        #region Scope 实现

        public IScope CreateScope(IScope parentScope = null) => new Scope(this, parentScope as Scope);

        internal class Scope : IScope
        {
            private readonly DIContainer _container;
            public readonly Scope _parent;
            public ConcurrentDictionary<Type, object> ScopedInstances { get; } = new();
            public ConcurrentBag<IDisposable> Disposables { get; } = new();
            public IServiceProvider ServiceProvider { get; }
            private bool _disposed;

            public Scope(DIContainer container, Scope parent = null)
            {
                _container = container;
                _parent = parent;
                ServiceProvider = new ScopedServiceProvider(this);
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                foreach (var disposable in Disposables)
                    disposable.Dispose();
                Disposables.Clear();
                ScopedInstances.Clear();
            }

            private class ScopedServiceProvider : IServiceProvider
            {
                private readonly Scope _scope;
                public ScopedServiceProvider(Scope scope) => _scope = scope;
                public object GetService(Type serviceType) => _scope._container.ResolveService(serviceType, _scope);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var obj in _disposables)
                obj.Dispose();
            _disposables.Clear();
            _singletonInstances.Clear();
            _singletonFactoryResults.Clear();
            _constructorsCache.Clear();
            _serviceDescriptors.Clear();
        }

        #endregion
    }

    public static class ServiceProviderExtensions
    {
        public static T? GetService<T>(this IServiceProvider sp) => (T?)sp.GetService(typeof(T));
    }
}
