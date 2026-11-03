using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Survivor.Combat;
using Survivor.Core;
using Survivor.Data;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Survivor.Editor
{
    // Explicitly invoked Editor-only test driver. Sends ordinary WASD/1/2/3 input;
    // never alters health, drops, enemies, clock or gameplay balance.
    public sealed class M5EditorPlaythrough : ScriptableObject
    {
        [Serializable] public sealed class Sample
        {
            public double realSeconds, battleSeconds;
            public string state;
            public int frame, health, kills, level, enemies, drops;
            public long currency;
        }
        [Serializable] private sealed class Evidence
        {
            public string status;
            public bool normalSpeed = true, won, deathVerified, reloadVerified, purchased;
            public double battleSeconds, realSeconds;
            public int kills, level;
            public long currency;
            public List<Sample> samples = new List<Sample>();
        }
        private readonly Evidence evidence = new Evidence { status = "starting" };
        private Keyboard keyboard;
        private RecorderController recorder;
        private RecorderControllerSettings controllerSettings;
        private MovieRecorderSettings movie;
        private double started, nextSample;
        private int waypoint;
        private float choiceRemaining;
        private bool releaseFrame, finished;
        private int phase;
        private static M5EditorPlaythrough active;
        private IEnumerator routine;
        private int previousFrame = -1;
        private GameState previousState = GameState.MainMenu;
        private readonly Vector2[] route = { new Vector2(15, 8), new Vector2(-15, 8), new Vector2(-15, -8), new Vector2(15, -8) };
        public string Status => evidence.status;
        public static string ActiveStatus => active == null ? "idle" : active.Status;
        private const string EvidencePath = "Documentation/Reports/M5/playthrough.json";

        public static void Begin()
        {
            if (!EditorApplication.isPlaying || active != null) throw new InvalidOperationException("Start exactly one driver in Play Mode.");
            active = CreateInstance<M5EditorPlaythrough>(); active.routine = active.Start();
            EditorApplication.update += active.Update;
        }
        private IEnumerator Start()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            Directory.CreateDirectory("Documentation/Reports/M5"); Directory.CreateDirectory("Recordings/M5");
            controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            movie = ScriptableObject.CreateInstance<MovieRecorderSettings>(); movie.name = "M5 Normal Speed Editor"; movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings { EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Low, Codec = CoreEncoderSettings.OutputCodec.MP4 };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1280, OutputHeight = 720 };
            movie.CaptureAudio = false; movie.CaptureAlpha = false;
            movie.OutputFile = Path.GetFullPath("Recordings/M5/mvp-editor");
            controllerSettings.AddRecorderSettings(movie); controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 30; controllerSettings.FrameRatePlayback = FrameRatePlayback.Variable; controllerSettings.CapFrameRate = false;
            recorder = new RecorderController(controllerSettings); recorder.PrepareRecording();
            if (!recorder.StartRecording()) { evidence.status = "recording_failed"; WriteEvidence(); yield break; }
            started = Time.realtimeSinceStartupAsDouble;
            yield return new WaitForSecondsRealtime(1);
            Click("start"); yield return null; Click("weapon.bolt"); yield return null; Click("enter");
            evidence.status = "normal_speed_victory_attempt";
        }
        private static void Click(string key)
        {
            var button = GameObject.Find("Button_" + key)?.GetComponent<UnityEngine.UI.Button>();
            if (button == null || !button.interactable) throw new InvalidOperationException("Playthrough button unavailable: " + key);
            button.onClick.Invoke();
        }
        private void Update()
        {
            if (!EditorApplication.isPlaying) { DestroyImmediate(this); return; }
            if (Time.frameCount == previousFrame) return;
            previousFrame = Time.frameCount;
            if (routine != null)
            {
                if (routine.Current is AsyncOperation operation && !operation.isDone) return;
                if (routine.Current is WaitForSecondsRealtime wait && wait.keepWaiting) return;
                if (!routine.MoveNext()) routine = null;
                return;
            }
            if (keyboard == null || finished || started == 0) return;
            var flow = GameFlow.Instance; var world = BattleWorld.Instance;
            if (flow == null || flow.IsTransitioning) { Queue(); return; }
            if (flow.Session.State == GameState.Battle && previousState != GameState.Battle) releaseFrame = true;
            previousState = flow.Session.State;
            double real = Time.realtimeSinceStartupAsDouble - started;
            if (real >= nextSample)
            {
                var run = flow.Session.Run;
                evidence.samples.Add(new Sample { realSeconds = real, battleSeconds = flow.Session.BattleSeconds,
                    state = flow.Session.State.ToString(), frame = Time.frameCount, health = run?.CurrentHealth ?? 0,
                    kills = run?.Kills ?? 0, level = run?.Level ?? 0, currency = run?.CollectedCurrency ?? 0,
                    enemies = world?.Enemies.ActiveCount ?? 0, drops = world?.Pickups.Pool.ActiveCount ?? 0 });
                nextSample = real + 10; WriteEvidence();
            }
            if (real > 1100) { Finish("timeout"); return; }
            if (phase == 0 && flow.Session.State == GameState.Result)
            {
                var result = flow.Session.Result; evidence.won = result.Won;
                evidence.battleSeconds = result.SurvivalSeconds; evidence.kills = result.Kills;
                evidence.level = result.Level; evidence.currency = result.CollectedCurrency; evidence.realSeconds = real;
                ScreenCapture.CaptureScreenshot("Documentation/Reports/M5/victory.png");
                if (!result.Won) { Finish("victory_attempt_died"); return; }
                phase = 1; routine = PurchaseReloadAndDie(); Queue(); return;
            }
            if (phase == 2 && flow.Session.State == GameState.Result)
            {
                evidence.deathVerified = !flow.Session.Result.Won;
                ScreenCapture.CaptureScreenshot("Documentation/Reports/M5/defeat.png");
                Finish(evidence.deathVerified && evidence.reloadVerified && evidence.purchased ? "passed" : "verification_failed"); return;
            }
            choiceRemaining -= Time.unscaledDeltaTime;
            if (releaseFrame) { releaseFrame = false; Queue(); return; }
            if (flow.Session.State == GameState.LevelUp && choiceRemaining <= 0)
            {
                int selected = 0; float best = float.MinValue;
                for (int i = 0; i < flow.Progression.Choices.Length; i++)
                {
                    var option = flow.Progression.Choices[i];
                    float score = option.Effect.effect == UpgradeEffect.WeaponDamage ? 9 : option.Effect.effect == UpgradeEffect.Pierce ? 8 :
                        option.Effect.effect == UpgradeEffect.RegenAmount ? 7 : option.Effect.effect == UpgradeEffect.Attack ? 6 :
                        option.Effect.effect == UpgradeEffect.AttackSpeed ? 5 : option.Effect.effect == UpgradeEffect.Health ? 4 : 1;
                    if (score > best) { best = score; selected = i; }
                }
                Queue((Key)((int)Key.Digit1 + selected)); choiceRemaining = 0.5f; releaseFrame = true; return;
            }
            if ((phase != 0 && phase != 2) || flow.Session.State != GameState.Battle || world == null) { Queue(); return; }
            Vector2 player = world.Player.transform.position;
            if ((route[waypoint] - player).sqrMagnitude < 3) waypoint = (waypoint + 1) % route.Length;
            Vector2 desired = route[waypoint] - player;
            if (phase == 2)
            {
                var target = world.Weapon.FindNearest(player, world.Enemies.Items, 100);
                desired = target == null ? Vector2.zero : (Vector2)target.transform.position - player;
            }
            float nearestDrop = 64;
            foreach (var drop in world.Pickups.Pool.Items)
            {
                if (phase != 0) break;
                if (!drop.IsRented) continue;
                Vector2 offset = (Vector2)drop.transform.position - player; float distance = offset.sqrMagnitude;
                if (distance >= nearestDrop || distance < 1) continue;
                bool safe = true;
                foreach (var enemy in world.Enemies.Items)
                    if (enemy.IsAlive && ((Vector2)enemy.transform.position - (Vector2)drop.transform.position).sqrMagnitude < 6.25f) { safe = false; break; }
                if (safe) { nearestDrop = distance; desired = offset; }
            }
            Vector2 direction = desired.normalized;
            foreach (var enemy in world.Enemies.Items)
            {
                if (phase != 0) break;
                if (!enemy.IsAlive) continue;
                Vector2 away = player - (Vector2)enemy.transform.position; float distance = away.magnitude;
                if (distance < 3.5f && distance > 0.01f) direction += away.normalized * ((3.5f - distance) * 1.8f);
            }
            var keys = new List<Key>(2);
            if (direction.x > 0.2f) keys.Add(Key.D); else if (direction.x < -0.2f) keys.Add(Key.A);
            if (direction.y > 0.2f) keys.Add(Key.W); else if (direction.y < -0.2f) keys.Add(Key.S);
            Queue(keys.ToArray());
        }
        private IEnumerator PurchaseReloadAndDie()
        {
            var flow = GameFlow.Instance; Click("menu");
            while (flow.IsTransitioning) yield return null;
            yield return null; Click("meta"); yield return null; Click("meta.health"); yield return null;
            evidence.purchased = flow.PermanentData.metaUpgradeLevels[0] == 1;
            long currency = flow.PermanentData.currency, runs = flow.PermanentData.totalRuns;
            ScreenCapture.CaptureScreenshot("Documentation/Reports/M5/meta-purchased.png");
            yield return new WaitForSecondsRealtime(2);
            Destroy(flow.gameObject); yield return null;
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            flow = GameFlow.Instance;
            evidence.reloadVerified = flow.PermanentData.currency == currency && flow.PermanentData.totalRuns == runs && flow.PermanentData.metaUpgradeLevels[0] == 1;
            Click("start"); yield return null; Click("weapon.bolt"); yield return null; Click("enter");
            phase = 2; evidence.status = "normal_speed_death_attempt";
        }
        private void Queue(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private void WriteEvidence() => File.WriteAllText(EvidencePath, JsonUtility.ToJson(evidence, true));
        private void Finish(string status)
        { finished = true; evidence.status = status; Queue(); recorder?.StopRecording(); WriteEvidence(); }
        private void OnDestroy()
        {
            EditorApplication.update -= Update; if (active == this) active = null;
            recorder?.StopRecording();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (movie != null) Destroy(movie); if (controllerSettings != null) Destroy(controllerSettings);
        }
    }
}
