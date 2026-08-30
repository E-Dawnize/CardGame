using System.Collections.Generic;
using System.Linq;
using CardGame.Domain;
using CardGame.Runtime;
using NUnit.Framework;

namespace CardGame.Tests.EditMode
{
    public sealed class GameDataLoaderTests
    {
        private readonly GameDataLoader _loader =
            new GameDataLoader(new JsonUtilityGameDataSerializer());

        private static Dictionary<string, string> ValidRawJson()
        {
            return new Dictionary<string, string>
            {
                { "cards.json", @"{""cards"":[{""id"":""card_ice_01"",""name"":""寒冰箭"",""cost"":1,""type"":""Attack"",""rarity"":""Common"",""damage"":6,""block"":0}]}" },
                { "relics.json", @"{""relics"":[]}" },
                { "enemies.json", @"{""enemies"":[]}" },
                { "status.json", @"{""statuses"":[{""id"":""Freeze"",""name"":""冻结"",""iconKey"":""icon_freeze"",""maxStack"":5,""decayEachTurn"":1,""effectDesc"":""每层使目标的下一次攻击伤害 -1。"",""colorHex"":""#88BBFF""}]}" },
                { "events.json", @"{""events"":[]}" },
                { "dialogues.json", @"{""dialogues"":[{""id"":""dialogue_tavern_01"",""title"":""酒馆闲聊"",""lines"":[{""speakerId"":""player"",""portrait"":""Left"",""type"":""Normal"",""text"":""你好。"",""nextLineIndex"":-1}]}]}" },
                { "world.json", @"{""title"":""测试世界"",""layers"":[],""factions"":[],""speakers"":[{""id"":""player"",""name"":""冒险者"",""faction"":""""},{""id"":""narrator"",""name"":""旁白"",""faction"":""""}]}" },
                { "ui-strings.json", @"{""groups"":[{""id"":""battle"",""entries"":[{""key"":""end_turn"",""value"":""结束回合""}]}]}" }
            };
        }

        [Test]
        public void SampleRawJson_ProducesValidRepository()
        {
            var repository = _loader.Load(ValidRawJson());

            Assert.That(repository, Is.Not.Null);
            Assert.That(repository.GetCard("card_ice_01").Name, Is.EqualTo("寒冰箭"));
            Assert.That(repository.GetStatus("Freeze").Name, Is.EqualTo("冻结"));
            Assert.That(repository.World.Title, Is.EqualTo("测试世界"));
            Assert.That(repository.UiStrings.Get("battle", "end_turn"), Is.EqualTo("结束回合"));
        }

        [Test]
        public void MultipleBrokenFiles_ReportsAllInOneException()
        {
            var raw = ValidRawJson();
            raw["cards.json"] = @"{""cards"":[{""id"":""card_ice_01"",""type"":""Attack"",""rarity"":""Common"",""effects"":[{""trigger"":""OnPlay"",""target"":""Self"",""action"":""Nuke""}]}]}";
            raw["relics.json"] = @"{""relics"":[{""id"":""relic_ice_01"",""rarity"":""Nope""}]}";

            var ex = Assert.Throws<DataValidationException>(() => _loader.Load(raw));

            Assert.That(ex.Errors.Count, Is.EqualTo(2));
            Assert.That(ex.Errors.Any(e => e.Message.Contains("Nuke")), Is.True);
            Assert.That(ex.Errors.Any(e => e.Message.Contains("Nope")), Is.True);
        }

        [Test]
        public void UnknownFileName_Reported()
        {
            var raw = ValidRawJson();
            raw["foo.json"] = "{}";

            var ex = Assert.Throws<DataValidationException>(() => _loader.Load(raw));

            Assert.That(ex.Errors.Any(e => e.Message.Contains("未知数据文件")), Is.True);
        }

        [Test]
        public void MissingFile_Reported()
        {
            var raw = ValidRawJson();
            raw.Remove("cards.json");

            var ex = Assert.Throws<DataValidationException>(() => _loader.Load(raw));

            Assert.That(ex.Errors.Any(e => e.Message.Contains("缺失必需文件")), Is.True);
        }
    }
}
