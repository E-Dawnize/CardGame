namespace RazorFramework.Lifecycle
{
    /// <summary>
    /// 安装器接口 — 向 DI 容器注册服务。
    /// 实现类应为 ScriptableObject（Unity 编辑器资产）或纯 C# 类。
    /// </summary>
    public interface IInstaller
    {
        void Register(DI.DIContainer container);
    }
}
