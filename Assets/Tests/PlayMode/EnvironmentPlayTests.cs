using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace Survivor.Tests
{
    public class EnvironmentPlayTests
    {
        [UnityTest]
        public IEnumerator PlayerLoopAdvancesAndUrp2DCameraRenders()
        {
            bool previousBackground = Application.runInBackground;
            var cameraObject = new GameObject("M0 temporary render probe", typeof(Camera));
            var target = new RenderTexture(64, 64, 16);
            try
            {
                Application.runInBackground = true;
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.targetTexture = target;
                int initialFrame = Time.frameCount;
                yield return null;
                yield return null;
                Assert.That(Time.frameCount, Is.GreaterThan(initialFrame));
                Assert.That(GraphicsSettings.currentRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
                Assert.That(camera.GetUniversalAdditionalCameraData().scriptableRenderer.GetType().FullName,
                    Is.EqualTo("UnityEngine.Rendering.Universal.Renderer2D"));
                Assert.That(target.IsCreated(), Is.True, "Camera must produce an actual render target.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                cameraObject.GetComponent<Camera>().targetTexture = null;
                Object.Destroy(cameraObject);
                target.Release();
                Object.Destroy(target);
                Application.runInBackground = previousBackground;
            }
        }
    }
}
