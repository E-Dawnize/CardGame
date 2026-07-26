using System;
using System.Linq;
using System.Threading.Tasks;
using RazorFramework.DI;
using RazorFramework.Events;
using RazorFramework.Lifecycle;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RazorFramework.Boot
{
    /// <summary>
    /// 项目启动入口 — DI 容器初始化 + 生命周期协调。
    /// 
    /// 启动顺序：
    ///   1. 创建 DI 容器和 Project Scope
    ///   2. 加载 BootConfig → 运行 Global Installers
    ///   3. 验证依赖图
    ///   4. 设置 LifecycleRegistry
    ///   5. OnGlobalViewsReady（钩子：项目特定全局 View 初始化）
    ///   6. 预创建初始场景 Scope
    ///   7. InitializeAll → StartAll
    ///   8. 注册 ITickable → 启动 UpdateRunner
    ///   9. OnBootComplete（钩子：发布 GameReady 等）
    /// </summary>
    public class ProjectContext : MonoBehaviour
    {
        private static ProjectContext _instance;

        protected DIContainer Container { get; private set; }
        protected IScope ProjectScope { get; private set; }
        protected InstallerConfig Config { get; private set; }

        private const string BootConfigLabel = "BootConfig";

        // === 扩展钩子（项目特定逻辑注入点） ===

        /// <summary>全局 View 创建后调用 — 用于创建游戏特定的 DontDestroyOnLoad 对象</summary>
        protected virtual void OnGlobalViewsReady() { }

        /// <summary>启动完成回调 — 用于发布 GameReadyEvent、播放 BGM 等</summary>
        protected virtual void OnBootComplete() { }

        /// <summary>场景 Scope 创建后、初始化前调用 — 用于场景级服务注册</summary>
        protected virtual void OnSceneScopeCreated(IScope scope) { }

        #region 启动流程

        public static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("ProjectContext");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ProjectContext>();
            go.AddComponent<UpdateRunner>();
            _instance.Boot();
        }

        public static IScope GetProjectScope() => _instance?.ProjectScope;

        public static void ResetStaticState() => _instance = null;

        private async void Boot()
        {
            Debug.Log("[ProjectContext] Starting boot sequence...");
            ShowLoadingOverlay();

            try
            {
                CreateDIContainer();
                await RegisterInstallers();
                ValidateDependencies();
                SetupLifecycleRegistry();
                OnGlobalViewsReady();
                SetupSceneScoping();
                ExecuteLifecycle();
                StartGameLoop();
                HideLoadingOverlay();
                OnBootComplete();
                Debug.Log("[ProjectContext] Boot sequence completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProjectContext] Boot FAILED: {ex}");
                HideLoadingOverlay();
            }
        }

        private void CreateDIContainer()
        {
            Container = new DIContainer
            {
                // 将 Debug.Log 桥接到 DI 容器日志
                LogInfo = Debug.Log,
                LogWarning = Debug.LogWarning,
                LogError = Debug.LogError
            };
            // 将 DI 创建的实例自动注册到生命周期系统
            Container.OnInstanceCreated = LifecycleRegistry.Register;
            ProjectScope = Container.CreateScope();
        }

        private async Task RegisterInstallers()
        {
            var handle = Addressables.LoadAssetAsync<InstallerConfig>(BootConfigLabel).Task;
            Config = await handle;
            if (Config != null)
            {
                foreach (var installer in Config.GlobalInstallersSorted)
                    installer.Register(Container);
                Debug.Log($"[ProjectContext] Registered {Config.GlobalInstallersSorted.Count()} global installers");
            }
            else
            {
                Debug.LogWarning("[ProjectContext] No BootConfig found — using empty container");
            }
        }

        private void ValidateDependencies()
        {
            var result = Container.Validate();
            if (!result.IsValid)
                Debug.LogError($"[ProjectContext] DI validation failed:\n{string.Join("\n", result.Errors)}");
        }

        private void SetupLifecycleRegistry()
        {
            LifecycleRegistry.SetContainer(Container, ProjectScope);
        }

        private void ExecuteLifecycle()
        {
            // 预解析所有 IInitializable/IStartable 服务，触发创建 + 注册到 LifecycleRegistry
            Container.ResolveAll<IInitializable>(ProjectScope);
            Container.ResolveAll<IStartable>(ProjectScope);

            LifecycleRegistry.InitializeAll();
            LifecycleRegistry.StartAll();
        }

        private void StartGameLoop()
        {
            var updateRunner = GetComponent<UpdateRunner>();

            var tickables = Container.ResolveAll<ITickable>(ProjectScope);
            foreach (var t in tickables) updateRunner.Register(t);

            var sceneTickables = LifecycleRegistry.GetTickables();
            foreach (var t in sceneTickables) updateRunner.Register(t);

            Debug.Log($"[ProjectContext] Game loop started ({tickables.Count()} DI + {sceneTickables.Count} scene tickables)");
        }

        private void SetupSceneScoping()
        {
            if (Config == null) return;

            var scopeProvider = Container.GetService<IScopeProvider>();
            if (scopeProvider == null)
            {
                Debug.LogWarning("[ProjectContext] IScopeProvider not registered — scene scoping disabled");
                return;
            }

            var initialScope = Container.CreateScope();
            scopeProvider.CurrentScope = initialScope;
            OnSceneScopeCreated(initialScope);

            SceneScopeRunner.Attach(Config, Container, scopeProvider);
            Debug.Log("[ProjectContext] Scene scoping initialized");
        }

        #endregion

        #region 加载遮罩（可选）

        private GameObject _loadingOverlay;

        private void ShowLoadingOverlay()
        {
            _loadingOverlay = new GameObject("LoadingOverlay");
            DontDestroyOnLoad(_loadingOverlay);
            var canvas = _loadingOverlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            _loadingOverlay.AddComponent<UnityEngine.UI.CanvasScaler>();
            _loadingOverlay.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var img = new GameObject("Bg").AddComponent<UnityEngine.UI.Image>();
            img.transform.SetParent(_loadingOverlay.transform, false);
            img.color = Color.black;
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private void HideLoadingOverlay()
        {
            if (_loadingOverlay != null) { Destroy(_loadingOverlay); _loadingOverlay = null; }
        }

        #endregion

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            LifecycleRegistry.Clear();
            ProjectScope?.Dispose();
            Container?.Dispose();
        }
    }
}
