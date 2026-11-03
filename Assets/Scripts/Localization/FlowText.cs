using System;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Localization
{
    public sealed class FlowText
    {
        [Serializable] private sealed class Entry { public string key; public string text; }
        [Serializable] private sealed class Table { public Entry[] entries; }
        private readonly Dictionary<string, string> entries = new Dictionary<string, string>();
        public FlowText(TextAsset asset)
        {
            foreach (var entry in JsonUtility.FromJson<Table>(asset.text).entries) entries.Add(entry.key, entry.text);
        }
        public string Get(string key) => entries.TryGetValue(key, out var text) ? text : key;
    }
}
