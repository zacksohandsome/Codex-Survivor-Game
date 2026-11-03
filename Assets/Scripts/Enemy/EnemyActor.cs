using Survivor.Combat;
using Survivor.Data;
using Survivor.Pooling;
using UnityEngine;

namespace Survivor.Enemy
{
    public sealed class EnemyActor : PoolActor
    {
        public SpriteRenderer body, warning;
        public Transform healthBar, healthFill;
        public CircleCollider2D hitbox;
        public Health Health { get; private set; }
        public bool IsAlive => IsRented && warningRemaining <= 0 && Health != null && Health.Current > 0;
        public float WarningRemaining => warningRemaining;
        public int RecoveryCount { get; private set; }
        public ulong Token => ((ulong)(uint)GetInstanceID() << 32) | generation;
        private uint generation;
        private float warningRemaining, flashRemaining, stuckTimer;
        private Vector2 lastPosition;
        public void Initialize(EnemyDefinition definition, Vector2 position, float warningTime)
        {
            transform.position = position; lastPosition = position;
            Health = new Health(definition.maxHealth, definition.defense, 0);
            hitbox.radius = definition.radius; warningRemaining = warningTime;
            body.enabled = warningTime <= 0; hitbox.enabled = warningTime <= 0;
            warning.enabled = warningTime > 0;
        }
        public void Tick(float delta, Vector2 player, BattleConfig config, Rect view)
        {
            if (warningRemaining > 0)
            {
                warningRemaining = Mathf.Max(0, warningRemaining - delta);
                warning.transform.position = new Vector3(Mathf.Clamp(transform.position.x, view.xMin + 0.8f, view.xMax - 0.8f),
                    Mathf.Clamp(transform.position.y, view.yMin + 0.8f, view.yMax - 0.8f), 0);
                if (warningRemaining == 0) { warning.enabled = false; body.enabled = true; hitbox.enabled = true; }
                return;
            }
            if (!IsAlive) return;
            Vector2 position = transform.position;
            Vector2 direction = (player - position).normalized;
            position += direction * (config.enemy.moveSpeed * delta);
            stuckTimer += delta;
            if (stuckTimer >= config.stuckInterval)
            {
                if ((position - lastPosition).sqrMagnitude < config.stuckDistance * config.stuckDistance &&
                    (player - position).magnitude > config.enemy.radius + config.playerRadius)
                {
                    position += new Vector2(-direction.y, direction.x) * config.enemy.radius;
                    RecoveryCount++;
                }
                lastPosition = position; stuckTimer = 0;
            }
            transform.position = Survivor.Player.PlayerActor.ClampPosition(position, config.arenaHalfSize, config.enemy.radius);
            flashRemaining = Mathf.Max(0, flashRemaining - delta);
            body.color = flashRemaining > 0 ? Color.white : new Color(0.95f, 0.3f, 0.3f);
        }
        public int TakeDamage(float raw, float flashSeconds)
        {
            if (!IsAlive) return 0;
            int damage = Health.TakeDamage(raw); flashRemaining = flashSeconds;
            healthBar.gameObject.SetActive(true);
            healthFill.localScale = new Vector3((float)Health.Current / Health.Maximum, 1, 1);
            body.color = Color.white; return damage;
        }
        public override void ResetForRent()
        {
            generation++; Health = null; warningRemaining = 0; flashRemaining = 0; stuckTimer = 0;
            RecoveryCount = 0; body.enabled = false; body.color = new Color(0.95f, 0.3f, 0.3f);
            healthBar.gameObject.SetActive(false); healthFill.localScale = Vector3.one;
            warning.enabled = false; warning.transform.localPosition = Vector3.zero; hitbox.enabled = false;
        }
        public override void ResetForReturn()
        {
            Health = null; warningRemaining = 0; flashRemaining = 0; stuckTimer = 0;
            body.enabled = false; warning.enabled = false; hitbox.enabled = false; healthBar.gameObject.SetActive(false);
        }
    }
}
