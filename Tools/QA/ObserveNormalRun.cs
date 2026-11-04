if(!UnityEditor.EditorApplication.isPlaying || Survivor.Core.GameFlow.Instance==null)throw new System.InvalidOperationException("Start a normal run first");
string path="Documentation/Reports/M11/encounter-samples.csv";
System.IO.File.WriteAllText(path,"observerSeconds,battleSeconds,state,stage,health,enemies,specials,hostile,spawnRate,bossHealth,bursts\n");
double started=UnityEngine.Time.realtimeSinceStartupAsDouble,next=0;
int previousFrame=-1;
bool began=false;
UnityEditor.EditorApplication.CallbackFunction observe=null;
observe=()=>
{
    var flow=Survivor.Core.GameFlow.Instance;var world=Survivor.Combat.BattleWorld.Instance;
    if(!UnityEditor.EditorApplication.isPlaying || flow==null)
    {UnityEditor.EditorApplication.update-=observe;return;}
    if(world==null || flow.Session.Run==null){if(!began)return;UnityEditor.EditorApplication.update-=observe;return;}
    began=true;
    if(previousFrame==UnityEngine.Time.frameCount)return;previousFrame=UnityEngine.Time.frameCount;
    var state=flow.Session.State;bool ended=state==Survivor.Core.GameState.Victory || state==Survivor.Core.GameState.Defeat || state==Survivor.Core.GameState.Result;
    if(flow.Session.BattleSeconds>=next || ended)
    {
        next=flow.Session.BattleSeconds+1;var boss=world.Boss;
        string row=System.FormattableString.Invariant($"{UnityEngine.Time.realtimeSinceStartupAsDouble-started:F3},{flow.Session.BattleSeconds:F3},{state},{world.DifficultyStage},{world.Player.Health.Current},{world.Enemies.ActiveCount},{world.Specials.ActiveCount},{world.HostileProjectiles.ActiveCount},{world.OrdinarySpawnRate:F2},{(boss==null?0:boss.Health.Current)},{world.Bursts.ActiveCount}\n");
        System.IO.File.AppendAllText(path,row);
    }
    if(ended)UnityEditor.EditorApplication.update-=observe;
};
UnityEditor.EditorApplication.update+=observe;
return new {path=path,startingBattleSeconds=Survivor.Core.GameFlow.Instance.Session.BattleSeconds};
