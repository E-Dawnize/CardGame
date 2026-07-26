using RazorFramework.DI;
using RazorFramework.Lifecycle;
using UnityEngine;

namespace RazorFramework.Boot
{
    /// <summary>
    /// Installer 资产基类 — 在 DI 容器中注册服务。
    /// 继承此类创建 ScriptableObject Installer，放入 InstallerConfig 的 globalInstallers 或 sceneInstallers。
    /// </summary>
    public abstract class InstallerAsset : ScriptableObject, IInstaller
    {
        [Tooltip("安装顺序，数字越小越先执行")]
        public int order;

        public abstract void Register(DIContainer container);
    }
}
