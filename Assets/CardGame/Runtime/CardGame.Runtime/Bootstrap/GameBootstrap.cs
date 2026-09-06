using System;
using CardGame.Domain;
using UnityEngine;

namespace CardGame.Runtime
{
    /// <summary>
    /// 场景唯一入口（单场景形态，见 2026-09-02 决策问题 5 A1）：
    /// 持有数据 TextAsset 引用 → 加载仓库 → 建组合根 → 场景 [Inject] 注入 → 显式启动 → 退出时确定性释放。
    /// [DefaultExecutionOrder(-32000)] 保证本组件 Awake 全场景最先：注入先于其他脚本的
    /// Awake/OnEnable，其他脚本可在自己的 Awake/OnEnable 中安全使用 [Inject] 成员。
    /// 数据加载的 Addressables 换装点在 LoadRepository()（替换此处的 TextAsset 管线，外部不变）。
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset[] dataFiles = Array.Empty<TextAsset>();

        private GameComposition _composition;

        private void Awake()
        {
            _composition = new GameComposition(LoadRepository());
            SceneInjection.InjectScene(_composition.Container);
            _composition.StartFlow();
        }

        private void OnDestroy()
        {
            _composition?.Dispose();
            _composition = null;
        }

        private DataRepository LoadRepository()
        {
            var loader = new GameDataLoader(new JsonUtilityGameDataSerializer());
            return loader.Load(dataFiles);
        }
    }
}