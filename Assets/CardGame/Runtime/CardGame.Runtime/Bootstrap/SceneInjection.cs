using RazorFramework.DI;
using RazorFramework.Unity.DI;
using UnityEngine;

namespace CardGame.Runtime
{
    /// <summary>
    /// 场景 [Inject] 成员注入驱动（规格：docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md）。
    /// 对齐法则：对象的生命周期必须 ≤ 它被注入的 resolver 的生命周期——
    /// 常驻场景对象只能由根容器注入（仅 Singleton；注入 Scoped 服务会抛 ScopeMismatch）；
    /// scope 拥有的屏/对象由创建该 scope 的驱动方从 scope 注入，且必须先于 scope 销毁。
    /// 运行时动态创建的对象不在本驱动范围：谁创建谁注入（拿 UnityObjectInjector 显式调用）。
    /// </summary>
    public static class SceneInjection
    {
        /// <summary>扫描当前场景（含 inactive）全部 MonoBehaviour，注入 [Inject]/[InjectOptional] 成员；必需依赖缺失原样抛出。</summary>
        public static void InjectScene(IServiceResolver resolver)
        {
            var injector = new UnityObjectInjector(resolver);
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                injector.Inject(behaviour);
            }
        }
    }
}
