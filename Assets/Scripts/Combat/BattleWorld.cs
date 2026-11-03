using Survivor.Core;
using Survivor.Data;
using Survivor.Enemy;
using Survivor.Player;
using Survivor.Pooling;
using Survivor.Drop;
using Survivor.Progression;
using UnityEngine;

namespace Survivor.Combat
{
    [DefaultExecutionOrder(100)]
    public sealed class BattleWorld : MonoBehaviour
    {
        public static BattleWorld Instance { get; private set; }
        public BattleConfig config;
        public PlayerActor playerPrefab;
        public EnemyActor enemyPrefab;
        public ProjectileActor projectilePrefab;
        public DamageNumber damageNumberPrefab;
        public PickupActor pickupPrefab;
        public AreaEffectActor areaPrefab;
        public Camera battleCamera;
        public PlayerActor Player { get; private set; }
        public BoundedPool<EnemyActor> Enemies { get; private set; }
        public BoundedPool<ProjectileActor> Projectiles { get; private set; }
        public BoundedPool<DamageNumber> DamageNumbers { get; private set; }
        public BoundedPool<AreaEffectActor> Areas { get; private set; }
        public WeaponController Weapon { get; } = new WeaponController();
        public PassiveController Passives { get; } = new PassiveController();
        private readonly System.Collections.Generic.Dictionary<string, WeaponController> controllers = new System.Collections.Generic.Dictionary<string, WeaponController>();
        public WeaponController ControllerFor(string id) => id == config.weapon.id ? Weapon : controllers[id];
        public PickupManager Pickups { get; private set; }
        public BuildState CurrentBuild => GameFlow.Instance.Session.Run.Build;
        public int EnemyLayerMask => 1 << enemyPrefab.gameObject.layer;
        public int ReservedNumbers { get; private set; }
        public int DisplayCapacity => config.damageNumberCapacity - DamageNumbers.ActiveCount - ReservedNumbers;
        public bool AutomaticSpawning { get; set; } = true;
        public bool AutomaticAttacking { get; set; } = true;
        private float spawnRemaining;
        private System.Random random;
        private float regenerationRemaining;
        private int buildRevision = -1;
        private void Awake()
        {
            Instance = this; random = new System.Random(config.randomSeed);
            Player = Instantiate(playerPrefab, transform); Player.gameObject.SetActive(true); Player.Initialize(config);
            Enemies = new BoundedPool<EnemyActor>(config.enemyCapacity, () => Instantiate(enemyPrefab, transform));
            Projectiles = new BoundedPool<ProjectileActor>(config.projectileCapacity, () => Instantiate(projectilePrefab, transform));
            DamageNumbers = new BoundedPool<DamageNumber>(config.damageNumberCapacity, () => Instantiate(damageNumberPrefab, transform));
            Pickups = new PickupManager(this, pickupPrefab, config.progression);
            Areas = new BoundedPool<AreaEffectActor>(config.areaCapacity, () => Instantiate(areaPrefab, transform));
            foreach (var weapon in GameFlow.Instance.Config.weapons)
                if (weapon.id != config.weapon.id) controllers.Add(weapon.id, new WeaponController());
            regenerationRemaining = config.progression.regenInterval;
            UpdateCamera();
        }
        private void Update()
        {
            var flow = GameFlow.Instance;
            if (flow == null || flow.IsTransitioning || flow.Session.Run == null) return;
            ApplyBuild();
            SyncHud(flow.Session.Run);
            if (flow.Session.State == GameState.Battle) Tick(Time.deltaTime, flow.Movement);
        }
        public Rect ViewBounds
        {
            get
            {
                float h = battleCamera.orthographicSize, w = h * (16f / 9f);
                return new Rect(battleCamera.transform.position.x - w, battleCamera.transform.position.y - h, w * 2, h * 2);
            }
        }
        private void UpdateCamera()
        {
            const float aspect = 16f / 9f;
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            battleCamera.rect = screenAspect >= aspect
                ? new Rect((1 - aspect / screenAspect) / 2, 0, aspect / screenAspect, 1)
                : new Rect(0, (1 - screenAspect / aspect) / 2, 1, screenAspect / aspect);
            battleCamera.orthographicSize = config.cameraHalfHeight;
            battleCamera.transform.position = ArenaCamera.Follow(Player.transform.position, config.arenaHalfSize, config.cameraHalfHeight, aspect);
        }
        public void Tick(float delta, Vector2 movement)
        {
            var flow = GameFlow.Instance;
            if (flow == null || flow.IsTransitioning || flow.Session.State != GameState.Battle || delta <= 0) return;
            ApplyBuild();
            foreach (var number in DamageNumbers.Items)
            {
                if (!number.IsRented) continue;
                number.Tick(delta, config.damageNumberRiseSpeed);
                if (number.Remaining <= 0) DamageNumbers.Return(number);
            }
            Player.Tick(delta, movement); UpdateCamera();
            foreach (var enemy in Enemies.Items)
            {
                if (!enemy.IsRented) continue;
                var p = enemy.transform.position;
                if (Mathf.Abs(p.x) > config.arenaHalfSize.x || Mathf.Abs(p.y) > config.arenaHalfSize.y) { Enemies.Return(enemy); continue; }
                enemy.Tick(delta, Player.transform.position, config, ViewBounds);
            }
            SeparateEnemies(delta);
            Physics2D.SyncTransforms();
            if (AutomaticAttacking)
            {
                if (CurrentBuild.Level(config.weapon.id) > 0) Weapon.Tick(delta, this, config.weapon);
                foreach (var weapon in flow.Config.weapons)
                    if (weapon.id != config.weapon.id && CurrentBuild.Level(weapon.id) > 0) controllers[weapon.id].Tick(delta, this, weapon);
            }
            Passives.Tick(delta, this);
            foreach (var projectile in Projectiles.Items) if (projectile.IsRented) projectile.Tick(delta, this);
            foreach (var area in Areas.Items) if (area.IsRented) area.Tick(delta, this);
            foreach (var enemy in Enemies.Items)
            {
                if (!enemy.IsAlive || Player.Health.InvincibleRemaining > 0 || DisplayCapacity <= 0) continue;
                if (enemy.hitbox.Distance(Player.hitbox).distance <= 0)
                {
                    int damage = Player.Health.TakeDamage(config.enemy.contactDamage);
                    if (damage > 0) EmitNumber(Player.transform.position, damage, true);
                    if (Player.Health.Current == 0) { flow.Session.Finish(false); break; }
                    if (damage > 0) Passives.OnPlayerHit(this);
                }
            }
            if (flow.Session.State == GameState.Battle) Pickups.Tick(delta);
            if (flow.Session.State == GameState.Battle && CurrentBuild.RegenAmount > 0)
            {
                regenerationRemaining -= delta;
                float interval = Mathf.Max(1, config.progression.regenInterval - CurrentBuild.RegenIntervalReduction);
                while (regenerationRemaining <= 0) { Player.Health.Heal(CurrentBuild.RegenAmount); regenerationRemaining += interval; }
            }
            if (flow.Session.State == GameState.Battle && AutomaticSpawning)
            {
                spawnRemaining = Mathf.Max(0, spawnRemaining - delta);
                if (spawnRemaining == 0 && TrySpawnEnemy()) spawnRemaining = config.spawnInterval;
            }
            SyncHud(flow.Session.Run);
        }
        private void SeparateEnemies(float delta)
        {
            var items = Enemies.Items;
            for (int i = 0; i < items.Length; i++)
            {
                if (!items[i].IsAlive) continue;
                for (int j = i + 1; j < items.Length; j++)
                {
                    if (!items[j].IsAlive) continue;
                    Vector2 difference = items[i].transform.position - items[j].transform.position;
                    float length = difference.magnitude, desired = config.enemy.radius * 2;
                    if (length >= desired) continue;
                    Vector2 direction = length > 0.001f ? difference / length : Vector2.right;
                    Vector2 push = direction * Mathf.Min((desired - length) / 2, config.separationSpeed * delta);
                    items[i].transform.position = PlayerActor.ClampPosition((Vector2)items[i].transform.position + push, config.arenaHalfSize, config.enemy.radius);
                    items[j].transform.position = PlayerActor.ClampPosition((Vector2)items[j].transform.position - push, config.arenaHalfSize, config.enemy.radius);
                }
            }
        }
        public bool TrySpawnEnemy()
        {
            if (Enemies.ActiveCount >= config.enemyCapacity) return false;
            if (!SpawnPlanner.Choose(config.arenaHalfSize, config.enemy.radius, Player.transform.position, ViewBounds,
                config.minSpawnDistance, config.spawnCandidates, random, out var position)) return false;
            return SpawnEnemy(position, config.spawnWarningSeconds) != null;
        }
        public EnemyActor SpawnEnemy(Vector2 position, float warningSeconds)
        {
            var enemy = Enemies.Rent();
            if (enemy != null) enemy.Initialize(config.enemy, position, warningSeconds);
            return enemy;
        }
        public ProjectileActor LaunchProjectile(Vector2 position, Vector2 direction, float damage, WeaponDefinition weapon, int extraHits = 0, float orbitAngle = 0)
        {
            int maxHits = weapon.maxHits + extraHits;
            if (direction.sqrMagnitude == 0 || DisplayCapacity < maxHits) return null;
            var projectile = Projectiles.Rent();
            if (projectile == null) return null;
            ReservedNumbers += maxHits;
            projectile.Fire(position, direction, damage, weapon.projectileSpeed, weapon.projectileLifetime, maxHits, config.enemyCapacity);
            projectile.ConfigurePattern(weapon.pattern, orbitAngle, weapon.areaRadius + CurrentBuild.AreaFor(weapon.id));
            return projectile;
        }
        public void ApplyProjectileHit(ProjectileActor projectile, EnemyActor enemy, float raw)
        {
            ReservedNumbers--;
            DealEnemyDamage(enemy, raw);
        }
        public void DealEnemyDamage(EnemyActor enemy, float raw, bool triggerEcho = true)
        {
            if (!enemy.IsAlive || raw <= 0 || DisplayCapacity <= 0) return;
            Vector2 hitPosition = enemy.transform.position;
            int damage = enemy.TakeDamage(raw, config.hitFlashSeconds);
            EmitNumber(enemy.transform.position, damage, false);
            if (enemy.Health.Current == 0)
            {
                var position = enemy.transform.position;
                Enemies.Return(enemy); GameFlow.Instance.Session.Run.Kills++;
                Pickups.Spawn(position, config.progression.enemyExperience, 0);
                if (random.NextDouble() < config.progression.currencyChance) Pickups.Spawn(position, 0, config.progression.currencyValue);
            }
            if (triggerEcho) Passives.OnHit(this, enemy, hitPosition, raw);
        }
        public AreaEffectActor EmitArea(Vector2 position, float radius, float damage, float seconds, float delay, bool expanding, Color tint)
        {
            var area = Areas.Rent();
            if (area != null) area.Fire(position, radius, damage, seconds, delay, expanding, tint);
            return area;
        }
        public void RecycleProjectile(ProjectileActor projectile)
        {
            if (!projectile.IsRented) return;
            ReservedNumbers -= projectile.RemainingHits; Projectiles.Return(projectile);
        }
        private void EmitNumber(Vector2 position, int damage, bool playerHit)
        {
            var number = DamageNumbers.Rent();
            if (number == null) throw new System.InvalidOperationException("Damage display reservation invariant broken");
            number.Show(position, damage, config.damageNumberSeconds, playerHit);
        }
        private void SyncHud(RunContext run)
        {
            run.CurrentHealth = Player.Health.Current; run.MaximumHealth = Player.Health.Maximum;
            run.Attack = Player.Attack; run.AttackSpeed = Player.AttackSpeed; run.MoveSpeed = Player.MoveSpeed; run.Defense = Player.Health.Defense;
        }
        public void ApplyBuild()
        {
            if (buildRevision == CurrentBuild.Revision) return;
            Player.ApplyBuild(CurrentBuild); buildRevision = CurrentBuild.Revision;
            regenerationRemaining = Mathf.Min(regenerationRemaining, Mathf.Max(1, config.progression.regenInterval - CurrentBuild.RegenIntervalReduction));
            SyncHud(GameFlow.Instance.Session.Run);
        }
        public bool TryShowCurrency(Vector2 position, long amount)
        {
            if (DisplayCapacity <= 0) return false;
            var number = DamageNumbers.Rent();
            number.ShowCurrency(position, amount, config.damageNumberSeconds); return true;
        }
        public void ClearForEnd()
        {
            foreach (var enemy in Enemies.Items) Enemies.Return(enemy);
            foreach (var projectile in Projectiles.Items) RecycleProjectile(projectile);
            foreach (var number in DamageNumbers.Items) DamageNumbers.Return(number);
            foreach (var area in Areas.Items) Areas.Return(area);
            Pickups.Clear(); Player.hitbox.enabled = false;
        }
        public void PresentDefeat(float remaining)
        {
            Player.body.color = new Color(0.15f, 0.9f, 0.9f, Mathf.Clamp01(remaining / 0.45f));
            Player.body.transform.localScale = Vector3.one * (0.6f + 0.3f * Mathf.Clamp01(remaining / 0.45f));
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
