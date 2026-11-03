using UnityEngine;

namespace Survivor.Data
{
    [CreateAssetMenu(menuName = "Survivor/Battle Config")]
    public sealed class BattleConfig : ScriptableObject
    {
        public Vector2 arenaHalfSize = new Vector2(24, 15);
        public float cameraHalfHeight = 7;
        public int playerHealth = 100;
        public float playerAttack = 10, playerAttackSpeed = 1, playerMoveSpeed = 5.5f, playerDefense = 10;
        public float moveSpeedCap = 10, attackSpeedCap = 5;
        public float playerRadius = 0.45f, invincibleSeconds = 1, blinkInterval = 0.1f;
        public int enemyCapacity = 64, projectileCapacity = 96, damageNumberCapacity = 256;
        public float spawnInterval = 1.25f, spawnWarningSeconds = 0.75f, minSpawnDistance = 8;
        public int spawnCandidates = 64, randomSeed = 7303;
        public float separationSpeed = 2.5f, stuckInterval = 1, stuckDistance = 0.05f;
        public float hitFlashSeconds = 0.12f, damageNumberSeconds = 0.6f, damageNumberRiseSpeed = 1;
        public WeaponDefinition weapon;
        public EnemyDefinition enemy;
    }
}
