using System;
using Survivor.Core;
using Survivor.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Survivor.UI
{
    public sealed class FlowView : MonoBehaviour
    {
        private GameFlow flow;
        private FlowText text;
        private RectTransform content;
        private TextMeshProUGUI clock;
        private TextMeshProUGUI status;
        private UnityEngine.UI.Image backgroundImage;
        private string page;
        private string confirmationKey;
        private Action confirmation;
        public bool HasModal => confirmation != null;
        private readonly Color ink = new Color(0.88f, 0.92f, 0.96f);

        public void Initialize(GameFlow owner, TextAsset table)
        {
            flow = owner; text = new FlowText(table);
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
            label.text = value; label.fontSize = size; label.color = ink;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private void Button(string key, Action action, bool enabled = true, bool selected = false, float height = 62)
        {
            var go = new GameObject("Button_" + key, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
            go.transform.SetParent(content, false);
            go.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = selected ? new Color(0.12f, 0.45f, 0.40f) : new Color(0.10f, 0.16f, 0.24f);
            var button = go.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.interactable = enabled;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            button.onClick.AddListener(() => action());
            var caption = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
            caption.transform.SetParent(go.transform, false); Stretch(caption.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var label = caption.GetComponent<TextMeshProUGUI>(); label.text = text.Get(key); label.fontSize = 28;
            label.alignment = TextAlignmentOptions.Center; label.color = enabled ? ink : Color.gray; label.raycastTarget = false;
        }
        public void Back()
        {
            if (HasModal) { confirmation = null; Render(); }
            else if (page != null) { page = null; Render(); }
        }
        private void Ask(string key, Action action) { confirmationKey = key; confirmation = action; Render(); }
        public void Render()
        {
            // These are distinct screen contents, not edits to the persistent Canvas or EventSystem.
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject; child.SetActive(false); Destroy(child);
            }
            clock = null; status = null;
            bool battleHud = !flow.IsTransitioning && !HasModal &&
                (flow.Session.State == GameState.Battle || flow.Session.State == GameState.IntroCountdown);
            backgroundImage.color = battleHud ? Color.clear : new Color(0.035f, 0.055f, 0.09f, 0.97f);
            Stretch(content, battleHud ? new Vector2(0.02f, 0.77f) : new Vector2(0.18f, 0.06f),
                battleHud ? new Vector2(0.98f, 0.99f) : new Vector2(0.82f, 0.94f));
            content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = battleHud ? 6 : 12;
            Label(text.Get("stage"), 20, 32);
            if (flow.IsTransitioning) { Label(text.Get("loading"), 40); return; }
            if (HasModal)
            {
                Label(text.Get(confirmationKey), 30, 130);
                Button("confirm", () => { var action = confirmation; confirmation = null; action(); });
                Button("cancel", () => { confirmation = null; Render(); }); return;
            }
            var session = flow.Session;
            switch (session.State)
            {
                case GameState.MainMenu:
                    if (page != null)
                    {
                        Label(text.Get(page), 48, 100); Label(text.Get(page + ".note"), 28, 150);
                        Button("back", () => { page = null; Render(); }); break;
                    }
                    Label(text.Get("title"), 64, 110); Label(text.Get("menu.note"), 28, 100);
                    Label(string.Format(text.Get("currency"), flow.PermanentData.currency), 22, 40);
                    Button("start", () => { page = null; flow.OpenWeaponSelection(); });
                    foreach (string key in new[] { "meta", "settings", "stats" })
                    { string target = key; Button(key, () => { page = target; Render(); }); }
                    Button("quit", () => Ask("quit.confirm", () => { Application.Quit(); Render(); })); break;
                case GameState.WeaponSelection:
                    Label(text.Get("select"), 48, 90); Label(text.Get("select.note"), 24, 80);
                    foreach (var weapon in flow.Config.weapons)
                    { var choice = weapon; Button(weapon.nameKey, () => flow.SelectWeapon(choice.id), true, session.SelectedWeaponId == choice.id); }
                    var selected = Array.Find(flow.Config.weapons, w => w.id == session.SelectedWeaponId);
                    Label(text.Get(selected == null ? "selection.empty" : selected.descriptionKey), 24, 70);
                    Button("enter", () => flow.StartRun(), selected != null && selected.combatAvailable);
                    Button("back", () => session.ReturnToMenu()); break;
                case GameState.IntroCountdown:
                case GameState.Battle:
                    clock = Label("", 28, 32); status = Label("", 22, 50);
                    Label(text.Get("battle.note"), 20, 24);
                    Button("pause", () => session.Pause(), height: 44); RefreshClock(); break;
                case GameState.Pause:
                    Label(text.Get("pause"), 52, 100); clock = Label("", 32, 70); RefreshClock();
                    Label($"Level {session.Run.Level}  /  Kills {session.Run.Kills}", 26, 60);
                    Button("resume", () => flow.Resume());
                    Button("restart", () => Ask("abandon", () => flow.ReturnToMenu(true)));
                    Button("menu", () => Ask("abandon", () => flow.ReturnToMenu())); break;
                case GameState.LevelUp: Label(text.Get("levelup"), 36, 160); break;
                case GameState.Victory: Label(text.Get("victory"), 52, 120); break;
                case GameState.Defeat:
                    Label(text.Get("defeat"), 52, 100); Label(text.Get("defeat.note"), 28, 100);
                    Button("restart", () => flow.ReturnToMenu(true));
                    Button("menu", () => flow.ReturnToMenu()); break;
                case GameState.Result:
                    Label(text.Get("result"), 36, 160); Button("menu", () => flow.ReturnToMenu()); break;
            }
        }
        public void RefreshClock()
        {
            if (clock == null) return;
            var session = flow.Session;
            clock.text = session.State == GameState.IntroCountdown
                ? text.Get("countdown") + "  " + Mathf.CeilToInt((float)session.IntroRemaining)
                : string.Format(text.Get("timer"), (int)session.BattleSeconds / 60, (int)session.BattleSeconds % 60);
            if (status != null && session.Run != null)
            {
                var run = session.Run;
                status.text = string.Format(text.Get("hud.stats"), run.CurrentHealth, run.MaximumHealth, run.Kills,
                    run.Attack, run.AttackSpeed, run.MoveSpeed, run.Defense);
                status.color = run.MaximumHealth > 0 && run.CurrentHealth < run.MaximumHealth * 0.3f
                    ? new Color(1, 0.4f, 0.3f) : ink;
            }
        }
    }
}
