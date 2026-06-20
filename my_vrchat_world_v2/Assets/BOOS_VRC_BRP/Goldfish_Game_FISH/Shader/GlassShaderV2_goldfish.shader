Shader "Custom/GlassShaderV2_goldfish" {
    Properties {
        [Header(Base Color and Transparency)]
        _Albedo_Base_Color ("Albedo Base Color", Color) = (0,0,0,0)
        _Glass_Color ("Glass Tint Color", Color) = (1,1,1,1)
        _Exposure ("Exposure", Range(0, 2)) = 1.0

        [Header(Refraction and Depth)]
        [Toggle] _Use_Distortion ("Use Distortion", Float ) = 1
        _Distortion ("Distortion Strength", Range(0, 5)) = 0.5
        _GlassThickness ("Glass Thickness (Depth)", Range(0.1, 10)) = 1.0
        _ChromaticAberration ("Chromatic Aberration (RGB Shift)", Range(0, 0.1)) = 0.02

        [Header(Underwater Effect)]
        [Toggle] _Use_Underwater ("Enable Underwater Effect", Float) = 0
        _Water_Color ("Water Tint Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _Water_Wobble_Speed ("Wobble Speed", Range(0, 5)) = 1.0
        
        _Water_Wobble_Strength ("Wobble Strength", Range(0, 1.0)) = 0.05
        _Water_Wobble_Scale ("Wobble Scale", Range(0, 50)) = 15.0

        [Header(Reflections and Fresnel)]
        [NoScaleOffset] _Cubemap ("Reflection Cubemap", Cube) = "black" {}
        _ReflectionIntensity ("Cubemap Intensity", Range(0, 5)) = 1.0
        _Reflection_Color ("Reflection Color Tint", Color) = (1,1,1,1)
        _FresnelPower ("Fresnel Power (Edge Reflection)", Range(0.1, 5.0)) = 2.0
        
        [Header(Surface and Imperfections)]
        _Specular ("Specular Intensity", Range(0, 1)) = 0.4
        _Gloss ("Base Glossiness", Range(0, 1)) = 0.96
        _SurfaceMap ("Surface Map (Smudges/Scratches)", 2D) = "white" {}
        _SurfaceMapIntensity ("Surface Map Strength", Range(0, 1)) = 0.5

        [Header(Normal Map)]
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalIntensity ("Normal Intensity", Range(0, 1)) = 1

        [Header(Matcap (Optional))]
        [Toggle] _UseMatcap ("Enable Matcap", Float) = 0
        _Matcap ("Matcap Texture", 2D) = "black" {}
        _Matcap_Strength ("Matcap Strength", Range(0, 1)) = 0.18
        _Matcap_Scale ("Matcap Scale", Range(0, 5)) = 0.5
        _Emission_Base_Color ("Matcap Tint", Color) = (0,0,0,1)

        [Header(Extra)]
        _Texture ("Overlay Texture", 2D) = "black" {}
        _TextureOpacity ("Texture Opacity", Range(0, 1)) = 0
        _Incident_Color ("Incident Color", Color) = (1,1,1,1)

        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite Mode", Float) = 0
    }
    
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "VRCFallback" = "Hidden"
        }
        GrabPass{ }
        
        Pass {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            ZWrite [_ZWrite]
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define _GLOSSYENV 1
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma target 3.0

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_GrabTexture);
            uniform float _Use_Distortion;
            uniform float _Distortion;
            uniform float _GlassThickness;
            uniform float _ChromaticAberration;

            uniform float _Use_Underwater;
            uniform float4 _Water_Color;
            uniform float _Water_Wobble_Speed;
            uniform float _Water_Wobble_Strength;
            uniform float _Water_Wobble_Scale;

            uniform samplerCUBE _Cubemap;
            uniform float _ReflectionIntensity;
            uniform float _FresnelPower;
            uniform float4 _Reflection_Color;

            uniform float _Specular;
            uniform float _Gloss;
            uniform sampler2D _SurfaceMap; uniform float4 _SurfaceMap_ST;
            uniform float _SurfaceMapIntensity;

            uniform float _UseMatcap;
            uniform sampler2D _Matcap; uniform float4 _Matcap_ST;
            uniform float _Matcap_Scale;
            uniform float _Matcap_Strength;
            uniform float4 _Emission_Base_Color;

            uniform float4 _Albedo_Base_Color;
            uniform float4 _Glass_Color;
            uniform float4 _Incident_Color;
            uniform sampler2D _NormalMap; uniform float4 _NormalMap_ST;
            uniform float _NormalIntensity;
            
            uniform sampler2D _Texture; uniform float4 _Texture_ST;
            uniform float _TextureOpacity;
            uniform float _Exposure;

            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD3;
                float3 normalDir : TEXCOORD4;
                float3 tangentDir : TEXCOORD5;
                float3 bitangentDir : TEXCOORD6;
                float4 projPos : TEXCOORD7;
                UNITY_FOG_COORDS(8)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(VertexOutput, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.uv0 = v.texcoord0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                o.projPos = ComputeScreenPos(o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }

            float4 frag(VertexOutput i) : COLOR {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                i.normalDir = normalize(i.normalDir);
                float3x3 tangentTransform = float3x3(i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalMap = UnpackNormal(tex2D(_NormalMap, TRANSFORM_TEX(i.uv0, _NormalMap)));
                float3 normalLocal = lerp(float3(0,0,1), normalMap, _NormalIntensity);
                float3 normalDirection = normalize(mul(normalLocal, tangentTransform));
                float viewDotNormal = max(0.001, dot(normalDirection, viewDirection));

                float surfaceNoise = tex2D(_SurfaceMap, TRANSFORM_TEX(i.uv0, _SurfaceMap)).r;
                float currentGloss = lerp(_Gloss, _Gloss * surfaceNoise, _SurfaceMapIntensity);
                float roughness = 1.0 - currentGloss;

                float2 sceneUVs = (i.projPos.xy / i.projPos.w);
                float3 sceneColor = float3(0,0,0);

                
                float2 waterOffset = float2(0,0);
                if (_Use_Underwater > 0.5) {
                    float time = _Time.y * _Water_Wobble_Speed;
                    
                    
                    float waveX = sin(time + (i.posWorld.x + i.posWorld.z) * _Water_Wobble_Scale);
                    float waveY = cos(time + (i.posWorld.y + i.posWorld.z) * _Water_Wobble_Scale);
                    
                    
                    float distanceFactor = 1.0 / max(i.projPos.w, 0.001);
                    
                    
                    waterOffset = float2(waveX, waveY) * _Water_Wobble_Strength * distanceFactor * 0.01;
                }

                if (_Use_Distortion > 0.5) {
                    float thicknessFactor = _GlassThickness / viewDotNormal;
                    float2 offsetDir = mul((float3x3)UNITY_MATRIX_V, normalDirection).xy;
                    float2 distortionOffset = offsetDir * _Distortion * thicknessFactor * 0.005;
                    
                    float2 finalOffsetR = distortionOffset * (1.0 + _ChromaticAberration) + waterOffset;
                    float2 finalOffsetG = distortionOffset + waterOffset;
                    float2 finalOffsetB = distortionOffset * (1.0 - _ChromaticAberration) + waterOffset;

                    sceneColor.r = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, sceneUVs + finalOffsetR).r;
                    sceneColor.g = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, sceneUVs + finalOffsetG).g;
                    sceneColor.b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, sceneUVs + finalOffsetB).b;
                } else {
                    sceneColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, sceneUVs + waterOffset).rgb;
                }

                if (_Use_Underwater > 0.5) {
                    float3 tintedWater = sceneColor * _Water_Color.rgb;
                    sceneColor = lerp(sceneColor, tintedWater, _Water_Color.a);
                }

                float4 texVar = tex2D(_Texture, TRANSFORM_TEX(i.uv0, _Texture));
                float3 baseTransmission = sceneColor * _Glass_Color.rgb * _Exposure;
                baseTransmission += texVar.rgb * _TextureOpacity * _Incident_Color.rgb;

                float3 viewReflectDirection = reflect(-viewDirection, normalDirection);
                float mipLevel = roughness * UNITY_SPECCUBE_LOD_STEPS;
                float4 envSample = texCUBElod(_Cubemap, float4(viewReflectDirection, mipLevel));
                float3 customReflection = envSample.rgb * _ReflectionIntensity * _Reflection_Color.rgb * _Specular;

                float fresnel = pow(1.0 - viewDotNormal, _FresnelPower);
                fresnel *= (1.0 - roughness * 0.5); 
                
                float3 finalGlassColor = lerp(baseTransmission, customReflection + (baseTransmission * 0.2), fresnel);
                
                float3 emissiveMatcap = float3(0,0,0);
                if (_UseMatcap > 0.5) {
                    float2 matcapUV = (mul(UNITY_MATRIX_V, float4(viewReflectDirection, 0)).xy * _Matcap_Scale * 0.5) + 0.5;
                    float4 matcapTex = tex2D(_Matcap, TRANSFORM_TEX(matcapUV, _Matcap));
                    emissiveMatcap = matcapTex.rgb * _Matcap_Strength * _Emission_Base_Color.rgb * _Reflection_Color.rgb;
                }

                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfDirection = normalize(viewDirection + lightDirection);
                float NdotL = saturate(dot(normalDirection, lightDirection));
                float NdotH = saturate(dot(normalDirection, halfDirection));
                float specTerm = pow(NdotH, currentGloss * 128.0) * _Specular;
                float3 directSpecular = _LightColor0.rgb * specTerm * NdotL;
                
                float3 finalColor = finalGlassColor + emissiveMatcap + directSpecular;
                fixed4 finalRGBA = fixed4(finalColor, 1);
                
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

/*
License / 利用規約

This shader may be used in VRChat avatars and worlds.
Redistribution, resale, or inclusion of this shader in any asset
package or product is strictly prohibited.

本シェーダーの再配布・再販売・アセットへの同梱を禁止します。

© 2026 Sakanasan
https://sakanasan.booth.pm/
*/