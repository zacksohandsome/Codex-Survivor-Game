using Survivor.Combat;
using Survivor.Data;
using UnityEngine;

namespace Survivor.Player
{
    public sealed class PlayerActor : MonoBehaviour
    {
        public SpriteRenderer body;
        public CircleCollider2D hitbox;
        public Health Health { get; private set; }
        public float Attack { get; private set; }
        public float AttackSpeed { get; private set; }
        public float MoveSpeed { get; private set; }
        private BattleConfig config;
        public void Initialize(BattleConfig settings)
        {
            config = settings; Health = new Health(settings.playerHealth, settings.playerDefense, settings.invincibleSeconds);
            Attack = settings.playerAttack; AttackSpeed = Mathf.Clamp(settings.playerAttackSpeed, 0.1f, settings.attackSpeedCap);
            MoveSpeed = Mathf.Clamp(settings.playerMoveSpeed, 0, settings.moveSpeedCap);
            hitbox.radius = settings.playerRadius; transform.position = Vector3.zero;
        }
        public void Tick(float delta, Vector2 direction)
        {
            Health.Tick(delta);
            transform.position = ClampPosition((Vector2)transform.position + Vector2.ClampMagnitude(direction, 1) * (MoveSpeed * delta), config.arenaHalfSize, config.playerRadius);
            bool blink = Health.InvincibleRemaining > 0 && (int)(Health.InvincibleRemaining / config.blinkInterval) % 2 == 0;
            body.color = blink ? new Color(1, 1, 1, 0.3f) : new Color(0.15f, 0.9f, 0.9f);
        }
        public static Vector2 ClampPosition(Vector2 value, Vector2 half, float radius)
            => new Vector2(Mathf.Clamp(value.x, -half.x + radius, half.x - radius), Mathf.Clamp(value.y, -half.y + radius, half.y - radius));
    }
}
