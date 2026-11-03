using Survivor.Data;
using Survivor.Enemy;
using UnityEngine;

namespace Survivor.Combat
{
    public sealed class WeaponController
    {
        public float RemainingCooldown { get; private set; }
        private float orbitAngle;
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
            var player = world.Player; var build = world.CurrentBuild;
            int level = build.Level(weapon.id);
            Vector2 origin = player.transform.position;
            float damage = player.Attack * (weapon.damageMultiplier + build.DamageFor(weapon.id));
            float radius = weapon.areaRadius + build.AreaFor(weapon.id);
            bool fired = false;
            if (weapon.pattern == WeaponPattern.Orbit)
            {
                int count = level >= 3 ? 3 : 2;
                for (int i = 0; i < count; i++)
                {
                    float angle = orbitAngle + i * Mathf.PI * 2 / count;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    fired |= world.LaunchProjectile(origin + direction * radius, direction, damage, weapon,
                        build.PierceFor(weapon.id), angle) != null;
                }
                orbitAngle += 0.7f;
            }
            else if (weapon.pattern == WeaponPattern.Pulse)
            {
                fired = world.EmitArea(origin, radius, damage, 0.7f, 0, true, new Color(0.4f, 0.75f, 1)) != null;
                if (fired && level >= 3) world.EmitArea(origin, radius * 1.2f, damage * 0.5f, 0.7f, 0.4f, true, new Color(0.4f, 0.75f, 1));
            }
            else
            {
                var target = FindNearest(origin, world.Enemies.Items, weapon.range);
                if (target == null) return;
                Vector2 targetPosition = target.transform.position;
                if (weapon.pattern == WeaponPattern.Blast)
                {
                    fired = world.EmitArea(targetPosition, radius, damage, 0.2f, 0.35f, false, new Color(1, 0.5f, 0.2f)) != null;
                    if (fired && level >= 3) world.EmitArea(targetPosition, radius * 0.8f, damage * 0.6f, 0.2f, 0.75f, false, new Color(1, 0.75f, 0.2f));
                }
                else
                {
                    Vector2 direction = (targetPosition - origin).normalized;
                    int extra = build.PierceFor(weapon.id) + (level >= 3 && weapon.pattern == WeaponPattern.Homing ? 1 : 0) + (level >= 5 && weapon.pattern == WeaponPattern.Straight ? 1 : 0);
                    fired = world.LaunchProjectile(origin, direction, damage, weapon, extra) != null;
                    if (fired && ((level >= 3 && weapon.pattern == WeaponPattern.Straight) || (level >= 5 && weapon.pattern == WeaponPattern.Homing)))
                    {
                        Vector2 second = new Vector2(direction.x * 0.966f - direction.y * 0.259f, direction.x * 0.259f + direction.y * 0.966f);
                        world.LaunchProjectile(origin, second, damage, weapon, extra);
                    }
                }
            }
            if (fired) RemainingCooldown = weapon.cooldown / player.AttackSpeed;
        }
    }
}
