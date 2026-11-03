using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Survivor.Localization
{
    public sealed class FlowText
    {
        [Serializable] private sealed class Entry { public string key; public string text; }
        [Serializable] private sealed class Table { public Entry[] entries; }
        private readonly Dictionary<string,string> english=new Dictionary<string,string>(), traditional=new Dictionary<string,string>();
        private readonly StringTable englishTable, traditionalTable;
        public FlowText(TextAsset asset,TextAsset zh=null,StringTable enStrings=null,StringTable zhStrings=null)
        {
            foreach(var entry in JsonUtility.FromJson<Table>(asset.text).entries) english.Add(entry.key,entry.text);
            if(zh!=null) foreach(var entry in JsonUtility.FromJson<Table>(zh.text).entries) traditional.Add(entry.key,entry.text);
            englishTable=enStrings; traditionalTable=zhStrings;
        }
        public static bool UsesEnglish => !Application.isPlaying || LocalizationSettings.InitializationOperation.IsDone && LocalizationSettings.SelectedLocale!=null && LocalizationSettings.SelectedLocale.Identifier.Code=="en";
        public static CultureInfo Culture => CultureInfo.GetCultureInfo(UsesEnglish?"en-US":"zh-TW");
        public string Get(string key)
        {
            var table=UsesEnglish?englishTable:traditionalTable;
            var entry=table==null?null:table.GetEntry(key);
            if(entry!=null && !string.IsNullOrEmpty(entry.LocalizedValue)) return entry.LocalizedValue;
            if(table!=null)
            {
                var fallback=traditionalTable==null?null:traditionalTable.GetEntry(key);
                if(fallback!=null && !string.IsNullOrEmpty(fallback.LocalizedValue)) return fallback.LocalizedValue;
                return traditional.TryGetValue(key,out var fallbackText)?fallbackText:key;
            }
            var selected=UsesEnglish?english:traditional;
            if(selected.TryGetValue(key,out var text)) return text;
            if(traditional.TryGetValue(key,out text)) return text;
            return english.TryGetValue(key,out text)?text:key;
        }
        public string Format(string key,params object[] arguments) => string.Format(Culture,Get(key),arguments);
    }
}
