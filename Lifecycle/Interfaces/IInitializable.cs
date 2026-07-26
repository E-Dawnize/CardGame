namespace RazorFramework.Lifecycle
{
    /// <summary>
    /// 初始化接口 — 组件内部状态准备阶段。
    /// 保证：依赖注入已完成。
    /// 用途：初始化内部状态、设置默认值、验证配置。
    /// </summary>
    public interface IInitializable
    {
        void Initialize();
    }
}
