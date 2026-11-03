using System.IO;
using Survivor.Core;
using Survivor.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Survivor.Editor
{
    public static class M1ProjectSetup
    {
        [MenuItem("Survivor/Create M1 Assets")]
        public static void CreateAssets()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Stop Play Mode and save scene edits first.");
            Directory.CreateDirectory("Assets/Data/Flow");
            Directory.CreateDirectory("Assets/Input");
            AssetDatabase.Refresh();
            var config = AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FlowConfig>();
                string[] ids = { "bolt", "orbit", "blast", "homing", "pulse" };
                config.weapons = new WeaponDefinition[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                    weapon.id = ids[i]; weapon.nameKey = "weapon." + ids[i]; weapon.descriptionKey = weapon.nameKey + ".desc";
                    weapon.pattern = (WeaponPattern)i;
                    AssetDatabase.CreateAsset(weapon, "Assets/Data/Flow/" + ids[i] + ".asset");
                    config.weapons[i] = weapon;
                }
                AssetDatabase.CreateAsset(config, "Assets/Data/Flow/FlowConfig.asset");
            }
            const string inputPath = "Assets/Input/GameInput.inputactions";
            if (!File.Exists(inputPath))
            {
                var input = ScriptableObject.CreateInstance<InputActionAsset>();
                var gameplay = input.AddActionMap("Gameplay");
                var move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
                move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
                input.AddActionMap("Global").AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
                File.WriteAllText(inputPath, input.ToJson()); Object.DestroyImmediate(input);
                AssetDatabase.ImportAsset(inputPath);
            }
            const string menuPath = "Assets/Scenes/MainMenu.unity";
            if (!File.Exists(menuPath))
            {
                var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Camera("MenuCamera");
                new GameObject("GameFlow").AddComponent<GameFlow>();
                EditorSceneManager.SaveScene(menu, menuPath);
            }
            const string battlePath = "Assets/Scenes/Battle.unity";
            if (!File.Exists(battlePath))
            {
                var battle = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Camera("BattleCamera"); new GameObject("BattleRoot");
                EditorSceneManager.SaveScene(battle, battlePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(menuPath, true), new EditorBuildSettingsScene(battlePath, true) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(menuPath, OpenSceneMode.Single);
            // Reacquire persistent assets after imports; don't serialize transient creation handles.
            var root = Object.FindFirstObjectByType<GameFlow>();
            var serialized = new SerializedObject(root);
            serialized.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FlowConfig>("Assets/Data/Flow/FlowConfig.asset");
            serialized.FindProperty("inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
            serialized.FindProperty("textTable").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Localization/FlowText.json");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        private static void Camera(string name)
        {
            var go = new GameObject(name, typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera"; go.transform.position = new Vector3(0, 0, -10);
            var camera = go.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 8;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
        }
    }
}
