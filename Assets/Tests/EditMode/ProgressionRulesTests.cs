using System.Linq;
using NUnit.Framework;
using Survivor.Core;
using Survivor.Data;
using Survivor.Progression;
using Survivor.Localization;
using UnityEditor;
using UnityEngine;

namespace Survivor.Tests
{
    public class ProgressionRulesTests
    {
        private static ProgressionConfig Config => AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/Data/Progression/ProgressionConfig.asset");
        private static GameSession Battle()
        {
            var session = new GameSession(); session.OpenWeaponSelection(); session.SelectWeapon("bolt"); session.StartRun(3); session.Tick(3); return session;
        }
        [Test]
        public void NewAbilityIsPrioritizedAndIdenticalEffectsAreUniquePerRound()
        {
            var build = new BuildState("bolt");
            var choices = new UpgradeSelector(1).Draw(build, Config);
            Assert.That(choices.All(c => c.CurrentLevel == 0 && c.Category != UpgradeCategory.Attribute && !c.IsSpecial), Is.True);
            Assert.That(choices.Select(c => c.AbilityId + "/" + c.Effect.id).Distinct().Count(), Is.EqualTo(3));
        }
        [Test]
        public void SameEffectCanRepeatAcrossRoundsUntilTheAbilityIsMaxed()
        {
            var config = Object.Instantiate(Config);
            try
            {
                var bolt = config.abilities.First(a => a.id == "bolt"); config.abilities = new[] { bolt };
                var build = new BuildState("bolt"); var selector = new UpgradeSelector(2);
                for (int i = 1; i < 5; i++)
                {
                    var options = selector.Draw(build, config);
                    Assert.That(options.Count(c => c.AbilityId == "bolt"), Is.EqualTo(2), "Different effects of the same owned ability may coexist");
                    build.Apply(options.First(c => c.Effect.effect == UpgradeEffect.WeaponDamage));
                }
                Assert.That(build.WeaponDamage, Is.EqualTo(1));
                Assert.That(selector.Draw(build, config).All(c => c.IsSpecial), Is.True);
            }
            finally { Object.DestroyImmediate(config); }
        }
        [Test]
        public void FullSlotsExcludeNewAbilitiesAndAttributesDoNotConsumePassiveSlots()
        {
            var config = Object.Instantiate(Config);
            try
            {
                var build = new BuildState("bolt"); config.passiveSlots = 0; config.weaponSlots = 1; config.skillSlots = 0;
                build.Levels["stat.health"] = 3;
                var options = new UpgradeSelector(3).Draw(build, config);
                Assert.That(options.Any(c => c.Category == UpgradeCategory.Passive), Is.False);
                config.passiveSlots = 1;
                Assert.That(new UpgradeSelector(3).Draw(build, config)[0].Category, Is.EqualTo(UpgradeCategory.Passive));
            }
            finally { Object.DestroyImmediate(config); }
        }
        [Test]
        public void MaxedAbilitiesAndAttributesYieldThreeRepeatableSpecialRewards()
        {
            var build = new BuildState("bolt"); foreach (var ability in Config.abilities) build.Levels[ability.id] = ability.maxLevel;
            var options = new UpgradeSelector(7).Draw(build, Config);
            Assert.That(options.Length, Is.EqualTo(3)); Assert.That(options.All(c => c.IsSpecial), Is.True);
            Assert.That(options.Select(c => c.Effect.id).Distinct().Count(), Is.LessThan(3), "Two rewards may fill three slots with duplicates");
        }
        [Test]
        public void OverflowCreatesSequentialChoicesAndRejectsOldScreenCallbacks()
        {
            var session = Battle(); var progression = new RunProgression(session, Config, 2);
            progression.GainExperience(40); Assert.That(session.Run.Level, Is.EqualTo(2));
            int first = progression.ChoiceRevision;
            Assert.That(progression.Choose(0, first, out _), Is.True);
            Assert.That(session.State, Is.EqualTo(GameState.LevelUp));
            Assert.That(session.Run.Level, Is.EqualTo(3));
            Assert.That(progression.Choose(0, first, out _), Is.False);
            progression.Choose(0, progression.ChoiceRevision, out _);
            Assert.That(session.Run.Level, Is.EqualTo(4));
            progression.Choose(0, progression.ChoiceRevision, out _);
            Assert.That(session.State, Is.EqualTo(GameState.Battle));
            Assert.That(session.Run.Experience, Is.EqualTo(4));
        }
        [Test]
        public void TimeAndDeathAreResolvedByTheFirstActualFinishEvent()
        {
            var deathFirst = Battle(); deathFirst.Tick(599.9); deathFirst.Finish(false); deathFirst.Tick(1);
            Assert.That(deathFirst.Result.Won, Is.False);
            var timeFirst = Battle(); timeFirst.Tick(600); Assert.That(timeFirst.Finish(false), Is.False);
            Assert.That(timeFirst.Result.Won, Is.True); Assert.That(timeFirst.BattleSeconds, Is.EqualTo(600));
        }
        [Test]
        public void ResultCopiesAbilitiesAndOnlyCollectedCurrency()
        {
            var session = Battle(); session.Run.CollectedCurrency = 7; session.Run.AbilityLevels["bloom"] = 2;
            session.Finish(false); session.Run.CollectedCurrency = 99; session.Run.AbilityLevels["bloom"] = 5; session.ReturnToMenu();
            Assert.That(session.Result.CollectedCurrency, Is.EqualTo(7));
            Assert.That(session.Result.AbilityLevels["bloom"], Is.EqualTo(2));
        }
        [Test]
        public void ProgressionDataHasResolvedKeysAndIncreasingExperienceRequirements()
        {
            var text = new FlowText(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Localization/FlowText.json"));
            foreach (var ability in Config.abilities)
            {
                Assert.That(text.Get("ability." + ability.id), Is.Not.EqualTo("ability." + ability.id));
                foreach (var effect in ability.effects.Concat(ability.CanAcquire ? new[] { ability.acquisition } : new UpgradeEffectDefinition[0]))
                { Assert.That(text.Get(effect.nameKey), Is.Not.EqualTo(effect.nameKey)); Assert.That(text.Get(effect.descriptionKey), Is.Not.EqualTo(effect.descriptionKey)); }
            }
            for (int level = 1; level < 100; level++) Assert.That(RunProgression.Required(level + 1, Config), Is.GreaterThan(RunProgression.Required(level, Config)));
        }
        [Test]
        public void EmptyInlineAcquisitionIsNeverOfferedAsANewAbility()
        {
            var config = Object.Instantiate(Config);
            var weapon = Object.Instantiate(config.abilities.First(a => a.id == "bolt"));
            try
            {
                weapon.acquisition = new UpgradeEffectDefinition();
                config.abilities = new[] { weapon };
                Assert.That(config.abilities[0].CanAcquire, Is.False);
                Assert.That(new UpgradeSelector(1).Draw(new BuildState("other"), config).All(c => c.IsSpecial), Is.True);
            }
            finally { Object.DestroyImmediate(weapon); Object.DestroyImmediate(config); }
        }
        [Test]
        public void OneAttributeCannotAppearTwiceEvenWithMultipleConfiguredEffects()
        {
            var config = Object.Instantiate(Config); var attribute = Object.Instantiate(config.abilities.First(a => a.id == "stat.health"));
            try
            {
                attribute.effects = new[] { attribute.effects[0], new UpgradeEffectDefinition { id = "other.health", effect = UpgradeEffect.Health, amount = 10 } };
                config.abilities = new[] { attribute };
                var choices = new UpgradeSelector(1).Draw(new BuildState("bolt"), config);
                Assert.That(choices.Count(c => c.AbilityId == attribute.id), Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(attribute); Object.DestroyImmediate(config); }
        }
        [TestCase(UpgradeCategory.Weapon)]
        [TestCase(UpgradeCategory.Passive)]
        [TestCase(UpgradeCategory.Skill)]
        public void EachAbilityCategoryHonorsItsOwnSlotLimit(UpgradeCategory category)
        {
            var config = Object.Instantiate(Config); var owned = ScriptableObject.CreateInstance<UpgradeDefinition>();
            var candidate = ScriptableObject.CreateInstance<UpgradeDefinition>();
            try
            {
                owned.id = "owned"; owned.category = category; owned.effects = new UpgradeEffectDefinition[0];
                candidate.id = "new"; candidate.category = category; candidate.effects = new UpgradeEffectDefinition[0];
                candidate.acquisition = new UpgradeEffectDefinition { id = "acquire" };
                config.abilities = new[] { owned, candidate }; config.weaponSlots = config.passiveSlots = config.skillSlots = 1;
                Assert.That(new UpgradeSelector(1).Draw(new BuildState("owned"), config).All(c => c.IsSpecial), Is.True);
                config.weaponSlots = config.passiveSlots = config.skillSlots = 2;
                Assert.That(new UpgradeSelector(1).Draw(new BuildState("owned"), config)[0].AbilityId, Is.EqualTo("new"));
            }
            finally { Object.DestroyImmediate(owned); Object.DestroyImmediate(candidate); Object.DestroyImmediate(config); }
        }
    }
}
