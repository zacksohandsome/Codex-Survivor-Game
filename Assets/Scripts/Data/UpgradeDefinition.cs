using UnityEngine;

namespace Survivor.Data
{
    [CreateAssetMenu(menuName = "Survivor/Upgrade Ability")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        public string id;
        public UpgradeCategory category;
        public int maxLevel = 5;
        public UpgradeEffectDefinition acquisition;
        public UpgradeEffectDefinition[] effects;
        // Unity inline serialization turns a null class field into an empty instance.
        public bool CanAcquire => acquisition != null && !string.IsNullOrEmpty(acquisition.id);
    }
}
