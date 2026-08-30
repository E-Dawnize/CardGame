using System.Collections.Generic;
using CardGame.Domain;
using NUnit.Framework;

namespace CardGame.Tests.EditMode
{
    public sealed class EnumMapsTests
    {
        [Test]
        public void KnownString_MapsToTypedEnum()
        {
            Assert.That(
                EnumMaps.TryParse(EnumMaps.EffectActionMap, "DealDamage", out var action),
                Is.True);
            Assert.That(action, Is.EqualTo(EffectAction.DealDamage));
        }

        [Test]
        public void UnknownValue_ErrorCarriesFileIdFieldValue()
        {
            var dto = new CardSetDto
            {
                cards = new List<CardDto>
                {
                    new CardDto
                    {
                        id = "card_ice_01",
                        type = "Attack",
                        rarity = "Common",
                        effects = new List<EffectDto>
                        {
                            new EffectDto
                            {
                                trigger = "OnPlay",
                                target = "Self",
                                action = "Nuke",
                                value = 1
                            }
                        }
                    }
                }
            };
            var sink = new DataErrorSink();

            DtoMapper.MapCardSet(dto, sink);

            Assert.That(sink.Errors.Count, Is.EqualTo(1));
            var error = sink.Errors[0];
            Assert.That(error.File, Is.EqualTo(DtoMapper.CardsFile));
            Assert.That(error.Id, Is.EqualTo("card_ice_01"));
            Assert.That(error.Field, Is.EqualTo("action"));
            Assert.That(error.Message, Does.Contain("Nuke"));
        }

        [Test]
        public void WrongCaseValue_IsRejected()
        {
            Assert.That(
                EnumMaps.TryParse(EnumMaps.EffectActionMap, "dealdamage", out _),
                Is.False);
        }

        [Test]
        public void NullValue_IsRejected()
        {
            Assert.That(
                EnumMaps.TryParse(EnumMaps.CardTypeMap, null, out _),
                Is.False);
        }
    }
}
