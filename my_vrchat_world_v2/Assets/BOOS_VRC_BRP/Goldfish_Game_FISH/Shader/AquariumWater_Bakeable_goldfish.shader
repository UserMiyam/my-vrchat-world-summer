Shader "Custom/AquariumWater_Bakeable_goldfish"
{
    Properties
    {
        [HideInInspector] _MainTex ("Bake Dummy Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Bake Dummy Color", Color) = (1, 1, 1, 0)

        _BaseColor ("Water Base Color", Color) = (0.6, 0.85, 0.95, 0.3)
        
        _BumpMap ("Water Ripple Normal", 2D) = "bump" {}
        _NormalStrength ("Ripple Strength", Range(0, 2)) = 0.5
        _ScrollSpeedX ("Scroll Speed X", Float) = 0.02
        _ScrollSpeedY ("Scroll Speed Y", Float) = 0.03

        _Smoothness ("Smoothness", Range(0,1)) = 0.95
        _Metallic ("Metallic", Range(0,1)) = 0.1

        _WaveSpeed ("Vertex Wave Speed", Float) = 1.5
        _WaveFreq ("Vertex Wave Frequency", Float) = 10.0
        _WaveAmp ("Vertex Wave Amplitude", Float) = 0.002

        [Space(15)]
        [Header(Custom Reflection)]
        [Toggle(USE_CUSTOM_CUBEMAP)] _UseCustomCubemap ("Use Custom Cubemap", Float) = 0
        [NoScaleOffset] _CustomCubemap ("Custom Cubemap", Cube) = "" {}
        _CubemapStrength ("Cubemap Strength", Range(0, 3)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha:fade noshadow vertex:vert
        #pragma target 3.0
        #pragma shader_feature USE_CUSTOM_CUBEMAP

        sampler2D _BumpMap;
        samplerCUBE _CustomCubemap;

        struct Input
        {
            float2 uv_BumpMap;
            float3 worldRefl;
            float objectScaleXZ;
            INTERNAL_DATA
        };
        
        fixed4 _BaseColor;
        
        half _Smoothness;
        half _Metallic;
        half _NormalStrength;
        float _ScrollSpeedX;
        float _ScrollSpeedY;

        float _WaveSpeed;
        float _WaveFreq;
        float _WaveAmp;
        half _CubemapStrength;

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float3 worldScale = float3(
                length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20)),
                length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21)),
                length(float3(unity_ObjectToWorld._m02, unity_ObjectToWorld._m12, unity_ObjectToWorld._m22))
            );
            
            float scaleXZ = max((worldScale.x + worldScale.z) * 0.5, 0.001);
            float scaleY = max(worldScale.y, 0.001);
            
            o.objectScaleXZ = scaleXZ;

            float time = _Time.y * _WaveSpeed;
            float wave = sin(v.vertex.x * _WaveFreq + time) * cos(v.vertex.z * _WaveFreq + time);
            
            float localAmp = (_WaveAmp * scaleXZ) / scaleY;
            v.vertex.y += wave * localAmp;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _BaseColor.rgb;

            float2 uv1 = IN.uv_BumpMap + float2(_ScrollSpeedX, _ScrollSpeedY) * _Time.y;
            float2 uv2 = IN.uv_BumpMap + float2(-_ScrollSpeedX * 0.7, _ScrollSpeedY * 1.3) * _Time.y;

            float3 normal1 = UnpackNormal(tex2D(_BumpMap, uv1));
            float3 normal2 = UnpackNormal(tex2D(_BumpMap, uv2));
            
            float3 blendedNormal = normalize(float3(normal1.xy + normal2.xy, normal1.z * normal2.z));
            
            blendedNormal.xy *= (_NormalStrength * IN.objectScaleXZ);
            
            o.Normal = normalize(blendedNormal);
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            
            o.Alpha = _BaseColor.a;

            #if USE_CUSTOM_CUBEMAP
                float3 worldRefl = WorldReflectionVector(IN, o.Normal);
                half4 reflectionData = texCUBE(_CustomCubemap, worldRefl);
                o.Emission = reflectionData.rgb * _CubemapStrength * _Smoothness;
            #endif
        }
        ENDCG
    }
    
    FallBack Off 
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