using Survivor.Pooling;
using UnityEngine;

namespace Survivor.Drop
{
    public sealed class PickupActor : PoolActor
    {
        public SpriteRenderer body;
        public Sprite experienceSprite, currencySprite;
        public CircleCollider2D hitbox;
        public long Experience { get; private set; }
        public long Currency { get; private set; }
        public float Age { get; private set; }
        public bool Attracted { get; private set; }
        public void Add(long experience, long currency)
        {
            Experience = checked(Experience + experience); Currency = checked(Currency + currency);
            body.sprite = Experience > 0 ? experienceSprite : currencySprite;
            body.color = Experience > 0 && Currency > 0 ? new Color(0.8f, 0.45f, 1) : Experience > 0 ? new Color(0.25f, 1, 0.45f) : new Color(1, 0.8f, 0.1f);
        }
        public void Tick(float delta, Vector2 player, float magnetRadius, float speed)
        {
            Age += delta;
            if (((Vector2)transform.position - player).sqrMagnitude <= magnetRadius * magnetRadius) Attracted = true;
            if (Attracted) transform.position = Vector2.MoveTowards(transform.position, player, speed * delta);
        }
        public void ResetAge() { Age = 0; }
        public override void ResetForRent()
        { Experience = 0; Currency = 0; Age = 0; Attracted = false; hitbox.enabled = true; }
        public override void ResetForReturn()
        { Experience = 0; Currency = 0; Age = 0; Attracted = false; hitbox.enabled = false; }
    }
}
