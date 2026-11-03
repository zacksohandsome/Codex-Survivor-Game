using System.Collections.Generic;
using Survivor.Pooling;
using UnityEngine;

namespace Survivor.Combat
{
    public sealed class AreaEffectActor : PoolActor
    {
        public SpriteRenderer body;
        private readonly HashSet<ulong> hit = new HashSet<ulong>();
        private Vector2 center;
        private float radius, duration, remaining, delay, damage;
        private bool expanding;
        private Color color;
        public void Fire(Vector2 position, float reach, float rawDamage, float seconds, float warning, bool expand, Color tint)
        {
            center = position; transform.position = position; radius = reach; damage = rawDamage;
            duration = remaining = seconds; delay = warning; expanding = expand; color = tint;
            body.color = new Color(tint.r, tint.g, tint.b, warning > 0 ? 0.12f : 0.35f);
            transform.localScale = Vector3.one * reach * 2;
        }
        public void Tick(float delta, BattleWorld world)
        {
            if (delay > 0) { delay = Mathf.Max(0, delay - delta); return; }
            float progress = 1 - remaining / duration;
            float reach = expanding ? Mathf.Lerp(0.2f, radius, progress) : radius;
            transform.localScale = Vector3.one * reach * 2;
            body.color = new Color(color.r, color.g, color.b, 0.12f + 0.22f * (remaining / duration));
            foreach (var enemy in world.Enemies.Items)
            {
                if (damage <= 0) break;
                if (!enemy.IsAlive || hit.Contains(enemy.Token) || world.DisplayCapacity <= 0) continue;
                if ((enemy.hitbox.ClosestPoint(center) - center).sqrMagnitude > reach * reach) continue;
                hit.Add(enemy.Token); world.DealEnemyDamage(enemy, damage);
            }
            remaining -= delta;
            if (remaining <= 0) world.Areas.Return(this);
        }
        public override void ResetForRent() { hit.Clear(); remaining = 0; delay = 0; body.color = Color.clear; }
        public override void ResetForReturn() => ResetForRent();
    }
}
