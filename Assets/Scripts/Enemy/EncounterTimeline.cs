using System;

namespace Survivor.Enemy
{
    public sealed class EncounterTimeline
    {
        public bool EliteSpawned { get; private set; }
        public bool BossSpawned { get; private set; }
        public static int Stage(double seconds) => Math.Max(0, Math.Min(4, (int)(seconds / 120)));
        public bool EliteDue(double seconds, double trigger) => !EliteSpawned && seconds >= trigger;
        public bool BossDue(double seconds, double trigger) => !BossSpawned && seconds >= trigger;
        public void MarkElite() => EliteSpawned = true;
        public void MarkBoss() => BossSpawned = true;
        // D03: either living special suppresses ordinary spawns; rates do not multiply.
        public static float SpawnRate(bool elitePresent, bool bossPresent, float reducedRate)
            => elitePresent || bossPresent ? reducedRate : 1;
    }
}
