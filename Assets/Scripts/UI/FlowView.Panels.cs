using System;
using Survivor.Data;
using Survivor.Meta;
using Survivor.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Survivor.UI
{
    public sealed partial class FlowView
    {
        private RectTransform tooltip;
        private TextMeshProUGUI tooltipText;
        private sealed class BattleHudCache
        {
            public RectTransform Root;public Survivor.Core.RunContext Run;public bool English;public int Revision;
            public TextMeshProUGUI Clock,Status,Progress,Boss,Notice,Low;
            public UnityEngine.UI.Image Health,Experience,BossFill;
        }
        private sealed class AbilitySlot
        {
            public GameObject Root;public UnityEngine.UI.Image Image;public TextMeshProUGUI Label;
            public UpgradeCategory Category;public int Ordinal,Level;public string Id;
        }
        private BattleHudCache retainedHud;
        private sealed class UpgradeCard
        {public UpgradeChoiceButton Button;public TextMeshProUGUI Label;public UnityEngine.UI.Image Icon,Background;}
        private sealed class UpgradePanelCache
        {public RectTransform Root;public Survivor.Core.RunContext Run;public bool English;public TextMeshProUGUI Level;public UpgradeCard[] Cards;}
        private UpgradePanelCache upgradePanel;
        private uint upgradeGeneration;
        private readonly System.Collections.Generic.List<AbilitySlot> abilitySlots=new System.Collections.Generic.List<AbilitySlot>(10);
        private void PrepareBattleHud(bool visible)
        {
            if(retainedHud==null)return;
            bool discard=retainedHud.Run!=flow.Session.Run || retainedHud.English!=Survivor.Localization.FlowText.UsesEnglish;
            retainedHud.Root.gameObject.SetActive(visible && !discard);
            if(!discard)return;
            Destroy(retainedHud.Root.gameObject);retainedHud=null;abilitySlots.Clear();
        }
        private void ShowBattleHud()
        {
            if(retainedHud==null)
            {
                var overlay=content;
                var root=new GameObject("BattleHud",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(content.parent,false);
                content=root;BuildBattleHud();content=overlay;
                retainedHud=new BattleHudCache{Root=root,Run=flow.Session.Run,English=Survivor.Localization.FlowText.UsesEnglish,Revision=flow.Session.Run.Build.Revision,
                    Clock=clock,Status=status,Progress=progressText,Boss=bossText,Notice=encounterText,Low=lowHealthText,Health=healthFill,Experience=experienceFill,BossFill=bossFill};
                EnsureUpgradePanel();
            }
            clock=retainedHud.Clock;status=retainedHud.Status;progressText=retainedHud.Progress;bossText=retainedHud.Boss;encounterText=retainedHud.Notice;
            lowHealthText=retainedHud.Low;healthFill=retainedHud.Health;experienceFill=retainedHud.Experience;bossFill=retainedHud.BossFill;
            if(retainedHud.Revision!=flow.Session.Run.Build.Revision)
            { RefreshAbilitySlots();retainedHud.Revision=flow.Session.Run.Build.Revision; }
            retainedHud.Root.gameObject.SetActive(true);
        }
        private void PrepareUpgradePanel(bool visible)
        {
            if(upgradePanel==null)return;
            bool discard=upgradePanel.Run!=flow.Session.Run || upgradePanel.English!=Survivor.Localization.FlowText.UsesEnglish;
            upgradePanel.Root.gameObject.SetActive(visible && !discard);
            if(!discard)return;Destroy(upgradePanel.Root.gameObject);upgradePanel=null;
        }
        private void EnsureUpgradePanel()
        {
            if(upgradePanel!=null)return;
            var overlay=content;var root=new GameObject("UpgradePanel",typeof(RectTransform),typeof(UnityEngine.UI.VerticalLayoutGroup)).GetComponent<RectTransform>();
            root.SetParent(overlay.parent,false);Stretch(root,new Vector2(0.18f,0.06f),new Vector2(0.82f,0.94f));
            var layout=root.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=12;layout.childControlHeight=layout.childControlWidth=true;
            layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;layout.childAlignment=TextAnchor.MiddleCenter;
            root.gameObject.SetActive(false);content=root;
            Label(text.Get("stage"),20,32);Label(text.Get("levelup"),44,80);var level=Label("",26,60);
            var cards=new UpgradeCard[3];
            for(int i=0;i<3;i++)
            {
                Button("upgrade.card",()=>{},height:120,upgradeChoice:true);var go=content.GetChild(content.childCount-1).gameObject;go.name="Button_upgrade."+i;
                var label=go.GetComponentInChildren<TextMeshProUGUI>(true);label.fontSize=24;label.rectTransform.offsetMin=new Vector2(75,0);
                // Reserve text/mesh buffers during the intro, before any combat event.
                label.text=new string('W',512);label.ForceMeshUpdate(true,true);label.text="";
                var icon=new GameObject("Icon",typeof(RectTransform),typeof(UnityEngine.UI.Image));icon.transform.SetParent(go.transform,false);
                var rect=icon.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,0.5f);rect.sizeDelta=new Vector2(36,36);rect.anchoredPosition=new Vector2(38,0);
                var image=icon.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=false;
                cards[i]=new UpgradeCard{Button=go.GetComponent<UpgradeChoiceButton>(),Label=label,Icon=image,Background=go.GetComponent<UnityEngine.UI.Image>()};
            }
            Label(text.Get("upgrade.note"),24,60);content=overlay;
            upgradePanel=new UpgradePanelCache{Root=root,Run=flow.Session.Run,English=Survivor.Localization.FlowText.UsesEnglish,Level=level,Cards=cards};
        }
        private void ShowUpgradePanel()
        {
            EnsureUpgradePanel();upgradePanel.Level.text=text.Format("upgrade.level",flow.Session.Run.Level);
            var progression=flow.Progression;var choices=progression==null?null:progression.Choices;uint generation=++upgradeGeneration;
            for(int i=0;i<3;i++)
            {
                var card=upgradePanel.Cards[i];bool visible=choices!=null && i<choices.Length;card.Button.gameObject.SetActive(visible);if(!visible)continue;
                var option=choices[i];int choice=i,revision=progression.ChoiceRevision;var run=flow.Session.Run;
                card.Button.BindGeneration(generation);card.Button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
                card.Button.onClick.AddListener(()=>{if(!ReferenceEquals(run,flow.Session.Run))return;flow.Audio.PlayUi("ui");flow.ChooseUpgrade(choice,revision);});
                card.Label.text=(i+1)+"  "+text.Get(option.Effect.nameKey)+"\n"+text.Get(option.Effect.descriptionKey)+"\n"+(option.IsSpecial?text.Get("upgrade.reward"):text.Format("upgrade.current",option.CurrentLevel));
                card.Icon.sprite=flow.Config.Icon(option.IsSpecial?option.Effect.effect==UpgradeEffect.CurrencyReward?"coins":"stat.health":option.AbilityId);
                card.Icon.color=card.Icon.sprite!=null?Color.white:Color.cyan;
                card.Background.color=option.IsSpecial?new Color(0.35f,0.25f,0.12f):option.Category==UpgradeCategory.Passive?new Color(0.15f,0.32f,0.2f):new Color(0.1f,0.2f,0.32f);
            }
            upgradePanel.Root.gameObject.SetActive(true);
        }
        private void RefreshAbilitySlots()
        {
            foreach(var slot in abilitySlots)
            {
                string id=null;int level=0,ordinal=0;
                foreach(var ability in flow.Config.battle.progression.abilities)
                {
                    int current=flow.Session.Run.Build.Level(ability.id);
                    if(ability.category!=slot.Category || current<=0)continue;
                    if(ordinal++!=slot.Ordinal)continue;id=ability.id;level=current;break;
                }
                if(slot.Id==id && slot.Level==level)continue;
                slot.Id=id;slot.Level=level;slot.Root.name=id==null?"EmptySlot":"Ability_"+id;
                slot.Image.sprite=id==null?null:flow.Config.Icon(id);
                slot.Image.color=id==null?new Color(0.12f,0.17f,0.23f):Color.white;
                slot.Label.text=level==0?"":level.ToString(Survivor.Localization.FlowText.Culture);
            }
        }
        private RectTransform Panel(string name,Vector2 min,Vector2 max)
        {
            var rect=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.VerticalLayoutGroup)).GetComponent<RectTransform>();
            rect.SetParent(content,false); Stretch(rect,min,max);
            var layout=rect.GetComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.spacing=3;
            layout.childControlWidth=layout.childControlHeight=true; layout.childForceExpandWidth=true; layout.childForceExpandHeight=false;
            return rect;
        }
        private void BuildBattleHud()
        {
            var root=content; Stretch(root,Vector2.zero,Vector2.one);
            content=Panel("Vitals",new Vector2(0.015f,0.835f),new Vector2(0.29f,0.985f));
            status=Label("",18,65); healthFill=Bar("HealthBar",new Color(0.25f,0.9f,0.45f));
            progressText=Label("",18,40); experienceFill=Bar("ExperienceBar",new Color(0.2f,0.6f,1));
            lowHealthText=Label(text.Get("hud.lowhealth"),18,22); lowHealthText.gameObject.name="LowHealthWarning"; lowHealthText.color=new Color(1,0.3f,0.2f);
            content=root; content=Panel("ClockAndBoss",new Vector2(0.34f,0.865f),new Vector2(0.68f,0.985f));
            clock=Label("",32,42); bossText=Label("",22,30); bossText.gameObject.name="BossName";
            bossFill=Bar("BossHealth",new Color(0.85f,0.3f,0.9f));
            content=root; content=Panel("BattleActions",new Vector2(0.73f,0.89f),new Vector2(0.985f,0.985f));
            Label(text.Get("battle.note"),16,28); Button("pause",()=>flow.Session.Pause(),height:46);
            content=root; content=Panel("EncounterNotice",new Vector2(0.25f,0.78f),new Vector2(0.75f,0.84f));
            encounterText=Label("",26,50);
            content=root; content=Panel("AbilityInventory",new Vector2(0.015f,0.015f),new Vector2(0.68f,0.16f));
            AbilityGroup(UpgradeCategory.Weapon,"hud.group.weapons",3);
            AbilityGroup(UpgradeCategory.Passive,"hud.group.passives",5);
            AbilityGroup(UpgradeCategory.Skill,"hud.group.skills",2);
            content=root;
        }
        private void AbilityGroup(UpgradeCategory category,string title,int slots)
        {
            var row=new GameObject("AbilityGroup_"+category,typeof(RectTransform),typeof(UnityEngine.UI.HorizontalLayoutGroup),typeof(UnityEngine.UI.LayoutElement));
            row.transform.SetParent(content,false); row.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight=44;
            var layout=row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>(); layout.spacing=6; layout.childControlHeight=layout.childControlWidth=true;
            layout.childForceExpandHeight=true; layout.childForceExpandWidth=false; layout.childAlignment=TextAnchor.MiddleLeft;
            var parent=content; content=row.GetComponent<RectTransform>();
            var caption=Label(text.Get(title),18,40); caption.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth=110;
            int count=0;
            foreach(var ability in flow.Config.battle.progression.abilities)
            {
                int level=flow.Session.Run.Build.Level(ability.id);
                if(ability.category!=category || level<=0) continue;
                CreateAbilityIcon(ability.id,level,category); count++;
            }
            while(count++<slots) CreateAbilityIcon(null,0,category);
            content=parent;
        }
        private void CreateAbilityIcon(string id,int level,UpgradeCategory category)
        {
            var go=new GameObject(id==null?"EmptySlot":"Ability_"+id,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.LayoutElement),typeof(AbilityTooltip));
            go.transform.SetParent(content,false); go.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth=52;
            var image=go.GetComponent<UnityEngine.UI.Image>(); image.sprite=id==null?null:flow.Config.Icon(id);
            image.color=id==null?new Color(0.12f,0.17f,0.23f):image.sprite!=null?Color.white:
                category==UpgradeCategory.Weapon?new Color(0.12f,0.4f,0.55f):category==UpgradeCategory.Passive?new Color(0.15f,0.4f,0.25f):new Color(0.4f,0.2f,0.5f);
            var labelObject=new GameObject("Level",typeof(RectTransform),typeof(TextMeshProUGUI)); labelObject.transform.SetParent(go.transform,false);
            Stretch(labelObject.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
            var label=labelObject.GetComponent<TextMeshProUGUI>(); label.font=flow.Config.uiFont; label.fontSize=19;
            label.text=level==0?"":level.ToString(Survivor.Localization.FlowText.Culture); label.alignment=TextAlignmentOptions.BottomRight; label.raycastTarget=false;
            int ordinal=0;foreach(var existing in abilitySlots)if(existing.Category==category)ordinal++;
            var slot=new AbilitySlot{Root=go,Image=image,Label=label,Category=category,Ordinal=ordinal,Id=id,Level=level};abilitySlots.Add(slot);
            var hover=go.GetComponent<AbilityTooltip>();
            hover.Enter=()=>ShowTooltip(slot.Id==null?text.Get("hud.empty"):DescribeAbility(slot.Id)); hover.Exit=HideTooltip;
        }
        private string DescribeAbility(string id)
        {
            var build=flow.Session.Run.Build; var world=Survivor.Combat.BattleWorld.Instance;
            string effect;
            var weapon=Array.Find(flow.Config.weapons,w=>w.id==id);
            if(weapon!=null) effect=text.Format("effect.weapon",world.Player.Attack*(weapon.damageMultiplier+build.DamageFor(id)),weapon.cooldown/world.Player.AttackSpeed,weapon.range,weapon.maxHits+build.PierceFor(id),weapon.areaRadius+build.AreaFor(id));
            else switch(id)
            {
                case "bloom": effect=text.Format("effect.bloom",build.RegenAmount,Mathf.Max(1,world.config.progression.regenInterval-build.RegenIntervalReduction)); break;
                case "magnet": effect=text.Format("effect.magnet",world.config.progression.magnetRadius+build.MagnetRadiusBonus,world.config.progression.magnetSpeed+build.MagnetSpeedBonus); break;
                case "echo": effect=text.Format("effect.echo",build.EchoDamage*100,build.Level(id)>=3?2:1); break;
                case "thorns": effect=text.Format("effect.thorns",build.ThornsDamage*100,build.Level(id)>=3?4:2.5f); break;
                case "trail": effect=text.Format("effect.trail",build.TrailDamage*100,build.Level(id)>=3?2:0.8f); break;
                case "storm": effect=text.Format("effect.storm",build.StormDamage*100,world.Passives.StormRemaining); break;
                default: effect=text.Format("effect.sanctuary",build.SanctuaryDefense,build.Level(id)>=3?3:0); break;
            }
            return text.Format("ability.tooltip",text.Get("ability."+id),build.Level(id),effect+"\n"+text.Get("upgrade."+id+".new.desc"));
        }
        private void InitializeTooltip(Transform parent)
        {
            tooltip=new GameObject("AbilityTooltip",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            tooltip.SetParent(parent,false); tooltip.pivot=new Vector2(0,1); tooltip.sizeDelta=new Vector2(610,190);
            var background=tooltip.GetComponent<UnityEngine.UI.Image>(); background.color=new Color(0.025f,0.04f,0.07f,0.98f); background.raycastTarget=false;
            var label=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)); label.transform.SetParent(tooltip,false);
            Stretch(label.GetComponent<RectTransform>(),Vector2.zero,Vector2.one); label.GetComponent<RectTransform>().offsetMin=new Vector2(15,12); label.GetComponent<RectTransform>().offsetMax=new Vector2(-15,-12);
            tooltipText=label.GetComponent<TextMeshProUGUI>(); tooltipText.font=flow.Config.uiFont; tooltipText.fontSize=22; tooltipText.color=ink; tooltipText.raycastTarget=false;
            HideTooltip();
        }
        private void ShowTooltip(string description)
        {
            tooltipText.text=description; tooltip.gameObject.SetActive(true); tooltip.SetAsLastSibling();
            var canvas=tooltip.parent.GetComponent<RectTransform>();
            Vector2 pointer=Mouse.current==null?new Vector2(Screen.width/2,Screen.height/2):Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,pointer,null,out var point);
            tooltip.anchoredPosition=new Vector2(Mathf.Clamp(point.x+16,-canvas.rect.width/2,canvas.rect.width/2-tooltip.sizeDelta.x),
                Mathf.Clamp(point.y+180,-canvas.rect.height/2+tooltip.sizeDelta.y,canvas.rect.height/2));
        }
        private void HideTooltip() { if(tooltip!=null) tooltip.gameObject.SetActive(false); }
        private string PlayerPreview()
        {
            var c=flow.Config.battle; if(c==null) return ""; var levels=flow.PermanentData.metaUpgradeLevels;
            return text.Format("player.preview",c.playerHealth+levels[0]*MetaProgression.Bonuses[0],c.playerAttack+levels[1]*MetaProgression.Bonuses[1],
                Mathf.Min(c.attackSpeedCap,c.playerAttackSpeed+levels[2]*MetaProgression.Bonuses[2]),Mathf.Min(c.moveSpeedCap,c.playerMoveSpeed+levels[3]*MetaProgression.Bonuses[3]),c.playerDefense+levels[4]*MetaProgression.Bonuses[4]);
        }
        private void IconOnLastButton(string id)
        {
            var sprite=flow.Config.Icon(id); if(sprite==null) return;
            var button=content.GetChild(content.childCount-1);
            var icon=new GameObject("Icon",typeof(RectTransform),typeof(UnityEngine.UI.Image)); icon.transform.SetParent(button,false);
            var rect=icon.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(0,0.5f);rect.pivot=new Vector2(0,0.5f);rect.sizeDelta=new Vector2(48,44);rect.anchoredPosition=new Vector2(10,0);
            var image=icon.GetComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
            button.GetComponentInChildren<TextMeshProUGUI>().rectTransform.offsetMin=new Vector2(65,0);
        }
        private void RenderSettings()
        {
            Label(text.Get("settings"),40,60);
            Label(text.Get("settings.language"),22,28);
            Button("language.zh-TW",()=>flow.SetLanguage("zh-TW"),selected:!Survivor.Localization.FlowText.UsesEnglish,height:46); Button("language.en",()=>flow.SetLanguage("en"),selected:Survivor.Localization.FlowText.UsesEnglish,height:46);
            VolumeSlider("settings.music",flow.PermanentData.musicVolume,value=>flow.SetVolumes(value,flow.PermanentData.sfxVolume));
            VolumeSlider("settings.sfx",flow.PermanentData.sfxVolume,value=>flow.SetVolumes(flow.PermanentData.musicVolume,value));
            Label(text.Get("settings.display"),22,28);
            var applied=flow.Settings.Display.Applied;
            Button(applied.fullscreen?"settings.fullscreen":"settings.windowed",()=>flow.PreviewDisplay(applied.width,applied.height,!applied.fullscreen),height:44);
            foreach(var size in new[] { new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(2560,1440) })
            {
                var chosen=size; Button("display.resolution",()=>flow.PreviewDisplay(chosen.x,chosen.y,applied.fullscreen),SettingsController.Supported(new DisplayChoice(size.x,size.y,applied.fullscreen)),height:42);
                var control=content.GetChild(content.childCount-1); control.gameObject.name="Button_resolution."+size.x;
                control.GetComponentInChildren<TextMeshProUGUI>().text=text.Format("display.resolution",size.x,size.y);
            }
            if(flow.Meta.Notice!=null) Label(text.Get(flow.Meta.Notice),20,42);
            Button("back",()=>{page=null;Render();},height:46);
        }
        private void VolumeSlider(string key,float initial,Func<float,bool> changed)
        {
            var root=new GameObject("Slider_"+key,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Slider),typeof(UnityEngine.UI.LayoutElement));
            root.transform.SetParent(content,false); root.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight=48;
            var track=root.GetComponent<UnityEngine.UI.Image>(); track.color=new Color(0.1f,0.16f,0.24f);
            var fill=new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image)); fill.transform.SetParent(root.transform,false);
            Stretch(fill.GetComponent<RectTransform>(),Vector2.zero,Vector2.one); fill.GetComponent<UnityEngine.UI.Image>().color=new Color(0.12f,0.4f,0.4f); fill.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var caption=new GameObject("Caption",typeof(RectTransform),typeof(TextMeshProUGUI)); caption.transform.SetParent(root.transform,false); Stretch(caption.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
            var label=caption.GetComponent<TextMeshProUGUI>(); label.font=flow.Config.uiFont; label.fontSize=23; label.alignment=TextAlignmentOptions.Center; label.color=ink; label.raycastTarget=false;
            var slider=root.GetComponent<UnityEngine.UI.Slider>(); slider.fillRect=fill.GetComponent<RectTransform>(); slider.targetGraphic=track;
            slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(initial); label.text=text.Get(key)+"  "+Mathf.RoundToInt(initial*100)+"%";
            slider.navigation=new UnityEngine.UI.Navigation { mode=UnityEngine.UI.Navigation.Mode.None };
            slider.onValueChanged.AddListener(value=>{if(changed(value)) label.text=text.Get(key)+"  "+Mathf.RoundToInt(value*100)+"%";else{slider.SetValueWithoutNotify(initial);label.text=text.Get("save.failed");}});
        }
        public void ShowDisplayConfirmation() => Ask("display.confirm",()=>{flow.Settings.Confirm();Render();},()=>flow.Settings.Cancel());
        public void CloseDisplayConfirmation()
        { if(HasModal && confirmationKey=="display.confirm") { confirmation=null;cancellation=null;Render(); } }
    }
}
