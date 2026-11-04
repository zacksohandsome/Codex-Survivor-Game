// Explicit final stress variant: normal movement/attack ranges, synthetic high HP and zero damage.
// Run in a fresh Battle with an isolated save, after normal-speed recording has finished.
var type=typeof(Survivor.Editor.M10PerformanceProbe);
var activeField=type.GetField("active",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var profilesField=type.GetField("profiles",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
if(activeField==null || profilesField==null)throw new System.InvalidOperationException("Probe internals changed; update the explicit fixture first");
var world=Survivor.Combat.BattleWorld.Instance;
var originals=new[]{world.config.enemy,world.config.encounters.ranged,world.config.encounters.bomber,world.config.encounters.elite,world.config.encounters.boss};
Survivor.Editor.M10PerformanceProbe.Begin("optimized-moving-final-60s",60);
var probe=activeField.GetValue(null);
var copies=(System.Collections.Generic.List<Survivor.Data.EnemyDefinition>)profilesField.GetValue(probe);
foreach(var copy in copies)
{
    var original=System.Array.Find(originals,d=>d.kind==copy.kind);
    copy.moveSpeed=original.moveSpeed;copy.attackRange=original.attackRange;
}
return new {label="optimized-moving-final-60s",normalMovementAndAttackRanges=true,syntheticHealth=1000000,syntheticContactDamage=0};
