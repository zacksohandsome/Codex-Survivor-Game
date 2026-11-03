using System;
using System.Collections.Generic;
using Survivor.Data;

namespace Survivor.Progression
{
    public sealed class UpgradeOption
    {
        public string AbilityId { get; }
        public UpgradeEffectDefinition Effect { get; }
        public int CurrentLevel { get; }
        public bool IsSpecial { get; }
        public UpgradeCategory Category { get; }
        public UpgradeOption(string ability, UpgradeEffectDefinition effect, int level, bool special = false, UpgradeCategory category = UpgradeCategory.Weapon)
        { AbilityId = ability; Effect = effect; CurrentLevel = level; IsSpecial = special; Category = category; }
    }

    public sealed class UpgradeSelector
    {
        private readonly Random random;
        public UpgradeSelector(int seed) { random = new Random(seed); }
        public UpgradeOption[] Draw(BuildState build, ProgressionConfig config)
        {
            var acquisition = new List<UpgradeOption>(); var upgrades = new List<UpgradeOption>();
            foreach (var ability in config.abilities)
            {
                int level = build.Level(ability.id);
                if (level >= ability.maxLevel) continue;
                if (level == 0 && ability.category != UpgradeCategory.Attribute)
                {
                    if (HasSlot(build, config, ability.category) && ability.CanAcquire)
                        acquisition.Add(new UpgradeOption(ability.id, ability.acquisition, 0, category: ability.category));
                }
                else
                {
                    foreach (var effect in ability.effects)
                        upgrades.Add(new UpgradeOption(ability.id, effect, level, category: ability.category));
                }
            }
            var result = new List<UpgradeOption>(3);
            DrawWithoutReplacement(acquisition, result);
            DrawWithoutReplacement(upgrades, result);
            while (result.Count < 3)
            {
                var reward = config.specialRewards[random.Next(config.specialRewards.Length)];
                result.Add(new UpgradeOption("reward", reward, 0, true));
            }
            return result.ToArray();
        }
        private void DrawWithoutReplacement(List<UpgradeOption> pool, List<UpgradeOption> result)
        {
            while (result.Count < 3 && pool.Count > 0)
            {
                int index = random.Next(pool.Count); var option = pool[index]; pool.RemoveAt(index);
                // Same-round identical effects are unique; different effects of an owned ability may coexist.
                if (result.Exists(o => o.AbilityId == option.AbilityId && o.Effect.id == option.Effect.id)) continue;
                if (option.Category == UpgradeCategory.Attribute && result.Exists(o => o.AbilityId == option.AbilityId)) continue;
                result.Add(option);
            }
        }
        private static bool HasSlot(BuildState build, ProgressionConfig config, UpgradeCategory category)
        {
            int count = 0;
            foreach (var ability in config.abilities) if (ability.category == category && build.Level(ability.id) > 0) count++;
            int maximum = category == UpgradeCategory.Weapon ? config.weaponSlots : category == UpgradeCategory.Passive ? config.passiveSlots : config.skillSlots;
            return count < maximum;
        }
    }
}
