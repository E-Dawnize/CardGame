using RazorFramework.DI;
using UnityEngine;

namespace RazorFramework.Lifecycle
{
    /// <summary>
    /// 严格生命周期 MonoBehaviour 基类。
    /// 封存 Awake()/Start() 为 private，子类只能重写 OnInitialize → OnStartExternal → Tick → OnShutdown。
    /// 
    /// 生命周期顺序：
    ///   1. Awake() → LifecycleRegistry.Register(this) → DI 注入
    ///   2. OnInitialize() — 所有组件 Initialize 之前，内部状态准备
    ///   3. OnStartExternal() — 所有组件 Initialize 之后，开始跨组件交互
    ///   4. Tick(dt) — 每帧（仅当实现 ITickable 时驱动）
    ///   5. OnShutdown() → LifecycleRegistry.Unregister(this)
    /// </summary>
    public abstract class StrictLifecycleMonoBehaviour : MonoBehaviour, IInitializable, IStartable, ITickable
    {
        private void Awake()
        {
            LifecycleRegistry.Register(this);
        }

        private void Start() { }

        void IInitializable.Initialize() => OnInitialize();
        void IStartable.OnStart() => OnStartExternal();
        void ITickable.Tick(float deltaTime) => Tick(deltaTime);

        /// <summary>内部初始化 — DI 注入完成后，所有组件 Initialize 之前</summary>
        protected virtual void OnInitialize() { }

        /// <summary>外部启动 — 所有组件 Initialize 完成后，开始跨组件交互</summary>
        protected virtual void OnStartExternal() { }

        /// <summary>帧更新（仅当组件被注册为 ITickable 时由 UpdateRunner 驱动）</summary>
        protected virtual void Tick(float deltaTime) { }

        /// <summary>销毁前清理 — 取消事件订阅，释放资源</summary>
        protected virtual void OnShutdown() { }

        private void OnDestroy()
        {
            OnShutdown();
            LifecycleRegistry.Unregister(this);
        }
    }
}
