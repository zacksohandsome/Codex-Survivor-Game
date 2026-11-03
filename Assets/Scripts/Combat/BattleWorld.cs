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
        private static readonly Unity.Profiling.ProfilerMarker TickMarker=new Unity.Profiling.ProfilerMarker("Survivor.World");
        private static readonly Unity.Profiling.ProfilerMarker SeparationMarker=new Unity.Profiling.ProfilerMarker("Survivor.Separation");
        private static readonly Unity.Profiling.ProfilerMarker ProjectilesMarker=new Unity.Profiling.ProfilerMarker("Survivor.Projectiles");
        private static readonly Unity.Profiling.ProfilerMarker PickupsMarker=new Unity.Profiling.ProfilerMarker("Survivor.Pickups");
        public static BattleWorld Instance { get; private set; }
        public BattleConfig config;
        public PlayerActor playerPrefab;
        public EnemyActor enemyPrefab;
        public ProjectileActor projectilePrefab;
        public DamageNumber damageNumberPrefab;
        public PickupActor pickupPrefab;
        public AreaEffectActor areaPrefab;
        public HostileProjectileActor hostileProjectilePrefab;
        public Survivor.Visuals.BurstActor burstPrefab;
        public BoundedPool<Survivor.Visuals.BurstActor> Bursts { get; private set; }
        public Camera battleCamera;
        public PlayerActor Player { get; private set; }
        public BoundedPool<EnemyActor> Enemies { get; private set; }
        public BoundedPool<EnemyActor> Specials { get; private set; }
        public BoundedPool<HostileProjectileActor> HostileProjectiles { get; private set; }
        public EnemyActor[] Combatants { get; private set; }
        public EncounterTimeline Timeline { get; } = new EncounterTimeline();
        public int DifficultyStage { get; private set; }
        public string NoticeKey { get; private set; }
        public float NoticeRemaining { get; private set; }
        public EnemyActor Boss { get { foreach (var e in Specials.Items) if (e.IsRented && e.Kind == EnemyKind.Boss) return e; return null; } }
        public float OrdinarySpawnRate
        {
            get
            {
                bool elite = false, boss = false;
                foreach (var e in Specials.Items) if (e.IsRented) { elite |= e.Kind == EnemyKind.Elite; boss |= e.Kind == EnemyKind.Boss; }
                return EncounterTimeline.SpawnRate(elite, boss, config.encounters == null ? 0.6f : config.encounters.specialSpawnRate);
            }
        }
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
        private Vector2[] separationPositions;
        private float[] separationRadii;
        private bool[] separationAlive,separationMoved;
        private void Awake()
        {
            Instance = this; random = new System.Random(config.randomSeed);
            Player = Instantiate(playerPrefab, transform); Player.gameObject.SetActive(true); Player.Initialize(config);
            Enemies = new BoundedPool<EnemyActor>(config.enemyCapacity, () => Instantiate(enemyPrefab, transform));
            Specials = new BoundedPool<EnemyActor>(2, () => Instantiate(enemyPrefab, transform));
            Combatants = new EnemyActor[config.enemyCapacity + 2];
            separationPositions=new Vector2[Combatants.Length];separationRadii=new float[Combatants.Length];
            separationAlive=new bool[Combatants.Length];separationMoved=new bool[Combatants.Length];
            System.Array.Copy(Enemies.Items, Combatants, config.enemyCapacity);
            System.Array.Copy(Specials.Items, 0, Combatants, config.enemyCapacity, 2);
            HostileProjectiles = new BoundedPool<HostileProjectileActor>(config.encounters.hostileProjectileCapacity, () => Instantiate(hostileProjectilePrefab, transform));
            Projectiles = new BoundedPool<ProjectileActor>(config.projectileCapacity, () => Instantiate(projectilePrefab, transform));
            DamageNumbers = new BoundedPool<DamageNumber>(config.damageNumberCapacity, () => Instantiate(damageNumberPrefab, transform));
            Pickups = new PickupManager(this, pickupPrefab, config.progression);
            Areas = new BoundedPool<AreaEffectActor>(config.areaCapacity, () => Instantiate(areaPrefab, transform));
            if(burstPrefab!=null)Bursts=new BoundedPool<Survivor.Visuals.BurstActor>(48,()=>Instantiate(burstPrefab,transform));
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
            using var tickScope=TickMarker.Auto();
            NoticeRemaining = Mathf.Max(0, NoticeRemaining - delta);
            if(Bursts!=null)foreach(var burst in Bursts.Items)if(burst.IsRented){burst.Tick(delta);if(burst.Remaining<=0)Bursts.Return(burst);}
            ApplyBuild();
            foreach (var number in DamageNumbers.Items)
            {
                if (!number.IsRented) continue;
                number.Tick(delta, config.damageNumberRiseSpeed);
                if (number.Remaining <= 0) DamageNumbers.Return(number);
            }
            Player.Tick(delta, movement); UpdateCamera();
            // Telegraph damage must test the player's current frame position, including a last-moment dodge.
            Physics2D.SyncTransforms();
            foreach (var enemy in Combatants)
            {
                if (!enemy.IsRented) continue;
                var p = enemy.transform.position;
                if (Mathf.Abs(p.x) > config.arenaHalfSize.x || Mathf.Abs(p.y) > config.arenaHalfSize.y) { ReturnEnemy(enemy); continue; }
                enemy.Tick(delta, Player.transform.position, config, ViewBounds, this);
                if (flow.Session.State != GameState.Battle) break;
            }
            if (flow.Session.State != GameState.Battle) return;
            using(SeparationMarker.Auto()) SeparateEnemies(delta);
            Physics2D.SyncTransforms();
            if (AutomaticAttacking)
            {
                if (CurrentBuild.Level(config.weapon.id) > 0) Weapon.Tick(delta, this, config.weapon);
                foreach (var weapon in flow.Config.weapons)
                    if (weapon.id != config.weapon.id && CurrentBuild.Level(weapon.id) > 0) controllers[weapon.id].Tick(delta, this, weapon);
            }
            Passives.Tick(delta, this);
            using(ProjectilesMarker.Auto()) foreach (var projectile in Projectiles.Items) if (projectile.IsRented) projectile.Tick(delta, this);
            foreach (var area in Areas.Items) if (area.IsRented) area.Tick(delta, this);
            foreach (var projectile in HostileProjectiles.Items)
            {
                if (flow.Session.State != GameState.Battle) break;
                if (projectile.IsRented) projectile.Tick(delta, this);
            }
            foreach (var enemy in Combatants)
            {
                if (!enemy.IsAlive || Player.Health.InvincibleRemaining > 0) continue;
                if (enemy.hitbox.Distance(Player.hitbox).distance <= 0)
                {
                    if(HitPlayer(enemy.ContactDamage)>0)enemy.Motion.Strike((Vector2)Player.transform.position-(Vector2)enemy.transform.position);
                    if (flow.Session.State != GameState.Battle) break;
                }
            }
            if (flow.Session.State == GameState.Battle) using(PickupsMarker.Auto()) Pickups.Tick(delta);
            if (flow.Session.State == GameState.Battle && CurrentBuild.RegenAmount > 0)
            {
                regenerationRemaining -= delta;
                float interval = Mathf.Max(1, config.progression.regenInterval - CurrentBuild.RegenIntervalReduction);
                while (regenerationRemaining <= 0) { Player.Health.Heal(CurrentBuild.RegenAmount); regenerationRemaining += interval; }
            }
            if (flow.Session.State == GameState.Battle && AutomaticSpawning)
            {
                TickEncounters(flow.Session.BattleSeconds);
                spawnRemaining = Mathf.Max(0, spawnRemaining - delta);
                if (spawnRemaining == 0 && TrySpawnEnemy()) spawnRemaining = config.encounters.spawnIntervals[DifficultyStage] / OrdinarySpawnRate;
            }
            SyncHud(flow.Session.Run);
        }
        private void SeparateEnemies(float delta)
        {
            var items = Combatants;
            // Read native Transform/Collider state once per actor. Preserve pair order and immediate
            // position updates in the scratch buffer, then write only actors that actually moved.
            for(int i=0;i<items.Length;i++)
            {
                separationAlive[i]=items[i].IsAlive;separationMoved[i]=false;
                if(!separationAlive[i])continue;
                separationPositions[i]=items[i].transform.position;separationRadii[i]=items[i].hitbox.radius;
            }
            for (int i = 0; i < items.Length; i++)
            {
                if (!separationAlive[i]) continue;
                for (int j = i + 1; j < items.Length; j++)
                {
                    if (!separationAlive[j]) continue;
                    Vector2 difference = separationPositions[i] - separationPositions[j];
                    float desired = separationRadii[i] + separationRadii[j];
                    if (difference.sqrMagnitude >= desired*desired) continue;
                    float length=difference.magnitude;
                    Vector2 direction = length > 0.001f ? difference / length : Vector2.right;
                    Vector2 push = direction * Mathf.Min((desired - length) / 2, config.separationSpeed * delta);
                    separationPositions[i] = PlayerActor.ClampPosition(separationPositions[i] + push, config.arenaHalfSize, separationRadii[i]);
                    separationPositions[j] = PlayerActor.ClampPosition(separationPositions[j] - push, config.arenaHalfSize, separationRadii[j]);
                    separationMoved[i]=separationMoved[j]=true;
                }
            }
            for(int i=0;i<items.Length;i++)if(separationMoved[i])items[i].transform.position=separationPositions[i];
        }
        public bool TrySpawnEnemy()
        {
            if (Enemies.ActiveCount >= config.enemyCapacity) return false;
            if (!SpawnPlanner.Choose(config.arenaHalfSize, config.enemy.radius, Player.transform.position, ViewBounds,
                config.minSpawnDistance, config.spawnCandidates, random, out var position)) return false;
            double roll = random.NextDouble(); var encounters = config.encounters;
            var definition = roll < encounters.bomberChance[DifficultyStage] ? encounters.bomber : roll < encounters.bomberChance[DifficultyStage] + encounters.rangedChance[DifficultyStage] ? encounters.ranged : config.enemy;
            return SpawnEnemy(definition, position, config.spawnWarningSeconds, true) != null;
        }
        public EnemyActor SpawnEnemy(Vector2 position, float warningSeconds)
            => SpawnEnemy(config.enemy, position, warningSeconds);
        public EnemyActor SpawnEnemy(EnemyDefinition definition, Vector2 position, float warningSeconds, bool stageScaled = false)
        {
            bool special = definition.kind == EnemyKind.Elite || definition.kind == EnemyKind.Boss;
            var enemy = special ? Specials.Rent() : Enemies.Rent();
            if (enemy != null)
            {
                enemy.Initialize(definition, position, warningSeconds,
                    stageScaled ? config.encounters.healthScale[DifficultyStage] : 1,
                    stageScaled ? config.encounters.damageScale[DifficultyStage] : 1,
                    stageScaled ? config.encounters.speedScale[DifficultyStage] : 1);
                EmitBurst(position,new Color(0.5f,0.8f,1),special);
                GameFlow.Instance.Audio.PlayCombat("spawn",0.5f);
            }
            return enemy;
        }
        public ProjectileActor LaunchProjectile(Vector2 position, Vector2 direction, float damage, WeaponDefinition weapon, int extraHits = 0, float orbitAngle = 0)
        {
            int maxHits = weapon.maxHits + extraHits;
            if (direction.sqrMagnitude == 0 || DisplayCapacity < maxHits) return null;
            var projectile = Projectiles.Rent();
            if (projectile == null) return null;
            ReservedNumbers += maxHits;
            projectile.Fire(position, direction, damage, weapon.projectileSpeed, weapon.projectileLifetime, maxHits, Combatants.Length);
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
            EmitBurst(hitPosition,new Color(1,0.8f,0.4f),false);
            EmitNumber(enemy.transform.position, damage, false);
            if (enemy.Health.Current == 0)
            {
                var position = enemy.transform.position;
                var definition = enemy.Definition;
                EmitBurst(position,definition.kind==EnemyKind.Boss?new Color(0.8f,0.3f,1):new Color(0.9f,0.5f,0.2f),true);
                GameFlow.Instance.Audio.PlayCombat("death",0.15f);
                if (enemy.Kind == EnemyKind.Boss) Notify("encounter.boss.defeated");
                ReturnEnemy(enemy); GameFlow.Instance.Session.Run.Kills++;
                Pickups.Spawn(position, definition == config.enemy ? config.progression.enemyExperience : definition.experience, 0);
                float chance = definition == config.enemy ? config.progression.currencyChance : definition.currencyChance;
                int value = definition == config.enemy ? config.progression.currencyValue : definition.currencyValue;
                if (random.NextDouble() < chance) Pickups.Spawn(position, 0, value);
            }
            if (triggerEcho) Passives.OnHit(this, enemy, hitPosition, raw);
        }
        public AreaEffectActor EmitArea(Vector2 position, float radius, float damage, float seconds, float delay, bool expanding, Color tint)
        {
            var area = Areas.Rent();
            if (area != null) { area.Fire(position, radius, damage, seconds, delay, expanding, tint); EmitBurst(position,tint,true); GameFlow.Instance.Audio.PlayCombat("skill",0.3f); }
            return area;
        }
        public void RecycleProjectile(ProjectileActor projectile)
        {
            if (!projectile.IsRented) return;
            ReservedNumbers -= projectile.RemainingHits; Projectiles.Return(projectile);
        }
        public void ReturnEnemy(EnemyActor enemy)
        { if (enemy.IsSpecial) Specials.Return(enemy); else Enemies.Return(enemy); }
        public bool ChooseBossRanged() => random.Next(2) == 0;
        public void Notify(string key)
        {
            NoticeKey = key; NoticeRemaining = 4;
            GameFlow.Instance.Audio.PlayUi(key=="encounter.elite"?"elite":key=="encounter.boss"?"boss":key=="encounter.boss.defeated"?"boss.defeated":"stage");
        }
        public Survivor.Visuals.BurstActor EmitBurst(Vector2 position,Color color,bool strong)
        {
            var burst=Bursts?.Rent();if(burst!=null)burst.Fire(position,color,strong);return burst;
        }
        public HostileProjectileActor FireHostile(Vector2 position, Vector2 direction, float raw, float speed, float lifetime)
        {
            if (GameFlow.Instance.Session.State != GameState.Battle || direction.sqrMagnitude == 0) return null;
            var shot = HostileProjectiles.Rent();
            if (shot != null) shot.Fire(position, direction, speed, raw, lifetime);
            return shot;
        }
        public int HitPlayer(float raw)
        {
            if (GameFlow.Instance.Session.State != GameState.Battle) return 0;
            int damage = Player.Health.TakeDamage(raw);
            if(damage>0){GameFlow.Instance.Audio.PlayCombat("hurt",0.1f);EmitBurst(Player.transform.position,new Color(1,0.25f,0.25f),true);}
            if (Player.Health.Current == 0) GameFlow.Instance.Session.Finish(false);
            else if (damage > 0) Passives.OnPlayerHit(this);
            return damage;
        }
        public void HitPlayerInCircle(Vector2 center, float radius, float raw)
        {
            if ((Player.hitbox.ClosestPoint(center) - center).sqrMagnitude <= radius * radius) HitPlayer(raw);
        }
        public void TickEncounters(double seconds)
        {
            var settings = config.encounters;
            int stage = EncounterTimeline.Stage(seconds);
            if (stage != DifficultyStage) { DifficultyStage = stage; Notify("encounter.stage." + (stage + 1)); }
            if (Timeline.EliteDue(seconds, settings.eliteSeconds) && TrySpawnSpecial(settings.elite))
            { Timeline.MarkElite(); Notify("encounter.elite"); }
            if (Timeline.BossDue(seconds, settings.bossSeconds) && TrySpawnSpecial(settings.boss))
            { Timeline.MarkBoss(); Notify("encounter.boss"); }
        }
        private bool TrySpawnSpecial(EnemyDefinition definition)
        {
            if (!SpawnPlanner.Choose(config.arenaHalfSize, definition.radius, Player.transform.position, ViewBounds,
                config.minSpawnDistance, config.spawnCandidates, random, out var position)) return false;
            return SpawnEnemy(definition, position, config.spawnWarningSeconds) != null;
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
            foreach (var enemy in Specials.Items) Specials.Return(enemy);
            foreach (var projectile in HostileProjectiles.Items) HostileProjectiles.Return(projectile);
            foreach (var projectile in Projectiles.Items) RecycleProjectile(projectile);
            foreach (var number in DamageNumbers.Items) DamageNumbers.Return(number);
            foreach (var area in Areas.Items) Areas.Return(area);
            if(Bursts!=null)foreach(var burst in Bursts.Items)Bursts.Return(burst);
            Pickups.Clear(); Player.hitbox.enabled = false;
        }
        public void PresentDefeat(float remaining)
        {
            var tint=Player.tint;tint.a=Mathf.Clamp01(remaining/0.45f);Player.body.color=tint;
            Player.body.transform.localScale = Player.Motion.BaseScale * (0.6f + 0.3f * Mathf.Clamp01(remaining / 0.45f));
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
