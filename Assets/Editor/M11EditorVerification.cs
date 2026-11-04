using System;
using System.IO;
using System.Reflection;
using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Survivor.Editor
{
    // Explicit QA menus use already compiled code, keeping the dynamic evaluation
    // compiler and its reference cache out of the fresh-Editor measurement.
    [InitializeOnLoad]
    public static class M11EditorVerification
    {
        private const string ModeKey="Survivor.M11.Mode",PhaseKey="Survivor.M11.Phase",SaveKey="Survivor.M11.Save",MuteKey="Survivor.M11.Mute",RestoreKey="Survivor.M11.RestoreMute",StartKey="Survivor.M11.Start",BackgroundKey="Survivor.M11.Background";
        private static StreamWriter frames,encounters;
        private static ProfilerRecorder cpu,worldTime,allocations,collect,ui;
        private static int previousFrame=-1,runNumber;
        private static RunContext observedRun;
        private static double started,nextSample;
        [Serializable] private sealed class MemorySnapshot
        {public string status,unityVersion,runtimeHash,graphicsApi,scenePath;public long monoUsed,monoHeap,totalAllocated;public int width,height;public float timeScale,captureDelta;public bool runInBackground,focused,sceneDirty;}
        [Serializable] private sealed class VerificationStatus{public string mode,status,errorCategory;}
        [Serializable] private sealed class FinalResult
        {public string state,driver;public bool secondWon;public double secondSeconds;public int secondKills,healthUpgrade,saveVersion;public long secondCurrency,balance,runs,victories,healthSpend;}
        static M11EditorVerification()
        {
            EditorApplication.playModeStateChanged+=OnPlayState;
            if(string.IsNullOrEmpty(SessionState.GetString(ModeKey,"")))return;
            GameFlow.SaveDirectoryOverride=SessionState.GetString(SaveKey,"");Register();
        }
        [MenuItem("Survivor/QA/Run Clean Moving Probe")]
        public static void StartProbe()=>Start("probe");
        [MenuItem("Survivor/QA/Run Final Normal Playthrough")]
        public static void StartNormal()=>Start("normal");
        [MenuItem("Survivor/QA/Capture Editor State")]
        public static void CaptureEditorState()=>CaptureMemory("Documentation/Reports/M10/editor-current-state.json","editor-state");
        private static string Project(string relative)=>Path.GetFullPath(Path.Combine(Application.dataPath,"..",relative));
        private static void Start(string mode)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start verification from Edit Mode");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits before verification");
            SessionState.SetBool(MuteKey,EditorUtility.audioMasterMute);SessionState.SetBool(RestoreKey,true);EditorUtility.audioMasterMute=true;
            SessionState.SetBool(BackgroundKey,Application.runInBackground);
            SessionState.SetString(ModeKey,mode);SessionState.SetInt(PhaseKey,0);
            SessionState.SetFloat(StartKey,(float)EditorApplication.timeSinceStartup);
            SessionState.SetString(SaveKey,Path.Combine(Application.temporaryCachePath,"Survivor-M11-"+Guid.NewGuid().ToString("N")));
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            Register();EditorApplication.isPlaying=true;
        }
        private static void Register(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
        private static void Tick()
        {
            string mode=SessionState.GetString(ModeKey,"");if(string.IsNullOrEmpty(mode)){EditorApplication.update-=Tick;return;}
            if(!EditorApplication.isPlaying)return;
            try
            {
                int phase=SessionState.GetInt(PhaseKey,0);
                if(EditorApplication.timeSinceStartup-SessionState.GetFloat(StartKey,0)>(phase<3?120:1500)){Complete(mode,"failed","timeout");return;}
                if(phase==0)
                {
                    GameFlow.SaveDirectoryOverride=SessionState.GetString(SaveKey,"");
                    if(GameFlow.Instance!=null)Object.DestroyImmediate(GameFlow.Instance.gameObject);
                    SessionState.SetInt(PhaseKey,1);SceneManager.LoadScene("MainMenu");return;
                }
                Application.runInBackground=true;
                var flow=GameFlow.Instance;if(flow==null || flow.IsTransitioning)return;
                if(mode=="probe" && flow.Session.State==GameState.Pause){flow.gameObject.SendMessage("OnApplicationFocus",true);flow.Resume();}
                if(phase==1)
                {
                    if(mode=="probe")
                    {flow.OpenWeaponSelection();flow.SelectWeapon("bolt");flow.StartRun();SessionState.SetInt(PhaseKey,2);}
                    else
                    {
                        OpenObservation();M5EditorPlaythrough.BeginForMilestone(Project("Documentation/Reports/M11"),Project("Recordings/M11-final"));
                        CaptureMemory("Documentation/Reports/M11/editor-normal-start.json","normal-start");SessionState.SetInt(PhaseKey,3);
                    }
                    return;
                }
                if(mode=="probe" && phase==2)
                {
                    if(flow.Session.State!=GameState.Battle)return;
                    var world=BattleWorld.Instance;var originals=new[]{world.config.enemy,world.config.encounters.ranged,world.config.encounters.bomber,world.config.encounters.elite,world.config.encounters.boss};
                    string label=SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Direct3D11?"fresh-editor-d3d11-moving-60s":"fresh-editor-d3d12-moving-60s";
                    M10PerformanceProbe.Begin(label,60);
                    var type=typeof(M10PerformanceProbe);var probe=type.GetField("active",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                    var copies=(System.Collections.Generic.List<EnemyDefinition>)type.GetField("profiles",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(probe);
                    foreach(var copy in copies){var original=Array.Find(originals,d=>d.kind==copy.kind);copy.moveSpeed=original.moveSpeed;copy.attackRange=original.attackRange;}
                    CaptureMemory("Documentation/Reports/M10/editor-fresh-before-probe.json","fresh-before-probe");SessionState.SetInt(PhaseKey,3);return;
                }
                if(phase!=3)return;
                if(mode=="probe")
                {
                    if(M10PerformanceProbe.Status!="idle")return;
                    CaptureMemory("Documentation/Reports/M10/editor-fresh-after-probe.json","fresh-after-probe");Complete(mode,"completed",null);return;
                }
                Observe(flow);
                string status=M5EditorPlaythrough.ActiveStatus;
                if(status!="passed" && status!="verification_failed" && status!="recording_failed")return;
                var result=flow.Session.Result;
                if(result!=null)
                {
                    var final=new FinalResult{state=flow.Session.State.ToString(),driver=status,secondWon=result.Won,secondSeconds=result.SurvivalSeconds,secondKills=result.Kills,secondCurrency=result.CollectedCurrency,
                        balance=flow.PermanentData.currency,runs=flow.PermanentData.totalRuns,victories=flow.PermanentData.victories,healthUpgrade=flow.PermanentData.metaUpgradeLevels[0],healthSpend=flow.PermanentData.metaUpgradeSpent[0],saveVersion=flow.PermanentData.version};
                    File.WriteAllText(Project("Documentation/Reports/M11/final-result.json"),JsonUtility.ToJson(final,true));
                }
                CaptureMemory("Documentation/Reports/M11/editor-normal-finish.json",status);Complete(mode,status,null);
            }
            catch(Exception error){Complete(mode,"failed",error.GetType().Name);}
        }
        private static void OpenObservation()
        {
            Directory.CreateDirectory(Project("Documentation/Reports/M11"));
            frames=new StreamWriter(Project("Documentation/Reports/M11/event-frames.csv"));encounters=new StreamWriter(Project("Documentation/Reports/M11/encounter-samples.csv"));
            frames.WriteLine("run,frame,battleSeconds,state,level,kills,enemies,specials,bursts,cpuMs,worldMs,gcMs,allocatedBytes,uiMs,uiRenders,timeScale,captureDelta");
            encounters.WriteLine("run,observerSeconds,battleSeconds,state,stage,health,enemies,specials,hostile,spawnRate,bossHealth,bursts");
            cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);worldTime=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Survivor.World",1);
            allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);collect=ProfilerRecorder.StartNew(new ProfilerCategory("GC"),"GC.Collect",1);
            ui=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Survivor.UI.Render",1);started=Time.realtimeSinceStartupAsDouble;previousFrame=-1;runNumber=0;observedRun=null;
        }
        private static void Observe(GameFlow flow)
        {
            var world=BattleWorld.Instance;if(world==null || flow.Session.Run==null || previousFrame==Time.frameCount)return;previousFrame=Time.frameCount;
            var run=flow.Session.Run;if(!ReferenceEquals(run,observedRun)){observedRun=run;runNumber++;nextSample=0;}
            frames.WriteLine(FormattableString.Invariant($"{runNumber},{previousFrame},{flow.Session.BattleSeconds:F3},{flow.Session.State},{run.Level},{run.Kills},{world.Enemies.ActiveCount},{world.Specials.ActiveCount},{world.Bursts.ActiveCount},{cpu.LastValue/1000000.0:F4},{worldTime.LastValue/1000000.0:F4},{collect.LastValue/1000000.0:F4},{allocations.LastValue},{ui.LastValue/1000000.0:F4},{(ui.Count>0?ui.GetSample(ui.Count-1).Count:0)},{Time.timeScale:F1},{Time.captureDeltaTime:F3}"));
            bool ended=flow.Session.State==GameState.Victory || flow.Session.State==GameState.Defeat || flow.Session.State==GameState.Result;
            if(flow.Session.BattleSeconds<nextSample && !ended)return;nextSample=flow.Session.BattleSeconds+1;var boss=world.Boss;
            encounters.WriteLine(FormattableString.Invariant($"{runNumber},{Time.realtimeSinceStartupAsDouble-started:F3},{flow.Session.BattleSeconds:F3},{flow.Session.State},{world.DifficultyStage},{world.Player.Health.Current},{world.Enemies.ActiveCount},{world.Specials.ActiveCount},{world.HostileProjectiles.ActiveCount},{world.OrdinarySpawnRate:F2},{(boss==null?0:boss.Health.Current)},{world.Bursts.ActiveCount}"));
            frames.Flush();encounters.Flush();
        }
        private static void CaptureMemory(string relative,string status)
        {
            string hash="";using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(typeof(GameFlow).Assembly.Location))).Replace("-","").ToLowerInvariant();
            var data=new MemorySnapshot{status=status,unityVersion=Application.unityVersion,runtimeHash=hash,monoUsed=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),monoHeap=UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong(),
                totalAllocated=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),width=Screen.width,height=Screen.height,timeScale=Time.timeScale,captureDelta=Time.captureDeltaTime,runInBackground=Application.runInBackground,focused=Application.isFocused,
                graphicsApi=SystemInfo.graphicsDeviceType.ToString(),scenePath=SceneManager.GetActiveScene().path,sceneDirty=SceneManager.GetActiveScene().isDirty};
            string path=Project(relative);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(data,true));
        }
        private static void Complete(string mode,string status,string category)
        {
            CloseObservation();string directory=mode=="probe"?"Documentation/Reports/M10":"Documentation/Reports/M11";
            Directory.CreateDirectory(Project(directory));File.WriteAllText(Project(directory+"/verification-status.json"),JsonUtility.ToJson(new VerificationStatus{mode=mode,status=status,errorCategory=category},true));
            SessionState.SetString(ModeKey,"");EditorApplication.update-=Tick;if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
        }
        private static void CloseObservation()
        {
            frames?.Dispose();encounters?.Dispose();frames=encounters=null;
            cpu.Dispose();worldTime.Dispose();allocations.Dispose();collect.Dispose();ui.Dispose();
        }
        private static void OnPlayState(PlayModeStateChange state)
        {
            string mode=SessionState.GetString(ModeKey,"");
            if(state==PlayModeStateChange.ExitingPlayMode && !string.IsNullOrEmpty(mode))Complete(mode,"interrupted",null);
            if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(RestoreKey,false))return;
            EditorUtility.audioMasterMute=SessionState.GetBool(MuteKey,false);Application.runInBackground=SessionState.GetBool(BackgroundKey,false);SessionState.EraseBool(RestoreKey);
        }
    }
}
