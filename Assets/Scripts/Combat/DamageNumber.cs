using Survivor.Pooling;
using TMPro;
using UnityEngine;

namespace Survivor.Combat
{
    public sealed class DamageNumber : PoolActor
    {
        public TextMeshPro label;
        public float Remaining { get; private set; }
        public void Show(Vector2 position, int damage, float lifetime, bool playerHit)
        {
            transform.position = position; Remaining = lifetime;
            label.text = damage.ToString(); label.color = playerHit ? new Color(1, 0.85f, 0.25f) : Color.white;
        }
        public void Tick(float delta, float speed) { Remaining -= delta; transform.position += Vector3.up * (delta * speed); }
        public override void ResetForRent() { Remaining = 0; label.text = ""; label.color = Color.white; }
        public override void ResetForReturn() { Remaining = 0; label.text = ""; }
    }
}
