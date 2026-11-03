using Survivor.Combat;
using Survivor.Data;
using Survivor.Progression;
using Survivor.Core;
using Survivor.Meta;
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
        public void ApplyBuild(BuildState build)
        {
            var levels = GameFlow.Instance.PermanentData.metaUpgradeLevels;
            Health.IncreaseMaximum(config.playerHealth + build.HealthBonus + (int)(levels[0] * MetaProgression.Bonuses[0]) - Health.Maximum);
            Health.Defense = config.playerDefense + build.DefenseBonus + build.SanctuaryDefense + levels[4] * MetaProgression.Bonuses[4];
            Attack = config.playerAttack + build.AttackBonus + levels[1] * MetaProgression.Bonuses[1];
            AttackSpeed = Mathf.Clamp(config.playerAttackSpeed + build.AttackSpeedBonus + levels[2] * MetaProgression.Bonuses[2], 0.1f, config.attackSpeedCap);
            MoveSpeed = Mathf.Clamp(config.playerMoveSpeed + build.MoveSpeedBonus + levels[3] * MetaProgression.Bonuses[3], 0, config.moveSpeedCap);
        }
        public static Vector2 ClampPosition(Vector2 value, Vector2 half, float radius)
            => new Vector2(Mathf.Clamp(value.x, -half.x + radius, half.x - radius), Mathf.Clamp(value.y, -half.y + radius, half.y - radius));
    }
}
