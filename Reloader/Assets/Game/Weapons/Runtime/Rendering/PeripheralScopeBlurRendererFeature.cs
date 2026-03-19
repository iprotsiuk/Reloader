using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Reloader.Game.Weapons.Rendering
{
    public sealed class PeripheralScopeBlurRendererFeature : ScriptableRendererFeature
    {
        private const string DefaultShaderName = "Hidden/Reloader/PeripheralScopeBlurComposite";

        [SerializeField] private Shader _compositeShader;
        [SerializeField] private RenderPassEvent _injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        private Material _material;
        private PeripheralScopeBlurPass _pass;

        public override void Create()
        {
            if (_compositeShader == null)
            {
                _compositeShader = Shader.Find(DefaultShaderName);
            }

            if (_compositeShader != null)
            {
                _material = CoreUtils.CreateEngineMaterial(_compositeShader);
            }

            _pass ??= new PeripheralScopeBlurPass();
            _pass.renderPassEvent = _injectionPoint;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            if (_material == null)
            {
                return;
            }

            if (!ShouldEnqueueForCamera(cameraData.camera, cameraData.cameraType, cameraData.renderType == CameraRenderType.Base))
            {
                return;
            }

            _pass.Setup(_material);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }

        private sealed class PeripheralScopeBlurPass : ScriptableRenderPass
        {
            private static readonly int BlurStrengthId = Shader.PropertyToID("_BlurStrength");
            private static readonly int BlurSampleRadiusId = Shader.PropertyToID("_BlurSampleRadius");
            private static readonly int BlendAlphaId = Shader.PropertyToID("_BlendAlpha");
            private static readonly int CenterSizeId = Shader.PropertyToID("_CenterSize");
            private static readonly int SoftEdgeId = Shader.PropertyToID("_SoftEdgeNormalized");

            private Material _material;

            public PeripheralScopeBlurPass()
            {
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material)
            {
                _material = material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_material == null || !PeripheralScopeBlurRuntimeState.IsActive)
                {
                    return;
                }

                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                if (!ShouldEnqueueForCamera(cameraData.camera, cameraData.cameraType, cameraData.renderType == CameraRenderType.Base))
                {
                    return;
                }

                var blurAmount = PeripheralScopeBlurRuntimeState.BlurAmount;
                if (blurAmount <= 0.001f || PeripheralScopeBlurRuntimeState.BlendAlpha <= 0.001f)
                {
                    return;
                }

                _material.SetFloat(BlurStrengthId, blurAmount);
                _material.SetFloat(BlurSampleRadiusId, ResolveBlurSampleRadius(blurAmount));
                _material.SetFloat(BlendAlphaId, PeripheralScopeBlurRuntimeState.BlendAlpha);
                _material.SetVector(
                    CenterSizeId,
                    new Vector4(
                        PeripheralScopeBlurRuntimeState.CenterWidthNormalized,
                        PeripheralScopeBlurRuntimeState.CenterHeightNormalized,
                        0f,
                        0f));
                _material.SetFloat(SoftEdgeId, PeripheralScopeBlurRuntimeState.SoftEdgeNormalized);

                var source = resourceData.activeColorTexture;
                var textureDesc = renderGraph.GetTextureDesc(source);
                textureDesc.clearBuffer = false;
                textureDesc.msaaSamples = MSAASamples.None;
                textureDesc.depthBufferBits = 0;

                var downsampleDivisor = Mathf.Lerp(1.75f, 4f, blurAmount);
                textureDesc.width = Mathf.Max(1, Mathf.RoundToInt(textureDesc.width / downsampleDivisor));
                textureDesc.height = Mathf.Max(1, Mathf.RoundToInt(textureDesc.height / downsampleDivisor));

                textureDesc.name = "_PeripheralScopeBlurLowRes";
                var lowResPeripheral = renderGraph.CreateTexture(textureDesc);
                textureDesc.name = "_PeripheralScopeBlurTemp";
                var intermediateBlur = renderGraph.CreateTexture(textureDesc);

                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(source, lowResPeripheral, _material, 0),
                    "PeripheralScopeBlur Downsample");
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(lowResPeripheral, intermediateBlur, _material, 1),
                    "PeripheralScopeBlur Horizontal");
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(intermediateBlur, lowResPeripheral, _material, 2),
                    "PeripheralScopeBlur Vertical");
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(lowResPeripheral, resourceData.activeColorTexture, _material, 3),
                    "PeripheralScopeBlur Composite");
            }
        }

        private static float ResolveBlurSampleRadius(float blurAmount)
        {
            var clampedBlur = Mathf.Clamp01(blurAmount);
            return Mathf.Lerp(0.35f, 1.05f, clampedBlur);
        }

        private static bool ShouldEnqueueForCamera(Camera camera, CameraType cameraType, bool isBaseCamera)
        {
            if (camera == null)
            {
                return false;
            }

            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            {
                return false;
            }

            if (cameraType != CameraType.Game || !isBaseCamera)
            {
                return false;
            }

            return camera.targetTexture == null;
        }
    }
}
