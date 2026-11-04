using System;
using Survivor.Core;
using Survivor.Localization;
using Survivor.Progression;
using Survivor.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Survivor.UI
{
    public sealed partial class FlowView : MonoBehaviour
    {
        private static readonly Unity.Profiling.ProfilerMarker RenderMarker=new Unity.Profiling.ProfilerMarker("Survivor.UI.Render");
        private GameFlow flow;
        private FlowText text;
        private RectTransform content;
        private TextMeshProUGUI clock;
        private TextMeshProUGUI status;
        private UnityEngine.UI.Image backgroundImage;
        private UnityEngine.UI.Image healthFill, experienceFill;
        private TextMeshProUGUI progressText, buildText;
        private TextMeshProUGUI bossText, encounterText;
        private TextMeshProUGUI lowHealthText;
        private UnityEngine.UI.Image bossFill;
        private bool clockCached,vitalsCached,progressCached,bossCached,noticeCached;
        private (GameState State,int Seconds) lastClock;
        private (int Health,int Maximum,int Kills,float Attack,float Speed,float Move,float Defense) lastVitals;
        private (int Level,long Experience,long Required,long Currency,int Revision) lastProgress;
        private (int Instance,int Health,int Maximum) lastBoss;
        private (string Key,int Level) lastNotice;
        private string page;
        private string confirmationKey;
        private Action confirmation;
        private Action cancellation;
        private TextMeshProUGUI confirmationLabel;
        public bool HasModal => confirmation != null;
        private readonly Color ink = new Color(0.88f, 0.92f, 0.96f);

        public void Initialize(GameFlow owner, TextAsset table)
        {
            flow = owner; text = new FlowText(table, owner.Config.traditionalText, owner.Config.englishStrings, owner.Config.traditionalStrings);
            var canvasObject = new GameObject("FlowCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var background = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            background.transform.SetParent(canvasObject.transform, false);
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var image = background.GetComponent<UnityEngine.UI.Image>();
            backgroundImage = image;
            image.color = new Color(0.035f, 0.055f, 0.09f); image.raycastTarget = false;
            content = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup)).GetComponent<RectTransform>();
            content.SetParent(canvasObject.transform, false);
            Stretch(content, new Vector2(0.18f, 0.06f), new Vector2(0.82f, 0.94f));
            var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 12; layout.childControlHeight = true; layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.MiddleCenter;
            var events = new GameObject("FlowEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            var module = events.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            // Mouse UI only. Keyboard ESC is routed through GameFlow; no controller navigation.
            module.move = null; module.submit = null; module.cancel = null;
            InitializeTooltip(canvasObject.transform);
        }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        private TextMeshProUGUI Label(string value, float size = 28, float height = 60)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
            go.transform.SetParent(content, false);
            go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
            var label = go.GetComponent<TextMeshProUGUI>();
            if (flow.Config.uiFont != null) label.font = flow.Config.uiFont;
            label.text = value; label.fontSize = size; label.color = ink;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private UnityEngine.UI.Image Bar(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
            go.transform.SetParent(content, false); go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 8;
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(0.12f, 0.16f, 0.20f);
            go.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image)); fill.transform.SetParent(go.transform, false);
            Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var image = fill.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        private static void SetBar(UnityEngine.UI.Image fill, float ratio)
        {
            var rect = fill.rectTransform; rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1); rect.offsetMax = Vector2.zero;
        }
        private void Button(string key, Action action, bool enabled = true, bool selected = false, float height = 62,bool upgradeChoice=false)
        {
            var go = new GameObject("Button_" + key, typeof(RectTransform), typeof(UnityEngine.UI.Image), upgradeChoice?typeof(UpgradeChoiceButton):typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
            go.transform.SetParent(content, false);
            go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = selected ? new Color(0.12f, 0.45f, 0.40f) : new Color(0.10f, 0.16f, 0.24f);
            var button = go.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.interactable = enabled;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            button.onClick.AddListener(() => { flow.Audio.PlayUi("ui"); action(); });
            var caption = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
            caption.transform.SetParent(go.transform, false); Stretch(caption.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var label = caption.GetComponent<TextMeshProUGUI>(); label.text = text.Get(key); label.fontSize = 28;
            if (flow.Config.uiFont != null) label.font = flow.Config.uiFont;
            label.alignment = TextAlignmentOptions.Center; label.color = enabled ? ink : Color.gray; label.raycastTarget = false;
        }
        public void Back()
        {
            if (HasModal) { confirmation = null; var cancel = cancellation; cancellation = null; cancel?.Invoke(); Render(); }
            else if (page != null) { page = null; Render(); }
        }
        private void Ask(string key, Action action, Action onCancel = null) { confirmationKey = key; confirmation = action; cancellation = onCancel; Render(); }
        public void Render()
        {
            using var renderScope=RenderMarker.Auto();
            clockCached=vitalsCached=progressCached=bossCached=noticeCached=false;
            HideTooltip();
            bool battleHud = !flow.IsTransitioning && !HasModal &&
                (flow.Session.State == GameState.Battle || flow.Session.State == GameState.IntroCountdown);
            bool upgrade=!flow.IsTransitioning && !HasModal && flow.Session.State==GameState.LevelUp;
            PrepareBattleHud(battleHud);PrepareUpgradePanel(upgrade);content.gameObject.SetActive(!battleHud && !upgrade);
            // These are distinct screen contents, not edits to the persistent Canvas or EventSystem.
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject; child.SetActive(false); Destroy(child);
            }
            clock = null; status = null; healthFill = null; experienceFill = null; progressText = null; buildText = null;
            bossText = encounterText = lowHealthText = null; bossFill = null;
            content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().enabled = !battleHud;
            backgroundImage.color = battleHud ? Color.clear : new Color(0.035f, 0.055f, 0.09f, flow.Session.State==GameState.MainMenu?0.25f:0.97f);
            Stretch(content, battleHud ? new Vector2(0.02f, 0.74f) : new Vector2(0.18f, 0.06f),
                battleHud ? new Vector2(0.98f, 0.99f) : new Vector2(0.82f, 0.94f));
            content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = battleHud ? 3 : 12;
            if (!battleHud && !upgrade) Label(text.Get("stage"), 20, 32);
            if (flow.IsTransitioning) { Label(text.Get("loading"), 40); return; }
            if (HasModal)
            {
                confirmationLabel = Label(text.Get(confirmationKey), 30, 130);
                Button("confirm", () => { var action = confirmation; confirmation = null; cancellation = null; action(); });
                Button("cancel", () => Back()); RefreshClock(); return;
            }
            var session = flow.Session;
            switch (session.State)
            {
                case GameState.MainMenu:
                    if (page == "meta")
                    {
                        Label(text.Get("meta"), 48, 80);
                        Label(text.Format("currency", flow.PermanentData.currency), 26, 45);
                        Label(PlayerPreview(), 20, 48);
                        for (int i = 0; i < 5; i++)
                        {
                            int stat = i, level = flow.PermanentData.metaUpgradeLevels[i];
                            Button("meta." + MetaProgression.StatIds[i], () => flow.PurchaseMeta(stat), flow.Meta.CanPurchase(stat), height: 70);
                            var caption = content.GetChild(content.childCount - 1).GetComponentInChildren<TextMeshProUGUI>();
                            caption.text = text.Format("meta.row", text.Get("meta." + MetaProgression.StatIds[i]), level,
                                level * MetaProgression.Bonuses[i], Mathf.Min(5, level+1) * MetaProgression.Bonuses[i],
                                level == 5 ? text.Get("meta.max") : text.Format("meta.cost", flow.Meta.Price(level)));
                            caption.fontSize = 24;
                        }
                        if (flow.Meta.Notice != null) Label(text.Get(flow.Meta.Notice), 22, 65);
                        Button("meta.refund", () => Ask("meta.refund.confirm", () => flow.RefundMeta()));
                        Button("back", () => { page = null; Render(); }); break;
                    }
                    if (page == "stats")
                    {
                        var saved = flow.PermanentData;
                        Label(text.Get("stats"), 48, 80);
                        Label(text.Format("stats.summary", saved.totalRuns, saved.victories, saved.totalKills,
                            (long)saved.totalSurvivalSeconds / 60, (long)saved.totalSurvivalSeconds % 60, saved.totalRuns-saved.victories), 30, 220);
                        Button("back", () => { page = null; Render(); }); break;
                    }
                    if (page == "settings") { RenderSettings(); break; }
                    if (page != null)
                    {
                        Label(text.Get(page), 48, 100); Label(text.Get(page + ".note"), 28, 150);
                        Button("back", () => { page = null; Render(); }); break;
                    }
                    Label(text.Get("title"), 64, 110); Label(text.Get("menu.note"), 28, 100);
                    Label(text.Format("currency", flow.PermanentData.currency), 22, 40);
                    if (flow.Meta.Notice != null) Label(text.Get(flow.Meta.Notice), 22, 70);
                    Button("start", () => { page = null; flow.OpenWeaponSelection(); });
                    foreach (string key in new[] { "meta", "settings", "stats" })
                    { string target = key; Button(key, () => { page = target; Render(); }); }
                    Button("quit", () => Ask("quit.confirm", () => { Application.Quit(); Render(); })); break;
                case GameState.WeaponSelection:
                    Label(text.Get("select"), 42, 65); Label(PlayerPreview(), 20, 50);
                    foreach (var weapon in flow.Config.weapons)
                    { var choice = weapon; Button(weapon.nameKey, () => flow.SelectWeapon(choice.id), true, session.SelectedWeaponId == choice.id); IconOnLastButton(choice.id); }
                    var selected = Array.Find(flow.Config.weapons, w => w.id == session.SelectedWeaponId);
                    Label(selected == null ? text.Get("selection.empty") : text.Format("weapon.details", text.Get(selected.nameKey),
                        selected.damageMultiplier, selected.cooldown, selected.range, text.Get(selected.descriptionKey)), 22, 140);
                    Button("enter", () => flow.StartRun(), selected != null && selected.combatAvailable);
                    Button("back", () => session.ReturnToMenu()); break;
                case GameState.IntroCountdown:
                case GameState.Battle:
                    ShowBattleHud(); RefreshClock(); break;
                case GameState.Pause:
                    Label(text.Get("pause"), 52, 100); clock = Label("", 32, 70); RefreshClock();
                    Label(text.Format("pause.summary", session.Run.Level, session.Run.Kills), 26, 60);
                    Button("resume", () => flow.Resume());
                    Button("restart", () => Ask("abandon", () => flow.ReturnToMenu(true)));
                    Button("menu", () => Ask("abandon", () => flow.ReturnToMenu())); break;
                case GameState.LevelUp:
                    ShowUpgradePanel();break;
                case GameState.Victory: Label(text.Get("victory"), 52, 120); break;
                case GameState.Defeat:
                    Label(text.Get("defeat"), 52, 100); Label(text.Get("defeat.note"), 28, 100);
                    Button("restart", () => flow.ReturnToMenu(true));
                    Button("menu", () => flow.ReturnToMenu()); break;
                case GameState.Result:
                    var result = session.Result;
                    Label(text.Get(result.Won ? "victory" : "defeat"), 52, 90).color = result.Won ? new Color(0.4f, 1, 0.6f) : new Color(1, 0.4f, 0.3f);
                    Label(text.Format("result.summary", (int)result.SurvivalSeconds / 60, (int)result.SurvivalSeconds % 60,
                        result.Kills, result.Level, result.CollectedCurrency), 28, 150);
                    Label(DescribeBuild(result.AbilityLevels), 20, 360);
                    Label(text.Get(flow.SettlementPending ? "save.failed" : "result.note"), 24, 70);
                    if (flow.SettlementPending) Button("save.retry", () => flow.RetrySettlement());
                    Button("restart", () => flow.ReturnToMenu(true), !flow.SettlementPending);
                    Button("menu", () => flow.ReturnToMenu(), !flow.SettlementPending); break;
            }
        }
        public void RefreshClock()
        {
            if (HasModal && confirmationKey == "display.confirm" && confirmationLabel != null)
                confirmationLabel.text = text.Format("display.confirm", Mathf.CeilToInt((float)flow.Settings.Display.Remaining));
            if (clock == null) return;
            var session = flow.Session;
            var encounterWorld = Survivor.Combat.BattleWorld.Instance;
            if(encounterText!=null)
            {
                bool encounter=encounterWorld!=null && encounterWorld.NoticeRemaining>0;
                var notice=(encounter?encounterWorld.NoticeKey:flow.UpgradeNoticeRemaining>0?flow.UpgradeNoticeKey:null,encounter?0:flow.UpgradeNoticeLevel);
                if(!noticeCached || notice!=lastNotice)
                {
                    lastNotice=notice;noticeCached=true;
                    encounterText.text=notice.Item1==null?"":notice.Item2>0?text.Format("upgrade.notice",text.Get(notice.Item1),notice.Item2):text.Get(notice.Item1);
                }
            }
            if (bossText != null)
            {
                var boss = encounterWorld == null ? null : encounterWorld.Boss;
                bool visible = boss != null;
                var stamp=(visible?boss.GetInstanceID():0,visible?boss.Health.Current:0,visible?boss.Health.Maximum:0);
                if (!bossCached || stamp!=lastBoss)
                {
                    lastBoss=stamp;bossCached=true;
                    bossText.gameObject.SetActive(visible); bossFill.transform.parent.gameObject.SetActive(visible);
                    if(visible)
                    {
                        bossText.text = text.Format("hud.boss", text.Get(boss.Definition.nameKey), boss.Health.Current, boss.Health.Maximum);
                        SetBar(bossFill, (float)boss.Health.Current / boss.Health.Maximum);
                    }
                }
            }
            int seconds=session.State==GameState.IntroCountdown?Mathf.CeilToInt((float)session.IntroRemaining):session.State==GameState.Pause?(int)session.BattleSeconds:Mathf.CeilToInt((float)(600-session.BattleSeconds));
            var clockStamp=(session.State,seconds);
            if(!clockCached || clockStamp!=lastClock)
            {
                lastClock=clockStamp;clockCached=true;
                clock.text=session.State==GameState.IntroCountdown?text.Get("countdown")+"  "+seconds:text.Format(session.State==GameState.Pause?"survival":"timer",seconds/60,seconds%60);
            }
            if (status != null && session.Run != null)
            {
                var run = session.Run;
                var vitals=(run.CurrentHealth,run.MaximumHealth,run.Kills,run.Attack,run.AttackSpeed,run.MoveSpeed,run.Defense);
                if(!vitalsCached || vitals!=lastVitals)
                {
                    lastVitals=vitals;vitalsCached=true;
                    if(lowHealthText!=null) lowHealthText.gameObject.SetActive(run.MaximumHealth>0 && run.CurrentHealth<run.MaximumHealth*0.3f);
                    status.text = text.Format("hud.stats", run.CurrentHealth, run.MaximumHealth, run.Kills,run.Attack,run.AttackSpeed,run.MoveSpeed,run.Defense);
                    status.color=run.MaximumHealth>0 && run.CurrentHealth<run.MaximumHealth*0.3f?new Color(1,0.4f,0.3f):ink;
                    if(healthFill!=null)SetBar(healthFill,(float)run.CurrentHealth/Mathf.Max(1,run.MaximumHealth));
                }
                if (healthFill != null)
                {
                    var world = Survivor.Combat.BattleWorld.Instance;
                    bool hitFlash = world != null && world.Player.Health.InvincibleRemaining > world.config.invincibleSeconds - world.config.hitFlashSeconds;
                    healthFill.color = hitFlash ? Color.white : run.CurrentHealth < run.MaximumHealth * 0.3f
                        ? new Color(1, 0.25f, 0.2f) : new Color(0.25f, 0.9f, 0.45f);
                }
                if (progressText != null && flow.Progression != null)
                {
                    var progress=(run.Level,run.Experience,flow.Progression.RequiredExperience,run.CollectedCurrency,run.Build.Revision);
                    if(!progressCached || progress!=lastProgress)
                    {
                        lastProgress=progress;progressCached=true;
                        progressText.text=text.Format("hud.progress",run.Level,run.Experience,flow.Progression.RequiredExperience,run.CollectedCurrency);
                        SetBar(experienceFill,(float)((double)run.Experience/flow.Progression.RequiredExperience));
                        if(buildText!=null)buildText.text=DescribeBuild(run.AbilityLevels,true);
                    }
                }
            }
        }
        private string DescribeBuild(System.Collections.Generic.IReadOnlyDictionary<string, int> levels, bool compact = false)
        {
            var entries = new System.Collections.Generic.List<string>();
            foreach (var item in levels)
            {
                if (compact && item.Key.StartsWith("stat.")) continue;
                entries.Add(text.Format("ability.level", text.Get("ability." + item.Key), item.Value));
            }
            return string.Join(compact ? "  |  " : "\n", entries);
        }
    }
}
