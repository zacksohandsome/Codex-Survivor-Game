using System.Collections.Generic;
using Survivor.Data;

namespace Survivor.Progression
{
    public sealed class BuildState
    {
        public Dictionary<string, int> Levels { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> EffectCounts { get; } = new Dictionary<string, int>();
        public float WeaponDamage { get; private set; }
        public int Pierce { get; private set; }
        public int RegenAmount { get; private set; }
        public float RegenIntervalReduction { get; private set; }
        public int HealthBonus { get; private set; }
        public float AttackBonus { get; private set; }
        public float AttackSpeedBonus { get; private set; }
        public float MoveSpeedBonus { get; private set; }
        public float DefenseBonus { get; private set; }
        public int Revision { get; private set; }
        public BuildState(string weapon) { Levels.Add(weapon, 1); }
        public int Level(string id) => Levels.TryGetValue(id, out var value) ? value : 0;
        public void Apply(UpgradeOption option)
        {
            if (!option.IsSpecial) Levels[option.AbilityId] = Level(option.AbilityId) + 1;
            string effectKey = option.AbilityId + "/" + option.Effect.id;
            EffectCounts.TryGetValue(effectKey, out var count); EffectCounts[effectKey] = count + 1;
            float amount = option.Effect.amount;
            switch (option.Effect.effect)
            {
                case UpgradeEffect.WeaponDamage: WeaponDamage += amount; break;
                case UpgradeEffect.Pierce: Pierce += (int)amount; break;
                case UpgradeEffect.RegenAmount: RegenAmount += (int)amount; break;
                case UpgradeEffect.RegenInterval: RegenIntervalReduction += amount; break;
                case UpgradeEffect.Health: HealthBonus += (int)amount; break;
                case UpgradeEffect.Attack: AttackBonus += amount; break;
                case UpgradeEffect.AttackSpeed: AttackSpeedBonus += amount; break;
                case UpgradeEffect.MoveSpeed: MoveSpeedBonus += amount; break;
                case UpgradeEffect.Defense: DefenseBonus += amount; break;
            }
            Revision++;
        }
    }
}
