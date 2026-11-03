using Survivor.Combat;
using Survivor.Data;
using Survivor.Pooling;
using UnityEngine;

namespace Survivor.Enemy
{
    public enum EnemyAttack { None, Shot, Explosion, BossMelee, BossVolley }
    public sealed class EnemyActor : PoolActor
    {
        public SpriteRenderer body, warning;
        public Transform healthBar, healthFill;
        public CircleCollider2D hitbox;
        public Health Health { get; private set; }
        public EnemyDefinition Definition { get; private set; }
        public EnemyKind Kind => Definition == null ? EnemyKind.Melee : Definition.kind;
        public bool IsSpecial => Kind == EnemyKind.Elite || Kind == EnemyKind.Boss;
        public bool IsAlive => IsRented && warningRemaining <= 0 && Health != null && Health.Current > 0;
        public float WarningRemaining => warningRemaining;
        public float AttackWarningRemaining { get; private set; }
        public float AttackCooldownRemaining { get; private set; }
        public EnemyAttack Attack { get; private set; }
        public Vector2 TelegraphCenter { get; private set; }
        public float TelegraphRadius { get; private set; }
        public float MoveSpeed { get; private set; }
        public float ContactDamage { get; private set; }
        public int RecoveryCount { get; private set; }
        public ulong Token => ((ulong)(uint)GetInstanceID() << 32) | generation;
        private uint generation;
        private float warningRemaining, flashRemaining, stuckTimer;
        private Vector2 lastPosition;
        private Sprite fallbackSprite;
        public Survivor.Visuals.CharacterMotion Motion { get; private set; }
        public void Initialize(EnemyDefinition definition, Vector2 position, float warningTime, float healthScale = 1, float damageScale = 1, float speedScale = 1)
        {
            Definition = definition; transform.position = position; lastPosition = position;
            Health = new Health(Mathf.CeilToInt(definition.maxHealth * healthScale), definition.defense, 0);
            MoveSpeed = definition.moveSpeed * speedScale; ContactDamage = definition.contactDamage * damageScale;
            hitbox.radius = definition.radius; warningRemaining = warningTime;
            body.enabled = warningTime <= 0; hitbox.enabled = warningTime <= 0; warning.enabled = warningTime > 0;
            if(fallbackSprite==null)fallbackSprite=body.sprite;
            body.sprite=definition.sprite!=null?definition.sprite:fallbackSprite;body.color=definition.color;
            Motion=GetComponent<Survivor.Visuals.CharacterMotion>()??gameObject.AddComponent<Survivor.Visuals.CharacterMotion>();
            Motion.Initialize(body,definition.radius*3.2f);
            healthBar.localPosition = Vector3.up * (definition.radius + 0.3f);
            healthBar.localScale = new Vector3(definition.radius * 2, 0.1f, 1);
            AttackCooldownRemaining = definition.attackCooldown;
            warning.transform.localScale = Vector3.one * definition.radius * 2.5f;
        }
        public void Tick(float delta, Vector2 player, BattleConfig config, Rect view, BattleWorld world = null)
        {
            if (warningRemaining > 0)
            {
                warningRemaining = Mathf.Max(0, warningRemaining - delta);
                warning.transform.position = new Vector3(Mathf.Clamp(transform.position.x, view.xMin + 0.8f, view.xMax - 0.8f), Mathf.Clamp(transform.position.y, view.yMin + 0.8f, view.yMax - 0.8f), 0);
                if (warningRemaining == 0) { warning.enabled = false; body.enabled = true; hitbox.enabled = true; }
                return;
            }
            if (!IsAlive) return;
            flashRemaining = Mathf.Max(0, flashRemaining - delta);
            body.color = flashRemaining > 0 ? new Color(1,0.45f,0.45f) : Definition.color;
            if (world != null && TickAttack(delta, player, world)) { Motion.Tick(delta,Vector2.zero); return; }
            Vector2 position = transform.position, direction = (player - position).normalized;
            if (Kind == EnemyKind.Ranged)
            {
                float distance = Vector2.Distance(player, position);
                direction = distance < 5 ? -direction : distance > 8 ? direction : Vector2.zero;
            }
            position += direction * (MoveSpeed * delta); stuckTimer += delta;
            if (stuckTimer >= config.stuckInterval)
            {
                if (direction.sqrMagnitude > 0 && (position - lastPosition).sqrMagnitude < config.stuckDistance * config.stuckDistance && (player - position).magnitude > Definition.radius + config.playerRadius)
                { position += new Vector2(-direction.y, direction.x) * Definition.radius; RecoveryCount++; }
                lastPosition = position; stuckTimer = 0;
            }
            transform.position = Survivor.Player.PlayerActor.ClampPosition(position, config.arenaHalfSize, Definition.radius);
            Motion.Tick(delta,direction);
        }
        private bool TickAttack(float delta, Vector2 player, BattleWorld world)
        {
            Vector2 position = transform.position;
            if (AttackWarningRemaining > 0)
            {
                AttackWarningRemaining = Mathf.Max(0, AttackWarningRemaining - delta);
                if (Attack == EnemyAttack.Shot || Attack == EnemyAttack.BossVolley) DrawAim(player);
                if (AttackWarningRemaining == 0)
                {
                    warning.enabled = false;
                    if (Attack == EnemyAttack.Explosion)
                    { world.HitPlayerInCircle(TelegraphCenter, TelegraphRadius, ContactDamage * 1.5f); world.ReturnEnemy(this); return true; }
                    if (Attack == EnemyAttack.BossMelee) world.HitPlayerInCircle(TelegraphCenter, TelegraphRadius, ContactDamage * 1.3f);
                    else
                    {
                        Vector2 direction = (player - position).normalized;
                        int count = Attack == EnemyAttack.BossVolley ? 5 : 1;
                        for (int i = 0; i < count; i++)
                        {
                            float angle = count == 1 ? 0 : (i - 2) * 12 * Mathf.Deg2Rad;
                            Vector2 shot = new Vector2(direction.x * Mathf.Cos(angle) - direction.y * Mathf.Sin(angle), direction.x * Mathf.Sin(angle) + direction.y * Mathf.Cos(angle));
                            world.FireHostile(position, shot, ContactDamage, Definition.projectileSpeed, Definition.projectileLifetime);
                        }
                    }
                    if (!IsRented) return true;
                    Attack = EnemyAttack.None; AttackCooldownRemaining = Definition.attackCooldown;
                }
                return true;
            }
            AttackCooldownRemaining = Mathf.Max(0, AttackCooldownRemaining - delta);
            if (Kind == EnemyKind.Bomber && Vector2.Distance(player, position) <= Definition.attackRange)
            { BeginAttack(EnemyAttack.Explosion, position, Definition.blastRadius); return true; }
            if (AttackCooldownRemaining > 0) return false;
            if (Kind == EnemyKind.Ranged && Vector2.Distance(player, position) <= Definition.attackRange)
            { BeginAttack(EnemyAttack.Shot, position, 0); DrawAim(player); return true; }
            if (Kind == EnemyKind.Boss)
            {
                bool ranged = world.ChooseBossRanged();
                BeginAttack(ranged ? EnemyAttack.BossVolley : EnemyAttack.BossMelee, position, ranged ? 0 : Definition.blastRadius);
                if (ranged) DrawAim(player); return true;
            }
            return false;
        }
        private void BeginAttack(EnemyAttack type, Vector2 center, float radius)
        {
            Motion.Strike(Vector2.right);
            Attack = type; AttackWarningRemaining = Definition.windupSeconds; TelegraphCenter = center; TelegraphRadius = radius;
            warning.enabled = true; warning.transform.position = center; warning.transform.localScale = Vector3.one * radius * 2;
            warning.transform.rotation = Quaternion.identity; warning.color = new Color(1, 0.2f, 0.1f, 0.45f);
        }
        private void DrawAim(Vector2 player)
        {
            Vector2 position = transform.position, direction = player - position;
            warning.transform.position = (position + player) * 0.5f;
            warning.transform.localScale = new Vector3(direction.magnitude, 0.1f, 1);
            warning.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
        public int TakeDamage(float raw, float flashSeconds)
        {
            if (!IsAlive) return 0;
            int damage = Health.TakeDamage(raw); flashRemaining = flashSeconds; healthBar.gameObject.SetActive(true);
            healthFill.localScale = new Vector3((float)Health.Current / Health.Maximum, 1, 1); body.color = new Color(1,0.45f,0.45f); return damage;
        }
        public override void ResetForRent()
        {
            Motion?.ResetPose();
            generation++; Health = null; Definition = null; warningRemaining = flashRemaining = stuckTimer = 0; RecoveryCount = 0; body.enabled = false;
            healthBar.gameObject.SetActive(false); healthFill.localScale = Vector3.one;
            warning.enabled = false; warning.transform.localPosition = Vector3.zero; warning.transform.rotation = Quaternion.identity;
            warning.color = new Color(1, 0.65f, 0.1f, 0.6f); hitbox.enabled = false;
            AttackWarningRemaining = AttackCooldownRemaining = 0; Attack = EnemyAttack.None;
            TelegraphRadius = 0; TelegraphCenter = Vector2.zero; MoveSpeed = ContactDamage = 0;
        }
        public override void ResetForReturn() => ResetForRent();
    }
}
