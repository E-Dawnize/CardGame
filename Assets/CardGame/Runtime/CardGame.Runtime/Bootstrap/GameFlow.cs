using CardGame.Domain;

namespace CardGame.Runtime
{
    /// <summary>
    /// 游戏流程占位服务：「DI 解析 → 显式 Start」的屏障语义落点（见 2026-09-02 决策问题 1）。
    /// 未来扩展为游戏流程状态机（新局 → 地图 → 遭遇 → 结算 → 局终），
    /// 届时 RunScope / EncounterScope 由本服务创建与释放。
    /// </summary>
    public sealed class GameFlow
    {
        private readonly DataRepository _data;

        public GameFlow(DataRepository data)
        {
            _data = data;
        }

        /// <summary>启动后从数据仓库取得的世界标题（占位：证明数据 → 容器 → 启动链路贯通）。</summary>
        public string WorldTitle { get; private set; }

        public void Start()
        {
            WorldTitle = _data.World?.Title ?? string.Empty;
        }
    }
}
