using System.Collections.Generic;
using CardGame.Domain;
using CardGame.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace CardGame.Tests.EditMode
{
    public sealed class JsonSerializationTests
    {
        private readonly JsonUtilityGameDataSerializer _serializer = new JsonUtilityGameDataSerializer();

        [Test]
        public void CardsJson_RoundTripsToDto()
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
                        rarity = "Common",
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
                        upgradeToId = "card_ice_02"
                    }
                }
            };

            var json = JsonUtility.ToJson(dto);
            var parsed = _serializer.FromJson<CardSetDto>(json);

            Assert.That(parsed.cards.Count, Is.EqualTo(1));
            var card = parsed.cards[0];
            Assert.That(card.id, Is.EqualTo("card_ice_01"));
            Assert.That(card.name, Is.EqualTo("寒冰箭"));
            Assert.That(card.type, Is.EqualTo("Attack"));
            Assert.That(card.cardClass, Is.EqualTo("ice"));
            Assert.That(card.effects.Count, Is.EqualTo(1));
            Assert.That(card.effects[0].action, Is.EqualTo("ApplyStatus"));
            Assert.That(card.effects[0].condition.param, Is.EqualTo("Freeze"));
        }

        [Test]
        public void MissingOptionalFields_UseDefaults()
        {
            var parsed = _serializer.FromJson<CardSetDto>("{\"cards\":[{\"id\":\"card_x_01\"}]}");

            Assert.That(parsed.cards.Count, Is.EqualTo(1));
            var card = parsed.cards[0];
            Assert.That(card.cost, Is.Zero);
            Assert.That(card.damage, Is.Zero);
            Assert.That(card.block, Is.Zero);
            // JsonUtility 语义：缺失字符串 → null；缺失 List → 空列表（非 null）。
            Assert.That(card.type, Is.Null);
            Assert.That(card.cardClass, Is.Null);
            Assert.That(card.effects, Is.Not.Null);
            Assert.That(card.effects.Count, Is.Zero);
            Assert.That(card.upgradeToId, Is.Null);
        }

        [Test]
        public void MalformedJson_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() => _serializer.FromJson<CardSetDto>("{not json"));
        }
    }
}
