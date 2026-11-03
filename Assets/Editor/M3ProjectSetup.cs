using System.IO;
using Survivor.Combat;
using Survivor.Data;
using Survivor.Drop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Survivor.Editor
{
    public static class M3ProjectSetup
    {
        [MenuItem("Survivor/Create M3 Assets")]
        public static void CreateAssets()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Stop Play Mode and save edits first.");
            Directory.CreateDirectory("Assets/Data/Progression"); Directory.CreateDirectory("Assets/Prefabs/Pickups"); AssetDatabase.Refresh();
            var config = AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/Data/Progression/ProgressionConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ProgressionConfig>();
                config.abilities = new[]
                {
                    Ability("bolt", UpgradeCategory.Weapon, null, Effect("bolt.damage", UpgradeEffect.WeaponDamage, 0.25f), Effect("bolt.pierce", UpgradeEffect.Pierce, 1)),
                    Ability("bloom", UpgradeCategory.Passive, Effect("bloom.new", UpgradeEffect.RegenAmount, 2), Effect("bloom.amount", UpgradeEffect.RegenAmount, 2), Effect("bloom.interval", UpgradeEffect.RegenInterval, 0.5f)),
                    Ability("stat.health", UpgradeCategory.Attribute, null, Effect("stat.health", UpgradeEffect.Health, 20)),
                    Ability("stat.attack", UpgradeCategory.Attribute, null, Effect("stat.attack", UpgradeEffect.Attack, 2)),
                    Ability("stat.speed", UpgradeCategory.Attribute, null, Effect("stat.speed", UpgradeEffect.AttackSpeed, 0.12f)),
                    Ability("stat.move", UpgradeCategory.Attribute, null, Effect("stat.move", UpgradeEffect.MoveSpeed, 0.5f)),
                    Ability("stat.defense", UpgradeCategory.Attribute, null, Effect("stat.defense", UpgradeEffect.Defense, 3))
                };
                config.specialRewards = new[] { Effect("reward.coins", UpgradeEffect.CurrencyReward, 5), Effect("reward.heal", UpgradeEffect.HealReward, 25) };
                AssetDatabase.CreateAsset(config, "Assets/Data/Progression/ProgressionConfig.asset");
            }
            var tagSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            int layer = LayerMask.NameToLayer("Enemy");
            if (layer < 0)
            {
                var layers = tagSettings.FindProperty("layers");
                for (int i = 8; i < layers.arraySize; i++)
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layer = i; layers.GetArrayElementAtIndex(i).stringValue = "Enemy"; break; }
                if (layer < 0) throw new System.InvalidOperationException("No free layer for Enemy");
                tagSettings.ApplyModifiedPropertiesWithoutUndo();
            }
            var enemy = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Enemies/Melee.prefab");
            try { enemy.layer = layer; PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/Prefabs/Enemies/Melee.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(enemy); }
            const string pickupPath = "Assets/Prefabs/Pickups/Pickup.prefab";
            if (!File.Exists(pickupPath))
            {
                var root = new GameObject("Pickup", typeof(PickupActor), typeof(CircleCollider2D));
                var pickup = root.GetComponent<PickupActor>();
                pickup.hitbox = root.GetComponent<CircleCollider2D>(); pickup.hitbox.isTrigger = true; pickup.hitbox.radius = config.pickupRadius;
                pickup.experienceSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholders/Square.png");
                pickup.currencySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholders/Circle.png");
                var body = new GameObject("Body", typeof(SpriteRenderer)); body.transform.SetParent(root.transform, false); body.transform.localScale = Vector3.one * 0.3f;
                pickup.body = body.GetComponent<SpriteRenderer>(); pickup.body.sprite = pickup.experienceSprite; pickup.body.sortingOrder = 5;
                pickup.body.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlaceholderUnlit.mat");
                root.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, pickupPath); Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            var battle = AssetDatabase.LoadAssetAtPath<BattleConfig>("Assets/Data/Battle/BattleConfig.asset");
            battle.progression = AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/Data/Progression/ProgressionConfig.asset");
            EditorUtility.SetDirty(battle); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var world = Object.FindFirstObjectByType<BattleWorld>();
            world.pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pickupPath).GetComponent<PickupActor>();
            EditorUtility.SetDirty(world); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        private static UpgradeEffectDefinition Effect(string id, UpgradeEffect kind, float amount)
            => new UpgradeEffectDefinition { id = id, effect = kind, amount = amount, nameKey = "upgrade." + id, descriptionKey = "upgrade." + id + ".desc" };
        private static UpgradeDefinition Ability(string id, UpgradeCategory category, UpgradeEffectDefinition acquisition, params UpgradeEffectDefinition[] effects)
        {
            string path = "Assets/Data/Progression/" + id + ".asset";
            var ability = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (ability != null) return ability;
            ability = ScriptableObject.CreateInstance<UpgradeDefinition>(); ability.id = id; ability.category = category;
            ability.acquisition = acquisition; ability.effects = effects;
            AssetDatabase.CreateAsset(ability, path); return ability;
        }
    }
}
