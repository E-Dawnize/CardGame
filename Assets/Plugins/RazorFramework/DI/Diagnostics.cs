using System;

namespace RazorFramework.DI
{
    public enum DiDiagnosticKind
    {
        ContainerBuilt,
        ScopeCreated,
        InstanceCreated,
        ResolutionFailed,
        ScopeDisposed,
        ContainerDisposed
    }

    public readonly struct DiDiagnosticEvent
    {
        public DiDiagnosticEvent(
            DiDiagnosticKind kind,
            Type serviceType = null,
            Type implementationType = null,
            Type scopeType = null,
            DependencyErrorCode? errorCode = null)
        {
            Kind = kind;
            ServiceType = serviceType;
            ImplementationType = implementationType;
            ScopeType = scopeType;
            ErrorCode = errorCode;
        }

        public DiDiagnosticKind Kind { get; }
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
        public Type ScopeType { get; }
        public DependencyErrorCode? ErrorCode { get; }
    }

    public interface IDiDiagnosticSink
    {
        void Write(DiDiagnosticEvent diagnosticEvent);
    }

    internal sealed class DiagnosticDispatcher
    {
        private readonly IDiDiagnosticSink _sink;

        public DiagnosticDispatcher(IDiDiagnosticSink sink)
        {
            _sink = sink;
        }

        public void Write(DiDiagnosticEvent diagnosticEvent)
        {
            if (_sink == null)
            {
                return;
            }

            try
            {
                _sink.Write(diagnosticEvent);
            }
            catch
            {
                // Diagnostics are observational and must not alter container behavior.
            }
        }
    }
}
