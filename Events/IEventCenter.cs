using System;
using RazorFramework.Lifecycle;

namespace RazorFramework.Events
{
    /// <summary>
    /// 强类型事件总线接口。所有事件必须为 struct 类型。
    /// 继承 IInitializable 以纳入统一生命周期管理。
    /// </summary>
    public interface IEventCenter : IInitializable
    {
        void Subscribe<T>(Action<T> handler) where T : struct;
        void Unsubscribe<T>(Action<T> handler) where T : struct;
        void Publish<T>(T evt) where T : struct;
    }
}
