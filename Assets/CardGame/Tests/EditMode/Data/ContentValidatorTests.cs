using System.Collections.Generic;
using CardGame.Domain;
using NUnit.Framework;

namespace CardGame.Tests.EditMode
{
    public sealed class ContentValidatorTests
    {
        private static IReadOnlyList<DataValidationError> Validate(
            IReadOnlyList<CardDef> cards = null,
            IReadOnlyList<RelicDef> relics = null,
            IReadOnlyList<EnemyDef> enemies = null,
            IReadOnlyList<StatusDef> statuses = null,
            IReadOnlyList<EventDef> events = null,
            IReadOnlyList<DialogueSequence> dialogues = null,
            WorldDef world = null)
        {
            return ContentValidator.ValidateAll(
                cards ?? new List<CardDef>(),
                relics ?? new List<RelicDef>(),
                enemies ?? new List<EnemyDef>(),
                statuses ?? new List<StatusDef>(),
                events ?? new List<EventDef>(),
                dialogues ?? new List<DialogueSequence>(),
                world ?? DataTestFactory.World());
        }

        [Test]
        public void DuplicateCardIds_Reported()
        {
            var errors = Validate(cards: new List<CardDef>
            {
                DataTestFactory.Card("card_ice_01"),
                DataTestFactory.Card("card_ice_01")
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Id == "card_ice_01" && e.Field == "id" && e.Message.Contains("重复")));
        }

        [Test]
        public void DanglingUpgradeToId_Reported()
        {
            var errors = Validate(cards: new List<CardDef>
            {
                DataTestFactory.Card("card_ice_01", upgradeToId: "card_zz_99")
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "upgradeToId" && e.Message.Contains("card_zz_99")));
        }

        [Test]
        public void UnknownEffectStatusType_Reported()
        {
            var errors = Validate(cards: new List<CardDef>
            {
                DataTestFactory.Card(effects: new List<EffectEntry>
                {
                    DataTestFactory.Effect(EffectAction.ApplyStatus, statusType: "Freeze")
                })
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "effects.statusType" && e.Message.Contains("Freeze")));
        }

        [Test]
        public void ApplyStatusWithoutStatusType_Reported()
        {
            var errors = Validate(cards: new List<CardDef>
            {
                DataTestFactory.Card(effects: new List<EffectEntry>
                {
                    DataTestFactory.Effect(EffectAction.ApplyStatus)
                })
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "effects.statusType" && e.Message.Contains("必须填写")));
        }

        [Test]
        public void UnknownSpeakerId_Reported()
        {
            var errors = Validate(
                dialogues: new List<DialogueSequence>
                {
                    DataTestFactory.Dialogue(lines: new List<DialogueLine>
                    {
                        DataTestFactory.Line(speakerId: "ghost")
                    })
                });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "lines[0].speakerId" && e.Message.Contains("ghost")));
        }

        [Test]
        public void DialogueNextLineIndexOutOfBounds_Reported()
        {
            var errors = Validate(
                dialogues: new List<DialogueSequence>
                {
                    DataTestFactory.Dialogue(lines: new List<DialogueLine>
                    {
                        DataTestFactory.Line(nextLineIndex: 5)
                    })
                });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "lines[0].nextLineIndex" && e.Message.Contains("越界")));
        }

        [Test]
        public void CardCostOutOfRange_Reported()
        {
            var errors = Validate(cards: new List<CardDef>
            {
                DataTestFactory.Card(cost: 6)
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "cost" && e.Message.Contains("0-5")));
        }

        [Test]
        public void DamageOnSkillCard_Reported()
        {
            var errors = Validate(cards: new List<CardDef>
            {
                DataTestFactory.Card(type: CardType.Skill, damage: 3)
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "damage" && e.Message.Contains("非 Attack")));
        }

        [Test]
        public void HasRelicConditionWithUnknownRelic_Reported()
        {
            var errors = Validate(cards: new List<CardDef>
            {
                DataTestFactory.Card(effects: new List<EffectEntry>
                {
                    DataTestFactory.Effect(condition: new ConditionEntry(ConditionType.HasRelic, "relic_zz_99"))
                })
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "effects.condition.param" && e.Message.Contains("relic_zz_99")));
        }

        [Test]
        public void EventWithoutChoices_Reported()
        {
            var errors = Validate(events: new List<EventDef>
            {
                DataTestFactory.Event(choices: new List<EventChoice>())
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "choices" && e.Message.Contains("至少需要 1 个选项")));
        }

        [Test]
        public void DanglingChoiceChain_Reported()
        {
            var errors = Validate(events: new List<EventDef>
            {
                DataTestFactory.Event(choices: new List<EventChoice>
                {
                    DataTestFactory.Choice(choiceChain: "event_zz_99")
                })
            });

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "choices.choiceChain" && e.Message.Contains("event_zz_99")));
        }

        [Test]
        public void UnknownMapLayerBossId_Reported()
        {
            var world = DataTestFactory.World(layers: new List<MapLayerDef>
            {
                new MapLayerDef(1, "冰封废墟", "", 10,
                    new NodeDistribution(5, 1, 1, 1, 2, "pool_act1"),
                    "enemy_zz_99", "", "")
            });

            var errors = Validate(world: world);

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "layers.bossId" && e.Message.Contains("enemy_zz_99")));
        }

        [Test]
        public void UnknownFactionRelationTarget_Reported()
        {
            var world = DataTestFactory.World(factions: new List<FactionDef>
            {
                new FactionDef("frost_cult", "霜印教团", "",
                    new List<FactionRelation>
                    {
                        new FactionRelation("zzz", RelationType.Hostile, null)
                    })
            });

            var errors = Validate(world: world);

            Assert.That(errors, Has.Some.Matches<DataValidationError>(
                e => e.Field == "relations.targetId" && e.Message.Contains("zzz")));
        }

        [Test]
        public void ValidSet_ReportsNoErrors()
        {
            var cards = new List<CardDef>
            {
                DataTestFactory.Card("card_ice_01", effects: new List<EffectEntry>
                {
                    DataTestFactory.Effect(EffectAction.ApplyStatus, statusType: "Freeze")
                }),
                DataTestFactory.Card("card_ice_02", type: CardType.Skill, damage: 0, block: 5, upgradeToId: null)
            };
            var statuses = new List<StatusDef> { DataTestFactory.Status("Freeze", "冻结") };
            var dialogues = new List<DialogueSequence>
            {
                DataTestFactory.Dialogue(lines: new List<DialogueLine>
                {
                    DataTestFactory.Line(speakerId: "elder_chen"),
                    DataTestFactory.Line(speakerId: "player", nextLineIndex: 1)
                })
            };

            var errors = Validate(cards: cards, statuses: statuses, dialogues: dialogues);

            Assert.That(errors, Is.Empty);
        }
    }
}
