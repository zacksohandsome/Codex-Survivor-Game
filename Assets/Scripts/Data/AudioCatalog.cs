using UnityEngine;

namespace Survivor.Data
{
    [CreateAssetMenu(menuName="Survivor/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        public AudioClip menuMusic,battleMusic;
        public string[] ids;
        public AudioClip[] clips;
        public AudioClip Get(string id)
        { if(ids==null || clips==null)return null;int i=System.Array.IndexOf(ids,id);return i>=0 && i<clips.Length?clips[i]:null; }
    }
}
