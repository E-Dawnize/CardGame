using RazorFramework.DI;

namespace RazorFramework.Lifecycle
{
    /// <summary>场景作用域访问器 — 追踪当前活跃场景的 DI Scope</summary>
    public interface IScopeProvider
    {
        IScope CurrentScope { get; set; }
    }

    public class ScopeProvider : IScopeProvider
    {
        public IScope CurrentScope { get; set; }
    }
}
