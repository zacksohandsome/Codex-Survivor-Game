using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Survivor.Audio;
using Survivor.Combat;
using Survivor.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Survivor.Tests
{
    public class PresentationIntegrationTests
    {
        private float volume;private bool background;
        private GameFlow Flow=>GameFlow.Instance;
        [UnitySetUp] public IEnumerator Setup()
        {
            volume=AudioListener.volume;AudioListener.volume=0;background=Application.runInBackground;Application.runInBackground=true;
            GameFlow.SaveDirectoryOverride=Path.Combine(Path.GetTempPath(),"Survivor-Presentation-"+Guid.NewGuid().ToString("N"));
            if(Flow!=null){Object.Destroy(Flow.gameObject);yield return null;}
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(Flow!=null)Object.Destroy(Flow.gameObject);if(BattleWorld.Instance!=null)Object.Destroy(BattleWorld.Instance.gameObject);
            yield return null;Time.timeScale=1;AudioListener.volume=volume;Application.runInBackground=background;
        }
        private IEnumerator Battle()
        {
            Flow.OpenWeaponSelection();Flow.SelectWeapon("bolt");Flow.StartRun();
            while(Flow.IsTransitioning)yield return null;Flow.Session.Tick(4);yield return null;
            BattleWorld.Instance.AutomaticAttacking=false;BattleWorld.Instance.AutomaticSpawning=false;
        }
        [UnityTest] public IEnumerator MusicLoopsCrossfadesAndPausesWhileUiRemainsUsable()
        {
            yield return new WaitForSecondsRealtime(1);
            Assert.That(Flow.Audio.CurrentMusic.clip,Is.EqualTo(Flow.Config.audio.menuMusic));Assert.That(Flow.Audio.CurrentMusic.loop,Is.True);
            Assert.That(Flow.Audio.CurrentMusic.volume,Is.EqualTo(Flow.PermanentData.musicVolume).Within(0.01));
            yield return Battle();yield return new WaitForSecondsRealtime(1);
            Assert.That(Flow.Audio.CurrentMusic.clip,Is.EqualTo(Flow.Config.audio.battleMusic));
            Assert.That(Flow.Audio.PlayCombat("attack"),Is.True);Flow.Session.Pause();yield return null;
            Assert.That(Flow.Audio.MusicPaused,Is.True);Assert.That(Flow.Audio.CurrentMusic.isPlaying,Is.False);Assert.That(Flow.Audio.ActiveCombatSounds,Is.Zero);
            Assert.That(Flow.Audio.PlayCombat("attack"),Is.False);Assert.That(Flow.Audio.PlayUi("ui"),Is.True);
            Assert.That(Flow.Audio.UiPlaying,Is.True);Flow.Resume();yield return null;Assert.That(Flow.Audio.CurrentMusic.isPlaying,Is.True);
            Assert.That(Flow.Audio.PlayUi("missing"),Is.False);Flow.SetVolumes(0.2f,0.3f);yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(Flow.Audio.CurrentMusic.volume,Is.EqualTo(0.2f).Within(0.01));
        }
        [UnityTest] public IEnumerator AudioHasFixedSourcesAndThrottlesRepeatedCombatEventsAndMissingCatalogIsSafe()
        {
            var audio=Flow.Audio;int sources=audio.GetComponentsInChildren<AudioSource>().Length;
            for(int i=0;i<100;i++)audio.PlayCombat("attack",1);
            Assert.That(audio.ActiveCombatSounds,Is.EqualTo(1));Assert.That(audio.PlayCombat("attack",1),Is.False);
            foreach(string key in Flow.Config.audio.ids)audio.PlayCombat(key,0);
            Assert.That(audio.ActiveCombatSounds,Is.LessThanOrEqualTo(8));Assert.That(audio.GetComponentsInChildren<AudioSource>().Length,Is.EqualTo(sources));
            var root=new GameObject("MissingAudio",typeof(AudioDirector));var missing=root.GetComponent<AudioDirector>();missing.Initialize(null,1,1);
            Assert.DoesNotThrow(()=>missing.ChangeState(GameState.Battle));Assert.That(missing.PlayUi("ui"),Is.False);Object.Destroy(root);yield return null;
        }
        [UnityTest] public IEnumerator MotionChangesOnlyVisualPoseAndBurstPoolResetsWithoutGrowth()
        {
            yield return Battle();var world=BattleWorld.Instance;world.enabled=false;var hero=world.Player;
            var position=hero.transform.position;hero.Motion.Tick(0.1f,Vector2.right);
            Assert.That(hero.body.transform.localPosition.y,Is.GreaterThan(0));Assert.That(hero.transform.position,Is.EqualTo(position));
            hero.Motion.Strike(Vector2.left);hero.Motion.Tick(0.05f,Vector2.zero);Assert.That(hero.body.flipX,Is.True);
            hero.Motion.ResetPose();Assert.That(hero.body.transform.localPosition,Is.EqualTo(Vector3.zero));
            for(int i=0;i<100;i++)world.EmitBurst(Vector2.zero,Color.cyan,true);
            Assert.That(world.Bursts.ActiveCount,Is.EqualTo(48));int children=world.transform.childCount;
            world.Tick(0.7f,Vector2.zero);Assert.That(world.Bursts.ActiveCount,Is.Zero);
            var burst=world.EmitBurst(Vector2.one,Color.red,false);Assert.That(burst.Remaining,Is.EqualTo(0.6f));
            Flow.Session.Pause();float before=burst.Remaining;world.Tick(1,Vector2.zero);Assert.That(burst.Remaining,Is.EqualTo(before));
            world.ClearForEnd();Assert.That(world.Bursts.ActiveCount,Is.Zero);Assert.That(burst.particles.particleCount,Is.Zero);Assert.That(world.transform.childCount,Is.EqualTo(children));
        }
    }
}
