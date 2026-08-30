using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CardGame.Domain
{
    // 类型化运行时模型：由 DtoMapper 从 DTO 构建，构建后不可变。
    // 自动生成字段（Description / PreviewText）由 DescriptionBuilder 在仓库构建期填充（internal set）。

    /// <summary>卡牌定义（不可变）。</summary>
    public sealed class CardDef
    {
        public CardDef(
            string id, string name, int cost, CardType type, CardRarity rarity, string cardClass,
            int damage, int block, IReadOnlyList<EffectEntry> effects, IReadOnlyList<string> keywords,
            string upgradeToId, string flavorText)
        {
            Id = id;
            Name = name;
            Cost = cost;
            Type = type;
            Rarity = rarity;
            Class = cardClass;
            Damage = damage;
            Block = block;
            Effects = AsReadOnly(effects);
            Keywords = AsReadOnly(keywords);
            UpgradeToId = upgradeToId;
            FlavorText = flavorText;
        }

        public string Id { get; }
        public string Name { get; }
        public int Cost { get; }
        public CardType Type { get; }
        public CardRarity Rarity { get; }
        public string Class { get; }
        public int Damage { get; }
        public int Block { get; }
        public IReadOnlyList<EffectEntry> Effects { get; }
        public IReadOnlyList<string> Keywords { get; }
        public string UpgradeToId { get; }
        public string FlavorText { get; }

        /// <summary>自动生成（策划不填）。由 DescriptionBuilder 填充。</summary>
        public string Description { get; internal set; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>效果原语（卡牌与遗物共用）。</summary>
    public sealed class EffectEntry
    {
        public EffectEntry(
            EffectTrigger trigger, EffectTarget target, EffectAction action,
            int value, string statusType, ConditionEntry condition)
        {
            Trigger = trigger;
            Target = target;
            Action = action;
            Value = value;
            StatusType = statusType;
            Condition = condition;
        }

        public EffectTrigger Trigger { get; }
        public EffectTarget Target { get; }
        public EffectAction Action { get; }
        public int Value { get; }
        public string StatusType { get; }
        public ConditionEntry Condition { get; }

        /// <summary>自动生成。由 DescriptionBuilder 填充。</summary>
        public string Description { get; internal set; }
    }

    /// <summary>效果条件。</summary>
    public sealed class ConditionEntry
    {
        public ConditionEntry(ConditionType type, string param)
        {
            Type = type;
            Param = param;
        }

        public ConditionType Type { get; }
        public string Param { get; }
    }

    /// <summary>遗物定义（不可变）。</summary>
    public sealed class RelicDef
    {
        public RelicDef(
            string id, string name, RelicRarity rarity, string relicClass,
            IReadOnlyList<EffectEntry> effects, string description, string flavorText,
            IReadOnlyList<string> keywords)
        {
            Id = id;
            Name = name;
            Rarity = rarity;
            Class = relicClass;
            Effects = AsReadOnly(effects);
            Description = description;
            FlavorText = flavorText;
            Keywords = AsReadOnly(keywords);
        }

        public string Id { get; }
        public string Name { get; }
        public RelicRarity Rarity { get; }
        public string Class { get; }
        public IReadOnlyList<EffectEntry> Effects { get; }
        public string Description { get; }   // 遗物描述为手写
        public string FlavorText { get; }
        public IReadOnlyList<string> Keywords { get; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>敌人定义（不可变）。</summary>
    public sealed class EnemyDef
    {
        public EnemyDef(
            string id, string name, int maxHp, IReadOnlyList<EnemyIntent> intentPool,
            IReadOnlyList<StatusApply> startingStatus, IReadOnlyList<string> keywords, string flavorText)
        {
            Id = id;
            Name = name;
            MaxHp = maxHp;
            IntentPool = AsReadOnly(intentPool);
            StartingStatus = AsReadOnly(startingStatus);
            Keywords = AsReadOnly(keywords);
            FlavorText = flavorText;
        }

        public string Id { get; }
        public string Name { get; }
        public int MaxHp { get; }
        public IReadOnlyList<EnemyIntent> IntentPool { get; }
        public IReadOnlyList<StatusApply> StartingStatus { get; }
        public IReadOnlyList<string> Keywords { get; }
        public string FlavorText { get; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>敌人意图（从意图池按权重选择）。</summary>
    public sealed class EnemyIntent
    {
        public EnemyIntent(
            IntentType type, int value, int count, IReadOnlyList<EffectEntry> effects,
            float weight, int minRound, int cooldown, string condition)
        {
            Type = type;
            Value = value;
            Count = count;
            Effects = effects == null ? new List<EffectEntry>().AsReadOnly() : new List<EffectEntry>(effects).AsReadOnly();
            Weight = weight;
            MinRound = minRound;
            Cooldown = cooldown;
            Condition = condition;
        }

        public IntentType Type { get; }
        public int Value { get; }
        public int Count { get; }
        public IReadOnlyList<EffectEntry> Effects { get; }
        public float Weight { get; }
        public int MinRound { get; }
        public int Cooldown { get; }
        public string Condition { get; }

        /// <summary>自动生成（如"攻击 8×2"）。由 DescriptionBuilder 填充。</summary>
        public string PreviewText { get; internal set; }
    }

    /// <summary>战斗初始状态施加（如"Block:10"）。</summary>
    public sealed class StatusApply
    {
        public StatusApply(string statusType, int stacks)
        {
            StatusType = statusType;
            Stacks = stacks;
        }

        public string StatusType { get; }
        public int Stacks { get; }
    }

    /// <summary>状态定义（每种状态一个条目）。</summary>
    public sealed class StatusDef
    {
        public StatusDef(
            string id, string name, string iconKey, int maxStack, int decayEachTurn,
            string effectDesc, IReadOnlyList<EffectEntry> onApplyEffect,
            IReadOnlyList<EffectEntry> onTickEffect, string colorHex)
        {
            Id = id;
            Name = name;
            IconKey = iconKey;
            MaxStack = maxStack;
            DecayEachTurn = decayEachTurn;
            EffectDesc = effectDesc;
            OnApplyEffect = AsReadOnly(onApplyEffect);
            OnTickEffect = AsReadOnly(onTickEffect);
            ColorHex = colorHex;
        }

        public string Id { get; }
        public string Name { get; }
        public string IconKey { get; }
        public int MaxStack { get; }
        public int DecayEachTurn { get; }
        public string EffectDesc { get; }
        public IReadOnlyList<EffectEntry> OnApplyEffect { get; }
        public IReadOnlyList<EffectEntry> OnTickEffect { get; }
        public string ColorHex { get; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>对话序列（一段对话）。</summary>
    public sealed class DialogueSequence
    {
        public DialogueSequence(string id, string title, IReadOnlyList<DialogueLine> lines)
        {
            Id = id;
            Title = title;
            Lines = lines == null ? new List<DialogueLine>().AsReadOnly() : new List<DialogueLine>(lines).AsReadOnly();
        }

        public string Id { get; }
        public string Title { get; }
        public IReadOnlyList<DialogueLine> Lines { get; }
    }

    /// <summary>对话行。</summary>
    public sealed class DialogueLine
    {
        public DialogueLine(
            string speakerId, string speakerName, PortraitPosition portrait, string emotion,
            string text, DialogueLineType type, IReadOnlyList<DialogueChoice> choices,
            int nextLineIndex, float autoDelay, IReadOnlyList<EffectEntry> onShowEffects)
        {
            SpeakerId = speakerId;
            SpeakerName = speakerName;
            Portrait = portrait;
            Emotion = emotion;
            Text = text;
            Type = type;
            Choices = choices == null ? new List<DialogueChoice>().AsReadOnly() : new List<DialogueChoice>(choices).AsReadOnly();
            NextLineIndex = nextLineIndex;
            AutoDelay = autoDelay;
            OnShowEffects = AsReadOnly(onShowEffects);
        }

        public string SpeakerId { get; }
        public string SpeakerName { get; }
        public PortraitPosition Portrait { get; }
        public string Emotion { get; }
        public string Text { get; }
        public DialogueLineType Type { get; }
        public IReadOnlyList<DialogueChoice> Choices { get; }
        public int NextLineIndex { get; }   // -1 = 结束对话
        public float AutoDelay { get; }
        public IReadOnlyList<EffectEntry> OnShowEffects { get; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>对话选项。</summary>
    public sealed class DialogueChoice
    {
        public DialogueChoice(
            string text, string tag, int nextLineIndex, string condition,
            string resultText, IReadOnlyList<EffectEntry> onSelectEffects)
        {
            Text = text;
            Tag = tag;
            NextLineIndex = nextLineIndex;
            Condition = condition;
            ResultText = resultText;
            OnSelectEffects = AsReadOnly(onSelectEffects);
        }

        public string Text { get; }
        public string Tag { get; }
        public int NextLineIndex { get; }   // -1 = 结束对话
        public string Condition { get; }
        public string ResultText { get; }
        public IReadOnlyList<EffectEntry> OnSelectEffects { get; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>叙事事件定义。</summary>
    public sealed class EventDef
    {
        public EventDef(
            string id, string title, string description, string imageKey, bool isRepeatable,
            IReadOnlyList<string> requirements, IReadOnlyList<EventChoice> choices)
        {
            Id = id;
            Title = title;
            Description = description;
            ImageKey = imageKey;
            IsRepeatable = isRepeatable;
            Requirements = AsReadOnly(requirements);
            Choices = choices == null ? new List<EventChoice>().AsReadOnly() : new List<EventChoice>(choices).AsReadOnly();
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string ImageKey { get; }
        public bool IsRepeatable { get; }
        public IReadOnlyList<string> Requirements { get; }
        public IReadOnlyList<EventChoice> Choices { get; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>事件选项。</summary>
    public sealed class EventChoice
    {
        public EventChoice(
            string text, string tag, string resultText, IReadOnlyList<string> requirements,
            IReadOnlyList<EventReward> rewards, IReadOnlyList<EventPenalty> penalties, string choiceChain)
        {
            Text = text;
            Tag = tag;
            ResultText = resultText;
            Requirements = AsReadOnly(requirements);
            Rewards = AsReadOnly(rewards);
            Penalties = AsReadOnly(penalties);
            ChoiceChain = choiceChain;
        }

        public string Text { get; }
        public string Tag { get; }
        public string ResultText { get; }
        public IReadOnlyList<string> Requirements { get; }
        public IReadOnlyList<EventReward> Rewards { get; }
        public IReadOnlyList<EventPenalty> Penalties { get; }
        public string ChoiceChain { get; }   // 可空：选择后触发的事件 ID

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>事件奖励。</summary>
    public sealed class EventReward
    {
        public EventReward(RewardType type, int value, string param)
        {
            Type = type;
            Value = value;
            Param = param;
        }

        public RewardType Type { get; }
        public int Value { get; }
        public string Param { get; }
    }

    /// <summary>事件惩罚。</summary>
    public sealed class EventPenalty
    {
        public EventPenalty(PenaltyType type, int value)
        {
            Type = type;
            Value = value;
        }

        public PenaltyType Type { get; }
        public int Value { get; }
    }

    /// <summary>世界观骨架（整个游戏唯一）。</summary>
    public sealed class WorldDef
    {
        public WorldDef(
            string title, string tagline, string premise, string playerConcept, string goal,
            IReadOnlyList<MapLayerDef> layers, IReadOnlyList<FactionDef> factions,
            IReadOnlyList<SpeakerDef> speakers)
        {
            Title = title;
            Tagline = tagline;
            Premise = premise;
            PlayerConcept = playerConcept;
            Goal = goal;
            Layers = AsReadOnly(layers);
            Factions = AsReadOnly(factions);
            Speakers = AsReadOnly(speakers);
        }

        public string Title { get; }
        public string Tagline { get; }
        public string Premise { get; }
        public string PlayerConcept { get; }
        public string Goal { get; }
        public IReadOnlyList<MapLayerDef> Layers { get; }
        public IReadOnlyList<FactionDef> Factions { get; }
        public IReadOnlyList<SpeakerDef> Speakers { get; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>地图层配置（包含在 WorldDef 中）。</summary>
    public sealed class MapLayerDef
    {
        public MapLayerDef(
            int index, string name, string description, int nodeCount,
            NodeDistribution nodeDistribution, string bossId, string musicKey, string ambientKey)
        {
            Index = index;
            Name = name;
            Description = description;
            NodeCount = nodeCount;
            NodeDistribution = nodeDistribution;
            BossId = bossId;
            MusicKey = musicKey;
            AmbientKey = ambientKey;
        }

        public int Index { get; }   // 1-based
        public string Name { get; }
        public string Description { get; }
        public int NodeCount { get; }
        public NodeDistribution NodeDistribution { get; }
        public string BossId { get; }
        public string MusicKey { get; }
        public string AmbientKey { get; }
    }

    /// <summary>层内节点类型分布。</summary>
    public sealed class NodeDistribution
    {
        public NodeDistribution(
            int battleCount, int eliteCount, int shopCount, int campfireCount,
            int eventCount, string randomPoolId)
        {
            BattleCount = battleCount;
            EliteCount = eliteCount;
            ShopCount = shopCount;
            CampfireCount = campfireCount;
            EventCount = eventCount;
            RandomPoolId = randomPoolId;
        }

        public int BattleCount { get; }
        public int EliteCount { get; }
        public int ShopCount { get; }
        public int CampfireCount { get; }
        public int EventCount { get; }
        public string RandomPoolId { get; }
    }

    /// <summary>势力/派系。</summary>
    public sealed class FactionDef
    {
        public FactionDef(string id, string name, string description, IReadOnlyList<FactionRelation> relations)
        {
            Id = id;
            Name = name;
            Description = description;
            Relations = AsReadOnly(relations);
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<FactionRelation> Relations { get; }

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>势力关系。</summary>
    public sealed class FactionRelation
    {
        public FactionRelation(string targetId, RelationType relation, string note)
        {
            TargetId = targetId;
            Relation = relation;
            Note = note;
        }

        public string TargetId { get; }
        public RelationType Relation { get; }
        public string Note { get; }
    }

    /// <summary>说话人/角色。</summary>
    public sealed class SpeakerDef
    {
        public SpeakerDef(
            string id, string name, string title, string description, string faction,
            IReadOnlyList<string> personalityTags, string firstMetIn)
        {
            Id = id;
            Name = name;
            Title = title;
            Description = description;
            Faction = faction;
            PersonalityTags = AsReadOnly(personalityTags);
            FirstMetIn = firstMetIn;
        }

        public string Id { get; }
        public string Name { get; }
        public string Title { get; }
        public string Description { get; }
        public string Faction { get; }   // 所属势力 ID，空串 = 无势力
        public IReadOnlyList<string> PersonalityTags { get; }
        public string FirstMetIn { get; }   // 首次出现的层名（当前为自由文本）

        private static IReadOnlyList<T> AsReadOnly<T>(IEnumerable<T> items)
        {
            return items == null ? new List<T>().AsReadOnly() : new List<T>(items).AsReadOnly();
        }
    }

    /// <summary>UI 文本散表（运行时按组/键读取）。</summary>
    public sealed class UiStrings
    {
        public UiStrings(IReadOnlyList<UiStringGroup> groups)
        {
            var all = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            var list = new List<UiStringGroup>();
            if (groups != null)
            {
                foreach (var group in groups)
                {
                    list.Add(group);
                    all[group.Id] = group.Entries;
                }
            }
            Groups = new ReadOnlyCollection<UiStringGroup>(list);
            _groups = new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(all);
        }

        public IReadOnlyList<UiStringGroup> Groups { get; }

        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _groups;

        /// <summary>按键取文本；组或键不存在返回 null。</summary>
        public string Get(string group, string key)
        {
            if (group == null || key == null) return null;
            return _groups.TryGetValue(group, out var entries) && entries.TryGetValue(key, out var value)
                ? value
                : null;
        }
    }

    /// <summary>UI 文本组（拍平后的一个组）。</summary>
    public sealed class UiStringGroup
    {
        public UiStringGroup(string id, IReadOnlyList<UiStringEntry> entries)
        {
            Id = id;
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (entries != null)
            {
                foreach (var entry in entries) map[entry.Key] = entry.Value;
            }
            Entries = new ReadOnlyDictionary<string, string>(map);
        }

        public string Id { get; }
        public IReadOnlyDictionary<string, string> Entries { get; }
    }

    /// <summary>UI 文本条目。</summary>
    public sealed class UiStringEntry
    {
        public UiStringEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Value { get; }
    }
}
