Shader "Custom/Neo_Standard_Lite_goldfish"
{
    Properties
    {
        [Header(Base)]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        _MinBrightness ("Minimum Brightness (Dark World Fix)", Range(0.0, 1.0)) = 0.0
        
        [Header(Normal and Occlusion)]
        [Toggle(_ENABLE_NORMAL_MAP)] _EnableNormalMap ("Enable Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        
        [Toggle(_ENABLE_OCCLUSION)] _EnableOcclusion ("Enable Occlusion", Float) = 0
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [Header(Height Parallax LOD)]
        [Toggle(_ENABLE_PARALLAX)] _EnableParallax ("Enable Parallax", Float) = 0
        _ParallaxMap ("Height Map (G)", 2D) = "black" {}
        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMinDist ("LOD Start Dist (Max Effect)", Float) = 1.0
        _ParallaxMaxDist ("LOD End Dist (Effect OFF)", Float) = 5.0

        [Header(Emission)]
        [Toggle(_ENABLE_EMISSION)] _EnableEmission ("Enable Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        // ★デフォルトをwhiteに変更し、テクスチャなしでも光るように修正
        _EmissionMap ("Emission Map (RGB)", 2D) = "white" {}

        [Header(Specular)]
        [Toggle(_ENABLE_SPECULAR)] _EnableSpecular ("Enable Specular", Float) = 0
        _SpecularColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess ("Specular Power (Shininess)", Range(1.0, 100.0)) = 20.0

        [Header(Metallic)]
        [Toggle(_ENABLE_METALLIC)] _EnableMetallic ("Enable Metallic", Float) = 0
        _MetallicMap ("Metallic Map (R)", 2D) = "white" {}
        _Metallic ("Metallic Strength", Range(0.0, 1.0)) = 1.0
        
        [Header(Reflection)]
        [Toggle(_ENABLE_REFLECTION)] _EnableReflection ("Enable Reflection", Float) = 0
        [NoScaleOffset] _Cube ("Cubemap", Cube) = "" {}
        _ReflectStrength ("Reflection Strength", Range(0, 5)) = 1.0
        _Roughness ("Roughness (MipMap Blur)", Range(0, 1)) = 0.5
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 4.0

        [Header(Realtime Lighting)]
        [Toggle(_ENABLE_REALTIME_LIGHT)] _EnableRealtimeLight ("Receive Realtime Light & Shadows", Float) = 0

        [Header(Fake Ceiling Light)]
        [Toggle(_ENABLE_CEILING_LIGHT)] _EnableCeilingLight ("Enable Ceiling Light (On/Off)", Float) = 0
        _CeilingLightColor ("Ceiling Light Color", Color) = (1,1,1,1)
        _CeilingLightPower ("Ceiling Light Sharpness", Range(1.0, 100.0)) = 20.0
        _CeilingLightStrength ("Ceiling Light Strength", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #pragma shader_feature _ENABLE_NORMAL_MAP
            #pragma shader_feature _ENABLE_OCCLUSION
            #pragma shader_feature _ENABLE_PARALLAX
            #pragma shader_feature _ENABLE_REFLECTION
            #pragma shader_feature _ENABLE_CEILING_LIGHT
            #pragma shader_feature _ENABLE_SPECULAR
            #pragma shader_feature _ENABLE_METALLIC
            #pragma shader_feature _ENABLE_REALTIME_LIGHT
            #pragma shader_feature _ENABLE_EMISSION
            
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : NORMAL;
                #ifdef _ENABLE_NORMAL_MAP
                    float3 worldTangent : TANGENT;
                    float3 worldBinormal : BINORMAL;
                #endif

                #ifdef _ENABLE_PARALLAX
                    float3 tangentViewDir : TEXCOORD3;
                #endif

                float dist : TEXCOORD4;
                #ifdef LIGHTMAP_ON
                    float2 uvLM : TEXCOORD5;
                #endif

                SHADOW_COORDS(6)
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _BumpMap;
            sampler2D _OcclusionMap;
            sampler2D _ParallaxMap;
            sampler2D _EmissionMap;
            samplerCUBE _Cube;
            fixed4 _Color;
            float _BumpScale;
            float _OcclusionStrength;
            float _Parallax;
            float _ParallaxMinDist;
            float _ParallaxMaxDist;
            fixed4 _SpecularColor;
            fixed4 _EmissionColor;
            float _Shininess;
            float _MinBrightness;
            
            sampler2D _MetallicMap;
            float _Metallic;

            half _ReflectStrength;
            half _Roughness;
            half _FresnelPower;
            fixed4 _CeilingLightColor;
            half _CeilingLightPower;
            half _CeilingLightStrength;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                #ifdef LIGHTMAP_ON
                    o.uvLM = v.uv2.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif

                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldNormal = worldNormal;

                #if defined(_ENABLE_NORMAL_MAP) || defined(_ENABLE_PARALLAX)
                    half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                    half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    half3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
                    #ifdef _ENABLE_NORMAL_MAP
                        o.worldTangent = worldTangent;
                        o.worldBinormal = worldBinormal;
                    #endif

                    #ifdef _ENABLE_PARALLAX
                        float3 worldViewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                        o.tangentViewDir.x = dot(worldViewDir, worldTangent);
                        o.tangentViewDir.y = dot(worldViewDir, worldBinormal);
                        o.tangentViewDir.z = dot(worldViewDir, worldNormal);
                    #endif
                #endif

                o.dist = distance(_WorldSpaceCameraPos, o.worldPos);
                #ifdef _ENABLE_REALTIME_LIGHT
                    TRANSFER_SHADOW(o)
                #endif
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                #ifdef _ENABLE_PARALLAX
                    float parallaxWeight = saturate(1.0 - (i.dist - _ParallaxMinDist) / (_ParallaxMaxDist - _ParallaxMinDist));
                    if (parallaxWeight > 0.0)
                    {
                        half h = tex2D(_ParallaxMap, uv).g;
                        float2 offset = i.tangentViewDir.xy * (h * _Parallax * parallaxWeight);
                        uv += offset;
                    }
                #endif

                half3 normalDir;
                #ifdef _ENABLE_NORMAL_MAP
                    half3 tangentNormal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
                    normalDir = normalize(
                        tangentNormal.x * i.worldTangent +
                        tangentNormal.y * i.worldBinormal +
                        tangentNormal.z * i.worldNormal
                    );
                #else
                    normalDir = normalize(i.worldNormal);
                #endif

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotL = max(0, dot(normalDir, lightDir));
                
                float3 ambient;
                #ifdef LIGHTMAP_ON
                    ambient = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uvLM));
                #else
                    ambient = ShadeSH9(float4(normalDir, 1));
                #endif

                fixed aoVal = 1.0;
                #ifdef _ENABLE_OCCLUSION
                    fixed4 ao = tex2D(_OcclusionMap, uv);
                    aoVal = lerp(1.0, ao.r, _OcclusionStrength);
                #endif
                
                fixed4 albedo = tex2D(_MainTex, uv) * _Color;

                float metallicVal = 0.0;
                #ifdef _ENABLE_METALLIC
                    metallicVal = tex2D(_MetallicMap, uv).r * _Metallic;
                #endif

                float3 diffuseAlbedo = albedo.rgb * (1.0 - metallicVal);

                float3 directLight = float3(0,0,0);
                float atten = 1.0;

                #ifdef _ENABLE_REALTIME_LIGHT
                    UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
                    atten = attenuation;
                    directLight = _LightColor0.rgb * NdotL * atten;
                #endif

                
                float3 totalLight = ambient + directLight;
                totalLight = max(totalLight, _MinBrightness);

                float3 diffuseColor = diffuseAlbedo * totalLight * aoVal;

                float3 specularColor = float3(0,0,0);
                #ifdef _ENABLE_SPECULAR
                    float NdotH = max(0, dot(normalDir, halfDir));
                    float specular = pow(NdotH, _Shininess) * NdotL;
                    float3 specBaseColor = lerp(_SpecularColor.rgb, albedo.rgb, metallicVal);
                    specularColor = specBaseColor * specular * atten;
                #endif

                float3 reflectionColor = float3(0,0,0);
                #ifdef _ENABLE_REFLECTION
                    float3 reflectDir = reflect(-viewDir, normalDir);
                    fixed4 reflection = texCUBElod(_Cube, float4(reflectDir, _Roughness * 7.0));
                    float fresnel = pow(1.0 - saturate(dot(normalDir, viewDir)), _FresnelPower);
                    float3 reflectTint = lerp(float3(1,1,1), albedo.rgb, metallicVal);
                    reflectionColor = reflection.rgb * _ReflectStrength * fresnel * aoVal * reflectTint;
                #endif

                float3 emissionColor = float3(0,0,0);
                #ifdef _ENABLE_EMISSION
                    
                    emissionColor = tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
                #endif

                fixed4 finalColor;
                finalColor.rgb = diffuseColor + specularColor + reflectionColor + emissionColor;

                #ifdef _ENABLE_CEILING_LIGHT
                    float3 fakeLightDir = float3(0, 1, 0);
                    float3 fakeHalfDir = normalize(fakeLightDir + viewDir);
                    float fakeNdotH = max(0, dot(normalDir, fakeHalfDir));
                    float fakeSpecular = pow(fakeNdotH, _CeilingLightPower);
                    finalColor.rgb += _CeilingLightColor.rgb * fakeSpecular * _CeilingLightStrength * aoVal;
                #endif

                finalColor.a = albedo.a;
                return finalColor;
            }
            ENDCG
        }

        Pass
        {
            Name "FORWARD_ADD"
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            
            #pragma shader_feature _ENABLE_NORMAL_MAP
            #pragma shader_feature _ENABLE_SPECULAR
            #pragma shader_feature _ENABLE_METALLIC
            #pragma shader_feature _ENABLE_REALTIME_LIGHT
            
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : NORMAL;
                #ifdef _ENABLE_NORMAL_MAP
                    float3 worldTangent : TANGENT;
                    float3 worldBinormal : BINORMAL;
                #endif

                float3 tangentViewDir : TEXCOORD3;
                unityShadowCoord4 _ShadowCoord : TEXCOORD4;
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _BumpMap;
            fixed4 _Color;
            float _BumpScale;
            fixed4 _SpecularColor;
            float _Shininess;

            sampler2D _MetallicMap;
            float _Metallic;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldNormal = worldNormal;
                #ifdef _ENABLE_NORMAL_MAP
                    half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                    half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    half3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
                    o.worldTangent = worldTangent;
                    o.worldBinormal = worldBinormal;
                #endif

                o.tangentViewDir = 0;
                o._ShadowCoord = 0;
                #ifdef _ENABLE_REALTIME_LIGHT
                    TRANSFER_SHADOW(o)
                #endif

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #ifdef _ENABLE_REALTIME_LIGHT
                
                    half3 normalDir;
                    #ifdef _ENABLE_NORMAL_MAP
                        half3 tangentNormal = UnpackScaleNormal(tex2D(_BumpMap, i.uv), _BumpScale);
                        normalDir = normalize(
                            tangentNormal.x * i.worldTangent +
                            tangentNormal.y * i.worldBinormal +
                            tangentNormal.z * i.worldNormal
                        );
                    #else
                        normalDir = normalize(i.worldNormal);
                    #endif

                    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                    float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                    float3 halfDir = normalize(lightDir + viewDir);
                    float NdotL = max(0, dot(normalDir, lightDir));

                    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                    fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;

                    float metallicVal = 0.0;
                    #ifdef _ENABLE_METALLIC
                        metallicVal = tex2D(_MetallicMap, i.uv).r * _Metallic;
                    #endif

                    float3 diffuseAlbedo = albedo.rgb * (1.0 - metallicVal);
                    float3 diffuse = diffuseAlbedo * _LightColor0.rgb * NdotL * atten;
                    
                    float3 specularColor = float3(0,0,0);
                    #ifdef _ENABLE_SPECULAR
                        float NdotH = max(0, dot(normalDir, halfDir));
                        float specular = pow(NdotH, _Shininess) * NdotL;
                        float3 specBaseColor = lerp(_SpecularColor.rgb, albedo.rgb, metallicVal);
                        specularColor = specBaseColor * specular * atten;
                    #endif

                    return fixed4(diffuse + specularColor, 0);
                #else
                    return fixed4(0,0,0,0);
                #endif
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct v2f { V2F_SHADOW_CASTER;
            };
            v2f vert(appdata_base v) { v2f o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) return o;
            }
            float4 frag(v2f i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
    Fallback "Mobile/VertexLit"
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