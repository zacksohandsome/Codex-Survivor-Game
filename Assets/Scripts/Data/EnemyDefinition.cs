using UnityEngine;

namespace Survivor.Data
{
    public enum EnemyKind { Melee, Ranged, Bomber, Elite, Boss }
    [CreateAssetMenu(menuName = "Survivor/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public EnemyKind kind;
        public Sprite sprite;
        public string nameKey = "enemy.melee";
        public int maxHealth = 28;
        public float defense = 0, moveSpeed = 2.2f, contactDamage = 14, radius = 0.45f;
        public Color color = new Color(0.95f, 0.3f, 0.3f);
        public float attackCooldown = 2.2f, windupSeconds = 0.7f, attackRange = 10, blastRadius = 2.5f;
        public float projectileSpeed = 6, projectileLifetime = 6;
        public int experience = 4, currencyValue = 1;
        [Range(0, 1)] public float currencyChance = 0.3f;
    }
}
