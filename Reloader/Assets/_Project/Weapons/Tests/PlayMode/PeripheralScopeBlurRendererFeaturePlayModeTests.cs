using System.Reflection;
using NUnit.Framework;
using Reloader.Game.Weapons.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class PeripheralScopeBlurRendererFeaturePlayModeTests
    {
        [Test]
        public void ShouldEnqueueForCamera_SkipsRenderTextureGameBaseCamera()
        {
            var shouldEnqueueMethod = typeof(PeripheralScopeBlurRendererFeature).GetMethod(
                "ShouldEnqueueForCamera",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(shouldEnqueueMethod, Is.Not.Null);

            var cameraGo = new GameObject("ScopeCamera");
            var camera = cameraGo.AddComponent<Camera>();
            var renderTexture = new RenderTexture(256, 256, 16);
            camera.targetTexture = renderTexture;

            try
            {
                var result = shouldEnqueueMethod!.Invoke(null, new object[] { camera, CameraType.Game, true });
                Assert.That(result, Is.EqualTo(false));
            }
            finally
            {
                camera.targetTexture = null;
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(cameraGo);
            }
        }

        [Test]
        public void ShouldEnqueueForCamera_AllowsMainGameBaseCameraWithoutTargetTexture()
        {
            var shouldEnqueueMethod = typeof(PeripheralScopeBlurRendererFeature).GetMethod(
                "ShouldEnqueueForCamera",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(shouldEnqueueMethod, Is.Not.Null);

            var cameraGo = new GameObject("WorldCamera");
            var camera = cameraGo.AddComponent<Camera>();

            try
            {
                var result = shouldEnqueueMethod!.Invoke(null, new object[] { camera, CameraType.Game, true });
                Assert.That(result, Is.EqualTo(true));
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
            }
        }

        [Test]
        public void ShouldEnqueueForCamera_SkipsOverlayGameCameraWithoutTargetTexture()
        {
            var shouldEnqueueMethod = typeof(PeripheralScopeBlurRendererFeature).GetMethod(
                "ShouldEnqueueForCamera",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(shouldEnqueueMethod, Is.Not.Null);

            var cameraGo = new GameObject("OverlayCamera");
            var camera = cameraGo.AddComponent<Camera>();

            try
            {
                var result = shouldEnqueueMethod!.Invoke(null, new object[] { camera, CameraType.Game, false });
                Assert.That(result, Is.EqualTo(false));
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
            }
        }

        [Test]
        public void ResolveScopedWorldRenderScale_UsesFullResolutionWhenBlurIsZero()
        {
            var runtimeType = ResolveType("Reloader.Game.Weapons.Rendering.ScopedPeripheralWorldRenderScaleRuntime");
            Assert.That(runtimeType, Is.Not.Null);

            var resolveMethod = runtimeType!.GetMethod(
                "ResolveScopedWorldRenderScale",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null);

            var result = resolveMethod!.Invoke(null, new object[] { 0f });
            Assert.That(result, Is.EqualTo(1f));
        }

        [Test]
        public void ResolveScopedWorldRenderScale_ReachesConfiguredMinimumAtFullBlur()
        {
            var runtimeType = ResolveType("Reloader.Game.Weapons.Rendering.ScopedPeripheralWorldRenderScaleRuntime");
            Assert.That(runtimeType, Is.Not.Null);

            var resolveMethod = runtimeType!.GetMethod(
                "ResolveScopedWorldRenderScale",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null);

            var result = resolveMethod!.Invoke(null, new object[] { 1f });
            Assert.That(result, Is.EqualTo(0.5f));
        }

        [Test]
        public void ResolveBlurSampleRadius_StaysGentleAtFullBlur()
        {
            var resolveMethod = typeof(PeripheralScopeBlurRendererFeature).GetMethod(
                "ResolveBlurSampleRadius",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null);

            var result = resolveMethod!.Invoke(null, new object[] { 1f });
            Assert.That(result, Is.EqualTo(1.05f).Within(0.0001f));
        }

        [Test]
        public void ResolveBlurSampleRadius_UsesSoftRadiusAtMediumBlur()
        {
            var resolveMethod = typeof(PeripheralScopeBlurRendererFeature).GetMethod(
                "ResolveBlurSampleRadius",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null);

            var result = resolveMethod!.Invoke(null, new object[] { 0.5f });
            Assert.That(result, Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void ScopedWorldRenderScaleRuntime_RestoresPipelineSettingsAfterReset()
        {
            var runtimeType = ResolveType("Reloader.Game.Weapons.Rendering.ScopedPeripheralWorldRenderScaleRuntime");
            Assert.That(runtimeType, Is.Not.Null);

            var applyMethod = runtimeType!.GetMethod("ApplyToAsset", BindingFlags.Static | BindingFlags.NonPublic);
            var resetMethod = runtimeType.GetMethod("Reset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(applyMethod, Is.Not.Null);
            Assert.That(resetMethod, Is.Not.Null);

            var pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var originalRenderScale = pipelineAsset.renderScale;
            var originalUpscalingFilter = pipelineAsset.upscalingFilter;
            var previousRenderPipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;

            try
            {
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = pipelineAsset;
                applyMethod!.Invoke(null, new object[] { pipelineAsset, true, 1f });

                Assert.That(pipelineAsset.renderScale, Is.EqualTo(0.5f));
                Assert.That(pipelineAsset.upscalingFilter, Is.EqualTo(UpscalingFilterSelection.Linear));

                resetMethod!.Invoke(null, null);

                Assert.That(pipelineAsset.renderScale, Is.EqualTo(originalRenderScale));
                Assert.That(pipelineAsset.upscalingFilter, Is.EqualTo(originalUpscalingFilter));
            }
            finally
            {
                resetMethod!.Invoke(null, null);
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = previousRenderPipeline;
                Object.DestroyImmediate(pipelineAsset);
            }
        }

        [Test]
        public void ScopedWorldRenderScaleRuntime_RebindingAssetRestoresPreviousAssetState()
        {
            var runtimeType = ResolveType("Reloader.Game.Weapons.Rendering.ScopedPeripheralWorldRenderScaleRuntime");
            Assert.That(runtimeType, Is.Not.Null);

            var applyMethod = runtimeType!.GetMethod("ApplyToAsset", BindingFlags.Static | BindingFlags.NonPublic);
            var resetMethod = runtimeType.GetMethod("Reset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(applyMethod, Is.Not.Null);
            Assert.That(resetMethod, Is.Not.Null);

            var firstAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var secondAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var originalFirstRenderScale = firstAsset.renderScale;
            var originalFirstUpscalingFilter = firstAsset.upscalingFilter;

            try
            {
                resetMethod!.Invoke(null, null);

                applyMethod!.Invoke(null, new object[] { firstAsset, true, 1f });
                Assert.That(firstAsset.renderScale, Is.EqualTo(0.5f));
                Assert.That(firstAsset.upscalingFilter, Is.EqualTo(UpscalingFilterSelection.Linear));

                applyMethod.Invoke(null, new object[] { secondAsset, true, 1f });

                Assert.That(firstAsset.renderScale, Is.EqualTo(originalFirstRenderScale));
                Assert.That(firstAsset.upscalingFilter, Is.EqualTo(originalFirstUpscalingFilter));
                Assert.That(secondAsset.renderScale, Is.EqualTo(0.5f));
                Assert.That(secondAsset.upscalingFilter, Is.EqualTo(UpscalingFilterSelection.Linear));
            }
            finally
            {
                resetMethod!.Invoke(null, null);
                Object.DestroyImmediate(firstAsset);
                Object.DestroyImmediate(secondAsset);
            }
        }

        private static System.Type ResolveType(string fullName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
