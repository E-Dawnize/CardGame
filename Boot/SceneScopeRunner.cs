using System.Linq;
using RazorFramework.DI;
using RazorFramework.Lifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RazorFramework.Boot
{
    /// <summary>
    /// 场景作用域管理器。
    /// - 场景加载时创建 Scope + 注入场景 Installer
    /// - 场景卸载时清理 Scope
    /// </summary>
    public class SceneScopeRunner : MonoBehaviour
    {
        private InstallerConfig _config;
        private IScope _scope;
        private DIContainer _container;
        private IScopeProvider _scopeProvider;

        public static void Attach(InstallerConfig config, DIContainer container, IScopeProvider scopeProvider)
        {
            var go = new GameObject("SceneScopeRunner");
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<SceneScopeRunner>();
            runner._config = config;
            runner._container = container;
            runner._scopeProvider = scopeProvider;
            SceneManager.sceneLoaded += runner.OnSceneLoaded;
            SceneManager.sceneUnloaded += runner.OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_scope != null) return; // 初始场景由 ProjectContext 预创建
            CreateScopeAndInit();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _scopeProvider.CurrentScope = null;
            _scope?.Dispose();
            _scope = null;
        }

        private void CreateScopeAndInit()
        {
            _scope = _container.CreateScope();
            _scopeProvider.CurrentScope = _scope;

            foreach (var installer in _config.SceneInstallersSorted)
                installer.Register(_container);

            var scope = _scope as DIContainer.Scope;
            foreach (var init in _container.ResolveAll<IInitializable>(scope))
                init.Initialize();
            foreach (var start in _container.ResolveAll<IStartable>(scope))
                start.OnStart();
        }
    }
}
