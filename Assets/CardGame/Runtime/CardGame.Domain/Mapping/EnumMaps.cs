using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CardGame.Domain
{
    /// <summary>
    /// JSON 字符串枚举 → 类型化枚举的映射表（大小写敏感、精确匹配）。
    /// 未知值由调用方记录为带上下文的校验错误，不在此处抛出。
    /// </summary>
    public static class EnumMaps
    {
        public static readonly IReadOnlyDictionary<string, CardType> CardTypeMap = Build<CardType>();
        public static readonly IReadOnlyDictionary<string, CardRarity> CardRarityMap = Build<CardRarity>();
        public static readonly IReadOnlyDictionary<string, RelicRarity> RelicRarityMap = Build<RelicRarity>();
        public static readonly IReadOnlyDictionary<string, EffectTrigger> EffectTriggerMap = Build<EffectTrigger>();
        public static readonly IReadOnlyDictionary<string, EffectTarget> EffectTargetMap = Build<EffectTarget>();
        public static readonly IReadOnlyDictionary<string, EffectAction> EffectActionMap = Build<EffectAction>();
        public static readonly IReadOnlyDictionary<string, ConditionType> ConditionTypeMap = Build<ConditionType>();
        public static readonly IReadOnlyDictionary<string, IntentType> IntentTypeMap = Build<IntentType>();
        public static readonly IReadOnlyDictionary<string, RewardType> RewardTypeMap = Build<RewardType>();
        public static readonly IReadOnlyDictionary<string, PenaltyType> PenaltyTypeMap = Build<PenaltyType>();
        public static readonly IReadOnlyDictionary<string, PortraitPosition> PortraitPositionMap = Build<PortraitPosition>();
        public static readonly IReadOnlyDictionary<string, DialogueLineType> DialogueLineTypeMap = Build<DialogueLineType>();
        public static readonly IReadOnlyDictionary<string, RelationType> RelationTypeMap = Build<RelationType>();

        /// <summary>尝试解析；未知或 null 值返回 false。</summary>
        public static bool TryParse<T>(IReadOnlyDictionary<string, T> map, string value, out T result)
            where T : struct, Enum
        {
            return map.TryGetValue(value ?? string.Empty, out result);
        }

        private static IReadOnlyDictionary<string, T> Build<T>() where T : struct, Enum
        {
            var map = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                map[value.ToString()] = value;
            }
            return new ReadOnlyDictionary<string, T>(map);
        }
    }
}
