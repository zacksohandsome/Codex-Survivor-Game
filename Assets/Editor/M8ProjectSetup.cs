using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Survivor.Data;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.LowLevel;

namespace Survivor.Editor
{
    public static class M8ProjectSetup
    {
        [Serializable] private sealed class Entry { public string key,text; }
        [Serializable] private sealed class Table { public Entry[] entries; }
        [MenuItem("Survivor/Create M8 Localization")]
        public static void CreateAssets()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
            Directory.CreateDirectory("Assets/Localization/Tables"); Directory.CreateDirectory("Assets/Fonts"); AssetDatabase.Refresh();
            var settings=LocalizationEditorSettings.ActiveLocalizationSettings;
            if(settings==null)
            {
                settings=ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings,"Assets/Localization/LocalizationSettings.asset"); LocalizationEditorSettings.ActiveLocalizationSettings=settings;
            }
            var locales=new List<Locale>();
            foreach(string code in new[]{"zh-TW","en"})
            {
                var locale=LocalizationEditorSettings.GetLocales().FirstOrDefault(x=>x.Identifier.Code==code);
                if(locale==null) { locale=Locale.CreateLocale(code); AssetDatabase.CreateAsset(locale,"Assets/Localization/"+code+".asset"); LocalizationEditorSettings.AddLocale(locale); }
                locales.Add(locale);
            }
            if(locales[1].Metadata.GetMetadata<FallbackLocale>()==null) locales[1].Metadata.AddMetadata(new FallbackLocale(locales[0]));
            settings.GetStartupLocaleSelectors().Clear(); settings.GetStartupLocaleSelectors().Add(new SystemLocaleSelector());
            settings.GetStartupLocaleSelectors().Add(new SpecificLocaleSelector { LocaleId=new LocaleIdentifier("zh-TW") });
            EditorUtility.SetDirty(settings); EditorUtility.SetDirty(locales[1]);
            var collection=LocalizationEditorSettings.GetStringTableCollection("Game")??LocalizationEditorSettings.CreateStringTableCollection("Game","Assets/Localization/Tables",locales);
            var characters=new HashSet<uint>();
            for(int i=0;i<2;i++)
            {
                string path=i==0?"Assets/Localization/FlowText.zh-TW.json":"Assets/Localization/FlowText.json";
                var entries=JsonUtility.FromJson<Table>(File.ReadAllText(path)).entries;
                var table=(StringTable)collection.GetTable(locales[i].Identifier);
                foreach(var entry in entries)
                {
                    table.AddEntry(entry.key,entry.text);
                    foreach(char character in entry.text) if(character>=32) characters.Add(character);
                }
                EditorUtility.SetDirty(table);
            }
            for(uint c=32;c<127;c++) characters.Add(c);
            EditorUtility.SetDirty(collection); EditorUtility.SetDirty(collection.SharedData);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,collection);
            const string fontPath="Assets/Fonts/NotoSansTC-UI.asset";
            var fontAsset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(fontAsset==null)
            {
                var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKtc-Regular.otf");
                fontAsset=TMP_FontAsset.CreateFontAsset(font,40,4,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,false);
                fontAsset.name="Noto Sans TC UI";
                if(!fontAsset.TryAddCharacters(characters.ToArray(),out uint[] missing)) throw new InvalidOperationException("Font glyphs missing: "+missing.Length);
                fontAsset.atlasPopulationMode=AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(fontAsset,fontPath);
                foreach(var texture in fontAsset.atlasTextures) AssetDatabase.AddObjectToAsset(texture,fontAsset);
                AssetDatabase.AddObjectToAsset(fontAsset.material,fontAsset); EditorUtility.SetDirty(fontAsset);
            }
            var fontTables=LocalizationEditorSettings.GetAssetTableCollection("UI")??LocalizationEditorSettings.CreateAssetTableCollection("UI","Assets/Localization/Tables",locales);
            string guid=AssetDatabase.AssetPathToGUID(fontPath);
            foreach(var locale in locales)
            {
                var table=(AssetTable)fontTables.GetTable(locale.Identifier); table.AddEntry("ui.font",guid); EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(fontTables); EditorUtility.SetDirty(fontTables.SharedData); LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null,fontTables);
            var flow=AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");
            flow.battle=AssetDatabase.LoadAssetAtPath<BattleConfig>("Assets/Data/Battle/BattleConfig.asset");
            flow.traditionalText=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Localization/FlowText.zh-TW.json"); flow.uiFont=fontAsset;
            flow.traditionalStrings=(StringTable)collection.GetTable(locales[0].Identifier); flow.englishStrings=(StringTable)collection.GetTable(locales[1].Identifier);
            EditorUtility.SetDirty(flow); PlayerSettings.allowFullscreenSwitch=false; AssetDatabase.SaveAssets();
        }
    }
}
