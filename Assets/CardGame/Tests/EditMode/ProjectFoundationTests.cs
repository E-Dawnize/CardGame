using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace CardGame.Tests.EditMode
{
    public sealed class ProjectFoundationTests
    {
        [Test]
        public void ProjectSettings_UseApprovedIdentity()
        {
            Assert.That(PlayerSettings.productName, Is.EqualTo("CardGame"));
            Assert.That(PlayerSettings.companyName, Is.EqualTo("E-Dawnize"));
        }

        [Test]
        public void Bootstrap_IsFirstEnabledBuildScene()
        {
            var scene = EditorBuildSettings.scenes.First(item => item.enabled);

            Assert.That(
                scene.path,
                Is.EqualTo("Assets/CardGame/Scenes/Bootstrap.unity"));
        }
    }
}
