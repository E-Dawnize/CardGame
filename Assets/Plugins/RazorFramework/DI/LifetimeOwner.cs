using System;
using System.Collections.Generic;
using System.Threading;

namespace RazorFramework.DI
{
    internal sealed class LifetimeOwner : IDisposable
    {
        private readonly object _gate = new object();
        private readonly Dictionary<int, Lazy<object>> _instances =
            new Dictionary<int, Lazy<object>>();
        private readonly List<IDisposable> _ownedInstances =
            new List<IDisposable>();
        private bool _disposed;

        public object GetOrCreate(int registrationId, Func<object> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            lock (_gate)
            {
                EnsureNotDisposed();
                var isNew = !_instances.TryGetValue(
                    registrationId,
                    out var instance);
                if (isNew)
                {
                    instance = new Lazy<object>(
                        factory,
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    _instances.Add(registrationId, instance);
                }

                var value = instance.Value;
                if (isNew)
                {
                    Track(value);
                }

                return value;
            }
        }

        public object CreateTransient(Func<object> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            lock (_gate)
            {
                EnsureNotDisposed();
                var value = factory();
                Track(value);
                return value;
            }
        }

        public void Dispose()
        {
            List<IDisposable> instances;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                instances = new List<IDisposable>(_ownedInstances);
                _ownedInstances.Clear();
            }

            var errors = new List<Exception>();
            for (var index = instances.Count - 1; index >= 0; index--)
            {
                DisposalExceptionCollector.Capture(
                    errors,
                    instances[index].Dispose);
            }

            DisposalExceptionCollector.ThrowIfAny(errors);
        }

        private void Track(object instance)
        {
            if (instance is IDisposable disposable)
            {
                _ownedInstances.Add(disposable);
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new DependencyInjectionException(
                    DependencyErrorCode.ContainerDisposed,
                    "The lifetime owner has been disposed.");
            }
        }
    }

    internal static class DisposalExceptionCollector
    {
        public static void Capture(
            ICollection<Exception> errors,
            Action dispose)
        {
            try
            {
                dispose();
            }
            catch (AggregateException error)
            {
                foreach (var inner in error.Flatten().InnerExceptions)
                {
                    errors.Add(inner);
                }
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }

        public static void ThrowIfAny(ICollection<Exception> errors)
        {
            if (errors.Count > 0)
            {
                throw new AggregateException(
                    "One or more DI-owned instances failed to dispose.",
                    errors);
            }
        }
    }
}
