using UnityEngine;

namespace Survivor.Data
{
    [CreateAssetMenu(menuName = "Survivor/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public int maxHealth = 28;
        public float defense = 0, moveSpeed = 2.2f, contactDamage = 14, radius = 0.45f;
    }
}
