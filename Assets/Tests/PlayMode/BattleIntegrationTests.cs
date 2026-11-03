using System.Collections;
using NUnit.Framework;
using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Survivor.Tests
{
    public class BattleIntegrationTests
    {
        private BattleWorld world;
        private BattleConfig config;
        private bool background;
        private Keyboard keyboard;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameFlow.SaveDirectoryOverride = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Survivor-PlayTests-" + System.Guid.NewGuid().ToString("N"));
            background = Application.runInBackground; Application.runInBackground = true;
            if (GameFlow.Instance != null) { Object.Destroy(GameFlow.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("MainMenu");
            var flow = GameFlow.Instance; flow.OpenWeaponSelection(); flow.SelectWeapon("bolt"); flow.StartRun();
            float deadline = Time.realtimeSinceStartup + 10;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow.IsTransitioning, Is.False);
            world = BattleWorld.Instance; Assert.That(world, Is.Not.Null);
            world.enabled = false; world.AutomaticSpawning = false; world.AutomaticAttacking = false;
            config = Object.Instantiate(world.config); config.enemy = Object.Instantiate(config.enemy); config.weapon = Object.Instantiate(config.weapon);
            world.config = config; world.Player.Initialize(config);
            foreach (var enemy in world.Enemies.Items) world.Enemies.Return(enemy);
            config.enemy.moveSpeed = 0;
            flow.Session.Tick(3);
            Assert.That(flow.Session.State, Is.EqualTo(GameState.Battle));
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (GameFlow.Instance != null) Object.Destroy(GameFlow.Instance.gameObject);
            if (world != null) Object.Destroy(world.gameObject);
            if (config != null) { Object.Destroy(config.enemy); Object.Destroy(config.weapon); Object.Destroy(config); }
            yield return null; Time.timeScale = 1; Application.runInBackground = background;
        }
        [UnityTest, Category("Performance")]
        public IEnumerator SeparationAtFixedCapacityStaysWithinTargetMachineBudget()
        {
            for(int i=0;i<64;i++)world.SpawnEnemy(new Vector2(-10+i%16*1.3f,-6+i/16*3),0);
            var method=typeof(BattleWorld).GetMethod("SeparateEnemies",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var solve=(System.Action<float>)System.Delegate.CreateDelegate(typeof(System.Action<float>),world,method);
            for(int i=0;i<10;i++)solve(0.016f);
            var timer=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<1000;i++)solve(0.016f);timer.Stop();
            Assert.That(timer.Elapsed.TotalMilliseconds,Is.LessThan(300),"1000 separation updates on the M0 target machine");
            yield return null;
        }
        [UnityTest, Category("Performance")]
        public IEnumerator UnchangedHudAvoidsRepeatedFormattingAllocations()
        {
            var flow=GameFlow.Instance;flow.enabled=false;var view=flow.GetComponent<Survivor.UI.FlowView>();view.RefreshClock();
            using(var allocations=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",1))
            {
                yield return null;long baseline=allocations.LastValue;
                for(int i=0;i<1000;i++)view.RefreshClock();
                yield return null;long measured=allocations.LastValue;
                Assert.That(measured-baseline,Is.LessThan(100000),"1000 unchanged HUD refreshes should not format and box every field again");
            }
        }
        [UnityTest] public IEnumerator HudCacheRefreshesOnHealthCurrencyClockAndLocaleChanges()
        {
            var flow=GameFlow.Instance;flow.enabled=false;var view=flow.GetComponent<Survivor.UI.FlowView>();view.RefreshClock();
            world.HitPlayer(14);world.Tick(0.01f,Vector2.zero);view.RefreshClock();
            var labels=GameObject.Find("Vitals").GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            StringAssert.Contains(world.Player.Health.Current.ToString(),labels[0].text);
            flow.Session.Run.CollectedCurrency=19;view.RefreshClock();StringAssert.Contains("19",labels[1].text);
            var clockLabel=GameObject.Find("ClockAndBoss").GetComponentsInChildren<TMPro.TextMeshProUGUI>()[0];string before=clockLabel.text;
            flow.Session.Tick(2);view.RefreshClock();Assert.That(clockLabel.text,Is.Not.EqualTo(before));
            Assert.That(flow.SetLanguage("en"),Is.True);yield return null;
            labels=GameObject.Find("Vitals").GetComponentsInChildren<TMPro.TextMeshProUGUI>();StringAssert.Contains("HP",labels[0].text);
            Assert.That(flow.SetLanguage("zh-TW"),Is.True);yield return null;
            labels=GameObject.Find("Vitals").GetComponentsInChildren<TMPro.TextMeshProUGUI>();StringAssert.Contains("生命",labels[0].text);
        }
        [UnityTest] public IEnumerator SeparationMovesOverlapsButLeavesWarningsAndRecycledSlotsUntouched()
        {
            var a=world.SpawnEnemy(new Vector2(5,5),0);var b=world.SpawnEnemy(new Vector2(5,5),0);
            var warning=world.SpawnEnemy(new Vector2(5,5),10);
            var method=typeof(BattleWorld).GetMethod("SeparateEnemies",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var solve=(System.Action<float>)System.Delegate.CreateDelegate(typeof(System.Action<float>),world,method);
            solve(1);Assert.That(Vector2.Distance(a.transform.position,b.transform.position),Is.EqualTo(config.enemy.radius*2).Within(0.001));
            Assert.That(warning.transform.position,Is.EqualTo(new Vector3(5,5,0)));
            world.ReturnEnemy(b);var before=a.transform.position;solve(1);Assert.That(a.transform.position,Is.EqualTo(before));
            yield return null;
        }
        [UnityTest]
        public IEnumerator PlayerMovesNormalizesCrossesEnemiesAndSharesContactInvincibility()
        {
            world.Tick(0.1f, Vector2.one);
            Assert.That(world.Player.transform.position.magnitude, Is.EqualTo(0.55f).Within(0.001));
            world.Player.transform.position = Vector3.zero;
            world.SpawnEnemy(Vector2.zero, 0); world.SpawnEnemy(Vector2.zero, 0);
            world.Tick(0.01f, Vector2.zero);
            Assert.That(world.Player.Health.Current, Is.EqualTo(87));
            world.Tick(0.1f, Vector2.zero); Assert.That(world.Player.Health.Current, Is.EqualTo(87));
            world.Player.transform.position = new Vector3(-0.8f, 0);
            world.Tick(0.3f, Vector2.right);
            Assert.That(world.Player.transform.position.x, Is.EqualTo(0.85f).Within(0.001));
            world.Tick(100, Vector2.one);
            Assert.That(world.Player.transform.position.x, Is.LessThanOrEqualTo(23.55f));
            Assert.That(world.ViewBounds.xMax, Is.LessThanOrEqualTo(24.001f));
            Assert.That(world.ViewBounds.yMax, Is.LessThanOrEqualTo(15.001f));
            yield return null;
        }
        [UnityTest]
        public IEnumerator WeaponWaitsReadyWithoutTargetsAndLocksDirectionOnNearestTarget()
        {
            world.Weapon.Tick(10, world, config.weapon);
            Assert.That(world.Weapon.RemainingCooldown, Is.Zero);
            var far = world.SpawnEnemy(new Vector2(5, 0), 0);
            var near = world.SpawnEnemy(new Vector2(2, 0), 0);
            Assert.That(world.Weapon.FindNearest(Vector2.zero, world.Enemies.Items, 12), Is.SameAs(near));
            world.Weapon.Tick(0, world, config.weapon);
            Assert.That(world.Projectiles.ActiveCount, Is.EqualTo(1));
            ProjectileActor projectile = null;
            foreach (var item in world.Projectiles.Items) if (item.IsRented) projectile = item;
            near.transform.position = Vector2.up * 3;
            Assert.That(projectile.Direction, Is.EqualTo(Vector2.right));
            world.Weapon.Tick(0.01f, world, config.weapon);
            Assert.That(world.Projectiles.ActiveCount, Is.EqualTo(1));
            yield return null;
        }
        [UnityTest]
        public IEnumerator SweptProjectileDeduplicatesHitsPiercesAndResetsWhenReused()
        {
            config.weapon.maxHits = 2; config.weapon.projectileSpeed = 2;
            var first = world.SpawnEnemy(new Vector2(1, 0), 0);
            var second = world.SpawnEnemy(new Vector2(3, 0), 0);
            var projectile = world.LaunchProjectile(Vector2.zero, Vector2.right, 5, config.weapon);
            world.Tick(0.5f, Vector2.zero);
            Assert.That(first.Health.Current, Is.EqualTo(23));
            Assert.That(first.healthBar.gameObject.activeSelf, Is.True);
            world.Tick(0.05f, Vector2.zero);
            Assert.That(first.Health.Current, Is.EqualTo(23), "Same projectile cannot hit an overlapping enemy twice");
            world.Tick(1, Vector2.zero);
            Assert.That(second.Health.Current, Is.EqualTo(23));
            Assert.That(projectile.IsRented, Is.False);
            Assert.That(world.ReservedNumbers, Is.Zero);
            var reused = world.LaunchProjectile(Vector2.zero, Vector2.right, 5, config.weapon);
            Assert.That(reused, Is.SameAs(projectile)); world.Tick(0.5f, Vector2.zero);
            Assert.That(first.Health.Current, Is.EqualTo(18));
            world.Tick(3, Vector2.zero);
            Assert.That(reused.IsRented, Is.False);
            Assert.That(world.ReservedNumbers, Is.Zero);
            yield return null;
        }
        [UnityTest]
        public IEnumerator SpawnWarningCapacityEnemyDeathAndOutOfBoundsRecycleResetState()
        {
            Assert.That(world.TrySpawnEnemy(), Is.True);
            var enemy = world.Enemies.Items[0]; Assert.That(enemy.IsAlive, Is.False);
            Assert.That(enemy.hitbox.enabled, Is.False); world.Tick(0.3f, Vector2.zero);
            Assert.That(enemy.WarningRemaining, Is.GreaterThan(0)); Assert.That(enemy.warning.enabled, Is.True);
            world.Tick(0.5f, Vector2.zero); Assert.That(enemy.IsAlive, Is.True);
            Assert.That(enemy.hitbox.enabled, Is.True); Assert.That(enemy.warning.enabled, Is.False);
            enemy.transform.position = Vector2.right;
            world.LaunchProjectile(Vector2.zero, Vector2.right, 100, config.weapon); world.Tick(0.1f, Vector2.zero);
            Assert.That(enemy.IsRented, Is.False); Assert.That(GameFlow.Instance.Session.Run.Kills, Is.EqualTo(1));
            var token = enemy.Token;
            var reused = world.SpawnEnemy(new Vector2(3, 0), 0);
            Assert.That(reused, Is.SameAs(enemy)); Assert.That(reused.Token, Is.Not.EqualTo(token));
            Assert.That(reused.Health.Current, Is.EqualTo(28)); Assert.That(reused.healthBar.gameObject.activeSelf, Is.False);
            reused.transform.position = new Vector2(100, 0); world.Tick(0.01f, Vector2.zero);
            Assert.That(reused.IsRented, Is.False); Assert.That(GameFlow.Instance.Session.Run.Kills, Is.EqualTo(1));
            for (int i = 0; i < config.enemyCapacity; i++) Assert.That(world.TrySpawnEnemy(), Is.True);
            Assert.That(world.TrySpawnEnemy(), Is.False); Assert.That(world.Enemies.Items.Length, Is.EqualTo(64));
            yield return null;
        }
        [UnityTest]
        public IEnumerator PauseFreezesAllTimersAndSceneUnloadDestroysPools()
        {
            var enemy = world.SpawnEnemy(Vector2.right * 5, 1);
            var projectile = world.LaunchProjectile(Vector2.zero, Vector2.up, 5, config.weapon);
            var flow = GameFlow.Instance; flow.Session.Pause();
            world.Tick(2, Vector2.right);
            Assert.That(enemy.WarningRemaining, Is.EqualTo(1));
            Assert.That(projectile.RemainingLife, Is.EqualTo(config.weapon.projectileLifetime));
            Assert.That(world.Player.transform.position, Is.EqualTo(Vector3.zero));
            flow.Session.Resume(); world.Tick(0.1f, Vector2.zero);
            Assert.That(enemy.WarningRemaining, Is.LessThan(1));
            flow.ReturnToMenu(); float deadline = Time.realtimeSinceStartup + 10;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(world == null, Is.True); Assert.That(enemy == null, Is.True); Assert.That(projectile == null, Is.True);
            Assert.That(BattleWorld.Instance == null, Is.True);
        }
        [UnityTest]
        public IEnumerator StuckRecoverySeparationDamageDisplayBackpressureAndDeathStopCombat()
        {
            var enemy = world.SpawnEnemy(new Vector2(3, 0), 0);
            var other = world.SpawnEnemy(new Vector2(3, 0), 0);
            world.Tick(1.1f, Vector2.zero);
            Assert.That(enemy.RecoveryCount, Is.GreaterThan(0));
            Assert.That(Vector2.Distance(enemy.transform.position, other.transform.position), Is.GreaterThan(0));
            config.weapon.maxHits = 1;
            for (int i = 0; i < config.projectileCapacity; i++)
                Assert.That(world.LaunchProjectile(Vector2.zero, Vector2.up, 5, config.weapon), Is.Not.Null);
            Assert.That(world.LaunchProjectile(Vector2.zero, Vector2.up, 5, config.weapon), Is.Null);
            Assert.That(world.Projectiles.Items.Length, Is.EqualTo(96));
            foreach (var projectile in world.Projectiles.Items) world.RecycleProjectile(projectile);
            Assert.That(world.ReservedNumbers, Is.Zero);
            world.Enemies.Return(other); enemy.transform.position = Vector2.zero;
            for (int i = 0; i < 8; i++) world.Tick(1.01f, Vector2.zero);
            Assert.That(world.Player.Health.Current, Is.Zero);
            Assert.That(GameFlow.Instance.Session.State, Is.EqualTo(GameState.Defeat));
            float life = enemy.WarningRemaining; world.Tick(10, Vector2.right);
            Assert.That(enemy.WarningRemaining, Is.EqualTo(life));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }
        [UnityTest]
        public IEnumerator KeyboardDiagonalAndOppositeKeysDriveThePlayerCorrectly()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D)); yield return null;
            Assert.That(GameFlow.Instance.Movement.magnitude, Is.EqualTo(1).Within(0.001));
            world.Tick(0.1f, GameFlow.Instance.Movement);
            Assert.That(world.Player.transform.position.magnitude, Is.EqualTo(0.55f).Within(0.001));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.S)); yield return null;
            Assert.That(GameFlow.Instance.Movement, Is.EqualTo(Vector2.zero));
            var previous = world.Player.transform.position; world.Tick(0.1f, GameFlow.Instance.Movement);
            Assert.That(world.Player.transform.position, Is.EqualTo(previous));
        }
        [UnityTest]
        public IEnumerator FullEnemyDamageDisplayDoesNotProtectPlayerAndProjectilesExpireWithoutLeakingReservations()
        {
            world.SpawnEnemy(Vector2.zero, 0);
            for (int i = 0; i < config.damageNumberCapacity; i++)
                world.DamageNumbers.Rent().Show(Vector2.zero, 1, 10, false);
            Assert.That(world.DisplayCapacity, Is.Zero);
            Assert.That(world.LaunchProjectile(Vector2.zero, Vector2.up, 5, config.weapon), Is.Null);
            world.Tick(0.1f, Vector2.zero);
            Assert.That(world.Player.Health.Current, Is.EqualTo(87));
            world.DamageNumbers.Return(world.DamageNumbers.Items[0]);
            world.Tick(0.1f, Vector2.zero);
            Assert.That(world.Player.Health.Current, Is.EqualTo(87));
            Assert.That(world.DamageNumbers.ActiveCount, Is.EqualTo(255));
            foreach (var number in world.DamageNumbers.Items) world.DamageNumbers.Return(number);
            var projectile = world.LaunchProjectile(Vector2.right * 10, Vector2.up, 5, config.weapon);
            world.Tick(3, Vector2.zero);
            Assert.That(projectile.IsRented, Is.False);
            Assert.That(world.ReservedNumbers, Is.Zero);
            Assert.That(world.Projectiles.ActiveCount, Is.Zero);
            LogAssert.NoUnexpectedReceived(); yield return null;
        }
    }
}

