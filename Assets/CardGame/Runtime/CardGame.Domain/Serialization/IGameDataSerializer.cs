namespace CardGame.Domain
{
    /// <summary>
    /// JSON 反序列化接缝：Domain 保持纯 C#（不引用 UnityEngine.JsonUtility），
    /// 由 Unity 侧（CardGame.Runtime）提供实现。将来可无痛替换为其他序列化器。
    /// </summary>
    public interface IGameDataSerializer
    {
        T FromJson<T>(string json);
    }
}
