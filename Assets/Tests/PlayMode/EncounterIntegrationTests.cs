using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using Survivor.Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Survivor.Tests
{
    public class EncounterIntegrationTests
    {
        private bool background;
        private readonly List<EnemyDefinition> profiles = new List<EnemyDefinition>();
        private GameFlow Flow => GameFlow.Instance;
        private BattleWorld World => BattleWorld.Instance;
        [UnitySetUp] public IEnumerator Setup()
        {
            background=Application.runInBackground; Application.runInBackground=true;
            GameFlow.SaveDirectoryOverride=Path.Combine(Path.GetTempPath(),"Survivor-EncounterTests-"+Guid.NewGuid().ToString("N"));
            if(Flow!=null) { Object.Destroy(Flow.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("MainMenu"); Flow.OpenWeaponSelection(); Flow.SelectWeapon("bolt"); Flow.StartRun();
            float deadline=Time.realtimeSinceStartup+10;
            while(Flow.IsTransitioning && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(Flow.IsTransitioning,Is.False); World.enabled=false; World.AutomaticSpawning=false; World.AutomaticAttacking=false;
            Flow.Session.Tick(3); World.ApplyBuild();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(Flow!=null) Object.Destroy(Flow.gameObject); if(World!=null) Object.Destroy(World.gameObject);
            foreach(var profile in profiles) Object.Destroy(profile); profiles.Clear();
            yield return null; Time.timeScale=1; Application.runInBackground=background;
        }
        private EnemyDefinition Profile(EnemyDefinition source)
        { var profile=Object.Instantiate(source); profile.moveSpeed=0; profiles.Add(profile); return profile; }
        [UnityTest] public IEnumerator DifficultySnapshotsAndSpecialSlotsAndD03Restoration()
        {
            var old=World.SpawnEnemy(World.config.enemy,Vector2.right*8,0,true);
            int health=old.Health.Maximum; float speed=old.MoveSpeed;
            World.TickEncounters(120);
            var fresh=World.SpawnEnemy(World.config.enemy,Vector2.left*8,0,true);
            Assert.That(fresh.Health.Maximum,Is.GreaterThan(health)); Assert.That(old.Health.Maximum,Is.EqualTo(health)); Assert.That(old.MoveSpeed,Is.EqualTo(speed));
            while(World.Enemies.ActiveCount<World.config.enemyCapacity) World.SpawnEnemy(Vector2.up*10,0);
            World.TickEncounters(420);
            Assert.That(World.Enemies.ActiveCount,Is.EqualTo(64)); Assert.That(World.Specials.ActiveCount,Is.EqualTo(2));
            Assert.That(World.Timeline.EliteSpawned && World.Timeline.BossSpawned,Is.True);
            Assert.That(World.OrdinarySpawnRate,Is.EqualTo(0.6f));
            foreach(var actor in World.Specials.Items) if(actor.Kind==EnemyKind.Elite) World.ReturnEnemy(actor);
            Assert.That(World.OrdinarySpawnRate,Is.EqualTo(0.6f)); World.ReturnEnemy(World.Boss);
            Assert.That(World.OrdinarySpawnRate,Is.EqualTo(1)); World.TickEncounters(500); Assert.That(World.Specials.ActiveCount,Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator RangedWindupPausesThenFiresAtCurrentPositionAndBulletStaysStraight()
        {
            var profile=Profile(World.config.encounters.ranged); profile.attackCooldown=0.1f; profile.windupSeconds=0.5f;
            var enemy=World.SpawnEnemy(profile,Vector2.right*6,0); World.Tick(0.11f,Vector2.zero);
            Assert.That(enemy.Attack,Is.EqualTo(EnemyAttack.Shot)); Assert.That(enemy.warning.enabled,Is.True); Assert.That(World.HostileProjectiles.ActiveCount,Is.Zero);
            Flow.Session.Pause(); float warning=enemy.AttackWarningRemaining; World.Tick(3,Vector2.zero);
            Assert.That(enemy.AttackWarningRemaining,Is.EqualTo(warning)); Flow.Session.Resume();
            World.Player.transform.position=Vector2.up*2; Vector2 expected=((Vector2)World.Player.transform.position-(Vector2)enemy.transform.position).normalized;
            World.Tick(0.51f,Vector2.zero); Assert.That(World.HostileProjectiles.ActiveCount,Is.EqualTo(1));
            HostileProjectileActor shot=null; foreach(var p in World.HostileProjectiles.Items) if(p.IsRented) shot=p;
            Assert.That(Vector2.Distance(shot.Direction,expected),Is.LessThan(0.001));
            World.Player.transform.position=Vector2.down*3; World.Tick(0.1f,Vector2.zero);
            Assert.That(Vector2.Distance(shot.Direction,expected),Is.LessThan(0.001)); yield return null;
        }
        [UnityTest] public IEnumerator BomberTelegraphCanBeDodgedAndSelfDestructionDoesNotAwardPlayerKill()
        {
            var profile=Profile(World.config.encounters.bomber); var bomber=World.SpawnEnemy(profile,Vector2.right*1.5f,0);
            World.Tick(0.01f,Vector2.zero); Assert.That(bomber.AttackWarningRemaining,Is.GreaterThan(0)); Assert.That(bomber.TelegraphRadius,Is.EqualTo(2.5f));
            World.Player.transform.position=Vector2.up*8; World.Tick(1.1f,Vector2.zero);
            Assert.That(bomber.IsRented,Is.False); Assert.That(World.Player.Health.Current,Is.EqualTo(100));
            Assert.That(Flow.Session.Run.Kills,Is.Zero); Assert.That(World.Pickups.Pool.ActiveCount,Is.Zero);
            World.Player.transform.position=Vector2.zero; World.SpawnEnemy(profile,Vector2.right*1.5f,0); World.Tick(0.01f,Vector2.zero); World.Tick(1.1f,Vector2.zero);
            Assert.That(World.Player.Health.Current,Is.LessThan(100)); yield return null;
        }
        [UnityTest] public IEnumerator ContactBulletsAndBlastsShareInvulnerabilityAndEnemyBulletsRecycle()
        {
            Assert.That(World.HitPlayer(14),Is.EqualTo(13)); int health=World.Player.Health.Current;
            var shot=World.FireHostile(Vector2.zero,Vector2.right,100,6,2); World.Tick(0.01f,Vector2.zero);
            Assert.That(shot.IsRented,Is.False); World.HitPlayerInCircle(Vector2.zero,3,100);
            Assert.That(World.Player.Health.Current,Is.EqualTo(health));
            World.Tick(1.01f,Vector2.zero); World.HitPlayerInCircle(Vector2.zero,3,14);
            Assert.That(World.Player.Health.Current,Is.EqualTo(health-13)); yield return null;
        }
        [UnityTest] public IEnumerator BossUsesBothTelegraphedAttacksAndDeathAwardsGuaranteedDrops()
        {
            var boss=World.SpawnEnemy(World.config.encounters.boss,Vector2.right*12,0); bool melee=false,volley=false;
            for(int i=0;i<20;i++)
            {
                World.Tick(3.01f,Vector2.zero); melee|=boss.Attack==EnemyAttack.BossMelee; volley|=boss.Attack==EnemyAttack.BossVolley;
                Assert.That(boss.AttackWarningRemaining,Is.GreaterThan(0)); World.Tick(1.01f,Vector2.zero);
                foreach(var shot in World.HostileProjectiles.Items) World.HostileProjectiles.Return(shot);
            }
            Assert.That(melee && volley,Is.True); World.DealEnemyDamage(boss,10000);
            Assert.That(World.Boss,Is.Null); Assert.That(World.Pickups.UncollectedExperience,Is.EqualTo(180)); Assert.That(World.Pickups.UncollectedCurrency,Is.EqualTo(120));
            Assert.That(Flow.Session.Run.Kills,Is.EqualTo(1)); Assert.That(World.NoticeKey,Is.EqualTo("encounter.boss.defeated")); yield return null;
        }
        [UnityTest] public IEnumerator LiveBossDoesNotPreventVictoryAndAllHostileStateIsCleared()
        {
            World.TickEncounters(420); World.FireHostile(Vector2.right*5,Vector2.left,10,3,6);
            Flow.Session.Tick(600); Assert.That(Flow.Session.Result.Won,Is.True);
            Assert.That(World.Specials.ActiveCount,Is.Zero); Assert.That(World.HostileProjectiles.ActiveCount,Is.Zero);
            Assert.That(World.Enemies.ActiveCount,Is.Zero); Assert.That(World.Areas.ActiveCount,Is.Zero);
            foreach(var enemy in World.Combatants) { Assert.That(enemy.Attack,Is.EqualTo(EnemyAttack.None)); Assert.That(enemy.gameObject.activeSelf,Is.False); }
            yield return null;
        }
    }
}
