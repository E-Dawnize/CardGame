using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CardGame.Tests.EditMode
{
    public sealed class ProjectFoundationTests
    {
        [Test]
        public void ProjectSettings_UseApprovedIdentity()
        {
            Assert.That(PlayerSettings.productName, Is.EqualTo("CardGame"));
            Assert.That(PlayerSettings.companyName, Is.EqualTo("E-Dawnize"));
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone),
                Is.EqualTo("com.edawnize.cardgame"));
        }

        [Test]
        public void Bootstrap_IsFirstEnabledBuildScene()
        {
            var scene = EditorBuildSettings.scenes.First(item => item.enabled);

            Assert.That(
                scene.path,
                Is.EqualTo("Assets/CardGame/Scenes/Bootstrap.unity"));
        }

        [Test]
        public void TemplateDefaultScene_IsBootstrapAndExists()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var projectSettingsPath = Path.Combine(
                projectRoot,
                "ProjectSettings",
                "ProjectSettings.asset");
            var templateDefaultScene = File.ReadLines(projectSettingsPath)
                .Single(line => line.TrimStart().StartsWith("templateDefaultScene:"))
                .Split(':', 2)[1]
                .Trim();

            Assert.That(
                templateDefaultScene,
                Is.EqualTo("Assets/CardGame/Scenes/Bootstrap.unity"));
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(templateDefaultScene),
                Is.Not.Null);
        }
    }
}
