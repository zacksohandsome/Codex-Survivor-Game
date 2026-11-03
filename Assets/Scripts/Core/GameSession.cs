using System;
using System.Collections.Generic;

namespace Survivor.Core
{
    public enum GameState { MainMenu, WeaponSelection, IntroCountdown, Battle, LevelUp, Pause, Victory, Defeat, Result }

    public sealed class RunContext
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public string StartingWeaponId { get; }
        public int Level { get; set; } = 1;
        public int Kills { get; set; }
        public int CollectedCurrency { get; set; }
        public Dictionary<string, int> AbilityLevels { get; } = new Dictionary<string, int>();
        public RunContext(string weaponId)
        {
            StartingWeaponId = weaponId;
            AbilityLevels.Add(weaponId, 1);
        }
    }

    // Immutable snapshot: never keeps a reference to scene objects or mutable run data.
    public sealed class RunResult
    {
        public string RunId { get; }
        public string WeaponId { get; }
        public bool Won { get; }
        public double SurvivalSeconds { get; }
        public int Level { get; }
        public int Kills { get; }
        public int CollectedCurrency { get; }
        public RunResult(RunContext run, bool won, double seconds)
        {
            RunId = run.Id; WeaponId = run.StartingWeaponId; Won = won;
            SurvivalSeconds = seconds; Level = run.Level; Kills = run.Kills;
            CollectedCurrency = run.CollectedCurrency;
        }
    }

    public sealed class GameSession
    {
        public GameState State { get; private set; } = GameState.MainMenu;
        public RunContext Run { get; private set; }
        public RunResult Result { get; private set; }
        public string SelectedWeaponId { get; private set; }
        public double BattleSeconds { get; private set; }
        public double IntroRemaining { get; private set; }
        private GameState pausedState;
        public event Action Changed;

        public bool OpenWeaponSelection()
        {
            if (State != GameState.MainMenu) return false;
            SelectedWeaponId = null;
            SetState(GameState.WeaponSelection); return true;
        }
        public bool SelectWeapon(string id)
        {
            if (State != GameState.WeaponSelection || string.IsNullOrWhiteSpace(id)) return false;
            SelectedWeaponId = id; Changed?.Invoke(); return true;
        }
        public bool StartRun(double countdownSeconds)
        {
            if (State != GameState.WeaponSelection || SelectedWeaponId == null) return false;
            Run = new RunContext(SelectedWeaponId); Result = null;
            BattleSeconds = 0; IntroRemaining = Math.Max(0, countdownSeconds);
            SetState(GameState.IntroCountdown); return true;
        }
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) return;
            if (State == GameState.IntroCountdown)
            {
                IntroRemaining = Math.Max(0, IntroRemaining - seconds);
                // Countdown's final frame is not charged to the battle clock.
                if (IntroRemaining == 0) SetState(GameState.Battle);
            }
            else if (State == GameState.Battle) BattleSeconds += seconds;
        }
        public bool Pause()
        {
            if (State != GameState.Battle && State != GameState.IntroCountdown) return false;
            pausedState = State; SetState(GameState.Pause); return true;
        }
        public bool Resume()
        {
            if (State != GameState.Pause) return false;
            SetState(pausedState); return true;
        }
        public bool BeginLevelUp()
        {
            if (State != GameState.Battle) return false;
            SetState(GameState.LevelUp); return true;
        }
        public bool EndLevelUp()
        {
            if (State != GameState.LevelUp) return false;
            SetState(GameState.Battle); return true;
        }
        public bool Finish(bool won)
        {
            if (State != GameState.Battle || Result != null) return false;
            Result = new RunResult(Run, won, BattleSeconds);
            SetState(won ? GameState.Victory : GameState.Defeat); return true;
        }
        public bool ShowResult()
        {
            if (State != GameState.Victory && State != GameState.Defeat) return false;
            SetState(GameState.Result); return true;
        }
        public void ReturnToMenu()
        {
            Run = null; SelectedWeaponId = null; IntroRemaining = 0; BattleSeconds = 0;
            // Result stays a detached snapshot until a new run starts; menu doesn't display it.
            SetState(GameState.MainMenu);
        }
        private void SetState(GameState state) { State = state; Changed?.Invoke(); }
    }
}
