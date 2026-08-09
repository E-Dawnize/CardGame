using System;

namespace RazorFramework.Unity.DI
{
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class InjectAttribute : Attribute
    {
    }

    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class InjectOptionalAttribute : Attribute
    {
    }
}
