using System.Linq;
using NUnit.Framework;
using Survivor.Data;
using Survivor.Progression;
using UnityEditor;
using UnityEngine;

namespace Survivor.Tests
{
    public class ContentRulesTests
    {
        private static ProgressionConfig Config => AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/Data/Progression/ProgressionConfig.asset");
        [Test] public void FullCatalogHasFiveWeaponsFivePassivesTwoSkillsAndFiveAttributes()
        {
            foreach (var group in new[] { (UpgradeCategory.Weapon, 5), (UpgradeCategory.Passive, 5), (UpgradeCategory.Skill, 2), (UpgradeCategory.Attribute, 5) })
                Assert.That(Config.abilities.Count(x => x.category == group.Item1), Is.EqualTo(group.Item2));
            Assert.That(Config.abilities.Select(a => a.id).Distinct().Count(), Is.EqualTo(17));
            foreach (var ability in Config.abilities)
            { Assert.That(ability.maxLevel, Is.EqualTo(5)); Assert.That(ability.effects.Length, Is.GreaterThan(0)); }
            var flow = AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");
            Assert.That(flow.weapons.All(w => w.combatAvailable), Is.True);
            Assert.That(flow.weapons.Select(w => w.pattern).Distinct().Count(), Is.EqualTo(5));
        }
        [Test] public void WeaponEffectsDoNotLeakBetweenOwnedWeapons()
        {
            var build = new BuildState("bolt"); var bolt = Config.abilities.First(a => a.id == "bolt");
            var orbit = Config.abilities.First(a => a.id == "orbit");
            build.Apply(new UpgradeOption("orbit", orbit.acquisition, 0));
            build.Apply(new UpgradeOption("bolt", bolt.effects.First(e => e.effect == UpgradeEffect.WeaponDamage), 1));
            build.Apply(new UpgradeOption("orbit", orbit.effects.First(e => e.effect == UpgradeEffect.Pierce), 1));
            Assert.That(build.DamageFor("bolt"), Is.EqualTo(0.25f)); Assert.That(build.DamageFor("orbit"), Is.Zero);
            Assert.That(build.PierceFor("bolt"), Is.Zero); Assert.That(build.PierceFor("orbit"), Is.EqualTo(1));
        }
        [Test] public void ThreeWeaponsExcludeAcquisitionsWhileBothSkillsCanCoexist()
        {
            var build = new BuildState("bolt"); build.Levels["orbit"] = 1; build.Levels["blast"] = 1;
            foreach (var skill in Config.abilities.Where(a => a.category == UpgradeCategory.Skill))
                build.Apply(new UpgradeOption(skill.id, skill.acquisition, 0, category: UpgradeCategory.Skill));
            Assert.That(build.Level("storm"), Is.EqualTo(1)); Assert.That(build.Level("sanctuary"), Is.EqualTo(1));
            for (int seed = 0; seed < 100; seed++)
            {
                var choices = new UpgradeSelector(seed).Draw(build, Config);
                Assert.That(choices.Any(c => c.AbilityId == "homing" || c.AbilityId == "pulse"), Is.False);
                Assert.That(choices.Where(c => !c.IsSpecial).Select(c => c.AbilityId + "/" + c.Effect.id).Distinct().Count(), Is.EqualTo(choices.Count(c => !c.IsSpecial)));
            }
        }
        [Test] public void EveryAcquisitionHasAnEffectAndAllMaxedContentYieldsOnlyRewards()
        {
            var build = new BuildState("bolt");
            foreach (var ability in Config.abilities)
            {
                if (ability.category != UpgradeCategory.Attribute) Assert.That(ability.CanAcquire, Is.True);
                build.Levels[ability.id] = ability.maxLevel;
            }
            for (int seed = 0; seed < 30; seed++) Assert.That(new UpgradeSelector(seed).Draw(build, Config).All(x => x.IsSpecial), Is.True);
        }
    }
}
