using System.Collections;
using System.Linq;
using NUnit.Framework;
using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using Survivor.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Survivor.Tests
{
    public class ProgressionIntegrationTests
    {
        private BattleWorld world;
        private BattleConfig config;
        private Keyboard keyboard;
        private bool background;
        private GameFlow Flow => GameFlow.Instance;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            background = Application.runInBackground; Application.runInBackground = true;
            if (Flow != null) { Object.Destroy(Flow.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Flow.OpenWeaponSelection(); Flow.SelectWeapon("bolt"); Flow.StartRun();
            yield return WaitForScene();
            world = BattleWorld.Instance; world.enabled = false;
            world.AutomaticAttacking = false; world.AutomaticSpawning = false;
            config = Object.Instantiate(world.config); config.enemy = Object.Instantiate(config.enemy);
            config.weapon = Object.Instantiate(config.weapon); config.progression = Object.Instantiate(config.progression);
            world.config = config; config.enemy.moveSpeed = 0;
            foreach (var enemy in world.Enemies.Items) world.Enemies.Return(enemy);
            Flow.Session.Tick(3); Assert.That(Flow.Progression, Is.Not.Null);
        }
        private IEnumerator WaitForScene()
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (Flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Flow.IsTransitioning, Is.False);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (Flow != null) Object.Destroy(Flow.gameObject);
            if (world != null) Object.Destroy(world.gameObject);
            if (config != null) { Object.Destroy(config.enemy); Object.Destroy(config.weapon); Object.Destroy(config.progression); Object.Destroy(config); }
            yield return null; Time.timeScale = 1; Application.runInBackground = background;
        }
        [UnityTest]
        public IEnumerator EnemyDeathDropsValuesMagnetCreditsOnceAndOutOfBoundsDropsNothing()
        {
            config.progression.currencyChance = 1;
            var enemy = world.SpawnEnemy(Vector2.right * 2, 0);
            world.LaunchProjectile(Vector2.zero, Vector2.right, 100, config.weapon); world.Tick(0.15f, Vector2.zero);
            Assert.That(enemy.IsRented, Is.False);
            Assert.That(world.Pickups.UncollectedExperience, Is.EqualTo(4));
            Assert.That(world.Pickups.UncollectedCurrency, Is.EqualTo(1));
            Assert.That(world.Pickups.Pool.ActiveCount, Is.EqualTo(2));
            world.Tick(0.2f, Vector2.zero);
            Assert.That(Flow.Session.Run.Experience, Is.EqualTo(4));
            Assert.That(Flow.Session.Run.CollectedCurrency, Is.EqualTo(1));
            Assert.That(world.DamageNumbers.Items.Any(n => n.IsRented && n.label.text == "+1"), Is.True);
            Assert.That(world.Pickups.Pool.ActiveCount, Is.Zero);
            var outside = world.SpawnEnemy(Vector2.right * 100, 0); world.Tick(0.1f, Vector2.zero);
            Assert.That(outside.IsRented, Is.False); Assert.That(world.Pickups.Pool.ActiveCount, Is.Zero);
            Assert.That(Flow.Session.Run.CollectedCurrency, Is.EqualTo(1));
            yield return null;
        }
        [UnityTest]
        public IEnumerator FullPoolAndExpiredDropsMergeWithoutLossOrAutomaticCollection()
        {
            for (int i = 0; i < 256; i++) world.Pickups.Spawn(Vector2.right * 10, 1, 1);
            world.Pickups.Spawn(Vector2.right * 10, 100, 40);
            Assert.That(world.Pickups.Pool.ActiveCount, Is.EqualTo(256));
            Assert.That(world.Pickups.UncollectedExperience, Is.EqualTo(356));
            Assert.That(world.Pickups.UncollectedCurrency, Is.EqualTo(296));
            Assert.That(Flow.Session.Run.CollectedCurrency, Is.Zero);
            world.Tick(31, Vector2.zero);
            Assert.That(world.Pickups.Pool.ActiveCount, Is.EqualTo(1));
            Assert.That(world.Pickups.UncollectedExperience, Is.EqualTo(356));
            Assert.That(world.Pickups.UncollectedCurrency, Is.EqualTo(296));
            world.Player.transform.position = Vector2.right * 10; world.Tick(0.01f, Vector2.zero);
            Assert.That(Flow.Session.Run.CollectedCurrency, Is.EqualTo(296));
            Assert.That(Flow.Session.Run.Experience, Is.EqualTo(348));
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.LevelUp));
            Assert.That(world.Pickups.Pool.ActiveCount, Is.Zero);
            world.Tick(5, Vector2.zero); Assert.That(Flow.Session.Run.CollectedCurrency, Is.EqualTo(296));
            yield return null;
        }
        [UnityTest]
        public IEnumerator HeldNumberAndStaleButtonsCannotSelectQueuedUpgradesTwice()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            Flow.Progression.GainExperience(40);
            var oldEvent = GameObject.Find("Button_upgrade.0").GetComponent<UnityEngine.UI.Button>().onClick;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1, Key.W)); yield return null;
            Assert.That(Flow.Session.Run.Level, Is.EqualTo(3));
            oldEvent.Invoke(); Assert.That(Flow.Session.Run.Level, Is.EqualTo(3));
            yield return null; Assert.That(Flow.Session.Run.Level, Is.EqualTo(3));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1, Key.W, Key.Escape)); yield return null;
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.LevelUp));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1, Key.W)); yield return null;
            Assert.That(Flow.Session.Run.Level, Is.EqualTo(4));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1, Key.W)); yield return null;
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.Battle));
            Assert.That(Flow.Movement, Is.EqualTo(Vector2.zero));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            Assert.That(Flow.Movement.y, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator PauseUpgradeAndFocusLossFreezeDropsCombatAndSelection()
        {
            var pickup = world.Pickups.Spawn(Vector2.right * 2, 4, 0);
            var enemy = world.SpawnEnemy(Vector2.right * 10, 2);
            var projectile = world.LaunchProjectile(Vector2.zero, Vector2.up, 5, config.weapon);
            Flow.Session.Pause(); world.Tick(3, Vector2.right);
            Assert.That(pickup.Age, Is.Zero); Assert.That(enemy.WarningRemaining, Is.EqualTo(2));
            Assert.That(projectile.RemainingLife, Is.EqualTo(2.5f));
            Flow.Session.Resume(); Flow.Progression.GainExperience(8); world.Tick(3, Vector2.right);
            Assert.That(pickup.Age, Is.Zero);
            Flow.gameObject.SendMessage("OnApplicationFocus", false);
            Assert.That(Flow.ChooseUpgrade(0, Flow.Progression.ChoiceRevision), Is.False);
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.LevelUp));
            Flow.gameObject.SendMessage("OnApplicationFocus", true);
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.LevelUp));
            Flow.ChooseUpgrade(0, Flow.Progression.ChoiceRevision); world.Tick(0.1f, Vector2.zero);
            Assert.That(pickup.Age, Is.GreaterThan(0));
            Flow.gameObject.SendMessage("OnApplicationPause", true);
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.Pause));
            Flow.gameObject.SendMessage("OnApplicationPause", false);
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.Pause), "Restoring a minimized window must not resume combat automatically");
            Flow.gameObject.SendMessage("OnApplicationFocus", true); Flow.Resume();
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.Battle));
            yield return null;
        }
        [UnityTest]
        public IEnumerator PassiveAndAttributeBonusesApplyWithoutResettingHealthOrInvincibility()
        {
            var run = Flow.Session.Run;
            var bloom = config.progression.abilities.First(a => a.id == "bloom");
            run.Build.Apply(new UpgradeOption("bloom", bloom.acquisition, 0)); world.ApplyBuild();
            world.Player.Health.TakeDamage(14);
            world.Tick(6.1f, Vector2.zero);
            Assert.That(world.Player.Health.Current, Is.EqualTo(89));
            world.Player.Health.TakeDamage(14); float invincible = world.Player.Health.InvincibleRemaining;
            var health = config.progression.abilities.First(a => a.id == "stat.health");
            run.Build.Apply(new UpgradeOption(health.id, health.effects[0], 0)); world.ApplyBuild();
            Assert.That(world.Player.Health.Maximum, Is.EqualTo(120));
            Assert.That(world.Player.Health.Current, Is.EqualTo(96));
            Assert.That(world.Player.Health.InvincibleRemaining, Is.EqualTo(invincible));
            world.Player.Health.Heal(999); Assert.That(world.Player.Health.Current, Is.EqualTo(120));
            yield return null;
        }
        [UnityTest]
        public IEnumerator VictoryClearsEverythingAndResultSnapshotSurvivesMenuAndNewRun()
        {
            var run = Flow.Session.Run; run.CollectedCurrency = 7; run.AbilityLevels["bloom"] = 2;
            world.Pickups.Spawn(Vector2.right * 15, 50, 20); world.SpawnEnemy(Vector2.right * 5, 0);
            world.LaunchProjectile(Vector2.zero, Vector2.up, 5, config.weapon);
            Flow.Session.Tick(600);
            Assert.That(Flow.Session.Result.Won, Is.True); Assert.That(Flow.Session.Result.CollectedCurrency, Is.EqualTo(7));
            Assert.That(world.Enemies.ActiveCount, Is.Zero); Assert.That(world.Projectiles.ActiveCount, Is.Zero);
            Assert.That(world.Pickups.Pool.ActiveCount, Is.Zero); Assert.That(world.DamageNumbers.ActiveCount, Is.Zero);
            Assert.That(world.ReservedNumbers, Is.Zero);
            yield return null;
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.Result));
            Assert.That(GameObject.Find("Button_menu"), Is.Not.Null);
            Assert.That(Flow.PermanentData.currency, Is.Zero, "M3 must not perform M4 permanent settlement");
            run.AbilityLevels["bloom"] = 5; var snapshot = Flow.Session.Result;
            Flow.ReturnToMenu(); yield return WaitForScene();
            Assert.That(world == null, Is.True); Assert.That(snapshot.AbilityLevels["bloom"], Is.EqualTo(2));
            Flow.OpenWeaponSelection(); Flow.SelectWeapon("bolt"); Flow.StartRun(); yield return WaitForScene();
            Assert.That(Flow.Session.Run.Experience, Is.Zero); Assert.That(Flow.Session.Run.CollectedCurrency, Is.Zero);
            Assert.That(Flow.Session.Run.Build.Level("bloom"), Is.Zero); Assert.That(Flow.Session.Run.Build.EffectCounts.Count, Is.Zero);
            world = BattleWorld.Instance;
        }
        [UnityTest]
        public IEnumerator LethalContactWinsIfItOccursBeforeTimeAndShowsDelayedDefeatResult()
        {
            config.enemy.contactDamage = 999;
            world.Pickups.Spawn(Vector2.right * 15, 50, 20); world.SpawnEnemy(Vector2.zero, 0);
            world.Tick(0.01f, Vector2.zero); Flow.Session.Tick(600);
            Assert.That(Flow.Session.Result.Won, Is.False); Assert.That(Flow.Session.Result.CollectedCurrency, Is.Zero);
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.Defeat)); Assert.That(world.Pickups.Pool.ActiveCount, Is.Zero);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.Result)); Assert.That(world.Player.body.enabled, Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator DensePickupCollidersCannotHideAnEnemyFromProjectileSweeps()
        {
            for (int i = 0; i < 256; i++) world.Pickups.Spawn(Vector2.right, 1, 0);
            var enemy = world.SpawnEnemy(Vector2.right * 3, 0);
            world.LaunchProjectile(Vector2.zero, Vector2.right, 100, config.weapon);
            world.Tick(0.3f, Vector2.zero);
            Assert.That(enemy.IsRented, Is.False, "The sweep must filter enemy layers before filling the bounded hit buffer");
            Assert.That(Flow.Session.Run.Kills, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived(); yield return null;
        }
    }
}
