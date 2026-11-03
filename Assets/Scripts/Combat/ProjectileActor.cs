using System.Collections.Generic;
using Survivor.Data;
using Survivor.Enemy;
using Survivor.Pooling;
using UnityEngine;

namespace Survivor.Combat
{
    public sealed class ProjectileActor : PoolActor
    {
        public float radius = 0.12f;
        public int RemainingHits { get; private set; }
        public Vector2 Direction { get; private set; }
        public float RemainingLife { get; private set; }
        private float speed, damage, orbitAngle, orbitRadius;
        private WeaponPattern pattern;
        private readonly HashSet<ulong> hitEnemies = new HashSet<ulong>();
        private RaycastHit2D[] hits;
        private SpriteRenderer body;
        public void Fire(Vector2 position, Vector2 direction, float rawDamage, float velocity, float lifetime, int maxHits, int maxEnemies)
        {
            transform.position = position; Direction = direction.normalized; damage = rawDamage; speed = velocity;
            RemainingLife = lifetime; RemainingHits = maxHits;
            if (hits == null || hits.Length != maxEnemies + 1) hits = new RaycastHit2D[maxEnemies + 1];
        }
        public void ConfigurePattern(WeaponPattern mode, float angle, float reach)
        {
            pattern = mode; orbitAngle = angle; orbitRadius = reach;
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            body.color = mode == WeaponPattern.Orbit ? new Color(1, 0.8f, 0.3f) : mode == WeaponPattern.Homing ? new Color(0.8f, 0.5f, 1) : new Color(0.35f, 0.8f, 1);
        }
        public void Tick(float delta, BattleWorld world)
        {
            float step = Mathf.Min(delta, RemainingLife);
            Vector2 origin = transform.position, end;
            if (pattern == WeaponPattern.Homing)
            {
                var target = world.Weapon.FindNearest(origin, world.Combatants, 20);
                if (target != null) Direction = ((Vector2)target.transform.position - origin).normalized;
            }
            if (pattern == WeaponPattern.Orbit)
            {
                orbitAngle += step * 3.5f;
                end = (Vector2)world.Player.transform.position + new Vector2(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle)) * orbitRadius;
            }
            else end = origin + Direction * (speed * step);
            Vector2 travel = end - origin; float distance = travel.magnitude;
            var filter = new ContactFilter2D { useTriggers = true }; filter.SetLayerMask(world.EnemyLayerMask);
            int count = Physics2D.CircleCast(origin, radius, distance > 0 ? travel / distance : Vector2.right, filter, hits, distance);
            for (int i = 0; i < count && IsRented; i++)
            {
                var enemy = hits[i].collider.GetComponent<EnemyActor>();
                if (enemy == null || !enemy.IsAlive || !hitEnemies.Add(enemy.Token)) continue;
                world.ApplyProjectileHit(this, enemy, damage); RemainingHits--;
                if (RemainingHits == 0) world.RecycleProjectile(this);
            }
            if (!IsRented) return;
            transform.position = end; RemainingLife -= delta;
            if (RemainingLife <= 0) world.RecycleProjectile(this);
        }
        public override void ResetForRent()
        {
            hitEnemies.Clear(); RemainingHits = 0; RemainingLife = 0; Direction = Vector2.zero; speed = 0; damage = 0;
            pattern = WeaponPattern.Straight; orbitAngle = 0; orbitRadius = 0;
        }
        public override void ResetForReturn() => ResetForRent();
    }
}
