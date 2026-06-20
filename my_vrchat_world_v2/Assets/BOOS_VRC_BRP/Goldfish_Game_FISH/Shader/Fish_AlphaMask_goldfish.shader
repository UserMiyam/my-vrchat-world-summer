Shader "Custom/Fish_AlphaMask_goldfish"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _AlphaTex ("Alpha Mask (Grayscale)", 2D) = "white" {}
        
        _AlphaIntensity ("Alpha Intensity", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        [Space(10)]
        [Header(Mobile and Baked Lighting Settings)]
        [Toggle] _UnlitMode ("Ignore Lighting (Unlit Mode)", Float) = 0
        _UnlitBrightness ("Unlit Brightness", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _AlphaTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_AlphaTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _AlphaIntensity; 
        
        
        half _UnlitMode;
        half _UnlitBrightness;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            fixed alphaMask = tex2D (_AlphaTex, IN.uv_AlphaTex).r;

            alphaMask = saturate(alphaMask * (_AlphaIntensity * 2.0));

            
            if (_UnlitMode > 0.5)
            {
                
                o.Albedo = 0;
                o.Emission = c.rgb * _UnlitBrightness;
            }
            else
            {
                
                o.Albedo = c.rgb;
                o.Emission = 0;
            }

            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            
            o.Alpha = c.a * alphaMask;
        }
        ENDCG
    }
    FallBack "Standard"
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