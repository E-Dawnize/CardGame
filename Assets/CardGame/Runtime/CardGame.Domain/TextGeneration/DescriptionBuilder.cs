using System.Collections.Generic;

namespace CardGame.Domain
{
    /// <summary>
    /// 卡牌描述与敌人意图预览的自动生成（策划不填）。
    /// 模板以 docs/CONTRACT.md §1/§2/§4 为契约；缺口动作的暂定模板见本类注释与 CONTRACT「协议落地」。
    /// </summary>
    public static class DescriptionBuilder
    {
        /// <summary>填充所有卡牌（含其效果）的描述。</summary>
        public static void FillCardDescriptions(
            IReadOnlyList<CardDef> cards,
            IReadOnlyDictionary<string, StatusDef> statuses,
            IReadOnlyDictionary<string, RelicDef> relics)
        {
            foreach (var card in cards)
            {
                card.Description = BuildCardDescription(card, statuses, relics);
            }
        }

        /// <summary>填充所有敌人意图的预览文本。</summary>
        public static void FillIntentPreviews(IReadOnlyList<EnemyDef> enemies)
        {
            foreach (var enemy in enemies)
            {
                foreach (var intent in enemy.IntentPool)
                {
                    intent.PreviewText = BuildIntentPreview(intent);
                }
            }
        }

        /// <summary>契约模板："{Cost}费。{Type 分支}。{各 Effect.Description}。"</summary>
        public static string BuildCardDescription(
            CardDef card,
            IReadOnlyDictionary<string, StatusDef> statuses,
            IReadOnlyDictionary<string, RelicDef> relics)
        {
            var parts = new List<string> { $"{card.Cost}费" };

            switch (card.Type)
            {
                case CardType.Attack:
                    parts.Add($"造成{card.Damage}点伤害");
                    break;
                case CardType.Skill:
                    if (card.Damage > 0) parts.Add($"造成{card.Damage}点伤害");
                    if (card.Block > 0) parts.Add($"获得{card.Block}点格挡");
                    break;
                case CardType.Power:
                    break;   // 无 Damage/Block 文本，仅效果描述
            }

            foreach (var effect in card.Effects)
            {
                var text = DescribeEffect(effect, statuses, relics);
                if (!string.IsNullOrEmpty(text)) parts.Add(text);
            }

            return string.Join("。", parts) + "。";
        }

        public static string DescribeEffect(
            EffectEntry effect,
            IReadOnlyDictionary<string, StatusDef> statuses,
            IReadOnlyDictionary<string, RelicDef> relics)
        {
            var text = DescribeAction(effect, statuses);
            if (effect.Condition != null)
            {
                var condition = DescribeCondition(effect.Condition, statuses, relics);
                if (!string.IsNullOrEmpty(condition))
                {
                    text = $"如果{condition}，{text}";
                }
            }
            return text;
        }

        public static string DescribeCondition(
            ConditionEntry condition,
            IReadOnlyDictionary<string, StatusDef> statuses,
            IReadOnlyDictionary<string, RelicDef> relics)
        {
            var param = condition.Param ?? "";
            switch (condition.Type)
            {
                case ConditionType.HasStatus:
                    return $"目标有{StatusName(param, statuses)}";
                case ConditionType.HpBelow:
                    return $"生命低于 {param}";
                case ConditionType.HpAbovePercent:
                    return $"生命高于 {param}%";
                case ConditionType.HasRelic:
                    return $"拥有遗物 {RelicName(param, relics)}";
                case ConditionType.CardPlayedThisTurnCount:
                    return $"本回合已打出 {param} 张牌";
                case ConditionType.EnemyCountAbove:
                    return $"敌人数多于 {param}";
                case ConditionType.NoEnemies:
                    return "没有敌人";
                case ConditionType.LastCardWas:
                    return $"上一张牌是 {param}";
                case ConditionType.PlayerHpLostThisCombat:
                    return $"本场战斗已损失 {param} 点生命";
                default:
                    return param;
            }
        }

