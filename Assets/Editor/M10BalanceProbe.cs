using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using Survivor.Progression;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Survivor.Editor
{
    // Fixed-step gameplay simulation: uses normal stats, offered upgrades and real collision logic.
    // This is balance evidence, never a normal-speed playthrough or an FPS benchmark.
    public sealed class M10BalanceProbe : ScriptableObject
    {
        [Serializable] private sealed class Point{public double seconds;public int health,kills,level,enemies;public long currency;public bool bossAlive;}
        [Serializable] private sealed class Result{public string weapon,status;public int seed,kills,level,health;public bool won,eliteKilled,bossKilled;public double seconds;public long currency;public List<string> choices=new List<string>();public List<Point> samples=new List<Point>();}
        private static M10BalanceProbe active;public static string Status=>active==null?"idle":active.result.status;
        private Result result;private GameFlow flow;private BattleWorld world;private int waypoint;private double nextSample;
        private readonly Vector2[] route={new Vector2(15,8),new Vector2(-15,8),new Vector2(-15,-8),new Vector2(15,-8)};
        public static void Begin(string weapon,int seed)
        {
            if(active!=null || !EditorApplication.isPlaying || GameFlow.Instance.Session.State!=GameState.MainMenu)throw new InvalidOperationException("Begin one balance probe at MainMenu");
            active=CreateInstance<M10BalanceProbe>();active.result=new Result{weapon=weapon,seed=seed,status="loading"};active.flow=GameFlow.Instance;
            active.flow.OpenWeaponSelection();active.flow.SelectWeapon(weapon);active.flow.StartRun();EditorApplication.update+=active.Tick;
        }
        private void Tick()
        {
            if(!EditorApplication.isPlaying || flow==null){Finish("interrupted");return;}
            if(result.status=="loading")
            {
                if(flow.IsTransitioning || BattleWorld.Instance==null)return;world=BattleWorld.Instance;
                typeof(GameFlow).GetField("<Progression>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(flow,new RunProgression(flow.Session,world.config.progression,result.seed));
                flow.Session.Tick(4);flow.enabled=false;world.enabled=false;result.status="simulating";
            }
            const float step=1f/30;
            for(int iteration=0;iteration<90;iteration++)
            {
                if(flow.Session.State==GameState.LevelUp)
                {
                    int best=0;float score=float.MinValue;
                    for(int i=0;i<flow.Progression.Choices.Length;i++)
                    {
                        var option=flow.Progression.Choices[i];float value=option.AbilityId==result.weapon?12:
                            option.AbilityId=="bloom"?10:option.Effect.effect==UpgradeEffect.Attack?8:option.Effect.effect==UpgradeEffect.AttackSpeed?7:
                            option.Effect.effect==UpgradeEffect.Health?6:option.Category==UpgradeCategory.Weapon?5:3;
                        if(value>score){score=value;best=i;}
                    }
                    var chosen=flow.Progression.Choices[best];result.choices.Add(chosen.AbilityId+":"+chosen.Effect.id);
                    if(!flow.ChooseUpgrade(best,flow.Progression.ChoiceRevision)){Finish("choice-blocked");return;}continue;
                }
                if(flow.Session.State!=GameState.Battle){Finish("completed");return;}
                flow.Session.Tick(step);if(flow.Session.State==GameState.Battle)world.Tick(step,Movement());
                if(flow.Session.State==GameState.Battle || flow.Session.State==GameState.LevelUp)
                {
                    bool elite=false;foreach(var enemy in world.Specials.Items)if(enemy.IsRented && enemy.Kind==EnemyKind.Elite)elite=true;
                    result.eliteKilled|=world.Timeline.EliteSpawned && !elite;
                    result.bossKilled|=world.NoticeKey=="encounter.boss.defeated";
                }
                if(flow.Session.BattleSeconds>=nextSample)
                {
                    result.samples.Add(new Point{seconds=flow.Session.BattleSeconds,health=world.Player.Health.Current,kills=flow.Session.Run.Kills,level=flow.Session.Run.Level,currency=flow.Session.Run.CollectedCurrency,enemies=world.Enemies.ActiveCount,bossAlive=world.Boss!=null});nextSample+=30;
                }
            }
        }
        private Vector2 Movement()
        {
            Vector2 player=world.Player.transform.position;if((route[waypoint]-player).sqrMagnitude<3)waypoint=(waypoint+1)%4;
            Vector2 desired=route[waypoint]-player;float best=64;
            foreach(var drop in world.Pickups.Pool.Items)
            {
                if(!drop.IsRented)continue;Vector2 offset=(Vector2)drop.transform.position-player;float distance=offset.sqrMagnitude;
                if(distance>=best || distance<1)continue;bool safe=true;
                foreach(var enemy in world.Combatants)if(enemy.IsAlive && ((Vector2)enemy.transform.position-(Vector2)drop.transform.position).sqrMagnitude<6.25f){safe=false;break;}
                if(safe){best=distance;desired=offset;}
            }
            Vector2 direction=desired.normalized;
            foreach(var enemy in world.Combatants)
            {
                if(!enemy.IsAlive)continue;Vector2 away=player-(Vector2)enemy.transform.position;float distance=away.magnitude;
                if(distance<3.5f && distance>0.01f)direction+=away.normalized*((3.5f-distance)*1.8f);
                if(enemy.AttackWarningRemaining>0 && enemy.TelegraphRadius>0){away=player-enemy.TelegraphCenter;if(away.magnitude<enemy.TelegraphRadius+1 && away.sqrMagnitude>0.001f)direction+=away.normalized*4;}
            }
            foreach(var shot in world.HostileProjectiles.Items)if(shot.IsRented){Vector2 away=player-(Vector2)shot.transform.position;if(away.sqrMagnitude<9 && away.sqrMagnitude>0.001f)direction+=away.normalized*2;}
            // Match keyboard's eight movement directions.
            return new Vector2(direction.x>0.2f?1:direction.x< -0.2f?-1:0,direction.y>0.2f?1:direction.y< -0.2f?-1:0).normalized;
        }
        private void Finish(string status)
        {
            EditorApplication.update-=Tick;result.status=status;
            if(flow!=null && flow.Session.Run!=null)
            {
                result.seconds=flow.Session.BattleSeconds;result.kills=flow.Session.Run.Kills;result.level=flow.Session.Run.Level;result.currency=flow.Session.Run.CollectedCurrency;result.health=world.Player.Health.Current;
                result.won=flow.Session.State==GameState.Victory || flow.Session.Result!=null && flow.Session.Result.Won;
            }
            Directory.CreateDirectory("Documentation/Reports/M10");File.WriteAllText("Documentation/Reports/M10/balance-"+result.weapon+"-"+result.seed+".json",JsonUtility.ToJson(result,true));
            if(flow!=null)flow.enabled=true;if(world!=null)world.enabled=true;active=null;Object.DestroyImmediate(this);
        }
    }
}
