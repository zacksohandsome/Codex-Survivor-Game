using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using Survivor.Pooling;
using UnityEngine;

namespace Survivor.Drop
{
    public sealed class PickupManager
    {
        public BoundedPool<PickupActor> Pool { get; }
        private readonly BattleWorld world;
        private readonly ProgressionConfig config;
        private readonly Vector2[] positions;
        public long UncollectedExperience { get { long total = 0; foreach (var p in Pool.Items) if (p.IsRented) total = checked(total + p.Experience); return total; } }
        public long UncollectedCurrency { get { long total = 0; foreach (var p in Pool.Items) if (p.IsRented) total = checked(total + p.Currency); return total; } }
        public PickupManager(BattleWorld owner, PickupActor prefab, ProgressionConfig settings)
        {
            world = owner; config = settings;
            Pool = new BoundedPool<PickupActor>(settings.dropCapacity, () => Object.Instantiate(prefab, owner.transform));
            positions=new Vector2[Pool.Items.Length];
        }
        public PickupActor Spawn(Vector2 position, long experience, long currency)
        {
            if (experience < 0 || currency < 0 || (experience == 0 && currency == 0)) return null;
            var pickup = Pool.Rent();
            if (pickup != null) pickup.transform.position = position;
            else pickup = Nearest(position, null);
            pickup.Add(experience, currency); return pickup;
        }
        private PickupActor Nearest(Vector2 position, PickupActor excluded,bool cached=false)
        {
            PickupActor result = null; float best = float.MaxValue;
            for(int i=0;i<Pool.Items.Length;i++)
            {
                var p=Pool.Items[i];
                if (!p.IsRented || p == excluded) continue;
                float distance = ((cached?positions[i]:(Vector2)p.transform.position) - position).sqrMagnitude;
                if (distance < best) { result = p; best = distance; }
            }
            return result;
        }
        public void Tick(float delta)
        {
            // Maintain the same sequential move/merge order without repeatedly crossing
            // into native Transform state for every candidate in every nearest search.
            bool cachePositions=false;
            foreach(var item in Pool.Items)if(item.IsRented && item.Age+delta>=config.mergeAfterSeconds){cachePositions=true;break;}
            if(cachePositions)for(int i=0;i<Pool.Items.Length;i++)if(Pool.Items[i].IsRented)positions[i]=Pool.Items[i].transform.position;
            for(int i=0;i<Pool.Items.Length;i++)
            {
                var pickup=Pool.Items[i];
                if (!pickup.IsRented) continue;
                float magnet = (config.magnetRadius + world.CurrentBuild.MagnetRadiusBonus) * (world.Passives.MagnetBurst ? 2 : 1);
                pickup.Tick(delta, world.Player.transform.position, magnet, config.magnetSpeed + world.CurrentBuild.MagnetSpeedBonus);
                if(cachePositions)positions[i]=pickup.transform.position;
                if (pickup.Age >= config.mergeAfterSeconds)
                {
                    var target = Nearest(cachePositions?positions[i]:(Vector2)pickup.transform.position, pickup,cachePositions);
                    if (target != null) { target.Add(pickup.Experience, pickup.Currency); Pool.Return(pickup); }
                    else pickup.ResetAge();
                }
            }
            Physics2D.SyncTransforms();
            var flow = GameFlow.Instance;
            foreach (var pickup in Pool.Items)
            {
                if (flow.Session.State != GameState.Battle) break;
                if (!pickup.IsRented || pickup.hitbox.Distance(world.Player.hitbox).distance > 0) continue;
                long currency = pickup.Currency, experience = pickup.Experience;
                // Keep the entire physical bundle if its currency notification cannot be shown yet.
                if (currency > 0 && !world.TryShowCurrency(pickup.transform.position, currency)) continue;
                flow.Session.Run.CollectedCurrency = checked(flow.Session.Run.CollectedCurrency + currency);
                if(currency>0)flow.Audio.PlayCombat("coin",0.15f);
                Pool.Return(pickup);
                flow.Progression.GainExperience(experience);
            }
        }
        public void Clear() { foreach (var pickup in Pool.Items) Pool.Return(pickup); }
    }
}
