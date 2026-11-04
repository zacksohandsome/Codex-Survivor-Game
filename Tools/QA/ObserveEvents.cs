if(!UnityEditor.EditorApplication.isPlaying || Survivor.Core.GameFlow.Instance==null)throw new System.InvalidOperationException("Start a normal run first");
var cpu=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,"Main Thread",1);
var worldTime=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Scripts,"Survivor.World",1);
var allocations=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",1);
var collect=Unity.Profiling.ProfilerRecorder.StartNew(new Unity.Profiling.ProfilerCategory("GC"),"GC.Collect",1);
var ui=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Scripts,"Survivor.UI.Render",1);
var writer=new System.IO.StreamWriter("Documentation/Reports/M11/event-frames.csv",false);
writer.WriteLine("frame,battleSeconds,state,level,kills,enemies,specials,bursts,cpuMs,worldMs,gcMs,allocatedBytes,uiMs,uiRenders");
int previous=-1;
bool began=false;
UnityEditor.EditorApplication.CallbackFunction observe=null;
observe=()=>
{
    var f=Survivor.Core.GameFlow.Instance;var w=Survivor.Combat.BattleWorld.Instance;
    bool stop=!UnityEditor.EditorApplication.isPlaying || f==null;
    if(!stop && (w==null || f.Session.Run==null)){if(!began)return;stop=true;}
    if(!stop && previous!=UnityEngine.Time.frameCount)
    {
        began=true;
        previous=UnityEngine.Time.frameCount;var run=f.Session.Run;
        writer.WriteLine(System.FormattableString.Invariant($"{previous},{f.Session.BattleSeconds:F3},{f.Session.State},{run.Level},{run.Kills},{w.Enemies.ActiveCount},{w.Specials.ActiveCount},{w.Bursts.ActiveCount},{cpu.LastValue/1000000.0:F4},{worldTime.LastValue/1000000.0:F4},{collect.LastValue/1000000.0:F4},{allocations.LastValue},{ui.LastValue/1000000.0:F4},{(ui.Count>0?ui.GetSample(ui.Count-1).Count:0)}"));
        stop=f.Session.State==Survivor.Core.GameState.Result || f.Session.State==Survivor.Core.GameState.Defeat || f.Session.State==Survivor.Core.GameState.Victory;
    }
    if(stop){UnityEditor.EditorApplication.update-=observe;writer.Dispose();cpu.Dispose();worldTime.Dispose();allocations.Dispose();collect.Dispose();ui.Dispose();}
};
UnityEditor.EditorApplication.update+=observe;
return new {path="Documentation/Reports/M11/event-frames.csv",startingBattleSeconds=Survivor.Core.GameFlow.Instance.Session.BattleSeconds};
