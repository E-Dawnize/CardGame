using System;
using System.Collections.Generic;

namespace CardGame.Domain
{
    /// <summary>
    /// DTO → 类型化模型映射。聚合全部错误（不 fail-fast）；
    /// 未知枚举记录错误并使用回退值，保证后续条目继续映射。
    /// </summary>
    public static class DtoMapper
    {
        // 文件清单（固定文件名 → 类别），加载器与映射器共用约定。
        public const string CardsFile = "cards.json";
        public const string RelicsFile = "relics.json";
        public const string EnemiesFile = "enemies.json";
        public const string StatusFile = "status.json";
        public const string EventsFile = "events.json";
        public const string DialoguesFile = "dialogues.json";
        public const string WorldFile = "world.json";
        public const string UiStringsFile = "ui-strings.json";

        public static List<CardDef> MapCardSet(CardSetDto dto, DataErrorSink sink)
        {
            var result = new List<CardDef>();
            if (dto?.cards == null) return result;
            foreach (var card in dto.cards)
            {
                if (card == null)
                {
                    sink.Add(CardsFile, "", "cards", "条目为 null");
                    continue;
                }
                result.Add(MapCard(card, sink));
            }
            return result;
        }

        public static List<RelicDef> MapRelicSet(RelicSetDto dto, DataErrorSink sink)
        {
            var result = new List<RelicDef>();
            if (dto?.relics == null) return result;
            foreach (var relic in dto.relics)
            {
                if (relic == null)
                {
                    sink.Add(RelicsFile, "", "relics", "条目为 null");
                    continue;
                }
                result.Add(MapRelic(relic, sink));
            }
            return result;
        }

        public static List<EnemyDef> MapEnemySet(EnemySetDto dto, DataErrorSink sink)
        {
            var result = new List<EnemyDef>();
            if (dto?.enemies == null) return result;
            foreach (var enemy in dto.enemies)
            {
                if (enemy == null)
                {
                    sink.Add(EnemiesFile, "", "enemies", "条目为 null");
                    continue;
                }
                result.Add(MapEnemy(enemy, sink));
            }
            return result;
        }

        public static List<StatusDef> MapStatusSet(StatusSetDto dto, DataErrorSink sink)
        {
            var result = new List<StatusDef>();
            if (dto?.statuses == null) return result;
            foreach (var status in dto.statuses)
            {
                if (status == null)
                {
                    sink.Add(StatusFile, "", "statuses", "条目为 null");
                    continue;
                }
                result.Add(MapStatus(status, sink));
            }
            return result;
        }

        public static List<DialogueSequence> MapDialogueSet(DialogueSetDto dto, DataErrorSink sink)
        {
            var result = new List<DialogueSequence>();
            if (dto?.dialogues == null) return result;
            foreach (var dialogue in dto.dialogues)
            {
                if (dialogue == null)
                {
                    sink.Add(DialoguesFile, "", "dialogues", "条目为 null");
                    continue;
                }
                result.Add(MapDialogueSequence(dialogue, sink));
            }
            return result;
        }

        public static List<EventDef> MapEventSet(EventSetDto dto, DataErrorSink sink)
        {
            var result = new List<EventDef>();
            if (dto?.events == null) return result;
            foreach (var evt in dto.events)
            {
                if (evt == null)
                {
                    sink.Add(EventsFile, "", "events", "条目为 null");
                    continue;
                }
                result.Add(MapEvent(evt, sink));
            }
            return result;
        }

