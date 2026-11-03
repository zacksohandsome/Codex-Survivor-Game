using Survivor.Pooling;
using UnityEngine;

namespace Survivor.Visuals
{
    public sealed class BurstActor : PoolActor
    {
        public ParticleSystem particles;
        public float Remaining { get; private set; }
        public void Fire(Vector2 position,Color color,bool strong)
        {
            transform.position=position; Remaining=0.6f;
            var main=particles.main;main.startColor=color;main.startSpeed=strong?3:1.4f;
            particles.Play(true);
        }
        public void Tick(float delta) { Remaining-=delta; }
        public override void ResetForRent() { Remaining=0;particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
        public override void ResetForReturn()=>ResetForRent();
    }
}
