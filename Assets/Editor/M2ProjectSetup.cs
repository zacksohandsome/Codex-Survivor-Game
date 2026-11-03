using System.IO;
using Survivor.Combat;
using Survivor.Data;
using Survivor.Enemy;
using Survivor.Player;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Survivor.Editor
{
    public static class M2ProjectSetup
    {
        [MenuItem("Survivor/Create M2 Assets")]
        public static void CreateAssets()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Stop Play Mode and save scene edits first.");
            foreach (var folder in new[] { "Assets/Art/Placeholders", "Assets/Prefabs/Player", "Assets/Prefabs/Enemies", "Assets/Prefabs/Combat", "Assets/Data/Battle", "Assets/Materials" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var circle = CreateSprite("Circle", true); var square = CreateSprite("Square", false);
            const string materialPath = "Assets/Materials/PlaceholderUnlit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var config = AssetDatabase.LoadAssetAtPath<BattleConfig>("Assets/Data/Battle/BattleConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BattleConfig>();
                AssetDatabase.CreateAsset(config, "Assets/Data/Battle/BattleConfig.asset");
            }
            var enemyData = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Data/Battle/Melee.asset");
            if (enemyData == null)
            {
                enemyData = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(enemyData, "Assets/Data/Battle/Melee.asset");
            }
            config.enemy = enemyData;
            config.weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Data/Flow/bolt.asset");
            config.weapon.combatAvailable = true;
            EditorUtility.SetDirty(config.weapon); EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            var player = PlayerPrefab(circle, material);
            var enemy = EnemyPrefab(circle, square, material);
            var projectile = ProjectilePrefab(circle, material);
            var number = NumberPrefab();
            EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var root = GameObject.Find("BattleRoot");
            var world = root.GetComponent<BattleWorld>();
            if (world == null) world = root.AddComponent<BattleWorld>();
            world.config = AssetDatabase.LoadAssetAtPath<BattleConfig>("Assets/Data/Battle/BattleConfig.asset");
            world.playerPrefab = player; world.enemyPrefab = enemy; world.projectilePrefab = projectile; world.damageNumberPrefab = number;
            world.battleCamera = Camera.main;
            if (GameObject.Find("Arena") == null)
            {
                var arena = new GameObject("Arena").transform;
                Visual("Floor", arena, square, material, new Color(0.045f, 0.07f, 0.10f), new Vector2(48, 30), -20);
                for (int x = -22; x <= 22; x += 2)
                    Visual("GridX", arena, square, material, new Color(0.12f, 0.18f, 0.22f), new Vector2(0.025f, 30), -19).transform.localPosition = new Vector3(x, 0);
                for (int y = -14; y <= 14; y += 2)
                    Visual("GridY", arena, square, material, new Color(0.12f, 0.18f, 0.22f), new Vector2(48, 0.025f), -19).transform.localPosition = new Vector3(0, y);
                foreach (int sign in new[] { -1, 1 })
                {
                    Visual("WallX", arena, square, material, new Color(0.2f, 0.5f, 0.6f), new Vector2(0.12f, 30), -18).transform.localPosition = new Vector3(sign * 24, 0);
                    Visual("WallY", arena, square, material, new Color(0.2f, 0.5f, 0.6f), new Vector2(48, 0.12f), -18).transform.localPosition = new Vector3(0, sign * 15);
                }
            }
            EditorUtility.SetDirty(world); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        private static Sprite CreateSprite(string name, bool circle)
        {
            string path = "Assets/Art/Placeholders/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color[32 * 32];
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                    pixels[y * 32 + x] = !circle || new Vector2(x - 15.5f, y - 15.5f).sqrMagnitude <= 240 ? Color.white : Color.clear;
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static SpriteRenderer Visual(string name, Transform parent, Sprite sprite, Material material, Color color, Vector2 size, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false); go.transform.localScale = new Vector3(size.x, size.y, 1);
            var visual = go.GetComponent<SpriteRenderer>(); visual.sprite = sprite; visual.sharedMaterial = material; visual.color = color; visual.sortingOrder = order;
            return visual;
        }
        private static T Save<T>(GameObject go, string path) where T : Component
        {
            go.SetActive(false); var asset = PrefabUtility.SaveAsPrefabAsset(go, path); Object.DestroyImmediate(go); return asset.GetComponent<T>();
        }
        private static PlayerActor PlayerPrefab(Sprite circle, Material material)
        {
            const string path = "Assets/Prefabs/Player/Player.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing.GetComponent<PlayerActor>();
            var go = new GameObject("Player", typeof(PlayerActor), typeof(CircleCollider2D));
            var actor = go.GetComponent<PlayerActor>(); actor.hitbox = go.GetComponent<CircleCollider2D>(); actor.hitbox.isTrigger = true;
            actor.body = Visual("Body", go.transform, circle, material, new Color(0.15f, 0.9f, 0.9f), Vector2.one * 0.9f, 10);
            return Save<PlayerActor>(go, path);
        }
        private static EnemyActor EnemyPrefab(Sprite circle, Sprite square, Material material)
        {
            const string path = "Assets/Prefabs/Enemies/Melee.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing.GetComponent<EnemyActor>();
            var go = new GameObject("Melee", typeof(EnemyActor), typeof(CircleCollider2D));
            var actor = go.GetComponent<EnemyActor>(); actor.hitbox = go.GetComponent<CircleCollider2D>(); actor.hitbox.isTrigger = true;
            actor.body = Visual("Body", go.transform, circle, material, new Color(0.95f, 0.3f, 0.3f), Vector2.one * 0.9f, 9);
            actor.warning = Visual("SpawnWarning", go.transform, circle, material, new Color(1, 0.65f, 0.1f, 0.6f), Vector2.one * 1.2f, 15);
            actor.healthBar = new GameObject("HealthBar").transform; actor.healthBar.SetParent(go.transform, false);
            actor.healthBar.localPosition = new Vector3(0, 0.75f); actor.healthBar.localScale = new Vector3(0.9f, 0.1f, 1);
            Visual("Background", actor.healthBar, square, material, Color.black, Vector2.one, 11);
            actor.healthFill = Visual("Fill", actor.healthBar, square, material, new Color(0.3f, 1, 0.3f), Vector2.one, 12).transform;
            return Save<EnemyActor>(go, path);
        }
        private static ProjectileActor ProjectilePrefab(Sprite circle, Material material)
        {
            const string path = "Assets/Prefabs/Combat/ArcaneBolt.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing.GetComponent<ProjectileActor>();
            var go = new GameObject("ArcaneBolt", typeof(ProjectileActor));
            Visual("Body", go.transform, circle, material, new Color(0.5f, 0.85f, 1), Vector2.one * 0.24f, 14);
            return Save<ProjectileActor>(go, path);
        }
        private static DamageNumber NumberPrefab()
        {
            const string path = "Assets/Prefabs/Combat/DamageNumber.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing.GetComponent<DamageNumber>();
            var go = new GameObject("DamageNumber", typeof(TextMeshPro), typeof(DamageNumber));
            var label = go.GetComponent<TextMeshPro>(); label.font = TMP_Settings.defaultFontAsset; label.fontSize = 4;
            label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(3, 1);
            go.GetComponent<MeshRenderer>().sortingOrder = 50; go.GetComponent<DamageNumber>().label = label;
            go.transform.localScale = Vector3.one * 0.6f;
            return Save<DamageNumber>(go, path);
        }
    }
}
