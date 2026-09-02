using System;
using System.Collections.Generic;

namespace CardGame.Domain
{
    // DTO 层：JSON 磁盘形态（JsonUtility 兼容——公开字段、无 Dictionary、枚举为字符串）。
    // JsonUtility 实际语义：缺失字符串 → null；缺失 List → 空列表；缺失嵌套类 → 实例化（字段为默认值）。
    // 映射层统一把 null/空视为“未提供”：可选 int 用哨兵值（NextLineIndex -1、MaxStack -1、Cooldown 0）；
    // 缺失 condition 以 type 非空判定（见 DtoMapper.MapEffects）。
    // 自动生成字段（CardDef.Description / EffectEntry.Description / EnemyIntent.PreviewText）不出现在 DTO。

    /// <summary>cards.json 根包装。</summary>
    [Serializable]
    public sealed class CardSetDto
    {
        public List<CardDto> cards;
    }

    [Serializable]
    public sealed class CardDto
    {
        public string id;
        public string name;
        public int cost;
        public string type;
        public string rarity;
        public string cardClass;   // 契约字段名 Class；JSON 键改 cardClass 规避 C# 关键字
        public int damage;
        public int block;
        public List<EffectDto> effects;
        public List<string> keywords;
        public string upgradeToId;
        public string flavorText;
    }

    [Serializable]
    public sealed class EffectDto
    {
        public string trigger;
        public string target;
        public string action;
        public int value;
        public string statusType;
        public ConditionDto condition;
    }

    [Serializable]
    public sealed class ConditionDto
    {
        public string type;
        public string param;
    }

    /// <summary>relics.json 根包装。</summary>
    [Serializable]
    public sealed class RelicSetDto
    {
        public List<RelicDto> relics;
    }

    [Serializable]
    public sealed class RelicDto
    {
        public string id;
        public string name;
        public string rarity;
        public string cardClass;   // 同 CardDto：契约字段名 Class
        public List<EffectDto> effects;
        public string description; // 遗物描述为手写，属于数据
        public string flavorText;
        public List<string> keywords;
    }

    /// <summary>enemies.json 根包装。</summary>
    [Serializable]
    public sealed class EnemySetDto
    {
        public List<EnemyDto> enemies;
    }

    [Serializable]
    public sealed class EnemyDto
    {
        public string id;
        public string name;
        public int maxHp;
        public List<EnemyIntentDto> intentPool;
        public List<StatusApplyDto> startingStatus;
        public List<string> keywords;
        public string flavorText;
    }

    [Serializable]
    public sealed class EnemyIntentDto
    {
        public string type;
        public int value;
        public int count;
        public List<EffectDto> effects;
        public float weight;
        public int minRound;
        public int cooldown;
        public string condition;   // 契约 §4：字符串条件（"HP&lt;50%" "HasStatus:Freeze"）
    }

    [Serializable]
    public sealed class StatusApplyDto
    {
        public string statusType;
        public int stacks;
    }

    /// <summary>status.json 根包装。</summary>
    [Serializable]
    public sealed class StatusSetDto
    {
        public List<StatusDto> statuses;
    }

    [Serializable]
    public sealed class StatusDto
    {
        public string id;
        public string name;
        public string iconKey;
        public int maxStack;       // -1 = 无上限
        public int decayEachTurn;  // 0 = 不衰减
        public string effectDesc;
        public List<EffectDto> onApplyEffect;
        public List<EffectDto> onTickEffect;
        public string colorHex;
    }

    /// <summary>dialogues.json 根包装。</summary>
    [Serializable]
    public sealed class DialogueSetDto
    {
        public List<DialogueSequenceDto> dialogues;
    }

    [Serializable]
    public sealed class DialogueSequenceDto
    {
        public string id;
        public string title;
        public List<DialogueLineDto> lines;
    }

    [Serializable]
    public sealed class DialogueLineDto
    {
        public string speakerId;
        public string speakerName; // 可空：空则从 SpeakerDef 取
        public string portrait;
        public string emotion;
        public string text;
        public string type;
        public List<DialogueChoiceDto> choices;
        public int nextLineIndex;  // -1 = 结束对话
        public float autoDelay;    // AutoAdvance 停留秒数
        public List<EffectDto> onShowEffects;
    }

    [Serializable]
    public sealed class DialogueChoiceDto
    {
        public string text;
        public string tag;
        public int nextLineIndex;  // -1 = 结束对话
        public string condition;
        public string resultText;
        public List<EffectDto> onSelectEffects;
    }

    /// <summary>events.json 根包装。</summary>
    [Serializable]
    public sealed class EventSetDto
    {
        public List<EventDto> events;
    }

    [Serializable]
    public sealed class EventDto
    {
        public string id;
        public string title;
        public string description;
        public string imageKey;
        public bool isRepeatable;
        public List<string> requirements;
        public List<EventChoiceDto> choices;
    }

    [Serializable]
    public sealed class EventChoiceDto
    {
        public string text;
        public string tag;
        public string resultText;
        public List<string> requirements;
        public List<EventRewardDto> rewards;
        public List<EventPenaltyDto> penalties;
        public string choiceChain;
    }

    [Serializable]
    public sealed class EventRewardDto
    {
        public string type;
        public int value;
        public string param;
    }

    [Serializable]
    public sealed class EventPenaltyDto
    {
        public string type;
        public int value;
    }

    /// <summary>world.json 根（WorldDef 全局唯一，无包装）。</summary>
    [Serializable]
    public sealed class WorldDto
    {
        public string title;
        public string tagline;
        public string premise;
        public string playerConcept;
        public string goal;
        public List<MapLayerDto> layers;
        public List<FactionDto> factions;
        public List<SpeakerDto> speakers;
    }

    [Serializable]
    public sealed class MapLayerDto
    {
        public int index;   // 1-based
        public string name;
        public string description;
        public int nodeCount;
        public NodeDistributionDto nodeDistribution;
        public string bossId;
        public string musicKey;
        public string ambientKey;
    }

    [Serializable]
    public sealed class NodeDistributionDto
    {
        public int battleCount;
        public int eliteCount;
        public int shopCount;
        public int campfireCount;
        public int eventCount;
        public string randomPoolId;
    }

    [Serializable]
    public sealed class FactionDto
    {
        public string id;
        public string name;
        public string description;
        public List<FactionRelationDto> relations;
    }

    [Serializable]
    public sealed class FactionRelationDto
    {
        public string targetId;
        public string relation;
        public string note;
    }

    [Serializable]
    public sealed class SpeakerDto
    {
        public string id;
        public string name;
        public string title;
        public string description;
        public string faction;
        public List<string> personalityTags;
        public string firstMetIn;
    }

    /// <summary>ui-strings.json 根（拍平形状：组 → 键值条目；协作者直接手编）。</summary>
    [Serializable]
    public sealed class UiStringsRootDto
    {
        public List<UiStringGroupDto> groups;
    }

    [Serializable]
    public sealed class UiStringGroupDto
    {
        public string id;
        public List<UiStringEntryDto> entries;
    }

    [Serializable]
    public sealed class UiStringEntryDto
    {
        public string key;
        public string value;
    }
}
