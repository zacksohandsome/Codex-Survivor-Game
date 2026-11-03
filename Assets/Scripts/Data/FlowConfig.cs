using UnityEngine;

namespace Survivor.Data
{
    [CreateAssetMenu(menuName = "Survivor/Flow Config")]
    public sealed class FlowConfig : ScriptableObject
    {
        [Min(0)] public float introSeconds = 3;
        public string mainMenuScene = "MainMenu";
        public string battleScene = "Battle";
        public WeaponDefinition[] weapons;
        public BattleConfig battle;
        public AudioCatalog audio;
        [Min(1)] public int metaBasePrice = Survivor.Meta.MetaProgression.DefaultBasePrice;
        public TextAsset traditionalText;
        public TMPro.TMP_FontAsset uiFont;
        public UnityEngine.Localization.Tables.StringTable englishStrings, traditionalStrings;
        public string[] iconIds;
        public Sprite[] icons;
        public Sprite Icon(string id)
        {
            if(iconIds==null || icons==null) return null;
            int index=System.Array.IndexOf(iconIds,id); return index>=0 && index<icons.Length?icons[index]:null;
        }
    }
}
