using System;

namespace Survivor.Save
{
    // Permanent data only; live battle objects never enter a save.
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public long currency;
        public int[] metaUpgradeLevels = new int[5];
        public long totalRuns;
        public long victories;
        public long totalKills;
        public double totalSurvivalSeconds;
        public string lastSettledRunId;
        public string[] settledRunIds = new string[0];
        public string language = "zh-TW";
    }
}
