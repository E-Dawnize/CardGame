using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using RazorFramework.DI;
using UnityEngine;

namespace RazorFramework.Unity.DI
{
    public sealed class UnityObjectInjector
    {
        private const BindingFlags DeclaredMembers =
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;

        private static readonly ConcurrentDictionary<
            Type,
            Lazy<MemberPlan[]>> CachedPlans =
                new ConcurrentDictionary<Type, Lazy<MemberPlan[]>>();

        private readonly IServiceResolver _resolver;
        public UnityObjectInjector(IServiceResolver resolver)
        {
            UnityMainThread.EnsureCurrent();
            _resolver = resolver ??
                throw new ArgumentNullException(nameof(resolver));
        }

        public void Inject(UnityEngine.Object target)
        {
            UnityMainThread.EnsureCurrent();
            if (target == null)
            {
                return;
            }

            var targetType = target.GetType();
            var plans = CachedPlans.GetOrAdd(
                targetType,
                type => new Lazy<MemberPlan[]>(
                    () => BuildPlans(type),
                    LazyThreadSafetyMode.ExecutionAndPublication)).Value;

            foreach (var plan in plans)
            {
                if (!_resolver.TryResolve(
                        plan.ServiceType,
                        out var service))
                {
                    if (plan.IsOptional)
                    {
                        continue;
                    }

                    throw new UnityInjectionException(
                        UnityInjectionErrorCode.MissingDependency,
                        "A required Unity member dependency is not registered.",
                        targetType,
                        plan.MemberName,
                        plan.ServiceType);
                }

                try
                {
                    plan.Assign(target, service);
                }
                catch (Exception error)
                {
                    throw new UnityInjectionException(
                        UnityInjectionErrorCode.AssignmentFailed,
                        "A resolved service could not be assigned to a Unity member.",
                        targetType,
                        plan.MemberName,
                        plan.ServiceType,
                        error);
                }
            }
        }

        private static MemberPlan[] BuildPlans(Type targetType)
        {
            var hierarchy = new Stack<Type>();
            for (var current = targetType;
                 current != null;
                 current = current.BaseType)
            {
                hierarchy.Push(current);
            }

            var plans = new List<MemberPlan>();
            while (hierarchy.Count > 0)
            {
                var declaringType = hierarchy.Pop();
                var members = declaringType
                    .GetMembers(DeclaredMembers)
                    .Where(IsInjectionMember)
                    .OrderBy(member => member.MetadataToken);
                foreach (var member in members)
                {
                    plans.Add(BuildMemberPlan(targetType, member));
                }
            }

            return plans.ToArray();
        }

        private static bool IsInjectionMember(MemberInfo member)
        {
            return member.IsDefined(typeof(InjectAttribute), false) ||
                   member.IsDefined(typeof(InjectOptionalAttribute), false);
        }

        private static MemberPlan BuildMemberPlan(
            Type targetType,
            MemberInfo member)
        {
            var isRequired =
                member.IsDefined(typeof(InjectAttribute), false);
            var isOptional =
                member.IsDefined(typeof(InjectOptionalAttribute), false);
            if (isRequired == isOptional)
            {
                throw InvalidMember(
                    targetType,
                    member,
                    "A Unity injection member must have exactly one injection attribute.");
            }

            if (member is FieldInfo field)
            {
                if (field.IsStatic || field.IsInitOnly || field.IsLiteral)
                {
                    throw InvalidMember(
                        targetType,
                        member,
                        "Injected fields must be mutable instance fields.");
                }

                return new MemberPlan(
                    field.Name,
                    field.FieldType,
                    isOptional,
                    field.SetValue);
            }

            if (member is PropertyInfo property)
            {
                var setter = property.GetSetMethod(true);
                if (property.GetIndexParameters().Length != 0 ||
                    setter == null ||
                    setter.IsStatic)
                {
                    throw InvalidMember(
                        targetType,
                        member,
                        "Injected properties must be non-indexed writable instance properties.");
                }

                return new MemberPlan(
                    property.Name,
                    property.PropertyType,
                    isOptional,
                    property.SetValue);
            }

            throw InvalidMember(
                targetType,
                member,
                "Only fields and properties support Unity member injection.");
        }

        private static UnityInjectionException InvalidMember(
            Type targetType,
            MemberInfo member,
            string message)
        {
            return new UnityInjectionException(
                UnityInjectionErrorCode.InvalidMember,
                message,
                targetType,
                member.Name,
                GetServiceType(member));
        }

        private static Type GetServiceType(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.FieldType;
            }

            return member is PropertyInfo property
                ? property.PropertyType
                : null;
        }

        private sealed class MemberPlan
        {
            public MemberPlan(
                string memberName,
                Type serviceType,
                bool isOptional,
                Action<object, object> assign)
            {
                MemberName = memberName;
                ServiceType = serviceType;
                IsOptional = isOptional;
                Assign = assign;
            }

            public string MemberName { get; }
            public Type ServiceType { get; }
            public bool IsOptional { get; }
            public Action<object, object> Assign { get; }
        }
    }
}
