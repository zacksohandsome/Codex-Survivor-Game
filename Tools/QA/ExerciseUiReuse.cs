var flow=Survivor.Core.GameFlow.Instance;var world=Survivor.Combat.BattleWorld.Instance;
if(!UnityEditor.EditorApplication.isPlaying || flow==null || world==null || flow.Session.State!=Survivor.Core.GameState.Battle)throw new System.InvalidOperationException("Start an isolated Battle first");
bool flowEnabled=flow.enabled,worldEnabled=world.enabled;flow.enabled=world.enabled=false;
int previous=-1,phase=-2,completed=0,before=0;var counts=new System.Collections.Generic.List<int>();
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>
{
    if(!UnityEditor.EditorApplication.isPlaying || flow==null || world==null){UnityEditor.EditorApplication.update-=tick;return;}
    if(previous==UnityEngine.Time.frameCount)return;previous=UnityEngine.Time.frameCount;
    if(phase<0){phase++;if(phase==0)before=flow.GetComponentsInChildren<UnityEngine.Transform>(true).Length;return;}
    if(phase==0){flow.Progression.GainExperience(flow.Progression.RequiredExperience);phase=1;return;}
    if(phase==1)
    {
        if(!flow.ChooseUpgrade(0,flow.Progression.ChoiceRevision))throw new System.InvalidOperationException("UI exercise choice rejected");
        phase=2;return;
    }
    counts.Add(flow.GetComponentsInChildren<UnityEngine.Transform>(true).Length);completed++;phase=0;
    if(completed<12)return;
    string data="{\"syntheticUiExercise\":true,\"upgrades\":12,\"objectsBefore\":"+before+",\"objectsAfter\":"+counts[counts.Count-1]+",\"counts\":["+string.Join(",",counts)+"]}";
    System.IO.File.WriteAllText("Documentation/Reports/M11/ui-reuse.json",data);
    flow.enabled=flowEnabled;world.enabled=worldEnabled;UnityEditor.EditorApplication.update-=tick;
};
UnityEditor.EditorApplication.update+=tick;
return new {status="running",upgrades=12,syntheticUiExercise=true};
