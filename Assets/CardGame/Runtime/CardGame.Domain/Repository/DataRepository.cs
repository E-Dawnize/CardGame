using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CardGame.Domain
{
    /// <summary>
    /// 全量静态内容定义：Id 索引的不可变仓库（构建后冻结，对齐 DI V2 的 build-freeze 哲学）。
    /// 只能经 DataRepositoryBuilder 构建。
    /// </summary>
    public sealed class DataRepository
    {
        internal DataRepository(
            IReadOnlyDictionary<string, CardDef> cards,
            IReadOnlyDictionary<string, RelicDef> relics,
            IReadOnlyDictionary<string, EnemyDef> enemies,
            IReadOnlyDictionary<string, StatusDef> statuses,
            IReadOnlyDictionary<string, EventDef> events,
            IReadOnlyDictionary<string, DialogueSequence> dialogues,
            WorldDef world,
            UiStrings uiStrings)
        {
            Cards = cards;
            Relics = relics;
            Enemies = enemies;
            Statuses = statuses;
            Events = events;
            Dialogues = dialogues;
            World = world;
            UiStrings = uiStrings;
        }

        public IReadOnlyDictionary<string, CardDef> Cards { get; }
        public IReadOnlyDictionary<string, RelicDef> Relics { get; }
        public IReadOnlyDictionary<string, EnemyDef> Enemies { get; }
        public IReadOnlyDictionary<string, StatusDef> Statuses { get; }
        public IReadOnlyDictionary<string, EventDef> Events { get; }
        public IReadOnlyDictionary<string, DialogueSequence> Dialogues { get; }
        public WorldDef World { get; }
        public UiStrings UiStrings { get; }

        public CardDef GetCard(string id) => Cards.TryGetValue(id ?? "", out var v) ? v : null;

        public bool TryGetCard(string id, out CardDef card) => Cards.TryGetValue(id ?? "", out card);

        public RelicDef GetRelic(string id) => Relics.TryGetValue(id ?? "", out var v) ? v : null;

        public bool TryGetRelic(string id, out RelicDef relic) => Relics.TryGetValue(id ?? "", out relic);

        public EnemyDef GetEnemy(string id) => Enemies.TryGetValue(id ?? "", out var v) ? v : null;

        public bool TryGetEnemy(string id, out EnemyDef enemy) => Enemies.TryGetValue(id ?? "", out enemy);

        public StatusDef GetStatus(string id) => Statuses.TryGetValue(id ?? "", out var v) ? v : null;

        public bool TryGetStatus(string id, out StatusDef status) => Statuses.TryGetValue(id ?? "", out status);

        public EventDef GetEvent(string id) => Events.TryGetValue(id ?? "", out var v) ? v : null;

        public bool TryGetEvent(string id, out EventDef evt) => Events.TryGetValue(id ?? "", out evt);

        public DialogueSequence GetDialogue(string id) => Dialogues.TryGetValue(id ?? "", out var v) ? v : null;

        public bool TryGetDialogue(string id, out DialogueSequence dialogue) => Dialogues.TryGetValue(id ?? "", out dialogue);

        internal static IReadOnlyDictionary<string, T> IndexById<T>(IReadOnlyList<T> items, System.Func<T, string> idSelector)
        {
            var map = new Dictionary<string, T>();
            foreach (var item in items)
            {
                map[idSelector(item) ?? ""] = item;
            }
            return new ReadOnlyDictionary<string, T>(map);
        }
    }
}
