using CardGame.Domain;
using UnityEngine;

namespace CardGame.Runtime
{
    /// <summary>JsonUtility 实现的序列化接缝（Unity 侧唯一允许接触 JsonUtility 的位置）。</summary>
    public sealed class JsonUtilityGameDataSerializer : IGameDataSerializer
    {
        public T FromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
