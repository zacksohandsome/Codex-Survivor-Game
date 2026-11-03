using System.Collections;
using NUnit.Framework;
using Survivor.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Survivor.Tests
{
    public class FlowIntegrationTests
    {
        private bool background;
        private Keyboard keyboard;
        private static UnityEngine.UI.Button Button(string key) => GameObject.Find("Button_" + key).GetComponent<UnityEngine.UI.Button>();
        private static IEnumerator WaitForLoad(GameFlow flow)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow.IsTransitioning, Is.False, "Scene transition timed out");
            yield return null;
        }
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            background = Application.runInBackground; Application.runInBackground = true;
            if (GameFlow.Instance != null) { Object.Destroy(GameFlow.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            Assert.That(GameFlow.Instance, Is.Not.Null);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (GameFlow.Instance != null) Object.Destroy(GameFlow.Instance.gameObject);
            yield return null;
            Time.timeScale = 1; Application.runInBackground = background;
        }
        [UnityTest]
        public IEnumerator ButtonsAndRepeatedSceneRoundTripsDoNotLeakOrCarryRunData()
        {
            var flow = GameFlow.Instance;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                Assert.That(flow.Session.State, Is.EqualTo(GameState.MainMenu));
                Button("start").onClick.Invoke(); yield return null;
                Assert.That(Button("enter").interactable, Is.False);
                Assert.That(flow.Session.SelectedWeaponId, Is.Null);
                Button("weapon.bolt").onClick.Invoke(); yield return null;
                Assert.That(Button("enter").interactable, Is.True);
                Button("enter").onClick.Invoke();
                Assert.That(flow.StartRun(), Is.False, "Duplicate start must be locked during load");
                yield return WaitForLoad(flow);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Battle"));
                Assert.That(flow.Session.Run.Level, Is.EqualTo(1));
                Assert.That(flow.Session.Run.AbilityLevels.Count, Is.EqualTo(1));
                var marker = new GameObject("RunScopedMarker");
                flow.Session.Run.Level = 8; flow.Session.Run.AbilityLevels["orbit"] = 4;
                Assert.That(flow.ReturnToMenu(), Is.True);
                Assert.That(flow.ReturnToMenu(), Is.False);
                yield return WaitForLoad(flow);
                Assert.That(marker == null, Is.True, "Battle-owned object must be unloaded");
                Assert.That(SceneManager.GetSceneByName("Battle").isLoaded, Is.False);
                Assert.That(flow.Session.Run, Is.Null);
                Assert.That(Object.FindObjectsByType<GameFlow>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator KeyboardPauseReleaseGateAndFocusLossFollowStateRules()
        {
            var flow = GameFlow.Instance;
            flow.OpenWeaponSelection(); flow.SelectWeapon("bolt"); flow.StartRun();
            yield return WaitForLoad(flow);
            flow.Session.Tick(3); yield return null;
            Assert.That(flow.Session.State, Is.EqualTo(GameState.Battle));
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            Assert.That(flow.Movement.y, Is.EqualTo(1));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Escape)); yield return null;
            Assert.That(flow.Session.State, Is.EqualTo(GameState.Pause));
            Assert.That(flow.Movement, Is.EqualTo(Vector2.zero));
            double frozen = flow.Session.BattleSeconds;
            yield return null; yield return null;
            Assert.That(flow.Session.State, Is.EqualTo(GameState.Pause), "Holding ESC must not toggle again");
            Assert.That(flow.Session.BattleSeconds, Is.EqualTo(frozen));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Escape)); yield return null;
            Assert.That(flow.Session.State, Is.EqualTo(GameState.Battle));
            Assert.That(flow.Movement, Is.EqualTo(Vector2.zero), "Held movement must not resume");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null;
            Assert.That(flow.Movement.y, Is.EqualTo(1));
            flow.gameObject.SendMessage("OnApplicationFocus", false); yield return null;
            Assert.That(flow.Session.State, Is.EqualTo(GameState.Pause));
            flow.gameObject.SendMessage("OnApplicationFocus", true); yield return null;
            Assert.That(flow.Session.State, Is.EqualTo(GameState.Pause));
            flow.Resume(); Assert.That(flow.Session.State, Is.EqualTo(GameState.Battle));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
