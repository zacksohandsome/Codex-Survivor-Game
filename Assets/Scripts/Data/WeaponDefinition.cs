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
        // Combat numbers will be introduced and validated with the M2 implementation.
    }
}
