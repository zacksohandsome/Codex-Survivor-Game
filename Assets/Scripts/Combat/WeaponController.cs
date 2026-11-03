using Survivor.Data;
using Survivor.Enemy;
using UnityEngine;

namespace Survivor.Combat
{
    public sealed class WeaponController
    {
        public float RemainingCooldown { get; private set; }
        public EnemyActor FindNearest(Vector2 origin, EnemyActor[] enemies, float range)
        {
            EnemyActor nearest = null; float best = range * range;
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive) continue;
                float squareDistance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (squareDistance <= best) { best = squareDistance; nearest = enemy; }
            }
            return nearest;
        }
        public void Tick(float delta, BattleWorld world, WeaponDefinition weapon)
        {
            RemainingCooldown = Mathf.Max(0, RemainingCooldown - delta);
            if (RemainingCooldown > 0) return;
            var player = world.Player;
            var target = FindNearest(player.transform.position, world.Enemies.Items, weapon.range);
            if (target == null) return;
            if (world.LaunchProjectile(player.transform.position, (Vector2)(target.transform.position - player.transform.position),
                player.Attack * weapon.damageMultiplier, weapon) != null)
                RemainingCooldown = weapon.cooldown / player.AttackSpeed;
        }
    }
}
