Shader "Nature/Soft Occlusion Leaves"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Lambert90deg ("Lambert90deg", Range(0,1)) = 0.6
        _Lambert180deg ("Lambert180deg", Range(0,1)) = 0.8
        _AmbientAmount ("AmbientAmount", Range(0,1)) = 0.5
        _Ambient0deg ("Ambient0deg", Range(0,1)) = 0.0
        _Ambient90deg ("Ambient90deg", Range(0,1)) = 0.5
        _Ambient180deg ("Ambient180deg", Range(0,1)) = 1.0
        _OffsetAmbient ("OffsetAmbient", Range(-1,1)) = -0.1
        _Cutoff ("Transparency (Light transmission)", Range(0.01,1)) = 0.5
        _AlphaOffset ("Alpha offset", Range(-1,1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            AlphaToMask On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Lambert90deg;
                float _Lambert180deg;
                float _AmbientAmount;
                float _Ambient0deg;
                float _Ambient90deg;
                float _Ambient180deg;
                float _OffsetAmbient;
                float _Cutoff;
                float _AlphaOffset;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float alpha = tex.a * _Color.a + _AlphaOffset;
                clip(alpha - _Cutoff);

                float faceSign = IS_FRONT_VFACE(face, 1.0, -1.0);
                float3 normalWS = normalize(IN.normalWS * faceSign);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float ndl = clamp(dot(normalWS, mainLight.direction), -1.0, 1.0);

                float diff = ndl >= 0.0 ? lerp(_Lambert90deg, 1.0, ndl) : lerp(_Lambert90deg, _Lambert180deg, -ndl);
                float amb = ndl >= 0.0 ? lerp(_Ambient90deg, _Ambient0deg, ndl) : lerp(_Ambient90deg, _Ambient180deg, -ndl);

                float lightTerm = max(0.0, _OffsetAmbient + (mainLight.distanceAttenuation * mainLight.shadowAttenuation) + (_AmbientAmount * amb));
                float3 lit = tex.rgb * _Color.rgb * mainLight.color * (diff * lightTerm);

                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Punctual shadow caster position is provided by URP at draw time.
            // Directional light direction comes from _MainLightPosition.xyz.
            float3 _LightPosition;

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Cutoff;
                float _AlphaOffset;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 GetShadowPositionHClip(Attributes IN)
            {
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = normalize(_MainLightPosition.xyz);
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = GetShadowPositionHClip(IN);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 ShadowPassFragment(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float alpha = tex.a * _Color.a + _AlphaOffset;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
