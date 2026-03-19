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

            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
            {
                return;
            }

            if (cameraData.cameraType != CameraType.Game || cameraData.renderType != CameraRenderType.Base)
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

                if (cameraData.cameraType != CameraType.Game || cameraData.renderType != CameraRenderType.Base)
                {
                    return;
                }

                var blurAmount = PeripheralScopeBlurRuntimeState.BlurAmount;
                if (blurAmount <= 0.001f || PeripheralScopeBlurRuntimeState.BlendAlpha <= 0.001f)
                {
                    return;
                }

                _material.SetFloat(BlurStrengthId, blurAmount);
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

                textureDesc.name = "_PeripheralScopeBlurA";
                var blurA = renderGraph.CreateTexture(textureDesc);
                textureDesc.name = "_PeripheralScopeBlurB";
                var blurB = renderGraph.CreateTexture(textureDesc);

                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(source, blurA, _material, 0),
                    "PeripheralScopeBlur Downsample");
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(blurA, blurB, _material, 1),
                    "PeripheralScopeBlur Horizontal");
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(blurB, blurA, _material, 2),
                    "PeripheralScopeBlur Vertical");
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(blurA, resourceData.activeColorTexture, _material, 3),
                    "PeripheralScopeBlur Composite");
            }
        }
    }
}
