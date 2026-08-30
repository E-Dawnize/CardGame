using System.Collections.Generic;
using System.Linq;
using CardGame.Domain;
using CardGame.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CardGame.Tests.EditMode
{
    /// <summary>协议自证：全部提交的样例 JSON 必须加载且校验干净，交叉引用可解析。</summary>
    public sealed class SampleContentTests
    {
        private static readonly string[] SamplePaths =
        {
            "Assets/CardGame/Content/Data/cards.json",
            "Assets/CardGame/Content/Data/relics.json",
            "Assets/CardGame/Content/Data/enemies.json",
            "Assets/CardGame/Content/Data/status.json",
            "Assets/CardGame/Content/Data/events.json",
            "Assets/CardGame/Content/Data/dialogues.json",
            "Assets/CardGame/Content/Data/world.json",
            "Assets/CardGame/Content/Data/ui-strings.json"
        };

        private static DataRepository LoadSamples()
        {
            var assets = new List<TextAsset>();
            foreach (var path in SamplePaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                Assert.That(asset, Is.Not.Null, $"样例文件缺失：{path}");
                assets.Add(asset);
            }
            var loader = new GameDataLoader(new JsonUtilityGameDataSerializer());
            return loader.Load(assets);
        }

        [Test]
        public void AllCommittedJson_LoadAndValidateClean()
        {
            Assert.DoesNotThrow(() => LoadSamples());
        }

        [Test]
        public void AllCrossReferences_Resolve()
        {
            var repository = LoadSamples();

            // 升级对：寒冰箭 → 碎裂
            Assert.That(repository.GetCard("card_ice_01").UpgradeToId, Is.EqualTo("card_ice_02"));
            Assert.That(repository.GetCard("card_ice_02"), Is.Not.Null);

            // 状态：效果 statusType 与敌人初始状态都解析
            Assert.That(repository.GetStatus("Freeze"), Is.Not.Null);
            Assert.That(repository.GetEnemy("enemy_wolf_01").StartingStatus[0].StatusType, Is.EqualTo("Block"));

            // 地图层 Boss
            var bossId = repository.World.Layers[0].BossId;
            Assert.That(repository.GetEnemy(bossId), Is.Not.Null);

            // 对话说话人
            var dialogue = repository.GetDialogue("dialogue_tavern_01");
            Assert.That(dialogue, Is.Not.Null);
            Assert.That(
                repository.World.Speakers.Any(s => s.Id == dialogue.Lines[0].SpeakerId),
                Is.True);

            // UI 散表
            Assert.That(repository.UiStrings.Get("battle", "end_turn"), Is.EqualTo("结束回合"));
        }
    }
}
