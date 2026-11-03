using System.Collections.Generic;
using Survivor.Data;

namespace Survivor.Progression
{
    public sealed class BuildState
    {
        public Dictionary<string, int> Levels { get; } = new Dictionary<string, int>();
        public Dictionary<string, int> EffectCounts { get; } = new Dictionary<string, int>();
        private readonly Dictionary<string, float> weaponDamage = new Dictionary<string, float>();
        private readonly Dictionary<string, int> weaponPierce = new Dictionary<string, int>();
        private readonly Dictionary<string, float> weaponArea = new Dictionary<string, float>();
        public float WeaponDamage => DamageFor("bolt");
        public int Pierce => PierceFor("bolt");
        public float DamageFor(string id) => weaponDamage.TryGetValue(id, out var value) ? value : 0;
        public int PierceFor(string id) => weaponPierce.TryGetValue(id, out var value) ? value : 0;
        public float AreaFor(string id) => weaponArea.TryGetValue(id, out var value) ? value : 0;
        public int RegenAmount { get; private set; }
        public float RegenIntervalReduction { get; private set; }
        public int HealthBonus { get; private set; }
        public float AttackBonus { get; private set; }
        public float AttackSpeedBonus { get; private set; }
        public float MoveSpeedBonus { get; private set; }
        public float DefenseBonus { get; private set; }
        public float MagnetRadiusBonus { get; private set; }
        public float MagnetSpeedBonus { get; private set; }
        public float EchoDamage { get; private set; }
        public float ThornsDamage { get; private set; }
        public float TrailDamage { get; private set; }
        public float StormDamage { get; private set; }
        public float SanctuaryDefense { get; private set; }
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
                case UpgradeEffect.WeaponDamage: weaponDamage[option.AbilityId] = DamageFor(option.AbilityId) + amount; break;
                case UpgradeEffect.Pierce: weaponPierce[option.AbilityId] = PierceFor(option.AbilityId) + (int)amount; break;
                case UpgradeEffect.WeaponArea: weaponArea[option.AbilityId] = AreaFor(option.AbilityId) + amount; break;
                case UpgradeEffect.RegenAmount: RegenAmount += (int)amount; break;
                case UpgradeEffect.RegenInterval: RegenIntervalReduction += amount; break;
                case UpgradeEffect.Health: HealthBonus += (int)amount; break;
                case UpgradeEffect.Attack: AttackBonus += amount; break;
                case UpgradeEffect.AttackSpeed: AttackSpeedBonus += amount; break;
                case UpgradeEffect.MoveSpeed: MoveSpeedBonus += amount; break;
                case UpgradeEffect.Defense: DefenseBonus += amount; break;
                case UpgradeEffect.MagnetRadius: MagnetRadiusBonus += amount; break;
                case UpgradeEffect.MagnetSpeed: MagnetSpeedBonus += amount; break;
                case UpgradeEffect.EchoDamage: EchoDamage += amount; break;
                case UpgradeEffect.ThornsDamage: ThornsDamage += amount; break;
                case UpgradeEffect.TrailDamage: TrailDamage += amount; break;
                case UpgradeEffect.StormDamage: StormDamage += amount; break;
                case UpgradeEffect.SanctuaryDefense: SanctuaryDefense += amount; break;
            }
            Revision++;
        }
    }
}
