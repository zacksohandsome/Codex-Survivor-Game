using System;
using System.Collections.Generic;
using Survivor.Core;
using Survivor.Save;

namespace Survivor.Meta
{
    public sealed class MetaProgression
    {
        public const int MaximumLevel = 5;
        public static readonly string[] StatIds = { "health", "attack", "speed", "move", "defense" };
        public static readonly float[] Bonuses = { 10, 1, 0.05f, 0.2f, 2 };
        private readonly SaveRepository repository;
        public SaveData Data { get; private set; }
        public string Notice { get; private set; }
        public MetaProgression(SaveRepository repository)
        { this.repository = repository; Data = repository.Load(); Notice = repository.Notice; }
        public static long Price(int currentLevel) => 10L * (currentLevel + 1);
        public bool CanPurchase(int stat) => stat >= 0 && stat < 5 && !repository.IsReadOnly && Data.metaUpgradeLevels[stat] < MaximumLevel && Data.currency >= Price(Data.metaUpgradeLevels[stat]);
        public bool Purchase(int stat)
        {
            if (!CanPurchase(stat)) return false;
            var next = SaveRepository.Clone(Data);
            next.currency -= Price(next.metaUpgradeLevels[stat]); next.metaUpgradeLevels[stat]++;
            return Commit(next);
        }
        public bool Refund()
        {
            var next = SaveRepository.Clone(Data);
            try
            {
                for (int stat = 0; stat < 5; stat++)
                {
                    for (int level = 0; level < next.metaUpgradeLevels[stat]; level++) next.currency = checked(next.currency + Price(level));
                    next.metaUpgradeLevels[stat] = 0;
                }
                return Commit(next);
            }
            catch (OverflowException) { Notice = "save.failed"; return false; }
        }
        public bool Settle(RunResult result)
        {
            if (result == null) return false;
            if (Data.lastSettledRunId == result.RunId || Array.IndexOf(Data.settledRunIds, result.RunId) >= 0) return true;
            if (result.CollectedCurrency < 0 || result.Kills < 0) return false;
            var next = SaveRepository.Clone(Data);
            try
            {
                next.currency = checked(next.currency + result.CollectedCurrency);
                next.totalRuns = checked(next.totalRuns + 1); next.victories = checked(next.victories + (result.Won ? 1 : 0));
                next.totalKills = checked(next.totalKills + result.Kills); next.totalSurvivalSeconds += result.SurvivalSeconds;
                next.lastSettledRunId = result.RunId;
                var ids = new List<string>(next.settledRunIds) { result.RunId }; next.settledRunIds = ids.ToArray();
                return Commit(next);
            }
            catch (OverflowException) { Notice = "save.failed"; return false; }
        }
        public bool SetLanguage(string language)
        {
            if (language != "zh-TW" && language != "en") return false;
            var next = SaveRepository.Clone(Data); next.language = language; return Commit(next);
        }
        private bool Commit(SaveData next)
        {
            if (!repository.TrySave(next)) { Notice = "save.failed"; return false; }
            Data = next; Notice = null; return true;
        }
    }
}
