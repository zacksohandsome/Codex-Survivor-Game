using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Survivor.Core;
using Survivor.Meta;
using Survivor.Save;

namespace Survivor.Tests
{
    public class SaveTransactionTests
    {
        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
            public string FailOperation;
            public bool Exists(string path) => Files.ContainsKey(path);
            public string Read(string path) => Files[path];
            private void Check(string operation) { if (operation == FailOperation) throw new IOException("Synthetic storage failure"); }
            public void WriteDurable(string path, string contents) { Check("write"); Files[path] = contents; }
            public void Copy(string source, string destination) { Check("copy"); Files[destination] = Files[source]; }
            public void Replace(string temporary, string destination) { Check("replace"); Files[destination] = Files[temporary]; Files.Remove(temporary); }
        }
        private MemoryStorage disk;
        private SaveRepository repository;
        [SetUp] public void Setup() { disk = new MemoryStorage(); repository = new SaveRepository("synthetic-save", disk); }
        [Test] public void MissingFileDefaultsAndRoundTripsAllPermanentFields()
        {
            Assert.That(repository.Load().currency, Is.Zero);
            var data = new SaveData { currency = 55, totalRuns = 3, victories = 2, totalKills = 42, totalSurvivalSeconds = 1234, language = "en", metaUpgradeLevels = new[] { 1, 2, 3, 4, 5 } };
            Assert.That(repository.TrySave(data), Is.True);
            var loaded = repository.Load();
            Assert.That(loaded.currency, Is.EqualTo(55)); Assert.That(loaded.totalKills, Is.EqualTo(42));
            Assert.That(loaded.metaUpgradeLevels, Is.EqualTo(data.metaUpgradeLevels)); Assert.That(loaded.language, Is.EqualTo("en"));
        }
        [Test] public void KeepsThreeRecentValidBackupsAndRecoversPastDamagedBackups()
        {
            for (int i = 1; i <= 5; i++) Assert.That(repository.TrySave(new SaveData { currency = i }), Is.True);
            disk.Files[repository.Path] = "damaged"; disk.Files[repository.Path + ".bak1"] = "damaged";
            Assert.That(repository.Load().currency, Is.EqualTo(3)); Assert.That(repository.Notice, Is.EqualTo("save.recovered"));
            Assert.That(disk.Files.Keys, Has.Some.Contains(".corrupt."));
            Assert.That(SaveRepository.TryDecode(disk.Files[repository.Path + ".bak3"], out var third), Is.True);
            Assert.That(third.currency, Is.EqualTo(2));
        }
        [Test] public void MissingPrimaryRecoversBackupInsteadOfResettingProgress()
        {
            repository.TrySave(new SaveData { currency = 12 }); repository.TrySave(new SaveData { currency = 18 });
            disk.Files.Remove(repository.Path); Assert.That(repository.Load().currency, Is.EqualTo(12));
        }
        [Test] public void AllCorruptOrFutureVersionPreservesOriginalAndDisablesWrites()
        {
            disk.Files[repository.Path] = SaveRepository.Encode(new SaveData { version = 99, currency = 91 });
            string original = disk.Files[repository.Path]; repository.Load();
            Assert.That(repository.IsReadOnly, Is.True); Assert.That(repository.TrySave(new SaveData()), Is.False);
            Assert.That(disk.Files[repository.Path], Is.EqualTo(original));
        }
        [TestCase("write")] [TestCase("copy")] [TestCase("replace")]
        public void FailedPurchaseChangesNeitherBalanceNorLevelNorPrimary(string operation)
        {
            repository.TrySave(new SaveData { currency = 100 });
            var meta = new MetaProgression(repository); string original = disk.Files[repository.Path]; disk.FailOperation = operation;
            Assert.That(meta.Purchase(0), Is.False); Assert.That(meta.Data.currency, Is.EqualTo(100));
            Assert.That(meta.Data.metaUpgradeLevels[0], Is.Zero); Assert.That(disk.Files[repository.Path], Is.EqualTo(original));
            disk.FailOperation = null; Assert.That(meta.Purchase(0), Is.True);
            Assert.That(repository.Load().currency, Is.EqualTo(90)); Assert.That(repository.Load().metaUpgradeLevels[0], Is.EqualTo(1));
        }
        [Test] public void SettlementIsIdempotentAcrossReloadAndOlderResultReplay()
        {
            var meta = new MetaProgression(repository);
            var run = new RunContext("bolt") { CollectedCurrency = 25, Kills = 8 };
            var result = new RunResult(run, true, 600);
            Assert.That(meta.Settle(result), Is.True); Assert.That(meta.Settle(result), Is.True);
            var second = new RunResult(new RunContext("bolt") { CollectedCurrency = 5 }, false, 12);
            meta.Settle(second); meta = new MetaProgression(repository); meta.Settle(result);
            Assert.That(meta.Data.currency, Is.EqualTo(30)); Assert.That(meta.Data.totalRuns, Is.EqualTo(2));
            Assert.That(meta.Data.victories, Is.EqualTo(1)); Assert.That(meta.Data.totalKills, Is.EqualTo(8));
        }
        [Test] public void FailedSettlementCanRetryWithoutDoubleCredit()
        {
            var meta = new MetaProgression(repository);
            var result = new RunResult(new RunContext("bolt") { CollectedCurrency = 25 }, false, 20);
            disk.FailOperation = "replace"; Assert.That(meta.Settle(result), Is.False); Assert.That(meta.Data.currency, Is.Zero);
            disk.FailOperation = null; Assert.That(meta.Settle(result), Is.True); Assert.That(meta.Settle(result), Is.True);
            Assert.That(repository.Load().currency, Is.EqualTo(25)); Assert.That(meta.Data.totalRuns, Is.EqualTo(1));
        }
        [Test] public void MaxInsufficientFundsAndFullRefundUseOriginalPrices()
        {
            repository.TrySave(new SaveData { currency = 150 }); var meta = new MetaProgression(repository);
            for (int i = 0; i < 5; i++) Assert.That(meta.Purchase(0), Is.True);
            Assert.That(meta.Purchase(0), Is.False); Assert.That(meta.Purchase(1), Is.False);
            Assert.That(meta.Refund(), Is.True); Assert.That(meta.Data.currency, Is.EqualTo(150));
            Assert.That(meta.Data.metaUpgradeLevels, Is.EqualTo(new int[5]));
            Assert.That(meta.Refund(), Is.True); Assert.That(meta.Data.currency, Is.EqualTo(150));
        }
        [Test] public void TamperAndInvalidNumbersAreRejected()
        {
            string json = SaveRepository.Encode(new SaveData { currency = 100 });
            Assert.That(SaveRepository.TryDecode(json.Replace("100", "200"), out _), Is.False);
            Assert.That(repository.TrySave(new SaveData { currency = -1 }), Is.False);
            Assert.That(repository.TrySave(new SaveData { totalSurvivalSeconds = double.NaN }), Is.False);
            Assert.That(repository.TrySave(new SaveData { metaUpgradeLevels = new[] { 6, 0, 0, 0, 0 } }), Is.False);
        }
        [Test] public void ActualFilesystemRoundTripAndAtomicReplacement()
        {
            string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Survivor-SaveTests-" + Guid.NewGuid().ToString("N"));
            var real = new SaveRepository(folder);
            Assert.That(real.TrySave(new SaveData { currency = 10 }), Is.True);
            Assert.That(real.TrySave(new SaveData { currency = 20 }), Is.True);
            Assert.That(new SaveRepository(folder).Load().currency, Is.EqualTo(20));
            File.WriteAllText(real.Path, "synthetic corruption");
            Assert.That(new SaveRepository(folder).Load().currency, Is.EqualTo(10));
            // Retain this synthetic, uniquely named fixture for inspection; never touch real player saves.
        }
    }
}
