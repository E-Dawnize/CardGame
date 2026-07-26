using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace RazorFramework.Boot
{
    /// <summary>
    /// 项目启动引导 — 通过 RuntimeInitializeOnLoadMethod 自动触发。
    /// 负责：静态重置 → EnhancedTouch → ProjectContext.Ensure() → 输入系统修复。
    /// </summary>
    public static class ProjectBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ProjectContext.ResetStaticState();
            Lifecycle.LifecycleRegistry.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying) return;
            EnhancedTouchSupport.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
            ProjectContext.Ensure();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FixEventSystemInputModules();
        }

        /// <summary>
        /// 将场景中的 StandaloneInputModule 替换为 InputSystemUIInputModule。
        /// 这确保 Unity 新输入系统正常工作。
        /// </summary>
        public static void FixEventSystemInputModules()
        {
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            foreach (var es in eventSystems)
            {
                if (es.TryGetComponent<StandaloneInputModule>(out var oldModule))
                {
                    oldModule.enabled = false;
                    Object.Destroy(oldModule);
                }
                if (!es.TryGetComponent<InputSystemUIInputModule>(out _))
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
