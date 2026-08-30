using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CardGame.Domain
{
    /// <summary>
    /// 内容校验：按 docs/CONTRACT.md 的字段契约执行规则，聚合全部错误。
    /// 错误归因到来源文件（每类别固定一个文件）与条目 Id。
    /// </summary>
    public static class ContentValidator
    {
        private static readonly Regex CardIdPattern = new Regex("^card_[a-z]+_[0-9]{2,}$");
        private static readonly Regex RelicIdPattern = new Regex("^relic_[a-z]+_[0-9]{2,}$");
        private static readonly Regex EnemyIdPattern = new Regex("^enemy_[a-z]+_[0-9]{2,}$");
        private static readonly Regex EventIdPattern = new Regex("^event_[a-z]+_[0-9]{2,}$");
        private static readonly Regex DialogueIdPattern = new Regex("^dialogue_[a-z]+_[0-9]{2,}$");

        /// <summary>需要 statusType 的效果动作（CONTRACT + ReduceStatusToZero 修正）。</summary>
        private static readonly HashSet<EffectAction> ActionsRequiringStatusType =
            new HashSet<EffectAction>
            {
                EffectAction.ApplyStatus,
                EffectAction.DoubleStatus,
                EffectAction.RemoveStatus,
                EffectAction.ReduceStatusToZero
            };

        public static IReadOnlyList<DataValidationError> ValidateAll(
            IReadOnlyList<CardDef> cards,
            IReadOnlyList<RelicDef> relics,
            IReadOnlyList<EnemyDef> enemies,
            IReadOnlyList<StatusDef> statuses,
            IReadOnlyList<EventDef> events,
            IReadOnlyList<DialogueSequence> dialogues,
            WorldDef world)
        {
            var sink = new DataErrorSink();

            var statusIds = IdSet(statuses, s => s.Id);
            var relicIds = IdSet(relics, r => r.Id);
            var enemyIds = IdSet(enemies, e => e.Id);
            var speakerIds = world == null ? new HashSet<string>() : IdSet(world.Speakers, s => s.Id);
            var factionIds = world == null ? new HashSet<string>() : IdSet(world.Factions, f => f.Id);

            ValidateCards(cards, sink, statusIds, relicIds);
            ValidateRelics(relics, sink, statusIds, relicIds);
            ValidateEnemies(enemies, sink, statusIds, relicIds);
            ValidateStatuses(statuses, sink, statusIds, relicIds);
            ValidateEvents(events, sink);
            ValidateDialogues(dialogues, sink, speakerIds, statusIds, relicIds);
            if (world != null)
            {
                ValidateWorld(world, sink, enemyIds, factionIds, statusIds, relicIds);
            }

            return sink.Errors;
        }

        private static void ValidateCards(
            IReadOnlyList<CardDef> cards, DataErrorSink sink,
            HashSet<string> statusIds, HashSet<string> relicIds)
        {
            var seen = new HashSet<string>();
            foreach (var card in cards)
            {
                if (!AddUnique(seen, card.Id, DtoMapper.CardsFile, sink, "id")) continue;
                if (!CardIdPattern.IsMatch(card.Id ?? ""))
                {
                    sink.Add(DtoMapper.CardsFile, card.Id, "id", "Id 格式应为 card_流派_序号（如 card_ice_01）");
                }
                if (card.Cost < 0 || card.Cost > 5)
                {
                    sink.Add(DtoMapper.CardsFile, card.Id, "cost", $"费用应在 0-5 范围内，当前 {card.Cost}");
                }
                if (card.Type != CardType.Attack && card.Damage > 0)
                {
                    sink.Add(DtoMapper.CardsFile, card.Id, "damage", "非 Attack 卡牌 Damage 应为 0");
                }
                if (card.Type != CardType.Skill && card.Block > 0)
                {
                    sink.Add(DtoMapper.CardsFile, card.Id, "block", "非 Skill 卡牌 Block 应为 0");
                }
                if (!string.IsNullOrEmpty(card.UpgradeToId) && !HasId(cards, c => c.Id, card.UpgradeToId))
                {
                    sink.Add(DtoMapper.CardsFile, card.Id, "upgradeToId", $"升级目标卡牌 \"{card.UpgradeToId}\" 不存在");
                }
                foreach (var effect in card.Effects)
                {
                    ValidateEffect(effect, DtoMapper.CardsFile, card.Id, statusIds, relicIds, sink);
                }
            }
        }

        private static void ValidateRelics(
            IReadOnlyList<RelicDef> relics, DataErrorSink sink,
            HashSet<string> statusIds, HashSet<string> relicIds)
        {
            var seen = new HashSet<string>();
            foreach (var relic in relics)
            {
                if (!AddUnique(seen, relic.Id, DtoMapper.RelicsFile, sink, "id")) continue;
                if (!RelicIdPattern.IsMatch(relic.Id ?? ""))
                {
                    sink.Add(DtoMapper.RelicsFile, relic.Id, "id", "Id 格式应为 relic_流派_序号（如 relic_ice_01）");
                }
                foreach (var effect in relic.Effects)
                {
                    ValidateEffect(effect, DtoMapper.RelicsFile, relic.Id, statusIds, relicIds, sink);
                }
            }
        }

        private static void ValidateEnemies(
            IReadOnlyList<EnemyDef> enemies, DataErrorSink sink,
            HashSet<string> statusIds, HashSet<string> relicIds)
        {
            var seen = new HashSet<string>();
            foreach (var enemy in enemies)
            {
                if (!AddUnique(seen, enemy.Id, DtoMapper.EnemiesFile, sink, "id")) continue;
                if (!EnemyIdPattern.IsMatch(enemy.Id ?? ""))
                {
                    sink.Add(DtoMapper.EnemiesFile, enemy.Id, "id", "Id 格式应为 enemy_流派_序号（如 enemy_wolf_01）");
                }
                foreach (var apply in enemy.StartingStatus)
                {
                    if (!statusIds.Contains(apply.StatusType ?? ""))
                    {
                        sink.Add(DtoMapper.EnemiesFile, enemy.Id, "startingStatus.statusType", $"状态 \"{apply.StatusType}\" 未在 status.json 中定义");
                    }
                }
                foreach (var intent in enemy.IntentPool)
                {
                    if (intent.MinRound < 1)
                    {
                        sink.Add(DtoMapper.EnemiesFile, enemy.Id, "intentPool.minRound", $"MinRound 应 ≥ 1，当前 {intent.MinRound}");
                    }
                    if (intent.Cooldown < 0)
                    {
                        sink.Add(DtoMapper.EnemiesFile, enemy.Id, "intentPool.cooldown", $"Cooldown 应 ≥ 0，当前 {intent.Cooldown}");
                    }
                    foreach (var effect in intent.Effects)
                    {
                        ValidateEffect(effect, DtoMapper.EnemiesFile, enemy.Id, statusIds, relicIds, sink);
                    }
                }
            }
        }

        private static void ValidateStatuses(
            IReadOnlyList<StatusDef> statuses, DataErrorSink sink,
            HashSet<string> statusIds, HashSet<string> relicIds)
        {
            var seen = new HashSet<string>();
            foreach (var status in statuses)
            {
                if (!AddUnique(seen, status.Id, DtoMapper.StatusFile, sink, "id")) continue;
                if (status.MaxStack < -1)
                {
                    sink.Add(DtoMapper.StatusFile, status.Id, "maxStack", $"MaxStack 应 ≥ -1（-1 = 无上限），当前 {status.MaxStack}");
                }
                if (status.DecayEachTurn < 0)
                {
                    sink.Add(DtoMapper.StatusFile, status.Id, "decayEachTurn", $"DecayEachTurn 应 ≥ 0，当前 {status.DecayEachTurn}");
                }
                foreach (var effect in status.OnApplyEffect)
                {
                    ValidateEffect(effect, DtoMapper.StatusFile, status.Id, statusIds, relicIds, sink);
                }
                foreach (var effect in status.OnTickEffect)
                {
                    ValidateEffect(effect, DtoMapper.StatusFile, status.Id, statusIds, relicIds, sink);
                }
            }
        }

        private static void ValidateEvents(IReadOnlyList<EventDef> events, DataErrorSink sink)
        {
            var seen = new HashSet<string>();
            foreach (var evt in events)
            {
                if (!AddUnique(seen, evt.Id, DtoMapper.EventsFile, sink, "id")) continue;
                if (!EventIdPattern.IsMatch(evt.Id ?? ""))
                {
                    sink.Add(DtoMapper.EventsFile, evt.Id, "id", "Id 格式应为 event_流派_序号（如 event_wounded_01）");
                }
                if (evt.Choices.Count < 1)
                {
                    sink.Add(DtoMapper.EventsFile, evt.Id, "choices", "事件至少需要 1 个选项");
                }
                foreach (var choice in evt.Choices)
                {
                    if (!string.IsNullOrEmpty(choice.ChoiceChain) && !HasId(events, e => e.Id, choice.ChoiceChain))
                    {
                        sink.Add(DtoMapper.EventsFile, evt.Id, "choices.choiceChain", $"后续事件 \"{choice.ChoiceChain}\" 不存在");
                    }
                }
            }
        }

        private static void ValidateDialogues(
            IReadOnlyList<DialogueSequence> dialogues, DataErrorSink sink,
            HashSet<string> speakerIds, HashSet<string> statusIds, HashSet<string> relicIds)
        {
            var seen = new HashSet<string>();
            foreach (var dialogue in dialogues)
            {
                if (!AddUnique(seen, dialogue.Id, DtoMapper.DialoguesFile, sink, "id")) continue;
                if (!DialogueIdPattern.IsMatch(dialogue.Id ?? ""))
                {
                    sink.Add(DtoMapper.DialoguesFile, dialogue.Id, "id", "Id 格式应为 dialogue_流派_序号（如 dialogue_tavern_01）");
                }
                for (var i = 0; i < dialogue.Lines.Count; i++)
                {
                    var line = dialogue.Lines[i];
                    var lineField = $"lines[{i}]";
                    if (!speakerIds.Contains(line.SpeakerId ?? ""))
                    {
                        sink.Add(DtoMapper.DialoguesFile, dialogue.Id, lineField + ".speakerId", $"说话人 \"{line.SpeakerId}\" 未在 world.json 的 speakers 中定义");
                    }
                    if (line.NextLineIndex < -1 || line.NextLineIndex >= dialogue.Lines.Count)
                    {
                        sink.Add(DtoMapper.DialoguesFile, dialogue.Id, lineField + ".nextLineIndex", $"跳转索引 {line.NextLineIndex} 越界（行数 {dialogue.Lines.Count}）");
                    }
                    if (line.Type == DialogueLineType.Choice && line.Choices.Count < 1)
                    {
                        sink.Add(DtoMapper.DialoguesFile, dialogue.Id, lineField + ".choices", "Choice 行至少需要 1 个选项");
                    }
                    if (line.Type != DialogueLineType.Choice && line.Choices.Count > 0)
                    {
                        sink.Add(DtoMapper.DialoguesFile, dialogue.Id, lineField + ".choices", "仅 Choice 行允许携带选项");
                    }
                    for (var j = 0; j < line.Choices.Count; j++)
                    {
                        var choice = line.Choices[j];
                        if (choice.NextLineIndex < -1 || choice.NextLineIndex >= dialogue.Lines.Count)
                        {
                            sink.Add(DtoMapper.DialoguesFile, dialogue.Id, lineField + $".choices[{j}].nextLineIndex", $"跳转索引 {choice.NextLineIndex} 越界（行数 {dialogue.Lines.Count}）");
                        }
                        foreach (var effect in choice.OnSelectEffects)
                        {
                            ValidateEffect(effect, DtoMapper.DialoguesFile, dialogue.Id, statusIds, relicIds, sink);
                        }
                    }
                    foreach (var effect in line.OnShowEffects)
                    {
                        ValidateEffect(effect, DtoMapper.DialoguesFile, dialogue.Id, statusIds, relicIds, sink);
                    }
                }
            }
        }

        private static void ValidateWorld(
            WorldDef world, DataErrorSink sink, HashSet<string> enemyIds,
            HashSet<string> factionIds, HashSet<string> statusIds, HashSet<string> relicIds)
        {
            var layerIndexes = new HashSet<int>();
            foreach (var layer in world.Layers)
            {
                if (layer.Index < 1)
                {
                    sink.Add(DtoMapper.WorldFile, layer.Name ?? "", "layers.index", $"层索引应 ≥ 1，当前 {layer.Index}");
                }
                if (!layerIndexes.Add(layer.Index))
                {
                    sink.Add(DtoMapper.WorldFile, layer.Name ?? "", "layers.index", $"层索引 {layer.Index} 重复");
                }
                if (!string.IsNullOrEmpty(layer.BossId) && !enemyIds.Contains(layer.BossId))
                {
                    sink.Add(DtoMapper.WorldFile, layer.Name ?? "", "layers.bossId", $"Boss 敌人 \"{layer.BossId}\" 不存在");
                }
            }
            var factionSeen = new HashSet<string>();
            foreach (var faction in world.Factions)
            {
                if (!AddUnique(factionSeen, faction.Id, DtoMapper.WorldFile, sink, "factions.id")) continue;
                foreach (var relation in faction.Relations)
                {
                    if (!factionIds.Contains(relation.TargetId ?? ""))
                    {
                        sink.Add(DtoMapper.WorldFile, faction.Id, "relations.targetId", $"势力 \"{relation.TargetId}\" 不存在");
                    }
                }
            }
            var speakerSeen = new HashSet<string>();
            foreach (var speaker in world.Speakers)
            {
                if (!AddUnique(speakerSeen, speaker.Id, DtoMapper.WorldFile, sink, "speakers.id")) continue;
                if (!string.IsNullOrEmpty(speaker.Faction) && !factionIds.Contains(speaker.Faction))
                {
                    sink.Add(DtoMapper.WorldFile, speaker.Id, "faction", $"所属势力 \"{speaker.Faction}\" 不存在");
                }
            }
        }

        private static void ValidateEffect(
            EffectEntry effect, string file, string id,
            HashSet<string> statusIds, HashSet<string> relicIds, DataErrorSink sink)
        {
            if (ActionsRequiringStatusType.Contains(effect.Action) && string.IsNullOrEmpty(effect.StatusType))
            {
                sink.Add(file, id, "effects.statusType", $"动作 {effect.Action} 必须填写 statusType");
            }
            if (!string.IsNullOrEmpty(effect.StatusType) && !statusIds.Contains(effect.StatusType))
            {
                sink.Add(file, id, "effects.statusType", $"状态 \"{effect.StatusType}\" 未在 status.json 中定义");
            }
            if (effect.Condition != null)
            {
                if (effect.Condition.Type == ConditionType.HasStatus
                    && !statusIds.Contains(effect.Condition.Param ?? ""))
                {
                    sink.Add(file, id, "effects.condition.param", $"条件状态 \"{effect.Condition.Param}\" 未在 status.json 中定义");
                }
                if (effect.Condition.Type == ConditionType.HasRelic
                    && !relicIds.Contains(effect.Condition.Param ?? ""))
                {
                    sink.Add(file, id, "effects.condition.param", $"条件遗物 \"{effect.Condition.Param}\" 不存在");
                }
            }
        }

        private static bool AddUnique(HashSet<string> seen, string id, string file, DataErrorSink sink, string field)
        {
            if (seen.Add(id ?? "")) return true;
            sink.Add(file, id, field, $"Id \"{id}\" 重复");
            return false;
        }

        private static HashSet<string> IdSet<T>(IReadOnlyList<T> items, System.Func<T, string> idSelector)
        {
            var set = new HashSet<string>();
            if (items == null) return set;
            foreach (var item in items)
            {
                set.Add(idSelector(item) ?? "");
            }
            return set;
        }

        private static bool HasId<T>(IReadOnlyList<T> items, System.Func<T, string> idSelector, string target)
        {
            foreach (var item in items)
            {
                if (idSelector(item) == target) return true;
            }
            return false;
        }
    }
}
