using System;

namespace Survivor.Save
{
    // Permanent data only; live battle objects never enter a save.
    [Serializable]
    public sealed class SaveData
    {
        public int version = 2;
        public long currency;
        public int[] metaUpgradeLevels = new int[5];
        public long totalRuns;
        public long victories;
        public long totalKills;
        public double totalSurvivalSeconds;
        public string lastSettledRunId;
        public string[] settledRunIds = new string[0];
        public string language = "";
        public float musicVolume = 0.6f, sfxVolume = 0.7f;
        public int displayWidth = 1920, displayHeight = 1080;
        public bool fullscreen;
    }
}
