using System;

namespace RazorFramework.Unity.DI
{
    public enum UnityInjectionErrorCode
    {
        WrongThread,
        InvalidMember,
        MissingDependency,
        AssignmentFailed
    }

    public sealed class UnityInjectionException : InvalidOperationException
    {
        public UnityInjectionException(
            UnityInjectionErrorCode code,
            string message,
            Type targetType = null,
            string memberName = null,
            Type serviceType = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            Code = code;
            TargetType = targetType;
            MemberName = memberName;
            ServiceType = serviceType;
        }

        public UnityInjectionErrorCode Code { get; }
        public Type TargetType { get; }
        public string MemberName { get; }
        public Type ServiceType { get; }
    }
}
