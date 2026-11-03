using Survivor.Core;
using Survivor.Data;

namespace Survivor.Progression
{
    public sealed class RunProgression
    {
        private readonly GameSession session;
        private readonly ProgressionConfig config;
        private readonly UpgradeSelector selector;
        public UpgradeOption[] Choices { get; private set; }
        public int ChoiceRevision { get; private set; }
        public long RequiredExperience => Required(session.Run.Level, config);
        public RunProgression(GameSession game, ProgressionConfig settings, int seed)
        { session = game; config = settings; selector = new UpgradeSelector(seed); }
        public static long Required(int level, ProgressionConfig settings)
        {
            long late = System.Math.Max(0, level - settings.xpLateLevel);
            return settings.startingXp + (long)settings.xpLinear * (level - 1) + late * late;
        }
        public void GainExperience(long value)
        {
            if (session.State != GameState.Battle || value <= 0) return;
            session.Run.Experience = checked(session.Run.Experience + value);
            OpenNextLevel();
        }
        private bool OpenNextLevel()
        {
            if (session.Run.Experience < RequiredExperience) return false;
            session.Run.Experience -= RequiredExperience; session.Run.Level++;
            Choices = selector.Draw(session.Run.Build, config); ChoiceRevision++;
            session.BeginLevelUp(); return true;
        }
        public bool Choose(int index, int revision, out UpgradeEffectDefinition effect)
        {
            effect = null;
            if (session.State != GameState.LevelUp || Choices == null || revision != ChoiceRevision || index < 0 || index >= Choices.Length) return false;
            var option = Choices[index]; session.Run.Build.Apply(option); effect = option.Effect;
            Choices = null;
            // Avoid a transient Battle state while several level-ups are queued.
            if (!OpenNextLevel()) session.EndLevelUp();
            return true;
        }
    }
}
