using CardGame.Domain;
using UnityEngine;

namespace CardGame.Runtime
{
    /// <summary>
    /// 配置数据入口组件（最小引导钩子）：持有数据 TextAsset 引用，Initialize() 后提供仓库。
    /// 将来 Addressables/DI 接管加载时替换此组件背后实现，外部不变。
    /// </summary>
    public sealed class GameDataCatalog : MonoBehaviour
    {
        [SerializeField] private TextAsset[] dataFiles = System.Array.Empty<TextAsset>();

        private DataRepository _repository;

        public DataRepository Repository
        {
            get
            {
                if (_repository == null) Initialize();
                return _repository;
            }
        }

        public void Initialize()
        {
            if (_repository != null) return;
            var loader = new GameDataLoader(new JsonUtilityGameDataSerializer());
            _repository = loader.Load(dataFiles);
        }
    }
}
