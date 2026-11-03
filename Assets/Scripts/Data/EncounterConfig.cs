using UnityEngine;

namespace Survivor.Data
{
    [CreateAssetMenu(menuName = "Survivor/Encounter Config")]
    public sealed class EncounterConfig : ScriptableObject
    {
        public EnemyDefinition ranged, bomber, elite, boss;
        public float[] healthScale = { 1, 1.2f, 1.5f, 1.8f, 2.1f };
        public float[] damageScale = { 1, 1.1f, 1.2f, 1.35f, 1.5f };
        public float[] speedScale = { 1, 1.03f, 1.06f, 1.09f, 1.12f };
        public float[] spawnIntervals = { 1.25f, 1.05f, 0.9f, 0.75f, 0.65f };
        public float[] rangedChance = { 0, 0.25f, 0.3f, 0.3f, 0.35f };
        public float[] bomberChance = { 0, 0, 0.2f, 0.25f, 0.3f };
        [Range(0.1f, 1)] public float specialSpawnRate = 0.6f;
        public float eliteSeconds = 180, bossSeconds = 420;
        public int hostileProjectileCapacity = 128;
    }
}
