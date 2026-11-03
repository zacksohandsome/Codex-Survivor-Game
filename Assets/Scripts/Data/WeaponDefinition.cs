using UnityEngine;

namespace Survivor.Data
{
    public enum WeaponPattern { Straight, Orbit, Blast, Homing, Pulse }

    [CreateAssetMenu(menuName = "Survivor/Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string id;
        public string nameKey;
        public string descriptionKey;
        public WeaponPattern pattern;
        [Min(1)] public int maxLevel = 5;
        public bool combatAvailable;
        [Min(0.01f)] public float damageMultiplier = 1.6f;
        [Min(0.01f)] public float cooldown = 0.55f;
        [Min(0.01f)] public float range = 12;
        [Min(0.01f)] public float projectileSpeed = 14;
        [Min(0.01f)] public float projectileLifetime = 2.5f;
        [Min(1)] public int maxHits = 1;
    }
}
