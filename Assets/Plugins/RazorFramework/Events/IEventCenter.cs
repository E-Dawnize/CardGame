using System;

namespace RazorFramework.Events
{
    /// <summary>
    /// 强类型事件总线接口。所有事件必须为 struct 类型。
    /// 保持纯 C#，不依赖生命周期接口。
    /// </summary>
    public interface IEventCenter
    {
        void Subscribe<T>(Action<T> handler) where T : struct;
        void Unsubscribe<T>(Action<T> handler) where T : struct;
        void Publish<T>(T evt) where T : struct;
    }
}