        /// <summary>契约 §4 模板：攻击 8×2 / 攻击 8 / 施法 / 防御 8 / 强化 / 削弱 / 召唤 / 特殊行动。</summary>
        public static string BuildIntentPreview(EnemyIntent intent)
        {
            switch (intent.Type)
            {
                case IntentType.Attack:
                    if (intent.Value > 0)
                    {
                        return intent.Count > 1
                            ? $"攻击 {intent.Value}×{intent.Count}"
                            : $"攻击 {intent.Value}";
                    }
                    return intent.Effects.Count > 0 ? "施法" : "攻击";
                case IntentType.Defend:
                    return $"防御 {intent.Value}";
                case IntentType.Buff:
                    return "强化";
                case IntentType.Debuff:
                    return "削弱";
                case IntentType.Summon:
                    return "召唤";
                case IntentType.Special:
                    return "特殊行动";
                default:
                    return "";
            }
        }

        private static string DescribeAction(EffectEntry effect, IReadOnlyDictionary<string, StatusDef> statuses)
        {
            var value = effect.Value;
            var status = StatusName(effect.StatusType, statuses);
            switch (effect.Action)
            {
                case EffectAction.DealDamage:
                    return $"造成 {value} 点伤害";
                case EffectAction.GainBlock:
                    return $"获得 {value} 点格挡";
                case EffectAction.Heal:
                    return $"回复 {value} 点生命";
                case EffectAction.DrawCards:
                    return $"抽 {value} 张牌";
                case EffectAction.DiscardCards:
                    return $"弃 {value} 张牌";
                case EffectAction.ApplyStatus:
                    return $"施加 {value} 层 {status}";
                case EffectAction.RemoveStatus:
                    return $"移除 {value} 层 {status}";
                case EffectAction.DoubleStatus:
                    return $"{status} 层数翻倍";
                case EffectAction.ReduceStatusToZero:   // 暂定模板（CONTRACT 无）
                    return $"{status} 层数清零";
                case EffectAction.ExhaustCard:
                    return "消耗";
                case EffectAction.ExhaustRandomCard:    // 暂定模板（CONTRACT 无）
                    return "消耗一张随机手牌";
                case EffectAction.ReduceCost:
                    return $"本场战斗费用 -{value}";
                case EffectAction.ReduceAllCostsInHand: // 暂定模板（CONTRACT 无）
                    return $"本回合手牌费用 -{value}";
                case EffectAction.RandomUpgrade:
                    return "随机升级手牌中一张牌";
                case EffectAction.SpawnCopy:
                    return "将一张复制加入手牌";
                case EffectAction.RepeatLastCard:       // 暂定模板（CONTRACT 无）
                    return "重复打出上一张牌";
                case EffectAction.NextTurnDraw:
                    return $"下回合额外抽 {value} 张";
                case EffectAction.NextTurnEnergy:
                    return $"下回合获得 {value} 点能量";
                case EffectAction.GainGold:             // 暂定模板（CONTRACT 无）
                    return $"获得 {value} 金币";
                case EffectAction.Lifesteal:            // 暂定模板（CONTRACT 无）
                    return "本次伤害回复等量生命";
                case EffectAction.RetainCard:           // 暂定模板（CONTRACT 无）
                    return "保留手牌";
                default:
                    return "";
            }
        }

        private static string StatusName(string id, IReadOnlyDictionary<string, StatusDef> statuses)
        {
            if (!string.IsNullOrEmpty(id)
                && statuses.TryGetValue(id, out var status)
                && !string.IsNullOrEmpty(status.Name))
            {
                return status.Name;
            }
            return id ?? "";
        }

        private static string RelicName(string id, IReadOnlyDictionary<string, RelicDef> relics)
        {
            if (!string.IsNullOrEmpty(id)
                && relics.TryGetValue(id, out var relic)
                && !string.IsNullOrEmpty(relic.Name))
            {
                return relic.Name;
            }
            return id ?? "";
        }
    }
}
