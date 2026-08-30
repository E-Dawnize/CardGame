using System.Collections.Generic;
using CardGame.Domain;

namespace CardGame.Tests.EditMode
{
    /// <summary>测试用合法数据工厂（各测试在此基线上做单点破坏）。</summary>
    internal static class DataTestFactory
    {
        public static StatusDef Status(string id = "Freeze", string name = null)
        {
            return new StatusDef(id, name ?? id, "icon_" + id.ToLowerInvariant(), -1, 0,
                "", new List<EffectEntry>(), new List<EffectEntry>(), "#FFFFFF");
        }

        public static CardDef Card(
            string id = "card_ice_01", CardType type = CardType.Attack,
            int damage = 6, int block = 0, IReadOnlyList<EffectEntry> effects = null,
            string upgradeToId = null, int cost = 1)
        {
            return new CardDef(id, "测试卡", cost, type, CardRarity.Common, null,
                damage, block, effects ?? new List<EffectEntry>(),
                new List<string>(), upgradeToId, null);
        }

        public static EffectEntry Effect(
            EffectAction action = EffectAction.DealDamage, int value = 5,
            string statusType = null, ConditionEntry condition = null,
            EffectTrigger trigger = EffectTrigger.OnPlay, EffectTarget target = EffectTarget.Self)
        {
            return new EffectEntry(trigger, target, action, value, statusType, condition);
        }

        public static RelicDef Relic(string id = "relic_ice_01", IReadOnlyList<EffectEntry> effects = null)
        {
            return new RelicDef(id, "测试遗物", RelicRarity.Common, null,
                effects ?? new List<EffectEntry>(), "手写描述", null, new List<string>());
        }

        public static EnemyDef Enemy(
            string id = "enemy_wolf_01", IReadOnlyList<EnemyIntent> intents = null,
            IReadOnlyList<StatusApply> startingStatus = null)
        {
            return new EnemyDef(id, "测试敌人", 10, intents ?? new List<EnemyIntent>(),
                startingStatus ?? new List<StatusApply>(), new List<string>(), null);
        }

        public static EnemyIntent Intent(
            IntentType type = IntentType.Attack, int value = 8, int count = 1,
            IReadOnlyList<EffectEntry> effects = null, int minRound = 1, int cooldown = 0)
        {
            return new EnemyIntent(type, value, count, effects ?? new List<EffectEntry>(),
                50f, minRound, cooldown, null);
        }

        public static EventDef Event(string id = "event_wounded_01", IReadOnlyList<EventChoice> choices = null)
        {
            return new EventDef(id, "测试事件", "描述", null, false, new List<string>(),
                choices ?? new List<EventChoice>
                {
                    new EventChoice("选项", "tag", null, new List<string>(),
                        new List<EventReward>(), new List<EventPenalty>(), null)
                });
        }

        public static EventChoice Choice(string text = "选项", string choiceChain = null)
        {
            return new EventChoice(text, "tag", null, new List<string>(),
                new List<EventReward>(), new List<EventPenalty>(), choiceChain);
        }

        public static DialogueSequence Dialogue(string id = "dialogue_tavern_01", IReadOnlyList<DialogueLine> lines = null)
        {
            return new DialogueSequence(id, "测试对话",
                lines ?? new List<DialogueLine>
                {
                    new DialogueLine("player", null, PortraitPosition.Left, null, "你好",
                        DialogueLineType.Normal, new List<DialogueChoice>(), -1, 0f, new List<EffectEntry>())
                });
        }

        public static DialogueLine Line(
            string speakerId = "player", DialogueLineType type = DialogueLineType.Normal,
            IReadOnlyList<DialogueChoice> choices = null, int nextLineIndex = -1)
        {
            return new DialogueLine(speakerId, null, PortraitPosition.Left, null, "文本",
                type, choices, nextLineIndex, 0f, new List<EffectEntry>());
        }

        public static WorldDef World(
            IReadOnlyList<SpeakerDef> speakers = null,
            IReadOnlyList<FactionDef> factions = null,
            IReadOnlyList<MapLayerDef> layers = null)
        {
            return new WorldDef("标题", "tagline", "premise", "playerConcept", "goal",
                layers ?? new List<MapLayerDef>(),
                factions ?? new List<FactionDef>
                {
                    new FactionDef("frost_cult", "霜印教团", "", new List<FactionRelation>())
                },
                speakers ?? DefaultSpeakers());
        }

        public static IReadOnlyList<SpeakerDef> DefaultSpeakers()
        {
            return new List<SpeakerDef>
            {
                new SpeakerDef("player", "冒险者", null, null, "", new List<string>(), null),
                new SpeakerDef("narrator", "旁白", null, null, "", new List<string>(), null),
                new SpeakerDef("elder_chen", "陈长老", null, null, "frost_cult", new List<string>(), null)
            };
        }
    }
}
