using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RazorFramework.DI
{
    internal sealed class LifetimeOwner
    {
        private readonly ConcurrentDictionary<int, Lazy<object>> _instances =
            new ConcurrentDictionary<int, Lazy<object>>();

        public object GetOrCreate(int registrationId, Func<object> factory)
        {
            var instance = _instances.GetOrAdd(
                registrationId,
                _ => new Lazy<object>(
                    factory,
                    LazyThreadSafetyMode.ExecutionAndPublication));
            return instance.Value;
        }
    }
}
