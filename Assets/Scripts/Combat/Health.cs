using UnityEngine;

namespace Survivor.Combat
{
    public static class DamageCalculator
    {
        public static int Calculate(float raw, float defense)
            => Mathf.Max(1, Mathf.CeilToInt(raw * 100f / (100f + Mathf.Max(0, defense))));
    }

    public sealed class Health
    {
        public int Maximum { get; }
        public int Current { get; private set; }
        public float InvincibleRemaining { get; private set; }
        public float Defense { get; }
        private readonly float invincibleSeconds;
        public Health(int maximum, float defense, float invincibility)
        { Maximum = Mathf.Max(1, maximum); Current = Maximum; Defense = defense; invincibleSeconds = invincibility; }
        public void Tick(float delta) { InvincibleRemaining = Mathf.Max(0, InvincibleRemaining - delta); }
        public int TakeDamage(float raw)
        {
            if (Current == 0 || InvincibleRemaining > 0 || raw <= 0) return 0;
            int damage = DamageCalculator.Calculate(raw, Defense);
            Current = Mathf.Max(0, Current - damage); InvincibleRemaining = invincibleSeconds;
            return damage;
        }
    }
}
