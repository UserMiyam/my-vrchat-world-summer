Shader "Custom/GlassShader-Quest_goldfish" {
    Properties {
        [Header(Base Settings)]
        _Color ("Main Color (Tint)", Color) = (0.6, 0.8, 1, 0.02)
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode (Fix Double Layer)", Float) = 2

        [Header(Decal Texture)]
        [Toggle(_DECALTEX_ON)] _UseDecal ("Enable Decal Texture", Float) = 0
        _MainTex ("Decal / Sticker (RGBA)", 2D) = "white" {}
   
        _DecalAlpha ("Decal Transparency", Range(0, 1)) = 1.0

        [Header(Normal Map)]
        [Toggle(_NORMALMAP_ON)] _UseNormal ("Enable Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Intensity", Range(0, 2)) = 1.0

        [Header(Reflection)]
        [NoScaleOffset] _Cube ("Reflection Cubemap", Cube) = "black" {}
        _ReflectColor ("Reflection Tint", Color) = (1,1,1,1)
        _ReflectPower ("Reflection Power", Range(0, 5)) = 1.5
        
        [Header(Glass Physics)]
        _FresnelPower ("Fresnel Power (Edge Reflection)", Range(0.5, 10.0)) = 3.0
        _FresnelBias ("Fresnel Bias (Base Reflection)", Range(0.0, 1.0)) = 0.1
    }

    SubShader {
        Tags { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "VRCFallback"="Mobile/Particles/Additive"
        }

        Cull [_Cull]
        ZWrite Off
        
        Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            
            #pragma shader_feature _DECALTEX_ON
            #pragma shader_feature _NORMALMAP_ON

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT; 
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                
                #ifdef _NORMALMAP_ON
                    float3 normalWorld : TEXCOORD3;
                    float3 tangentWorld : TEXCOORD4;
                    float3 binormalWorld : TEXCOORD5;
                    float2 uvBump : TEXCOORD6;
                #else
                    float3 normalWorld : TEXCOORD3;
                #endif
            };

            uniform float4 _Color;
            uniform sampler2D _MainTex; float4 _MainTex_ST;
      
            uniform float _DecalAlpha;

            uniform sampler2D _BumpMap; float4 _BumpMap_ST;
            uniform float _BumpScale;
            uniform samplerCUBE _Cube; 
            uniform float4 _ReflectColor;
            uniform float _ReflectPower;
            uniform float _FresnelPower;
            uniform float _FresnelBias;
          

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = UnityWorldSpaceViewDir(worldPos);
                o.normalWorld = UnityObjectToWorldNormal(v.normal);
                #ifdef _NORMALMAP_ON
                    o.tangentWorld = UnityObjectToWorldDir(v.tangent.xyz);
                    o.binormalWorld = cross(o.normalWorld, o.tangentWorld) * v.tangent.w;
                    o.uvBump = TRANSFORM_TEX(v.uv, _BumpMap);
                #endif

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 viewDir = normalize(i.viewDir);
                float3 normalDirection;

                #ifdef _NORMALMAP_ON
                    float3 localNormal = UnpackNormal(tex2D(_BumpMap, i.uvBump));
                    localNormal.xy *= _BumpScale;
                    localNormal.z = sqrt(1.0 - saturate(dot(localNormal.xy, localNormal.xy)));
                    float3x3 tbn = float3x3(normalize(i.tangentWorld), normalize(i.binormalWorld), normalize(i.normalWorld));
                    normalDirection = normalize(mul(localNormal, tbn));
                #else
                    normalDirection = normalize(i.normalWorld);
                #endif

           
                float3 reflectDir = reflect(-viewDir, normalDirection);
                float fresnel = _FresnelBias + (1.0 - _FresnelBias) * pow(1.0 - saturate(dot(viewDir, normalDirection)), _FresnelPower);
                
                fixed4 reflection;
                reflection.rgb = texCUBE(_Cube, reflectDir).rgb * _ReflectColor.rgb * _ReflectPower * fresnel;
                reflection.a = 0;

             
                float3 tintColor = _Color.rgb * _Color.a;
                fixed4 finalColor;
                finalColor.rgb = tintColor + reflection.rgb;
                finalColor.a = _Color.a;

                #ifdef _DECALTEX_ON
                    fixed4 decal = tex2D(_MainTex, i.uv);
                    
            
                    decal.a *= _DecalAlpha;

                    float3 decalPremul = decal.rgb * decal.a;
                    finalColor.rgb = finalColor.rgb * (1.0 - decal.a) + decalPremul;
                    finalColor.a = max(finalColor.a, decal.a);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Mobile/Diffuse"
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