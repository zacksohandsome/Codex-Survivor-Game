using System;
using UnityEngine;

namespace Survivor.Data
{
    public enum UpgradeCategory { Weapon, Passive, Skill, Attribute }
    public enum UpgradeEffect { WeaponDamage, Pierce, RegenAmount, RegenInterval, Health, Attack, AttackSpeed, MoveSpeed, Defense, CurrencyReward, HealReward,
        WeaponArea, MagnetRadius, MagnetSpeed, EchoDamage, ThornsDamage, TrailDamage, StormDamage, SanctuaryDefense }
    [Serializable]
    public sealed class UpgradeEffectDefinition
    {
        public string id;
        public string nameKey;
        public string descriptionKey;
        public UpgradeEffect effect;
        public float amount;
    }
    [CreateAssetMenu(menuName = "Survivor/Progression Config")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        public UpgradeDefinition[] abilities;
        public UpgradeEffectDefinition[] specialRewards;
        public int dropCapacity = 256;
        public int enemyExperience = 4;
        [Range(0, 1)] public float currencyChance = 0.3f;
        public int currencyValue = 1;
        public float magnetRadius = 3, magnetSpeed = 7, pickupRadius = 0.15f, mergeAfterSeconds = 30;
        public float regenInterval = 6;
        public int startingXp = 8, xpLinear = 4, xpLateLevel = 10;
        public int weaponSlots = 3, passiveSlots = 5, skillSlots = 2;
    }
}
