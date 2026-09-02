using System;
using CardGame.Domain;
using UnityEngine;

namespace CardGame.Runtime
{
    /// <summary>
    /// 场景唯一入口（单场景形态，见 2026-09-02 决策问题 5 A1）：
    /// 持有数据 TextAsset 引用 → 加载仓库 → 建组合根 → 显式启动 → 退出时确定性释放。
    /// 数据加载的 Addressables 换装点在 LoadRepository()（替换此处的 TextAsset 管线，外部不变）。
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset[] dataFiles = Array.Empty<TextAsset>();

        private GameComposition _composition;

        private void Awake()
        {
            _composition = new GameComposition(LoadRepository());
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
