using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Survivor.Combat;
using Survivor.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Survivor.Tests
{
    public class MetaIntegrationTests
    {
        private bool background;
        private GameFlow Flow => GameFlow.Instance;
        private static UnityEngine.UI.Button Button(string key) => GameObject.Find("Button_" + key).GetComponent<UnityEngine.UI.Button>();
        [UnitySetUp] public IEnumerator Setup()
        {
            background = Application.runInBackground; Application.runInBackground = true;
            GameFlow.SaveDirectoryOverride = Path.Combine(Path.GetTempPath(), "Survivor-MetaTests-" + Guid.NewGuid().ToString("N"));
            if (Flow != null) { Object.Destroy(Flow.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("MainMenu");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (Flow != null) Object.Destroy(Flow.gameObject);
            if (BattleWorld.Instance != null) Object.Destroy(BattleWorld.Instance.gameObject);
            yield return null; Time.timeScale = 1; Application.runInBackground = background;
        }
        private IEnumerator StartRun()
        {
            Flow.OpenWeaponSelection(); Flow.SelectWeapon("bolt"); Flow.StartRun();
            yield return WaitForLoad(); BattleWorld.Instance.enabled = false;
            Flow.Session.Tick(3); BattleWorld.Instance.ApplyBuild();
        }
        private IEnumerator WaitForLoad()
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (Flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Flow.IsTransitioning, Is.False); yield return null;
        }
        [UnityTest] public IEnumerator SettlementPurchaseRestartAndSecondBattleKeepOnlyPermanentProgress()
        {
            yield return StartRun(); Flow.Session.Run.CollectedCurrency = 150;
            Flow.Session.Run.Kills = 9; Flow.Session.Tick(600); yield return null;
            Assert.That(Flow.Session.State, Is.EqualTo(GameState.Result));
            Assert.That(Flow.PermanentData.currency, Is.EqualTo(150)); Assert.That(Flow.RetrySettlement(), Is.True);
            Flow.ReturnToMenu(); yield return WaitForLoad();
            Button("meta").onClick.Invoke(); yield return null;
            Button("meta.health").onClick.Invoke(); yield return null;
            Assert.That(Flow.PermanentData.currency, Is.EqualTo(75));
            Object.Destroy(Flow.gameObject); yield return null;
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Assert.That(Flow.PermanentData.currency, Is.EqualTo(75)); Assert.That(Flow.PermanentData.totalKills, Is.EqualTo(9));
            Assert.That(Flow.PermanentData.metaUpgradeLevels[0], Is.EqualTo(1));
            yield return StartRun();
            Assert.That(BattleWorld.Instance.Player.Health.Maximum, Is.EqualTo(110));
            Assert.That(Flow.Session.Run.CollectedCurrency, Is.Zero); Assert.That(Flow.Session.Run.Kills, Is.Zero);
            Assert.That(Flow.Session.Run.Build.Level("bloom"), Is.Zero);
            Assert.That(Flow.PurchaseMeta(0), Is.False, "Meta purchases are only available from the main menu");
        }
        [UnityTest] public IEnumerator AbandonDoesNotSettleAndRefundRequiresConfirmation()
        {
            yield return StartRun(); Flow.Session.Run.CollectedCurrency = 100; Flow.Session.Run.Kills = 20;
            Flow.ReturnToMenu(); yield return WaitForLoad();
            Assert.That(Flow.PermanentData.currency, Is.Zero); Assert.That(Flow.PermanentData.totalRuns, Is.Zero);
            Button("meta").onClick.Invoke(); yield return null;
            Button("meta.refund").onClick.Invoke(); yield return null;
            Assert.That(GameObject.Find("Button_confirm"), Is.Not.Null);
            Button("cancel").onClick.Invoke(); yield return null;
            Assert.That(GameObject.Find("Button_meta.health"), Is.Not.Null);
        }
    }
}
