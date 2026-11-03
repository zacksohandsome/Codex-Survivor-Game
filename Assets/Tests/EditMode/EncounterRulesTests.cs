using NUnit.Framework;
using Survivor.Enemy;

namespace Survivor.Tests
{
    public class EncounterRulesTests
    {
        [TestCase(0,0)] [TestCase(119.99,0)] [TestCase(120,1)] [TestCase(240,2)] [TestCase(360,3)] [TestCase(480,4)] [TestCase(600,4)]
        public void StageChangesAtTwoMinuteBoundaries(double seconds, int stage) => Assert.That(EncounterTimeline.Stage(seconds), Is.EqualTo(stage));
        [Test] public void SpecialEventsStayPendingUntilSuccessfulSpawnAndOnlyOccurOnce()
        {
            var timeline = new EncounterTimeline();
            Assert.That(timeline.EliteDue(179.99,180), Is.False); Assert.That(timeline.EliteDue(180,180), Is.True);
            Assert.That(timeline.EliteDue(190,180), Is.True); timeline.MarkElite(); Assert.That(timeline.EliteDue(599,180), Is.False);
            Assert.That(timeline.BossDue(419.99,420), Is.False); Assert.That(timeline.BossDue(420,420), Is.True);
            timeline.MarkBoss(); Assert.That(timeline.BossDue(599,420), Is.False);
        }
        [TestCase(false,false,1)] [TestCase(true,false,0.6f)] [TestCase(false,true,0.6f)] [TestCase(true,true,0.6f)]
        public void EitherSpecialSuppressesOrdinarySpawnsWithoutMultiplication(bool elite, bool boss, float rate)
            => Assert.That(EncounterTimeline.SpawnRate(elite,boss,0.6f), Is.EqualTo(rate));
    }
}
