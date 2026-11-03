using System.Collections;
using Survivor.Data;
using Survivor.Input;
using Survivor.Save;
using Survivor.UI;
using Survivor.Combat;
using Survivor.Progression;
using Survivor.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Survivor.Core
{
    public sealed class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }
        [SerializeField] private FlowConfig config;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private TextAsset textTable;
        public GameSession Session { get; private set; }
        public SaveData PermanentData { get; } = new SaveData();
        public FlowConfig Config => config;
        public bool IsTransitioning { get; private set; }
        public Vector2 Movement { get; private set; }
        public RunProgression Progression { get; private set; }
        private GameInputRouter input;
        private FlowView view;
        private bool focused = true;
        private float originalTimeScale;
        private float outcomeRemaining;
        private OutcomeAudio outcomeAudio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { Instance = null; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; originalTimeScale = Time.timeScale;
            DontDestroyOnLoad(gameObject);
            Session = new GameSession();
            input = new GameInputRouter(inputActions);
            outcomeAudio = new OutcomeAudio(gameObject);
            view = gameObject.AddComponent<FlowView>();
            view.Initialize(this, textTable);
            Session.Changed += OnStateChanged;
            OnStateChanged();
        }
        private void Update()
        {
            if (IsTransitioning) return;
            if (Session.State == GameState.Victory || Session.State == GameState.Defeat)
            {
                outcomeRemaining -= Time.unscaledDeltaTime;
                if (Session.State == GameState.Defeat && BattleWorld.Instance != null) BattleWorld.Instance.PresentDefeat(outcomeRemaining);
                if (outcomeRemaining <= 0) Session.ShowResult();
                return;
            }
            if (input.PausePressed)
            {
                if (view.HasModal) view.Back();
                else if (Session.State == GameState.Pause) Resume();
                else if (Session.State == GameState.WeaponSelection) Session.ReturnToMenu();
                else if (Session.State == GameState.MainMenu) view.Back();
                else Session.Pause();
            }
            if (Session.State == GameState.LevelUp && input.UpgradePressed >= 0 && Progression != null)
                ChooseUpgrade(input.UpgradePressed, Progression.ChoiceRevision);
            Movement = input.ReadMovement(focused && Session.State == GameState.Battle && !view.HasModal);
            if (focused && !view.HasModal) Session.Tick(Time.unscaledDeltaTime);
            if (Session.State == GameState.Victory) Session.ShowResult();
            view.RefreshClock();
        }
        private void OnStateChanged()
        {
            input.BlockMovement(); Movement = Vector2.zero;
            Time.timeScale = Session.State == GameState.Battle ? 1 : 0;
            if (Session.State == GameState.Victory || Session.State == GameState.Defeat)
            {
                if (BattleWorld.Instance != null) BattleWorld.Instance.ClearForEnd();
                outcomeRemaining = Session.State == GameState.Defeat ? 0.45f : 0;
                outcomeAudio.Play(Session.State == GameState.Victory);
            }
            if (Session.State == GameState.Result && BattleWorld.Instance != null) BattleWorld.Instance.Player.body.enabled = false;
            view.Render();
        }
        public bool OpenWeaponSelection() => !IsTransitioning && Session.OpenWeaponSelection();
        public bool SelectWeapon(string id)
        {
            if (IsTransitioning || System.Array.Find(config.weapons, w => w.id == id) == null) return false;
            return Session.SelectWeapon(id);
        }
        public bool StartRun()
        {
            if (IsTransitioning || Session.State != GameState.WeaponSelection || Session.SelectedWeaponId == null) return false;
            if (!System.Array.Exists(config.weapons, w => w.id == Session.SelectedWeaponId && w.combatAvailable)) return false;
            StartCoroutine(LoadBattle()); return true;
        }
        private IEnumerator LoadBattle()
        {
            IsTransitioning = true; Movement = Vector2.zero; view.Render();
            yield return SceneManager.LoadSceneAsync(config.battleScene, LoadSceneMode.Single);
            Session.StartRun(config.introSeconds);
            Progression = new RunProgression(Session, BattleWorld.Instance.config.progression, Session.Run.Id.GetHashCode());
            if (!focused) Session.Pause();
            IsTransitioning = false; view.Render();
        }
        public void Resume()
        {
            if (focused && !IsTransitioning && !view.HasModal) Session.Resume();
        }
        public bool ReturnToMenu(bool selectWeapon = false)
        {
            if (IsTransitioning) return false;
            StartCoroutine(LoadMenu(selectWeapon)); return true;
        }
        private IEnumerator LoadMenu(bool selectWeapon)
        {
            IsTransitioning = true; Movement = Vector2.zero;
            Time.timeScale = 0; input.BlockMovement(); view.Render();
            yield return SceneManager.LoadSceneAsync(config.mainMenuScene, LoadSceneMode.Single);
            Progression = null;
            Session.ReturnToMenu();
            if (selectWeapon) Session.OpenWeaponSelection();
            IsTransitioning = false; view.Render();
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value && Session != null) { input.BlockMovement(); Movement = Vector2.zero; Session.Pause(); }
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused) OnApplicationFocus(false);
            else if (Application.isFocused) focused = true;
        }
        public bool ChooseUpgrade(int index, int revision)
        {
            if (!focused || IsTransitioning || Progression == null || !Progression.Choose(index, revision, out var effect)) return false;
            var world = BattleWorld.Instance;
            if (effect.effect == UpgradeEffect.CurrencyReward)
                Session.Run.CollectedCurrency = checked(Session.Run.CollectedCurrency + (long)effect.amount);
            else if (effect.effect == UpgradeEffect.HealReward) world.Player.Health.Heal((int)effect.amount);
            world.ApplyBuild(); view.Render(); return true;
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Session.Changed -= OnStateChanged;
            input.Dispose();
            outcomeAudio.Dispose();
            Time.timeScale = originalTimeScale; Instance = null;
        }
    }
}
