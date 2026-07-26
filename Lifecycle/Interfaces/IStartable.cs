namespace RazorFramework.Lifecycle
{
    /// <summary>
    /// 启动接口 — 所有组件 Initialize 完成后的跨组件交互阶段。
    /// 用途：注册事件监听、建立组件间连接、开始运行时逻辑。
    /// </summary>
    public interface IStartable
    {
        void OnStart();
    }
}
