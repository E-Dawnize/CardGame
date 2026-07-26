using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RazorFramework.Boot
{
    /// <summary>
    /// Boot 配置资产 — 聚合所有 Installer 的 ScriptableObject。
    /// 通过 Addressables 加载（label="BootConfig"）。
    /// </summary>
    [CreateAssetMenu(fileName = "BootConfig", menuName = "RazorFramework/BootConfig")]
    public class InstallerConfig : ScriptableObject
    {
        [Tooltip("全局 Installer，应用生命周期内只执行一次")]
        public List<InstallerAsset> globalInstallers = new();

        [Tooltip("场景 Installer，每次场景加载时执行（Scoped 服务在此注册）")]
        public List<InstallerAsset> sceneInstallers = new();

        public IEnumerable<InstallerAsset> GlobalInstallersSorted =>
            globalInstallers.OrderBy(i => i.order);

        public IEnumerable<InstallerAsset> SceneInstallersSorted =>
            sceneInstallers.OrderBy(i => i.order);
    }
}
