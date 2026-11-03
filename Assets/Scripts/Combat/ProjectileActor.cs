using System.Collections.Generic;
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
        private float speed, damage;
        private readonly HashSet<ulong> hitEnemies = new HashSet<ulong>();
        private RaycastHit2D[] hits;
        public void Fire(Vector2 position, Vector2 direction, float rawDamage, float velocity, float lifetime, int maxHits, int maxEnemies)
        {
            transform.position = position; Direction = direction.normalized; damage = rawDamage; speed = velocity;
            RemainingLife = lifetime; RemainingHits = maxHits;
            if (hits == null || hits.Length != maxEnemies + 1) hits = new RaycastHit2D[maxEnemies + 1];
        }
        public void Tick(float delta, BattleWorld world)
        {
            float step = Mathf.Min(delta, RemainingLife);
            float distance = speed * step;
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(world.EnemyLayerMask);
            int count = Physics2D.CircleCast(transform.position, radius, Direction, filter, hits, distance);
            // CircleCast fills hits in distance order. One slot also covers the player's trigger.
            for (int i = 0; i < count && IsRented; i++)
            {
                var enemy = hits[i].collider.GetComponent<EnemyActor>();
                if (enemy == null || !enemy.IsAlive || !hitEnemies.Add(enemy.Token)) continue;
                world.ApplyProjectileHit(this, enemy, damage);
                RemainingHits--;
                if (RemainingHits == 0) world.RecycleProjectile(this);
            }
            if (!IsRented) return;
            transform.position += (Vector3)(Direction * distance); RemainingLife -= delta;
            if (RemainingLife <= 0) world.RecycleProjectile(this);
        }
        public override void ResetForRent()
        { hitEnemies.Clear(); RemainingHits = 0; RemainingLife = 0; Direction = Vector2.zero; speed = 0; damage = 0; }
        public override void ResetForReturn() => ResetForRent();
    }
}