        public static WorldDef MapWorld(WorldDto dto, DataErrorSink sink)
        {
            if (dto == null) return null;
            var layers = new List<MapLayerDef>();
            if (dto.layers != null)
            {
                foreach (var layer in dto.layers)
                {
                    if (layer == null)
                    {
                        sink.Add(WorldFile, "", "layers", "条目为 null");
                        continue;
                    }
                    layers.Add(MapLayer(layer, sink));
                }
            }
            var factions = new List<FactionDef>();
            if (dto.factions != null)
            {
                foreach (var faction in dto.factions)
                {
                    if (faction == null)
                    {
                        sink.Add(WorldFile, "", "factions", "条目为 null");
                        continue;
                    }
                    factions.Add(MapFaction(faction, sink));
                }
            }
            var speakers = new List<SpeakerDef>();
            if (dto.speakers != null)
            {
                foreach (var speaker in dto.speakers)
                {
                    if (speaker == null)
                    {
                        sink.Add(WorldFile, "", "speakers", "条目为 null");
                        continue;
                    }
                    speakers.Add(MapSpeaker(speaker, sink));
                }
            }
            return new WorldDef(
                dto.title, dto.tagline, dto.premise, dto.playerConcept, dto.goal,
                layers, factions, speakers);
        }

        public static UiStrings MapUiStrings(UiStringsRootDto dto, DataErrorSink sink)
        {
            var groups = new List<UiStringGroup>();
            if (dto?.groups == null) return new UiStrings(groups);
            foreach (var group in dto.groups)
            {
                if (group == null)
                {
                    sink.Add(UiStringsFile, "", "groups", "条目为 null");
                    continue;
                }
                var entries = new List<UiStringEntry>();
                if (group.entries != null)
                {
                    foreach (var entry in group.entries)
                    {
                        if (entry == null)
                        {
                            sink.Add(UiStringsFile, group.id, "entries", "条目为 null");
                            continue;
                        }
                        entries.Add(new UiStringEntry(entry.key, entry.value));
                    }
                }
                groups.Add(new UiStringGroup(group.id, entries));
            }
            return new UiStrings(groups);
        }

        private static CardDef MapCard(CardDto dto, DataErrorSink sink)
        {
            var id = dto.id ?? "";
            return new CardDef(
                id,
                dto.name,
                dto.cost,
                Parse(EnumMaps.CardTypeMap, dto.type, CardsFile, id, "type", CardType.Attack, sink),
                Parse(EnumMaps.CardRarityMap, dto.rarity, CardsFile, id, "rarity", CardRarity.Common, sink),
                dto.cardClass,
                dto.damage,
                dto.block,
                MapEffects(dto.effects, CardsFile, id, sink),
                EmptyIfNull(dto.keywords),
                dto.upgradeToId,
                dto.flavorText);
        }

        private static RelicDef MapRelic(RelicDto dto, DataErrorSink sink)
        {
            var id = dto.id ?? "";
            return new RelicDef(
                id,
                dto.name,
                Parse(EnumMaps.RelicRarityMap, dto.rarity, RelicsFile, id, "rarity", RelicRarity.Common, sink),
                dto.cardClass,
                MapEffects(dto.effects, RelicsFile, id, sink),
                dto.description,
                dto.flavorText,
                EmptyIfNull(dto.keywords));
        }

        private static EnemyDef MapEnemy(EnemyDto dto, DataErrorSink sink)
        {
            var id = dto.id ?? "";
            var intents = new List<EnemyIntent>();
            if (dto.intentPool != null)
            {
                foreach (var intent in dto.intentPool)
                {
                    if (intent == null)
                    {
                        sink.Add(EnemiesFile, id, "intentPool", "条目为 null");
                        continue;
                    }
                    intents.Add(MapIntent(intent, id, sink));
                }
            }
            var starts = new List<StatusApply>();
            if (dto.startingStatus != null)
            {
                foreach (var apply in dto.startingStatus)
                {
                    if (apply == null)
                    {
                        sink.Add(EnemiesFile, id, "startingStatus", "条目为 null");
                        continue;
                    }
                    starts.Add(new StatusApply(apply.statusType, apply.stacks));
                }
            }
            return new EnemyDef(
                id, dto.name, dto.maxHp, intents, starts, EmptyIfNull(dto.keywords), dto.flavorText);
        }

