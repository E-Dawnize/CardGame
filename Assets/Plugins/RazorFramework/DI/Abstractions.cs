using System;
using System.Collections.Generic;

namespace RazorFramework.DI
{
    public enum ServiceLifetime
    {
        Singleton,
        Scoped,
        Transient
    }

    public interface IServiceResolver
    {
        object Resolve(Type serviceType);
        T Resolve<T>() where T : class;
        bool TryResolve(Type serviceType, out object service);
        IReadOnlyList<T> ResolveAll<T>() where T : class;
    }

    public sealed class ContainerOptions
    {
        public IDiDiagnosticSink DiagnosticSink { get; set; }
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class InjectConstructorAttribute : Attribute
    {
    }
}
