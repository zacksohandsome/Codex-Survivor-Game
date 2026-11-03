using NUnit.Framework;
using Survivor.Core;
using Survivor.Input;
using UnityEngine;

namespace Survivor.Tests
{
    public class FlowRulesTests
    {
        private static GameSession StartBattle()
        {
            var session = new GameSession(); session.OpenWeaponSelection(); session.SelectWeapon("bolt"); session.StartRun(3);
            session.Tick(3); return session;
        }
        [Test]
        public void SelectionIsRequiredAndDuplicateStartIsRejected()
        {
            var session = new GameSession();
            Assert.That(session.StartRun(3), Is.False);
            session.OpenWeaponSelection(); Assert.That(session.StartRun(3), Is.False);
            session.SelectWeapon("bolt"); Assert.That(session.StartRun(3), Is.True);
            Assert.That(session.StartRun(3), Is.False);
            Assert.That(session.BattleSeconds, Is.Zero);
        }
        [Test]
        public void PauseAndLevelUpFreezeClockAndCannotOverlap()
        {
            var session = StartBattle(); session.Tick(2);
            Assert.That(session.BeginLevelUp(), Is.True);
            Assert.That(session.Pause(), Is.False); session.Tick(50);
            Assert.That(session.BattleSeconds, Is.EqualTo(2));
            session.EndLevelUp(); session.Pause(); session.Tick(50);
            Assert.That(session.BeginLevelUp(), Is.False);
            Assert.That(session.BattleSeconds, Is.EqualTo(2));
            session.Resume(); session.Tick(1); Assert.That(session.BattleSeconds, Is.EqualTo(3));
        }
        [Test]
        public void CountdownCanPauseWithoutConsumingBattleTime()
        {
            var session = new GameSession(); session.OpenWeaponSelection(); session.SelectWeapon("bolt"); session.StartRun(3);
            session.Tick(1); session.Pause(); session.Tick(30); session.Resume();
            Assert.That(session.State, Is.EqualTo(GameState.IntroCountdown));
            Assert.That(session.IntroRemaining, Is.EqualTo(2));
            session.Tick(2); Assert.That(session.State, Is.EqualTo(GameState.Battle));
            Assert.That(session.BattleSeconds, Is.Zero);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void FinishUsesFirstOutcomeAndDetachedSnapshot(bool won)
        {
            var session = StartBattle(); session.Tick(12);
            session.Run.Kills = 4; session.Run.CollectedCurrency = 7;
            Assert.That(session.Finish(won), Is.True);
            Assert.That(session.State, Is.EqualTo(won ? GameState.Victory : GameState.Defeat));
            Assert.That(session.Finish(!won), Is.False);
            session.Run.Kills = 99; session.Tick(100); session.ShowResult();
            session.ReturnToMenu();
            Assert.That(session.Result.Kills, Is.EqualTo(4));
            Assert.That(session.Result.SurvivalSeconds, Is.EqualTo(12));
            Assert.That(session.Result.CollectedCurrency, Is.EqualTo(7));
            Assert.That(session.Result.Won, Is.EqualTo(won));
        }
        [Test]
        public void NewRunDoesNotRetainPreviousGrowthOrSelection()
        {
            var session = StartBattle(); var firstId = session.Run.Id;
            session.Run.Level = 9; session.Run.AbilityLevels["orbit"] = 5;
            session.ReturnToMenu(); session.OpenWeaponSelection();
            Assert.That(session.SelectedWeaponId, Is.Null);
            session.SelectWeapon("pulse"); session.StartRun(3);
            Assert.That(session.Run.Id, Is.Not.EqualTo(firstId));
            Assert.That(session.Run.Level, Is.EqualTo(1));
            Assert.That(session.Run.AbilityLevels.Count, Is.EqualTo(1));
            Assert.That(session.Run.AbilityLevels["pulse"], Is.EqualTo(1));
        }
        [Test]
        public void HeldMovementMustBeReleasedBeforeFreshInputIsAccepted()
        {
            var gate = new MovementGate();
            Assert.That(gate.Read(false, true, Vector2.up), Is.EqualTo(Vector2.zero));
            Assert.That(gate.Read(true, true, Vector2.up), Is.EqualTo(Vector2.zero));
            gate.Read(true, false, Vector2.zero);
            Assert.That(gate.Read(true, true, Vector2.one).magnitude, Is.EqualTo(1).Within(0.001));
            gate.Block(); Assert.That(gate.Read(true, true, Vector2.up), Is.EqualTo(Vector2.zero));
        }
    }
}
