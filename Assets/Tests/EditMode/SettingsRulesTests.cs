using System;
using System.Linq;
using NUnit.Framework;
using Survivor.Data;
using Survivor.Save;
using Survivor.Settings;
using UnityEditor;

namespace Survivor.Tests
{
    public class SettingsRulesTests
    {
        [TestCase(1280,720)] [TestCase(1600,900)] [TestCase(1920,1080)] [TestCase(2560,1440)]
        public void AllFourSupportedSizesAreRecognized(int width,int height) => Assert.That(DisplayPreview.ValidSize(width,height),Is.True);
        [Test] public void DisplayPreviewDoesNotPersistUntilConfirmAndTimeoutRestoresOriginal()
        {
            var original=new DisplayChoice(1920,1080,false); var model=new DisplayPreview(original);
            Assert.That(model.Begin(new DisplayChoice(1280,720,true),true),Is.True);
            Assert.That(model.Saved,Is.EqualTo(original)); Assert.That(model.Pending,Is.True);
            Assert.That(model.Tick(14.9),Is.False); Assert.That(model.Tick(0.2),Is.True);
            Assert.That(model.Applied,Is.EqualTo(original)); Assert.That(model.Saved,Is.EqualTo(original));
        }
        [Test] public void FailedDisplaySaveRollsBackAndSuccessfulConfirmCannotBeReplayed()
        {
            var model=new DisplayPreview(new DisplayChoice(1920,1080,false)); var target=new DisplayChoice(1600,900,true);
            model.Begin(target,true); Assert.That(model.Confirm(_=>false),Is.False); Assert.That(model.Applied.width,Is.EqualTo(1920));
            model.Begin(target,true); int saves=0;
            Assert.That(model.Confirm(_=>{saves++;return true;}),Is.True); Assert.That(model.Confirm(_=>{saves++;return true;}),Is.False);
            Assert.That(saves,Is.EqualTo(1)); Assert.That(model.Tick(100),Is.False); Assert.That(model.Saved,Is.EqualTo(target));
        }
        [Test] public void UnsupportedAndConcurrentPreviewsAreRejectedAndInvalidTimeIgnored()
        {
            var model=new DisplayPreview(new DisplayChoice(1920,1080,false));
            Assert.That(model.Begin(new DisplayChoice(100,100,false),true),Is.False);
            Assert.That(model.Begin(new DisplayChoice(1280,720,false),false),Is.False);
            model.Begin(new DisplayChoice(1280,720,false),true); Assert.That(model.Begin(new DisplayChoice(2560,1440,false),true),Is.False);
            model.Tick(double.NaN);model.Tick(double.PositiveInfinity);model.Tick(-3);Assert.That(model.Remaining,Is.EqualTo(15));
            Assert.That(model.Cancel(),Is.True); Assert.That(model.Applied.width,Is.EqualTo(1920));
        }
        [Test] public void LegacyVersionOneMigratesPreferencesWithoutChangingPermanentProgress()
        {
            var legacy=new SaveData { version=1,currency=44,totalRuns=3,victories=1,totalKills=90,totalSurvivalSeconds=800,language="en",metaUpgradeLevels=new[]{1,2,0,0,0},musicVolume=0.99f,displayWidth=1280,displayHeight=720 };
            Assert.That(SaveRepository.TryDecode(SaveRepository.Encode(legacy),out var migrated),Is.True);
            Assert.That(migrated.version,Is.EqualTo(SaveData.CurrentVersion)); Assert.That(migrated.currency,Is.EqualTo(44)); Assert.That(migrated.metaUpgradeLevels,Is.EqualTo(legacy.metaUpgradeLevels));
            Assert.That(migrated.musicVolume,Is.EqualTo(0.6f)); Assert.That(migrated.sfxVolume,Is.EqualTo(0.7f)); Assert.That(migrated.displayWidth,Is.EqualTo(1920));
        }
        [Test] public void InvalidPreferenceValuesNeverEnterTheSave()
        {
            Assert.That(SaveRepository.Valid(new SaveData { musicVolume=float.NaN }),Is.False);
            Assert.That(SaveRepository.Valid(new SaveData { sfxVolume=1.1f }),Is.False);
            Assert.That(SaveRepository.Valid(new SaveData { language="unsupported" }),Is.False);
            Assert.That(SaveRepository.Valid(new SaveData { displayWidth=500,displayHeight=500 }),Is.False);
        }
        [Test] public void OfficialTablesHaveMatchingKeysPlaceholdersAndAllVisibleGlyphs()
        {
            var config=AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");
            Assert.That(config.englishStrings.Count,Is.EqualTo(config.traditionalStrings.Count));
            foreach(var pair in config.englishStrings)
            {
                var english=pair.Value; var chinese=config.traditionalStrings.GetEntry(english.KeyId);
                Assert.That(chinese,Is.Not.Null,english.Key); Assert.That(chinese.LocalizedValue,Is.Not.Empty);
                var en=System.Text.RegularExpressions.Regex.Matches(english.LocalizedValue,@"\{(\d+)").Cast<System.Text.RegularExpressions.Match>().Select(m=>m.Groups[1].Value).Distinct().OrderBy(x=>x);
                var zh=System.Text.RegularExpressions.Regex.Matches(chinese.LocalizedValue,@"\{(\d+)").Cast<System.Text.RegularExpressions.Match>().Select(m=>m.Groups[1].Value).Distinct().OrderBy(x=>x);
                Assert.That(zh,Is.EqualTo(en),english.Key);
                foreach(char c in english.LocalizedValue+chinese.LocalizedValue) if(c>=32) Assert.That(config.uiFont.HasCharacter((int)c),Is.True,"Missing glyph code "+(int)c);
            }
            Assert.That(config.icons.Length,Is.EqualTo(20)); Assert.That(config.icons.All(x=>x!=null),Is.True);
        }
    }
}