        private static EnemyIntent MapIntent(EnemyIntentDto dto, string enemyId, DataErrorSink sink)
        {
            return new EnemyIntent(
                Parse(EnumMaps.IntentTypeMap, dto.type, EnemiesFile, enemyId, "intentPool.type", IntentType.Attack, sink),
                dto.value,
                dto.count,
                MapEffects(dto.effects, EnemiesFile, enemyId, sink),
                dto.weight,
                dto.minRound,
                dto.cooldown,
                dto.condition);
        }

        private static StatusDef MapStatus(StatusDto dto, DataErrorSink sink)
        {
            var id = dto.id ?? "";
            return new StatusDef(
                id,
                dto.name,
                dto.iconKey,
                dto.maxStack,
                dto.decayEachTurn,
                dto.effectDesc,
                MapEffects(dto.onApplyEffect, StatusFile, id, sink),
                MapEffects(dto.onTickEffect, StatusFile, id, sink),
                dto.colorHex);
        }

        private static DialogueSequence MapDialogueSequence(DialogueSequenceDto dto, DataErrorSink sink)
        {
            var id = dto.id ?? "";
            var lines = new List<DialogueLine>();
            if (dto.lines != null)
            {
                foreach (var line in dto.lines)
                {
                    if (line == null)
                    {
                        sink.Add(DialoguesFile, id, "lines", "条目为 null");
                        continue;
                    }
                    lines.Add(MapDialogueLine(line, id, sink));
                }
            }
            return new DialogueSequence(id, dto.title, lines);
        }

        private static DialogueLine MapDialogueLine(DialogueLineDto dto, string dialogueId, DataErrorSink sink)
        {
            var choices = new List<DialogueChoice>();
            if (dto.choices != null)
            {
                foreach (var choice in dto.choices)
                {
                    if (choice == null)
                    {
                        sink.Add(DialoguesFile, dialogueId, "choices", "条目为 null");
                        continue;
                    }
                    choices.Add(new DialogueChoice(
                        choice.text,
                        choice.tag,
                        choice.nextLineIndex,
                        choice.condition,
                        choice.resultText,
                        MapEffects(choice.onSelectEffects, DialoguesFile, dialogueId, sink)));
                }
            }
            return new DialogueLine(
                dto.speakerId,
                dto.speakerName,
                Parse(EnumMaps.PortraitPositionMap, dto.portrait, DialoguesFile, dialogueId, "portrait", PortraitPosition.Left, sink),
                dto.emotion,
                dto.text,
                Parse(EnumMaps.DialogueLineTypeMap, dto.type, DialoguesFile, dialogueId, "type", DialogueLineType.Normal, sink),
                choices,
                dto.nextLineIndex,
                dto.autoDelay,
                MapEffects(dto.onShowEffects, DialoguesFile, dialogueId, sink));
        }

        private static EventDef MapEvent(EventDto dto, DataErrorSink sink)
        {
            var id = dto.id ?? "";
            var choices = new List<EventChoice>();
            if (dto.choices != null)
            {
                foreach (var choice in dto.choices)
                {
                    if (choice == null)
                    {
                        sink.Add(EventsFile, id, "choices", "条目为 null");
                        continue;
                    }
                    var rewards = new List<EventReward>();
                    if (choice.rewards != null)
                    {
                        foreach (var reward in choice.rewards)
                        {
                            if (reward == null)
                            {
                                sink.Add(EventsFile, id, "rewards", "条目为 null");
                                continue;
                            }
                            rewards.Add(new EventReward(
                                Parse(EnumMaps.RewardTypeMap, reward.type, EventsFile, id, "rewards.type", RewardType.Gold, sink),
                                reward.value,
                                reward.param));
                        }
                    }
                    var penalties = new List<EventPenalty>();
                    if (choice.penalties != null)
                    {
                        foreach (var penalty in choice.penalties)
                        {
                            if (penalty == null)
                            {
                                sink.Add(EventsFile, id, "penalties", "条目为 null");
                                continue;
                            }
                            penalties.Add(new EventPenalty(
                                Parse(EnumMaps.PenaltyTypeMap, penalty.type, EventsFile, id, "penalties.type", PenaltyType.LoseGold, sink),
                                penalty.value));
                        }
                    }
                    choices.Add(new EventChoice(
                        choice.text,
                        choice.tag,
                        choice.resultText,
                        EmptyIfNull(choice.requirements),
                        rewards,
                        penalties,
                        choice.choiceChain));
                }
            }
            return new EventDef(
                id, dto.title, dto.description, dto.imageKey, dto.isRepeatable,
                EmptyIfNull(dto.requirements), choices);
        }

