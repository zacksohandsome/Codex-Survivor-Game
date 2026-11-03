using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using Survivor.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Survivor.Tests
{
    public class ContentIntegrationTests
    {
        private bool background;
        private BattleConfig config;
        private GameFlow Flow => GameFlow.Instance;
        private BattleWorld World => BattleWorld.Instance;
        [UnitySetUp] public IEnumerator Setup()
        {
            background = Application.runInBackground; Application.runInBackground = true;
            GameFlow.SaveDirectoryOverride = Path.Combine(Path.GetTempPath(), "Survivor-ContentTests-" + Guid.NewGuid().ToString("N"));
            if (Flow != null) { Object.Destroy(Flow.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return Begin("bolt");
        }
        private IEnumerator Begin(string weapon)
        {
            Flow.OpenWeaponSelection(); Flow.SelectWeapon(weapon); Assert.That(Flow.StartRun(), Is.True);
            float until = Time.realtimeSinceStartup + 10;
            while (Flow.IsTransitioning && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(Flow.IsTransitioning, Is.False); World.enabled = false;
            config = Object.Instantiate(World.config); config.enemy = Object.Instantiate(config.enemy); config.enemy.moveSpeed = 0;
            World.config = config; World.AutomaticSpawning = false; Flow.Session.Tick(3); World.ApplyBuild();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (Flow != null) Object.Destroy(Flow.gameObject); if (World != null) Object.Destroy(World.gameObject);
            if (config != null) { Object.Destroy(config.enemy); Object.Destroy(config); }
            yield return null; Time.timeScale = 1; Application.runInBackground = background;
        }
        private void Acquire(string id)
        {
            var ability = config.progression.abilities.First(a => a.id == id);
            Flow.Session.Run.Build.Apply(new UpgradeOption(id, ability.acquisition, 0, category: ability.category)); World.ApplyBuild();
        }
        [UnityTest] public IEnumerator AllFiveStartingWeaponsCanHitAndKillThroughTheirActualPatterns()
        {
            foreach (string id in new[] { "bolt", "orbit", "blast", "homing", "pulse" })
            {
                if (id != "bolt")
                {
                    Flow.ReturnToMenu(); while (Flow.IsTransitioning) yield return null;
                    Object.Destroy(config.enemy); Object.Destroy(config); config = null; yield return Begin(id);
                }
                var enemy = World.SpawnEnemy(Vector2.right * (id == "orbit" ? 2.4f : 3), 0);
                for (int frame = 0; frame < 150 && enemy.IsAlive; frame++) World.Tick(0.05f, Vector2.zero);
                Assert.That(enemy.IsRented, Is.False, "Weapon must kill through real runtime: " + id);
                Assert.That(Flow.Session.Run.Kills, Is.GreaterThan(0));
                Assert.That(World.Pickups.UncollectedExperience + Flow.Session.Run.Experience + (Flow.Session.Run.Level > 1 ? 8 : 0), Is.GreaterThan(0));
            }
        }
        [UnityTest] public IEnumerator OwnedWeaponsUseIndependentCooldownsAndLevelThreeChangesMechanism()
        {
            Acquire("orbit"); Acquire("blast"); World.SpawnEnemy(Vector2.right * 4, 0); World.Tick(0.01f, Vector2.zero);
            Assert.That(World.ControllerFor("bolt").RemainingCooldown, Is.GreaterThan(0));
            Assert.That(World.ControllerFor("orbit").RemainingCooldown, Is.GreaterThan(0));
            Assert.That(World.ControllerFor("blast").RemainingCooldown, Is.GreaterThan(0));
            Assert.That(World.Areas.ActiveCount, Is.GreaterThan(0));
            foreach (var projectile in World.Projectiles.Items) World.RecycleProjectile(projectile);
            Flow.Session.Run.Build.Levels["bolt"] = 3;
            World.ControllerFor("bolt").Tick(2, World, config.weapon);
            Assert.That(World.Projectiles.ActiveCount, Is.EqualTo(2), "Level 3 bolt fires a second angled projectile");
            float cooldown = World.ControllerFor("bolt").RemainingCooldown;
            Flow.Session.Pause(); World.Tick(2, Vector2.zero);
            Assert.That(World.ControllerFor("bolt").RemainingCooldown, Is.EqualTo(cooldown));
            yield return null;
        }
        [UnityTest] public IEnumerator HomingTurnsWhileStraightProjectileKeepsItsLaunchDirection()
        {
            World.AutomaticAttacking = false;
            var homing = Flow.Config.weapons.First(w => w.id == "homing");
            var enemy = World.SpawnEnemy(new Vector2(3, 3), 0);
            var missile = World.LaunchProjectile(Vector2.zero, Vector2.right, 1, homing);
            var straight = World.LaunchProjectile(Vector2.zero, Vector2.right, 1, config.weapon);
            World.Tick(0.05f, Vector2.zero);
            Assert.That(missile.Direction.y, Is.GreaterThan(0)); Assert.That(straight.Direction, Is.EqualTo(Vector2.right));
            yield return null;
        }
        [UnityTest] public IEnumerator EchoThornsAndTrailHaveDistinctTriggersAndPoolReset()
        {
            World.AutomaticAttacking = false; Acquire("echo"); Acquire("thorns"); Acquire("trail");
            var first = World.SpawnEnemy(Vector2.right * 4, 0); var second = World.SpawnEnemy(Vector2.right * 5, 0);
            int before = second.Health.Current; World.DealEnemyDamage(first, 10);
            Assert.That(second.Health.Current, Is.LessThan(before), "Echo chains to a nearby different enemy");
            World.SpawnEnemy(Vector2.zero, 0); World.Tick(0.05f, Vector2.zero);
            Assert.That(World.Areas.ActiveCount, Is.GreaterThan(0), "Taking contact damage triggers thorns");
            int count = World.Areas.ActiveCount; World.Tick(0.1f, Vector2.up);
            Assert.That(World.Areas.ActiveCount, Is.GreaterThan(count), "Moving creates a trail");
            Flow.Session.Finish(false);
            Assert.That(World.Areas.ActiveCount, Is.Zero); Assert.That(World.Projectiles.ActiveCount, Is.Zero);
            foreach (var area in World.Areas.Items) Assert.That(area.IsRented, Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator BothSkillsTriggerAndMagnetAndRegenerationRemainActive()
        {
            World.AutomaticAttacking = false; Acquire("storm"); Acquire("sanctuary"); Acquire("magnet"); Acquire("bloom");
            var enemy = World.SpawnEnemy(Vector2.right * 2, 0); int before = enemy.Health.Current;
            World.Player.Health.TakeDamage(30); int damaged = World.Player.Health.Current;
            for (int i = 0; i < 70; i++) World.Tick(0.1f, Vector2.zero);
            Assert.That(!enemy.IsAlive || enemy.Health.Current < before, Is.True, "Storm deals automatic area damage");
            Assert.That(World.Player.Health.Current, Is.GreaterThan(damaged), "Bloom heals independently");
            Assert.That(World.Player.Health.Defense, Is.EqualTo(config.playerDefense + 4));
            var pickup = World.Pickups.Spawn((Vector2)World.Player.transform.position + Vector2.up * 3.3f, 1, 0);
            World.Pickups.Tick(0.01f); Assert.That(pickup.Attracted, Is.True, "Magnet extends shared pickup radius");
            yield return null;
        }
    }
}
