using System.Collections;
using Survivor.Data;
using Survivor.Input;
using Survivor.Save;
using Survivor.UI;
using Survivor.Combat;
using Survivor.Progression;
using Survivor.Audio;
using Survivor.Meta;
using Survivor.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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
        public static string SaveDirectoryOverride { get; set; }
        public MetaProgression Meta { get; private set; }
        public SettingsController Settings { get; private set; }
        public SaveData PermanentData => Meta.Data;
        public bool SettlementPending { get; private set; }
        public FlowConfig Config => config;
        public bool IsTransitioning { get; private set; }
        public Vector2 Movement { get; private set; }
        public RunProgression Progression { get; private set; }
        public string UpgradeNoticeKey { get; private set; }
        public int UpgradeNoticeLevel { get; private set; }
        public float UpgradeNoticeRemaining { get; private set; }
        private GameInputRouter input;
        private FlowView view;
        private bool focused = true;
        private float originalTimeScale;
        private float outcomeRemaining;
        public AudioDirector Audio { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { Instance = null; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; originalTimeScale = Time.timeScale;
            DontDestroyOnLoad(gameObject);
            Session = new GameSession();
            Meta = new MetaProgression(new SaveRepository(SaveDirectoryOverride ?? System.IO.Path.Combine(Application.persistentDataPath, "Saves")),Mathf.Max(1,config.metaBasePrice));
            Settings = new SettingsController(Meta);
            input = new GameInputRouter(inputActions);
            Audio = gameObject.AddComponent<AudioDirector>();
            Audio.Initialize(config.audio,Meta.Data.musicVolume,Meta.Data.sfxVolume);
            view = gameObject.AddComponent<FlowView>();
            view.Initialize(this, textTable);
            Session.Changed += OnStateChanged;
            OnStateChanged();
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            var initialization = LocalizationSettings.InitializationOperation;
            if (initialization.IsDone) InitializeLocale();
            else initialization.Completed += _ => { if (this != null && Instance == this) InitializeLocale(); };
        }
        private void Update()
        {
            if (IsTransitioning) return;
            if (Session.State == GameState.Battle) UpgradeNoticeRemaining = Mathf.Max(0, UpgradeNoticeRemaining-Time.unscaledDeltaTime);
            if (Settings.Tick(Time.unscaledDeltaTime)) view.CloseDisplayConfirmation();
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
            Audio.ChangeState(Session.State);
            if(Session.State==GameState.LevelUp)
            {
                Audio.PlayUi("levelup");
                if(BattleWorld.Instance!=null)BattleWorld.Instance.EmitBurst(BattleWorld.Instance.Player.transform.position,new Color(0.3f,1,0.8f),true);
            }
            input.BlockMovement(); Movement = Vector2.zero;
            Time.timeScale = Session.State == GameState.Battle ? 1 : 0;
            if (Session.State == GameState.Victory || Session.State == GameState.Defeat)
            {
                if (BattleWorld.Instance != null) BattleWorld.Instance.ClearForEnd();
                outcomeRemaining = Session.State == GameState.Defeat ? 0.45f : 0;
                Audio.PlayUi(Session.State == GameState.Victory?"victory":"defeat");
            }
            if (Session.State == GameState.Result)
            {
                SettlementPending = !Meta.Settle(Session.Result);
                if (BattleWorld.Instance != null) BattleWorld.Instance.Player.body.enabled = false;
            }
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
            if (IsTransitioning || SettlementPending) return false;
            StartCoroutine(LoadMenu(selectWeapon)); return true;
        }
        public bool RetrySettlement()
        {
            if (Session.State != GameState.Result) return false;
            SettlementPending = !Meta.Settle(Session.Result); view.Render(); return !SettlementPending;
        }
        private void InitializeLocale()
        {
            string language=Meta.Data.language;
            if (string.IsNullOrEmpty(language)) language=Application.systemLanguage==SystemLanguage.English?"en":"zh-TW";
            var locale=LocalizationSettings.AvailableLocales.GetLocale(language);
            if(locale!=null) LocalizationSettings.SelectedLocale=locale;
            view.Render();
        }
        private void OnLocaleChanged(Locale locale) { if(view!=null) view.Render(); }
        public bool SetLanguage(string language)
        {
            var locale=LocalizationSettings.AvailableLocales.GetLocale(language);
            if(locale==null || !Meta.SetLanguage(language)) return false;
            LocalizationSettings.SelectedLocale=locale; view.Render(); return true;
        }
        public bool SetVolumes(float music,float sfx)
        {
            if(!Meta.SavePreferences(Meta.Data.language,music,sfx,Meta.Data.displayWidth,Meta.Data.displayHeight,Meta.Data.fullscreen)) return false;
            Audio.SetVolumes(music,sfx); return true;
        }
        public bool PreviewDisplay(int width,int height,bool fullscreen)
        {
            if(Session.State!=GameState.MainMenu || !Settings.Begin(new DisplayChoice(width,height,fullscreen))) return false;
            view.ShowDisplayConfirmation(); return true;
        }
        public bool PurchaseMeta(int stat)
        {
            if (IsTransitioning || Session.State != GameState.MainMenu) return false;
            bool saved = Meta.Purchase(stat); view.Render(); return saved;
        }
        public bool RefundMeta()
        {
            if (IsTransitioning || Session.State != GameState.MainMenu) return false;
            bool saved = Meta.Refund(); view.Render(); return saved;
        }
        private IEnumerator LoadMenu(bool selectWeapon)
        {
            IsTransitioning = true; Movement = Vector2.zero;
            Time.timeScale = 0; input.BlockMovement(); view.Render();
            yield return SceneManager.LoadSceneAsync(config.mainMenuScene, LoadSceneMode.Single);
            Progression = null;
            UpgradeNoticeKey = null; UpgradeNoticeLevel = 0; UpgradeNoticeRemaining = 0;
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
            var option = Progression != null && Progression.Choices != null && index >= 0 && index < Progression.Choices.Length ? Progression.Choices[index] : null;
            if (!focused || IsTransitioning || Progression == null || !Progression.Choose(index, revision, out var effect)) return false;
            UpgradeNoticeKey=effect.nameKey; UpgradeNoticeLevel=option.IsSpecial?0:Session.Run.Build.Level(option.AbilityId); UpgradeNoticeRemaining=3;
            var world = BattleWorld.Instance;
            if (effect.effect == UpgradeEffect.CurrencyReward)
                Session.Run.CollectedCurrency = checked(Session.Run.CollectedCurrency + (long)effect.amount);
            else if (effect.effect == UpgradeEffect.HealReward) world.Player.Health.Heal((int)effect.amount);
            world.ApplyBuild(); view.Render(); return true;
        }
        private void Start() { Settings.ApplyInitial(); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            Settings?.Cancel();
            Session.Changed -= OnStateChanged;
            input.Dispose();
            Time.timeScale = originalTimeScale; Instance = null;
        }
    }
}
