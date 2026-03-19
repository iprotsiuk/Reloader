Shader "Hidden/Reloader/PeripheralScopeBlurComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Downsample"
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCopy

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 FragCopy(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "BlurHorizontal"
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurHorizontal

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurStrength;
            float _BlurSampleRadius;

            float4 SampleBlur(float2 uv, float2 axis)
            {
                const float weight0 = 0.22702703;
                const float weight1 = 0.31621622;
                const float weight2 = 0.07027027;

                float radius = max(_BlurSampleRadius, 0.01);
                float2 offset = axis * _BlitTexture_TexelSize.xy * radius;

                float4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0) * weight0;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset * 1.3846154, 0) * weight1;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - offset * 1.3846154, 0) * weight1;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset * 3.2307692, 0) * weight2;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - offset * 3.2307692, 0) * weight2;
                return color;
            }

            float4 FragBlurHorizontal(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SampleBlur(input.texcoord.xy, float2(1.0, 0.0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "BlurVertical"
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurVertical

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurStrength;
            float _BlurSampleRadius;

            float4 SampleBlur(float2 uv, float2 axis)
            {
                const float weight0 = 0.22702703;
                const float weight1 = 0.31621622;
                const float weight2 = 0.07027027;

                float radius = max(_BlurSampleRadius, 0.01);
                float2 offset = axis * _BlitTexture_TexelSize.xy * radius;

                float4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0) * weight0;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset * 1.3846154, 0) * weight1;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - offset * 1.3846154, 0) * weight1;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + offset * 3.2307692, 0) * weight2;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - offset * 3.2307692, 0) * weight2;
                return color;
            }

            float4 FragBlurVertical(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SampleBlur(input.texcoord.xy, float2(0.0, 1.0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlendAlpha;
            float2 _CenterSize;
            float _SoftEdgeNormalized;

            float ComputePeripheralMask(float2 uv)
            {
                float2 centerHalfExtents = max(_CenterSize * 0.5, float2(0.001, 0.001));
                float2 distanceFromCenter = abs(uv - 0.5) - centerHalfExtents;
                float edgeDistance = max(distanceFromCenter.x, distanceFromCenter.y);
                return smoothstep(0.0, max(_SoftEdgeNormalized, 0.001), edgeDistance);
            }

            float4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 blurred = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, 0);
                float mask = ComputePeripheralMask(input.texcoord.xy) * saturate(_BlendAlpha);
                return float4(blurred.rgb, mask);
            }
            ENDHLSL
        }
    }
}
