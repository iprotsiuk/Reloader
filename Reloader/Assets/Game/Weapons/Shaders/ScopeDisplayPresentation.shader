Shader "Reloader/Weapons/ScopeDisplayPresentation"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _EdgeStart("Edge Band Outer", Range(0.0, 0.5)) = 0.14
        _EdgeEnd("Edge Band Inner", Range(0.0, 0.5)) = 0.05
        _EdgeBlurStrength("Edge Blur Strength", Range(0.0, 1.0)) = 0.8
        _EdgeDistortionStrength("Edge Distortion Strength", Range(0.0, 0.1)) = 0.02
        _EdgeVignetteStrength("Edge Vignette Strength", Range(0.0, 1.0)) = 0.16

        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Cull [_Cull]
        ZWrite [_ZWrite]

        Pass
        {
            Name "ScopeDisplayPresentation"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseMap_TexelSize;
                float4 _BaseColor;
                float4 _Color;
                float _EdgeStart;
                float _EdgeEnd;
                float _EdgeBlurStrength;
                float _EdgeDistortionStrength;
                float _EdgeVignetteStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float4 SampleScopeDisplay(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
            }

            float SignedDistanceToHexEdge(float2 localPoint, float radius)
            {
                const float3 k = float3(-0.8660254, 0.5, 0.57735026);
                float2 foldedPoint = abs(localPoint);
                foldedPoint -= 2.0 * min(dot(k.xy, foldedPoint), 0.0) * k.xy;
                foldedPoint -= float2(clamp(foldedPoint.x, -k.z * radius, k.z * radius), radius);
                return length(foldedPoint) * sign(foldedPoint.y);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float2 centeredHexUv = uv - 0.5;
                float distanceFromHexEdge = max(0.0, -SignedDistanceToHexEdge(centeredHexUv, 0.5));
                float innerEdge = max(0.0, min(_EdgeStart, _EdgeEnd));
                float outerEdge = max(_EdgeStart, _EdgeEnd);
                float edgeMask = 1.0 - smoothstep(innerEdge, max(outerEdge, innerEdge + 0.001), distanceFromHexEdge);
                float rimMask = saturate(pow(edgeMask, 0.85));

                float2 distortionOffset = centeredHexUv * (_EdgeDistortionStrength * rimMask);
                float2 distortedUv = saturate(uv - distortionOffset);
                float2 blurStep = _BaseMap_TexelSize.xy * (4.0 + 14.0 * rimMask);

                float4 center = SampleScopeDisplay(distortedUv);
                float4 blurred = center * 0.22;
                blurred += SampleScopeDisplay(saturate(distortedUv + float2(blurStep.x, 0.0))) * 0.14;
                blurred += SampleScopeDisplay(saturate(distortedUv - float2(blurStep.x, 0.0))) * 0.14;
                blurred += SampleScopeDisplay(saturate(distortedUv + float2(0.0, blurStep.y))) * 0.14;
                blurred += SampleScopeDisplay(saturate(distortedUv - float2(0.0, blurStep.y))) * 0.14;
                blurred += SampleScopeDisplay(saturate(distortedUv + blurStep)) * 0.09;
                blurred += SampleScopeDisplay(saturate(distortedUv - blurStep)) * 0.09;
                blurred += SampleScopeDisplay(saturate(distortedUv + float2(blurStep.x, -blurStep.y))) * 0.09;
                blurred += SampleScopeDisplay(saturate(distortedUv + float2(-blurStep.x, blurStep.y))) * 0.09;

                float blurLerp = saturate(rimMask * _EdgeBlurStrength);
                float darkenMask = saturate(pow(rimMask, 1.8));
                float4 color = lerp(center, blurred, blurLerp);
                color.rgb *= lerp(1.0, 1.0 - _EdgeVignetteStrength, darkenMask);
                color *= _BaseColor * _Color;
                return color;
            }
            ENDHLSL
        }
    }
}
