using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Survivor.Combat;
using Survivor.Core;
using Survivor.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Survivor.Tests
{
    public class SettingsIntegrationTests
    {
        private bool background;
        private GameFlow Flow=>GameFlow.Instance;
        private static UnityEngine.UI.Button Button(string id)=>GameObject.Find("Button_"+id).GetComponent<UnityEngine.UI.Button>();
        [UnitySetUp] public IEnumerator Setup()
        {
            background=Application.runInBackground;Application.runInBackground=true;
            GameFlow.SaveDirectoryOverride=Path.Combine(Path.GetTempPath(),"Survivor-SettingsTests-"+Guid.NewGuid().ToString("N"));
            if(Flow!=null){Object.Destroy(Flow.gameObject);yield return null;}
            yield return SceneManager.LoadSceneAsync("MainMenu");
            float deadline=Time.realtimeSinceStartup+15;
            while(!LocalizationSettings.InitializationOperation.IsDone && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(LocalizationSettings.InitializationOperation.IsDone,Is.True);yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(Flow!=null)Object.Destroy(Flow.gameObject);if(BattleWorld.Instance!=null)Object.Destroy(BattleWorld.Instance.gameObject);
            yield return null;Time.timeScale=1;Application.runInBackground=background;
        }
        [UnityTest] public IEnumerator LanguageAndAudioUpdateImmediatelyAndSurviveReload()
        {
            Button("settings").onClick.Invoke();yield return null;
            Button("language.en").onClick.Invoke();yield return null;
            Assert.That(LocalizationSettings.SelectedLocale.Identifier.Code,Is.EqualTo("en"));
            GameObject.Find("Slider_settings.music").GetComponent<UnityEngine.UI.Slider>().value=0.25f;
            GameObject.Find("Slider_settings.sfx").GetComponent<UnityEngine.UI.Slider>().value=0.35f;
            Button("language.zh-TW").onClick.Invoke();yield return null;
            Button("back").onClick.Invoke();yield return null;
            Assert.That(Button("start").GetComponentInChildren<TextMeshProUGUI>().text,Is.EqualTo("開始遊戲"));
            Assert.That(Flow.PermanentData.musicVolume,Is.EqualTo(0.25f));Assert.That(Flow.PermanentData.sfxVolume,Is.EqualTo(0.35f));
            Object.Destroy(Flow.gameObject);yield return null;yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            Assert.That(LocalizationSettings.SelectedLocale.Identifier.Code,Is.EqualTo("zh-TW"));
            Assert.That(Flow.PermanentData.musicVolume,Is.EqualTo(0.25f));Assert.That(Flow.PermanentData.sfxVolume,Is.EqualTo(0.35f));
        }
        [UnityTest] public IEnumerator DisplayConfirmPersistsAndCancelRestoresWithoutSaving()
        {
            Assert.That(Flow.PreviewDisplay(1280,720,false),Is.True);yield return null;
            Assert.That(Flow.Settings.Display.Pending,Is.True);Assert.That(Flow.PermanentData.displayWidth,Is.EqualTo(1920));
            Button("cancel").onClick.Invoke();yield return null;
            Assert.That(Flow.Settings.Display.Applied.width,Is.EqualTo(1920));Assert.That(Flow.Settings.Display.Pending,Is.False);
            Assert.That(Flow.PreviewDisplay(1600,900,true),Is.True);yield return null;Button("confirm").onClick.Invoke();yield return null;
            Assert.That(Flow.PermanentData.displayWidth,Is.EqualTo(1600));Assert.That(Flow.PermanentData.fullscreen,Is.True);
            Object.Destroy(Flow.gameObject);yield return null;yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            Assert.That(Flow.PermanentData.displayWidth,Is.EqualTo(1600));Assert.That(Flow.Settings.Display.Applied.fullscreen,Is.True);
        }
        [UnityTest] public IEnumerator ActualFifteenSecondTimeoutClosesConfirmationAndRestores()
        {
            Flow.PreviewDisplay(1280,720,true);yield return new WaitForSecondsRealtime(15.2f);
            Assert.That(Flow.Settings.Display.Pending,Is.False);Assert.That(Flow.Settings.Display.Applied.width,Is.EqualTo(1920));
            Assert.That(GameObject.Find("Button_confirm"),Is.Null);Assert.That(Flow.PermanentData.displayWidth,Is.EqualTo(1920));
        }
        [UnityTest] public IEnumerator HudHasSeparateAbilityTypesCurrentTooltipAndLowHealthWarning()
        {
            Flow.SetLanguage("en");Flow.OpenWeaponSelection();Flow.SelectWeapon("bolt");Flow.StartRun();
            float deadline=Time.realtimeSinceStartup+10;while(Flow.IsTransitioning && Time.realtimeSinceStartup<deadline)yield return null;
            var world=BattleWorld.Instance;world.enabled=false;Flow.Session.Tick(3);world.ApplyBuild();yield return null;
            Assert.That(GameObject.Find("AbilityGroup_Weapon"),Is.Not.Null);Assert.That(GameObject.Find("AbilityGroup_Passive"),Is.Not.Null);Assert.That(GameObject.Find("AbilityGroup_Skill"),Is.Not.Null);
            var hover=GameObject.Find("Ability_bolt").GetComponent<AbilityTooltip>();hover.OnPointerEnter(new PointerEventData(EventSystem.current));
            var tooltip=GameObject.Find("AbilityTooltip");Assert.That(tooltip,Is.Not.Null);
            Assert.That(tooltip.GetComponentInChildren<TextMeshProUGUI>().text,Does.Contain("Damage 16"));
            hover.OnPointerExit(new PointerEventData(EventSystem.current));Assert.That(GameObject.Find("AbilityTooltip"),Is.Null);
            world.Player.Health.TakeDamage(80);world.Tick(0.01f,Vector2.zero);Flow.GetComponent<FlowView>().RefreshClock();
            Assert.That(GameObject.Find("LowHealthWarning"),Is.Not.Null);
        }
        [UnityTest] public IEnumerator BothLocalesRenderMenusAtFourEditorSizesAndBothModes()
        {
            foreach(string locale in new[]{"zh-TW","en"})foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(2560,1440)})foreach(bool fullscreen in new[]{false,true})
            {
                Flow.SetLanguage(locale);Assert.That(Flow.PreviewDisplay(size.x,size.y,fullscreen),Is.True);Button("confirm").onClick.Invoke();
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                Assert.That(Screen.width,Is.EqualTo(size.x));Assert.That(Screen.height,Is.EqualTo(size.y));
                foreach(var label in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                {
                    if(!label.gameObject.activeInHierarchy)continue;label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing,Is.False,locale+" "+size+" "+label.gameObject.name);
                }
                Assert.That(Flow.Settings.Display.Applied.width,Is.EqualTo(size.x));
            }
        }
    }
}
