using NUnit.Framework;
using Survivor.Combat;
using Survivor.Data;
using Survivor.Enemy;
using Survivor.Player;
using UnityEditor;
using UnityEngine;

namespace Survivor.Tests
{
    public class BattleRulesTests
    {
        [Test]
        public void DefenseHasDiminishingReductionAndNeverPreventsAllDamage()
        {
            Assert.That(DamageCalculator.Calculate(100, 0), Is.EqualTo(100));
            Assert.That(DamageCalculator.Calculate(100, 100), Is.EqualTo(50));
            Assert.That(DamageCalculator.Calculate(100, 200), Is.EqualTo(34));
            Assert.That(DamageCalculator.Calculate(1, 100000), Is.EqualTo(1));
            Assert.That(DamageCalculator.Calculate(14, 10), Is.EqualTo(13));
        }
        [Test]
        public void AllDamageSourcesShareOneInvincibilityWindow()
        {
            var health = new Health(100, 10, 1);
            Assert.That(health.TakeDamage(14), Is.EqualTo(13));
            Assert.That(health.TakeDamage(999), Is.Zero);
            health.Tick(0.99f); Assert.That(health.TakeDamage(999), Is.Zero);
            health.Tick(0.01f); Assert.That(health.TakeDamage(999), Is.GreaterThan(0));
            Assert.That(health.Current, Is.Zero);
            Assert.That(health.TakeDamage(10), Is.Zero);
        }
        [Test]
        public void MovementAndCameraStayInsideTheArenaIncludingBodyRadius()
        {
            Assert.That(PlayerActor.ClampPosition(new Vector2(100, -100), new Vector2(24, 15), 0.45f),
                Is.EqualTo(new Vector2(23.55f, -14.55f)));
            var camera = ArenaCamera.Follow(new Vector2(24, 15), new Vector2(24, 15), 7, 16f / 9f);
            Assert.That(camera.x + 7 * 16f / 9f, Is.EqualTo(24).Within(0.001));
            Assert.That(camera.y + 7, Is.EqualTo(15).Within(0.001));
        }
        [Test]
        public void EdgeSpawnPrefersOffscreenAndRespectsSafeDistance()
        {
            var view = new Rect(-12.45f, -7, 24.9f, 14);
            Assert.That(SpawnPlanner.Choose(new Vector2(24, 15), 0.45f, new Vector2(23, 14), view,
                8, 64, new System.Random(7303), out var point), Is.True);
            Assert.That(view.Contains(point), Is.False);
            Assert.That(Vector2.Distance(point, new Vector2(23, 14)), Is.GreaterThanOrEqualTo(8));
            Assert.That(Mathf.Abs(point.x) == 23.55f || Mathf.Abs(point.y) == 14.55f, Is.True);
        }
        [Test]
        public void SpawnFallbackIsAllowedOnlyWhenNoSafeOffscreenCandidateExists()
        {
            var allVisible = new Rect(-30, -30, 60, 60);
            Assert.That(SpawnPlanner.Choose(new Vector2(24, 15), 0.45f, Vector2.zero, allVisible,
                8, 64, new System.Random(1), out var point), Is.True);
            Assert.That(allVisible.Contains(point), Is.True);
            Assert.That(SpawnPlanner.Choose(Vector2.one, 0.45f, Vector2.zero, allVisible,
                8, 64, new System.Random(1), out _), Is.False);
        }
        [Test]
        public void BattleSceneReferencesItsConfigurationAndAllRepresentativePrefabs()
        {
            var dependencies = AssetDatabase.GetDependencies("Assets/Scenes/Battle.unity");
            foreach (var path in new[] { "Assets/Data/Battle/BattleConfig.asset", "Assets/Data/Battle/Melee.asset",
                "Assets/Prefabs/Player/Player.prefab", "Assets/Prefabs/Enemies/Melee.prefab",
                "Assets/Prefabs/Combat/ArcaneBolt.prefab", "Assets/Prefabs/Combat/DamageNumber.prefab" })
                Assert.That(dependencies, Does.Contain(path));
            var config = AssetDatabase.LoadAssetAtPath<BattleConfig>("Assets/Data/Battle/BattleConfig.asset");
            Assert.That(config.weapon.combatAvailable, Is.True);
            Assert.That(config.damageNumberCapacity, Is.GreaterThanOrEqualTo(config.projectileCapacity * config.weapon.maxHits));
        }
    }
}
