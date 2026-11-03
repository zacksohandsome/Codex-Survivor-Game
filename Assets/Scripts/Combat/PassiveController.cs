using Survivor.Enemy;
using UnityEngine;

namespace Survivor.Combat
{
    public sealed class PassiveController
    {
        private float echoRemaining, trailRemaining, stormRemaining = 4, sanctuaryRemaining = 6, magnetRemaining = 10, magnetBurst;
        private Vector2 lastPosition;
        public float StormRemaining => stormRemaining;
        public bool MagnetBurst => magnetBurst > 0;
        public void Tick(float delta, BattleWorld world)
        {
            var build = world.CurrentBuild;
            Vector2 player = world.Player.transform.position;
            echoRemaining = Mathf.Max(0, echoRemaining - delta);
            magnetBurst = Mathf.Max(0, magnetBurst - delta);
            if (build.Level("magnet") >= 3)
            {
                magnetRemaining -= delta;
                if (magnetRemaining <= 0) { magnetBurst = 1; magnetRemaining = 10; }
            }
            if (build.TrailDamage > 0)
            {
                trailRemaining -= delta;
                if (trailRemaining <= 0 && (player - lastPosition).sqrMagnitude > 0.01f)
                {
                    world.EmitArea(lastPosition, 1.3f, world.Player.Attack * build.TrailDamage, build.Level("trail") >= 3 ? 2 : 0.8f, 0, false, new Color(0.25f, 1, 0.5f));
                    trailRemaining = 1;
                }
            }
            lastPosition = player;
            if (build.StormDamage > 0)
            {
                stormRemaining = Mathf.Max(0, stormRemaining - delta);
                if (stormRemaining == 0)
                {
                    var target = world.Weapon.FindNearest(player, world.Enemies.Items, 12);
                    if (target != null && world.EmitArea(target.transform.position, 2.5f, world.Player.Attack * build.StormDamage, 0.2f, 0.2f, false, new Color(0.7f, 0.5f, 1)) != null)
                    {
                        if (build.Level("storm") >= 3) world.EmitArea(target.transform.position, 3, world.Player.Attack * build.StormDamage * 0.5f, 0.2f, 0.65f, false, new Color(0.7f, 0.5f, 1));
                        stormRemaining = 4;
                    }
                }
            }
            if (build.SanctuaryDefense > 0)
            {
                sanctuaryRemaining -= delta;
                if (sanctuaryRemaining <= 0)
                {
                    foreach (var enemy in world.Enemies.Items)
                    {
                        if (!enemy.IsAlive) continue;
                        Vector2 away = (Vector2)enemy.transform.position - player;
                        if (away.sqrMagnitude > 9) continue;
                        if (away.sqrMagnitude < 0.001f) away = Vector2.right;
                        enemy.transform.position = Survivor.Player.PlayerActor.ClampPosition((Vector2)enemy.transform.position + away.normalized * 1.5f, world.config.arenaHalfSize, world.config.enemy.radius);
                    }
                    if (build.Level("sanctuary") >= 3) world.Player.Health.Heal(3);
                    world.EmitArea(player, 3, 0, 0.35f, 0, true, new Color(0.4f, 1, 0.8f));
                    sanctuaryRemaining = 6;
                }
            }
        }
        public void OnHit(BattleWorld world, EnemyActor struck, Vector2 position, float raw)
        {
            var build = world.CurrentBuild;
            if (build.EchoDamage <= 0 || echoRemaining > 0) return;
            echoRemaining = 1;
            int maximum = build.Level("echo") >= 3 ? 2 : 1;
            for (int i = 0; i < maximum; i++)
            {
                EnemyActor nearest = null; float best = 9;
                foreach (var enemy in world.Enemies.Items)
                {
                    if (!enemy.IsAlive || enemy == struck) continue;
                    float distance = ((Vector2)enemy.transform.position - position).sqrMagnitude;
                    if (distance <= best) { best = distance; nearest = enemy; }
                }
                if (nearest == null || world.DisplayCapacity <= 0) break;
                world.DealEnemyDamage(nearest, raw * build.EchoDamage, false);
                position = nearest.transform.position; struck = nearest;
            }
        }
        public void OnPlayerHit(BattleWorld world)
        {
            var build = world.CurrentBuild;
            if (build.ThornsDamage <= 0 || world.Player.Health.Current <= 0) return;
            float radius = build.Level("thorns") >= 3 ? 4 : 2.5f;
            world.EmitArea(world.Player.transform.position, radius, world.Player.Attack * build.ThornsDamage, 0.3f, 0, false, new Color(1, 0.4f, 0.6f));
        }
    }
}
