using System;
using System.IO;
using Survivor.Combat;
using Survivor.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Survivor.Editor
{
    public static class M7ProjectSetup
    {
        [MenuItem("Survivor/Create M7 Assets")]
        public static void CreateAssets()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Stop Play Mode and save scene edits first.");
            var battle = AssetDatabase.LoadAssetAtPath<BattleConfig>("Assets/Data/Battle/BattleConfig.asset");
            var encounters = AssetDatabase.LoadAssetAtPath<EncounterConfig>("Assets/Data/Battle/EncounterConfig.asset");
            if (encounters == null) { encounters = ScriptableObject.CreateInstance<EncounterConfig>(); AssetDatabase.CreateAsset(encounters, "Assets/Data/Battle/EncounterConfig.asset"); }
            encounters.ranged = Definition("Ranged", EnemyKind.Ranged, 38, 1.8f, 12, 0.45f, new Color(0.7f, 0.4f, 1), 6, 0.4f, 2);
            encounters.bomber = Definition("Bomber", EnemyKind.Bomber, 22, 2.7f, 18, 0.4f, new Color(1, 0.65f, 0.15f), 5, 0.4f, 2);
            encounters.bomber.attackRange = 2.3f; encounters.bomber.windupSeconds = 1; encounters.bomber.blastRadius = 2.5f;
            encounters.elite = Definition("Elite", EnemyKind.Elite, 500, 2.65f, 24, 0.7f, new Color(1, 0.3f, 0.75f), 45, 1, 35);
            encounters.boss = Definition("Boss", EnemyKind.Boss, 2600, 1.5f, 30, 1.1f, new Color(0.55f, 0.25f, 0.9f), 180, 1, 120);
            encounters.boss.attackCooldown = 3; encounters.boss.windupSeconds = 1; encounters.boss.blastRadius = 3.5f; encounters.boss.projectileSpeed = 5;
            EditorUtility.SetDirty(encounters.bomber); EditorUtility.SetDirty(encounters.boss); EditorUtility.SetDirty(encounters);
            battle.encounters = encounters; EditorUtility.SetDirty(battle);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0)
            {
                var layers = settings.FindProperty("layers");
                for (int i = 8; i < layers.arraySize; i++)
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { playerLayer = i; layers.GetArrayElementAtIndex(i).stringValue = "Player"; break; }
                if (playerLayer < 0) throw new InvalidOperationException("No free layer for Player");
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var player = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player/Player.prefab");
            try { player.layer = playerLayer; PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player/Player.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            const string projectilePath = "Assets/Prefabs/Combat/HostileProjectile.prefab";
            if (!File.Exists(projectilePath))
            {
                var root = new GameObject("HostileProjectile", typeof(HostileProjectileActor));
                var visual = new GameObject("Body", typeof(SpriteRenderer)); visual.transform.SetParent(root.transform, false); visual.transform.localScale = Vector3.one * 0.36f;
                var body = visual.GetComponent<SpriteRenderer>(); body.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholders/Circle.png");
                body.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlaceholderUnlit.mat"); body.color = new Color(1, 0.35f, 0.1f); body.sortingOrder = 16;
                root.GetComponent<HostileProjectileActor>().body = body; root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, projectilePath); Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var world = Object.FindFirstObjectByType<BattleWorld>(); world.hostileProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectilePath).GetComponent<HostileProjectileActor>();
            EditorUtility.SetDirty(world); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        private static EnemyDefinition Definition(string name, EnemyKind kind, int hp, float speed, float damage, float radius, Color color, int xp, float chance, int coins)
        {
            string path = "Assets/Data/Battle/" + name + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (definition == null) { definition = ScriptableObject.CreateInstance<EnemyDefinition>(); AssetDatabase.CreateAsset(definition, path); }
            definition.kind = kind; definition.nameKey = "enemy." + name.ToLowerInvariant(); definition.maxHealth = hp; definition.moveSpeed = speed;
            definition.contactDamage = damage; definition.radius = radius; definition.color = color; definition.experience = xp; definition.currencyChance = chance; definition.currencyValue = coins;
            EditorUtility.SetDirty(definition); return definition;
        }
    }
}
