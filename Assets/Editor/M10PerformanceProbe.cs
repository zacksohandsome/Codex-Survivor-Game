using System;
using System.Collections.Generic;
using System.IO;
using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Survivor.Editor
{
    // Explicit synthetic stress fixture, never used by the normal game.
    public sealed class M10PerformanceProbe : ScriptableObject
    {
        [Serializable] private sealed class Frame { public double milliseconds,cpuMilliseconds,worldMilliseconds,separationMilliseconds,projectilesMilliseconds,pickupsMilliseconds,editorMilliseconds,playerLoopMilliseconds,gcMilliseconds,renderWaitMilliseconds;public long allocatedBytes,memoryBytes;public int enemies,projectiles,hostile,drops,numbers,bursts,areas,objects; }
        [Serializable] private sealed class Result { public string label,status;public int width,height,frames;public double seconds,averageFps,minimumFps,p99Milliseconds,p95Milliseconds,cpuAverageMilliseconds,allocationAverageBytes,memoryStartBytes,memoryEndBytes;public List<Frame> samples=new List<Frame>(); }
        private static M10PerformanceProbe active;
        public static string Status=>active==null?"idle":active.result.status;
        private Result result;private ProfilerRecorder cpu,gc,memory;private int frame=-1;private double started,previous;private readonly List<EnemyDefinition> profiles=new List<EnemyDefinition>();
        private ProfilerRecorder worldTime,separationTime,projectileTime,pickupTime;
        private ProfilerRecorder editorTime,playerLoopTime,gcTime,renderWaitTime;
        private bool uiEnabled,worldEnabled,flowEnabled;private float audioVolume;private string path;private BattleWorld world;private double duration;
        public static void Begin(string label,double seconds=20)
        {
            if(active!=null || !EditorApplication.isPlaying || GameFlow.Instance.Session.State!=GameState.Battle)throw new InvalidOperationException("Start one probe in Battle");
            active=CreateInstance<M10PerformanceProbe>();active.StartProbe(label,seconds);
        }
        private void StartProbe(string label,double seconds)
        {
            result=new Result{label=label,status="warming",width=Screen.width,height=Screen.height};duration=seconds;
            path="Documentation/Reports/M10/"+label+".json";Directory.CreateDirectory("Documentation/Reports/M10");world=BattleWorld.Instance;
            world.AutomaticAttacking=false;world.AutomaticSpawning=false;
            foreach(var enemy in world.Combatants)world.ReturnEnemy(enemy);
            var source=new[]{world.config.enemy,world.config.encounters.ranged,world.config.encounters.bomber,world.config.encounters.elite,world.config.encounters.boss};
            foreach(var item in source){var copy=Object.Instantiate(item);copy.maxHealth=1000000;copy.contactDamage=0;copy.moveSpeed=0;copy.attackRange=0;profiles.Add(copy);}
            uiEnabled=GameFlow.Instance.GetComponentInChildren<Canvas>().enabled;worldEnabled=world.enabled;flowEnabled=GameFlow.Instance.enabled;audioVolume=AudioListener.volume;AudioListener.volume=0;
            if(label.Contains("no-ui"))GameFlow.Instance.GetComponentInChildren<Canvas>().enabled=false;
            if(label.Contains("no-hud"))GameFlow.Instance.enabled=false;
            if(label.Contains("no-world"))world.enabled=false;
            cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);memory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);
            worldTime=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Survivor.World",1);separationTime=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Survivor.Separation",1);
            projectileTime=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Survivor.Projectiles",1);pickupTime=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Survivor.Pickups",1);
            editorTime=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"EditorLoop",1);playerLoopTime=ProfilerRecorder.StartNew(new ProfilerCategory("PlayerLoop"),"PlayerLoop",1);
            gcTime=ProfilerRecorder.StartNew(new ProfilerCategory("GC"),"GC.Collect",1);renderWaitTime=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Gfx.WaitForPresentOnGfxThread",1);
            started=previous=Time.realtimeSinceStartupAsDouble;EditorApplication.update+=Observe;
        }
        private void Fill()
        {
            while(world.Enemies.ActiveCount<64)
            {
                int i=world.Enemies.ActiveCount;world.SpawnEnemy(profiles[i%3],new Vector2(-10+i%16*1.3f,-6+i/16*3),0);
            }
            if(world.Specials.ActiveCount==0){world.SpawnEnemy(profiles[3],new Vector2(5,5),0);world.SpawnEnemy(profiles[4],new Vector2(-5,5),0);}
            int limit=96-world.Projectiles.ActiveCount;
            for(int i=0;i<limit;i++)if(world.LaunchProjectile(new Vector2(-12+i%20,7),Vector2.down,0,world.config.weapon)==null)break;
            while(world.HostileProjectiles.ActiveCount<100)world.FireHostile(new Vector2(7,6),Vector2.up,0,0,3);
            while(world.Pickups.Pool.ActiveCount<220)world.Pickups.Spawn(new Vector2(10+world.Pickups.Pool.ActiveCount%5,-7+world.Pickups.Pool.ActiveCount%15),1,0);
            while(world.DamageNumbers.ActiveCount<100 && world.DisplayCapacity>0){var n=world.DamageNumbers.Rent();n.Show(new Vector2(-10+world.DamageNumbers.ActiveCount%20,2),10,0.7f,false);}
            while(world.Bursts.ActiveCount<32)world.EmitBurst(new Vector2(-8+world.Bursts.ActiveCount%16,-3),Color.cyan,true);
            while(world.Areas.ActiveCount<16)world.EmitArea(new Vector2(-8+world.Areas.ActiveCount%16,3),1,0,0.7f,0,true,Color.cyan);
        }
        private void Observe()
        {
            if(!EditorApplication.isPlaying || world==null){Finish("interrupted");return;}if(frame==Time.frameCount)return;frame=Time.frameCount;
            double now=Time.realtimeSinceStartupAsDouble,delta=(now-previous)*1000;previous=now;Fill();
            if(now-started<5)return;
            if(result.status=="warming"){result.status="measuring";result.memoryStartBytes=memory.LastValue;}
            result.samples.Add(new Frame{milliseconds=delta,cpuMilliseconds=cpu.LastValue/1000000.0,worldMilliseconds=worldTime.LastValue/1000000.0,separationMilliseconds=separationTime.LastValue/1000000.0,projectilesMilliseconds=projectileTime.LastValue/1000000.0,pickupsMilliseconds=pickupTime.LastValue/1000000.0,editorMilliseconds=editorTime.LastValue/1000000.0,playerLoopMilliseconds=playerLoopTime.LastValue/1000000.0,gcMilliseconds=gcTime.LastValue/1000000.0,renderWaitMilliseconds=renderWaitTime.LastValue/1000000.0,allocatedBytes=gc.LastValue,memoryBytes=memory.LastValue,enemies=world.Enemies.ActiveCount+world.Specials.ActiveCount,projectiles=world.Projectiles.ActiveCount,hostile=world.HostileProjectiles.ActiveCount,drops=world.Pickups.Pool.ActiveCount,numbers=world.DamageNumbers.ActiveCount,bursts=world.Bursts.ActiveCount,areas=world.Areas.ActiveCount,objects=world.transform.childCount});
            if(now-started>=5+duration)Finish("completed");
        }
        private void Finish(string status)
        {
            EditorApplication.update-=Observe;result.status=status;result.frames=result.samples.Count;
            var times=new List<double>();double total=0,cpuTotal=0,allocation=0;
            foreach(var sample in result.samples){times.Add(sample.milliseconds);total+=sample.milliseconds;cpuTotal+=sample.cpuMilliseconds;allocation+=sample.allocatedBytes;}
            times.Sort();if(times.Count>0){result.seconds=total/1000;result.averageFps=1000*times.Count/total;result.minimumFps=1000/times[times.Count-1];result.p99Milliseconds=times[(int)((times.Count-1)*0.99)];result.p95Milliseconds=times[(int)((times.Count-1)*0.95)];result.cpuAverageMilliseconds=cpuTotal/times.Count;result.allocationAverageBytes=allocation/times.Count;result.memoryEndBytes=memory.LastValue;}
            File.WriteAllText(path,JsonUtility.ToJson(result,true));cpu.Dispose();gc.Dispose();memory.Dispose();
            worldTime.Dispose();separationTime.Dispose();projectileTime.Dispose();pickupTime.Dispose();
            editorTime.Dispose();playerLoopTime.Dispose();gcTime.Dispose();renderWaitTime.Dispose();
            if(GameFlow.Instance!=null){GameFlow.Instance.GetComponentInChildren<Canvas>().enabled=uiEnabled;GameFlow.Instance.enabled=flowEnabled;}if(world!=null)world.enabled=worldEnabled;
            AudioListener.volume=audioVolume;if(world!=null)foreach(var enemy in world.Combatants)world.ReturnEnemy(enemy);
            foreach(var profile in profiles)Object.Destroy(profile);active=null;Object.DestroyImmediate(this);
        }
    }
}
