using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Survivor.Tests
{
    public class EnvironmentTests
    {
        [Test]
        public void RequiredEditorAndWindowsTargetAreAvailable()
        {
            Assert.That(Application.unityVersion, Is.EqualTo("6000.3.27f2"));
            Assert.That(EditorUserBuildSettings.activeBuildTarget, Is.EqualTo(BuildTarget.StandaloneWindows64));
            Assert.That(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64), Is.True);
        }

        [Test]
        public void EveryQualityLevelUsesUrpWithA2DRenderer()
        {
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                var pipeline = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                Assert.That(pipeline, Is.Not.Null, QualitySettings.names[i]);
                // Renderer2D is internal in URP 17; inspect the active concrete type.
                Assert.That(pipeline.scriptableRenderer.GetType().FullName,
                    Is.EqualTo("UnityEngine.Rendering.Universal.Renderer2D"), QualitySettings.names[i]);
            }
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
        }

        [Test]
        public void NewInputSystemAndExistingActionAssetAreUsable()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Assert.That(settings.FindProperty("activeInputHandler").intValue, Is.EqualTo(1));
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            Assert.That(actions, Is.Not.Null);
            Assert.That(actions.actionMaps.Count, Is.GreaterThan(0));
            Assert.That(InputSystem.settings, Is.Not.Null);
        }
    }
}
