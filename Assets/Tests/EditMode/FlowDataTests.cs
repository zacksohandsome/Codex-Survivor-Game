using System.Linq;
using NUnit.Framework;
using Survivor.Data;
using Survivor.Localization;
using UnityEditor;
using UnityEngine;

namespace Survivor.Tests
{
    public class FlowDataTests
    {
        [Test]
        public void CatalogHasFiveDistinctWeaponsAndResolvedTextKeys()
        {
            var config = AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");
            var text = new FlowText(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Localization/FlowText.json"));
            Assert.That(config.weapons.Length, Is.EqualTo(5));
            Assert.That(config.weapons.Select(w => w.id).Distinct().Count(), Is.EqualTo(5));
            foreach (var weapon in config.weapons)
            {
                Assert.That(text.Get(weapon.nameKey), Is.Not.EqualTo(weapon.nameKey));
                Assert.That(text.Get(weapon.descriptionKey), Is.Not.EqualTo(weapon.descriptionKey));
            }
        }
        [Test]
        public void BuildStartsInMainMenuAndIncludesIndependentBattleScene()
        {
            Assert.That(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path),
                Is.EqualTo(new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Battle.unity" }));
        }
        [Test]
        public void MainMenuSerializesAllRequiredFlowDependencies()
        {
            var dependencies = AssetDatabase.GetDependencies("Assets/Scenes/MainMenu.unity");
            Assert.That(dependencies, Does.Contain("Assets/Data/Flow/FlowConfig.asset"));
            Assert.That(dependencies, Does.Contain("Assets/Input/GameInput.inputactions"));
            Assert.That(dependencies, Does.Contain("Assets/Localization/FlowText.json"));
        }
    }
}
