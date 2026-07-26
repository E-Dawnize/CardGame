namespace RazorFramework.Lifecycle
{
    /// <summary>
    /// 帧更新接口 — 纯 C# 类的 Tick 驱动。
    /// MonoBehaviour 组件应继承 StrictLifecycleMonoBehaviour 而非直接实现此接口。
    /// </summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
