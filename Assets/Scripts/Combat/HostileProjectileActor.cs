using Survivor.Pooling;
using UnityEngine;

namespace Survivor.Combat
{
    public sealed class HostileProjectileActor : PoolActor
    {
        public SpriteRenderer body;
        public Vector2 Direction { get; private set; }
        public float RemainingLife { get; private set; }
        private float speed, damage;
        private readonly RaycastHit2D[] hit = new RaycastHit2D[1];
        public void Fire(Vector2 position, Vector2 direction, float velocity, float raw, float lifetime)
        { transform.position = position; Direction = direction.normalized; speed = velocity; damage = raw; RemainingLife = lifetime; }
        public void Tick(float delta, BattleWorld world)
        {
            float distance = speed * Mathf.Min(delta, RemainingLife);
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(1 << world.Player.gameObject.layer);
            if (Physics2D.CircleCast(transform.position, 0.18f, Direction, filter, hit, distance) > 0 && hit[0].collider == world.Player.hitbox)
            { world.HitPlayer(damage); world.HostileProjectiles.Return(this); return; }
            transform.position += (Vector3)(Direction * distance); RemainingLife -= delta;
            Vector2 p = transform.position;
            if (RemainingLife <= 0 || Mathf.Abs(p.x) > world.config.arenaHalfSize.x || Mathf.Abs(p.y) > world.config.arenaHalfSize.y)
                world.HostileProjectiles.Return(this);
        }
        public override void ResetForRent() { Direction = Vector2.zero; RemainingLife = 0; speed = damage = 0; }
        public override void ResetForReturn() => ResetForRent();
    }
}
