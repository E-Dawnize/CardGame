using System.Collections.Generic;
using CardGame.Domain;
using NUnit.Framework;

namespace CardGame.Tests.EditMode
{
    public sealed class DtoMapperTests
    {
        [Test]
        public void FullCardDto_MapsEveryField()
        {
            var dto = new CardSetDto
            {
                cards = new List<CardDto>
                {
                    new CardDto
                    {
                        id = "card_ice_01",
                        name = "寒冰箭",
                        cost = 1,
                        type = "Attack",
                        rarity = "Uncommon",
                        cardClass = "ice",
                        damage = 6,
                        block = 0,
                        effects = new List<EffectDto>
                        {
                            new EffectDto
                            {
                                trigger = "OnPlay",
                                target = "SelectedEnemy",
                                action = "ApplyStatus",
                                value = 2,
                                statusType = "Freeze",
                                condition = new ConditionDto { type = "HasStatus", param = "Freeze" }
                            }
                        },
                        keywords = new List<string> { "ice" },
                        upgradeToId = "card_ice_02",
                        flavorText = "一支由纯冰凝成的箭矢"
                    }
                }
            };

            var cards = DtoMapper.MapCardSet(dto, new DataErrorSink());

            Assert.That(cards.Count, Is.EqualTo(1));
            var card = cards[0];
            Assert.That(card.Id, Is.EqualTo("card_ice_01"));
            Assert.That(card.Name, Is.EqualTo("寒冰箭"));
            Assert.That(card.Cost, Is.EqualTo(1));
            Assert.That(card.Type, Is.EqualTo(CardType.Attack));
            Assert.That(card.Rarity, Is.EqualTo(CardRarity.Uncommon));
            Assert.That(card.Class, Is.EqualTo("ice"));
            Assert.That(card.Damage, Is.EqualTo(6));
            Assert.That(card.UpgradeToId, Is.EqualTo("card_ice_02"));
            Assert.That(card.FlavorText, Is.EqualTo("一支由纯冰凝成的箭矢"));
            Assert.That(card.Keywords.Count, Is.EqualTo(1));
        }

        [Test]
        public void StringEnum_MapsToTypedEnum()
        {
            var dto = new CardSetDto
            {
                cards = new List<CardDto>
                {
                    new CardDto
                    {
                        id = "card_ice_01",
                        type = "Skill",
                        rarity = "Rare",
                        effects = new List<EffectDto>
                        {
                            new EffectDto { trigger = "OnTurnStart", target = "All", action = "DrawCards", value = 2 }
                        }
                    }
                }
            };

            var card = DtoMapper.MapCardSet(dto, new DataErrorSink())[0];

            Assert.That(card.Type, Is.EqualTo(CardType.Skill));
            Assert.That(card.Rarity, Is.EqualTo(CardRarity.Rare));
            var effect = card.Effects[0];
            Assert.That(effect.Trigger, Is.EqualTo(EffectTrigger.OnTurnStart));
            Assert.That(effect.Target, Is.EqualTo(EffectTarget.All));
            Assert.That(effect.Action, Is.EqualTo(EffectAction.DrawCards));
        }

        [Test]
        public void CardClass_MapsToClassProperty()
        {
            var dto = new CardSetDto
            {
                cards = new List<CardDto>
                {
                    new CardDto { id = "card_ice_01", type = "Attack", rarity = "Common", cardClass = "ice" }
                }
            };

            var card = DtoMapper.MapCardSet(dto, new DataErrorSink())[0];

            Assert.That(card.Class, Is.EqualTo("ice"));
        }

        [Test]
        public void NullEffectsList_MapsToEmpty()
        {
            var dto = new CardSetDto
            {
                cards = new List<CardDto>
                {
                    new CardDto { id = "card_neutral_01", type = "Skill", rarity = "Common" }
                }
            };

            var card = DtoMapper.MapCardSet(dto, new DataErrorSink())[0];

            Assert.That(card.Effects, Is.Empty);
            Assert.That(card.Keywords, Is.Empty);
        }

        [Test]
        public void MultipleBadRows_AllErrorsCollected()
        {
            var dto = new CardSetDto
            {
                cards = new List<CardDto>
                {
                    new CardDto { id = "card_a_01", type = "Nope", rarity = "Common" },
                    new CardDto { id = "card_b_01", type = "Attack", rarity = "AlsoNo" }
                }
            };
            var sink = new DataErrorSink();

            var cards = DtoMapper.MapCardSet(dto, sink);

            Assert.That(cards.Count, Is.EqualTo(2));
            Assert.That(sink.Errors.Count, Is.EqualTo(2));
        }
    }
}