        private static MapLayerDef MapLayer(MapLayerDto dto, DataErrorSink sink)
        {
            NodeDistribution distribution = null;
            if (dto.nodeDistribution != null)
            {
                distribution = new NodeDistribution(
                    dto.nodeDistribution.battleCount,
                    dto.nodeDistribution.eliteCount,
                    dto.nodeDistribution.shopCount,
                    dto.nodeDistribution.campfireCount,
                    dto.nodeDistribution.eventCount,
                    dto.nodeDistribution.randomPoolId);
            }
            return new MapLayerDef(
                dto.index, dto.name, dto.description, dto.nodeCount, distribution,
                dto.bossId, dto.musicKey, dto.ambientKey);
        }

        private static FactionDef MapFaction(FactionDto dto, DataErrorSink sink)
        {
            var id = dto.id ?? "";
            var relations = new List<FactionRelation>();
            if (dto.relations != null)
            {
                foreach (var relation in dto.relations)
                {
                    if (relation == null)
                    {
                        sink.Add(WorldFile, id, "relations", "条目为 null");
                        continue;
                    }
                    relations.Add(new FactionRelation(
                        relation.targetId,
                        Parse(EnumMaps.RelationTypeMap, relation.relation, WorldFile, id, "relations.relation", RelationType.Neutral, sink),
                        relation.note));
                }
            }
            return new FactionDef(id, dto.name, dto.description, relations);
        }

        private static SpeakerDef MapSpeaker(SpeakerDto dto, DataErrorSink sink)
        {
            return new SpeakerDef(
                dto.id,
                dto.name,
                dto.title,
                dto.description,
                dto.faction,
                EmptyIfNull(dto.personalityTags),
                dto.firstMetIn);
        }

        private static List<EffectEntry> MapEffects(List<EffectDto> dtos, string file, string id, DataErrorSink sink)
        {
            var result = new List<EffectEntry>();
            if (dtos == null) return result;
            foreach (var dto in dtos)
            {
                if (dto == null)
                {
                    sink.Add(file, id, "effects", "条目为 null");
                    continue;
                }
                ConditionEntry condition = null;
                // JsonUtility 在 JSON 缺失 condition 键时也会实例化嵌套类（type 为空串），
                // 因此必须以 type 非空判定“确实声明了条件”，避免幻影条件与误报错误。
                if (dto.condition != null && !string.IsNullOrEmpty(dto.condition.type))
                {
                    condition = new ConditionEntry(
                        Parse(EnumMaps.ConditionTypeMap, dto.condition.type, file, id, "condition.type", ConditionType.HasStatus, sink),
                        dto.condition.param);
                }
                result.Add(new EffectEntry(
                    Parse(EnumMaps.EffectTriggerMap, dto.trigger, file, id, "trigger", EffectTrigger.OnPlay, sink),
                    Parse(EnumMaps.EffectTargetMap, dto.target, file, id, "target", EffectTarget.Self, sink),
                    Parse(EnumMaps.EffectActionMap, dto.action, file, id, "action", EffectAction.DealDamage, sink),
                    dto.value,
                    dto.statusType,
                    condition));
            }
            return result;
        }

        private static T Parse<T>(
            IReadOnlyDictionary<string, T> map, string raw,
            string file, string id, string field, T fallback, DataErrorSink sink)
            where T : struct, Enum
        {
            if (EnumMaps.TryParse(map, raw, out var value)) return value;
            sink.Add(file, id, field, $"未知枚举值 \"{raw}\"");
            return fallback;
        }

        private static List<string> EmptyIfNull(List<string> items)
        {
            return items ?? new List<string>();
        }
    }
}
