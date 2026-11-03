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
    }
}
