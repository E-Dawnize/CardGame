using System.Collections.Generic;
using CardGame.Domain;
using NUnit.Framework;

namespace CardGame.Tests.EditMode
{
    public sealed class DescriptionBuilderTests
    {
        private static readonly IReadOnlyDictionary<string, StatusDef> Statuses =
            new Dictionary<string, StatusDef>
            {
                { "Freeze", DataTestFactory.Status("Freeze", "冻结") }
            };

        private static readonly IReadOnlyDictionary<string, RelicDef> Relics =
            new Dictionary<string, RelicDef>
            {
                { "relic_ice_01", DataTestFactory.Relic("relic_ice_01") }
            };

        private static string Describe(CardDef card)
        {
            return DescriptionBuilder.BuildCardDescription(card, Statuses, Relics);
        }

        [Test]
        public void AttackCard_UsesContractTemplate()
        {
            var card = DataTestFactory.Card("card_fire_01", CardType.Attack, damage: 9, cost: 2);

            Assert.That(Describe(card), Is.EqualTo("2费。造成9点伤害。"));
        }

        [Test]
        public void SkillWithBlock_WritesBlockOnly()
        {
            var card = DataTestFactory.Card("card_neutral_01", CardType.Skill, damage: 0, block: 5, cost: 1);

            Assert.That(Describe(card), Is.EqualTo("1费。获得5点格挡。"));
        }

        [Test]
        public void PowerCard_WritesEffectsOnly()
        {
            var card = DataTestFactory.Card("card_p_01", CardType.Power, damage: 0, block: 0, cost: 1,
                effects: new List<EffectEntry>
                {
                    DataTestFactory.Effect(EffectAction.GainBlock, value: 4)
                });

            Assert.That(Describe(card), Is.EqualTo("1费。获得 4 点格挡。"));
        }

        [Test]
        public void ConditionalEffect_UsesConditionPrefix()
        {
            var card = DataTestFactory.Card("card_ice_02", CardType.Attack, damage: 4, cost: 2,
                effects: new List<EffectEntry>
                {
                    DataTestFactory.Effect(EffectAction.DealDamage, value: 8,
                        condition: new ConditionEntry(ConditionType.HasStatus, "Freeze"))
                });

            Assert.That(
                Describe(card),
                Is.EqualTo("2费。造成4点伤害。如果目标有冻结，造成 8 点伤害。"));
        }

        [Test]
        public void ApplyStatus_UsesStatusDisplayName()
        {
            var effect = DataTestFactory.Effect(EffectAction.ApplyStatus, value: 2, statusType: "Freeze");

            Assert.That(
                DescriptionBuilder.DescribeEffect(effect, Statuses, Relics),
                Is.EqualTo("施加 2 层 冻结"));
        }

        [Test]
        public void IntentAttackMultiHit_Formats8x2()
        {
            var intent = DataTestFactory.Intent(IntentType.Attack, value: 8, count: 2);

            Assert.That(DescriptionBuilder.BuildIntentPreview(intent), Is.EqualTo("攻击 8×2"));
        }

        [Test]
        public void IntentAttackZeroDamageWithEffects_FormatsCast()
        {
            var intent = DataTestFactory.Intent(IntentType.Attack, value: 0,
                effects: new List<EffectEntry> { DataTestFactory.Effect() });

            Assert.That(DescriptionBuilder.BuildIntentPreview(intent), Is.EqualTo("施法"));
        }

        [Test]
        public void IntentDefend_FormatsValue()
        {
            var intent = DataTestFactory.Intent(IntentType.Defend, value: 8);

            Assert.That(DescriptionBuilder.BuildIntentPreview(intent), Is.EqualTo("防御 8"));
        }

        [Test]
        public void CardDescription_IsExposedOnCardDef()
        {
            var card = DataTestFactory.Card("card_fire_01", CardType.Attack, damage: 9, cost: 2);

            DescriptionBuilder.FillCardDescriptions(
                new List<CardDef> { card }, Statuses, Relics);

            Assert.That(card.Description, Is.EqualTo("2费。造成9点伤害。"));
        }
    }
}
