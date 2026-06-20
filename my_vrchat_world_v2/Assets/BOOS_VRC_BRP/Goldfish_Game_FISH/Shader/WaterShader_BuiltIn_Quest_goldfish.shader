Shader "Custom/WaterShader_BuiltIn_Quest_goldfish"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.2, 0.6, 0.7, 1.0)
        _DeepColor("Deep Water Color", Color) = (0.05, 0.2, 0.3, 1.0)
        _Alpha("Alpha (Transparency)", Range(0, 1)) = 0.85

        [Header(Wave Settings)]
        _NormalMap("Normal Map (Fine Waves)", 2D) = "bump" {}
        _WaveSpeed("Wave Speed", Float) = 1.0
        _NormalScale("Normal Scale", Range(0, 10)) = 0.1
        
        [Header(Geometric Wave Shape)]
        _WaveAmplitude("Wave Amplitude (Height)", Float) = 0.05
        _WaveFrequency("Wave Frequency (Density)", Float) = 15.0

        [Header(Surface Settings)]
        _Smoothness("Smoothness", Range(0, 1)) = 0.9
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 5.0
        _ReflectionColor("Reflection Color (Fallback)", Color) = (0.8, 0.9, 1.0, 1.0)
        _ReflectionCube("Reflection Cubemap (Optional)", Cube) = "" {}
        
        [Header(Glare Settings (No Light Needed))]
        _ReflectionIntensity("Reflection Intensity", Range(0, 5)) = 1.0
        _FakeLightDir("Fake Light Direction (X,Y,Z)", Vector) = (0.5, 1.0, -0.5, 0)
        _SpecularColor("Specular Color (Glare)", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularSharpness("Specular Sharpness", Range(10, 500)) = 200.0
        _SpecularIntensity("Specular Intensity", Range(0, 10)) = 2.0

        [Header(Fake Depth)]
        _DepthGradient("Depth Gradient", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            
            #pragma skip_variants LIGHTPROBE_SH DYNAMICLIGHTMAP_ON LIGHTMAP_ON LIGHTMAP_SHADOW_MIXING

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normalDir : TEXCOORD1;
                float3 tangentDir : TEXCOORD2;
                float3 bitangentDir : TEXCOORD3;
                float depth : TEXCOORD4;
                UNITY_FOG_COORDS(5)
                float3 localPos : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _NormalMap;
            float4 _NormalMap_ST; 
            samplerCUBE _ReflectionCube;
            
            float4 _BaseColor;
            float4 _DeepColor;
            float4 _ReflectionColor;
            float _Alpha;
            float _NormalScale;
            float _WaveSpeed;
            float _Smoothness;
            float _FresnelPower;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _DepthGradient;
            
            float _ReflectionIntensity;
            float4 _FakeLightDir;
            float4 _SpecularColor;
            float _SpecularSharpness;
            float _SpecularIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float time = _Time.y * _WaveSpeed * 0.5;
                float wave1 = sin((v.vertex.x + time) * _WaveFrequency * 0.1) * 0.5;
                float wave2 = cos((v.vertex.z - time) * _WaveFrequency * 0.15) * 0.5;
                float waveHeight = (wave1 + wave2) * _WaveAmplitude;
                
                v.vertex.y += waveHeight;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = worldPos;
                o.localPos = v.vertex.xyz;
                
                o.depth = saturate((v.vertex.y + 1.0) * _DepthGradient);

                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
                o.bitangentDir = cross(o.normalDir, o.tangentDir) * v.tangent.w;
                
                UNITY_TRANSFER_FOG(o, o.pos);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float time = _Time.y * _WaveSpeed;
                float2 scrollVec1 = float2(0.02, 0.01) * time;
                float2 scrollVec2 = float2(-0.01, -0.03) * time;
                
                float2 uv1 = (i.localPos.xz * 0.1) * _NormalMap_ST.xy + _NormalMap_ST.zw + scrollVec1;
                float2 uv2 = (i.localPos.xz * 0.05) * _NormalMap_ST.xy + _NormalMap_ST.zw + scrollVec2;

                half3 normalMap1 = UnpackNormal(tex2D(_NormalMap, uv1));
                half3 normalMap2 = UnpackNormal(tex2D(_NormalMap, uv2));
                half3 normalTS = normalize(normalMap1 + normalMap2);
                normalTS.xy *= _NormalScale;
                normalTS.z = sqrt(1.0 - saturate(dot(normalTS.xy, normalTS.xy)));

                float3x3 tbn = float3x3(
                    normalize(i.tangentDir), 
                    normalize(i.bitangentDir), 
                    normalize(i.normalDir)
                );
                float3 worldNormal = normalize(mul(normalTS, tbn));
        
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);

                float depthFactor = saturate(1.0 - i.depth);
                float3 waterColor = lerp(_BaseColor.rgb, _DeepColor.rgb, depthFactor);

                float3 reflectionDir = reflect(-viewDir, worldNormal);
                float3 simpleReflection;
                
                #ifdef SHADER_API_MOBILE
                    float4 reflectionSample = texCUBE(_ReflectionCube, reflectionDir);
                    if (reflectionSample.a > 0.01) {
                        simpleReflection = reflectionSample.rgb;
                    } else {
                        float skyFactor = saturate(reflectionDir.y * 0.5 + 0.5);
                        simpleReflection = lerp(_ReflectionColor.rgb * 0.3, _ReflectionColor.rgb, skyFactor);
                    }
                #else
                    float4 reflectionSample = texCUBE(_ReflectionCube, reflectionDir);
                    if (reflectionSample.a > 0.01) {
                        simpleReflection = reflectionSample.rgb;
                    } else {
                        float skyFactor = saturate(reflectionDir.y * 0.5 + 0.5);
                        simpleReflection = lerp(_ReflectionColor.rgb * 0.3, _ReflectionColor.rgb, skyFactor);
                    }
                #endif

                simpleReflection *= _ReflectionIntensity;

                float fresnel = pow(1.0 - saturate(dot(viewDir, worldNormal)), _FresnelPower);
                
                fixed3 finalColor = lerp(waterColor, simpleReflection, fresnel * _Smoothness);
                
                float3 fakeLightDir = normalize(_FakeLightDir.xyz);
                float spec = pow(max(0.0, dot(reflectionDir, fakeLightDir)), _SpecularSharpness);
                float3 specularGlare = spec * _SpecularColor.rgb * _SpecularIntensity;
                
                finalColor += specularGlare;

                float finalAlpha = lerp(_Alpha * 0.7, _Alpha, depthFactor);

                fixed4 result = fixed4(finalColor, finalAlpha);
                
                UNITY_APPLY_FOG(i.fogCoord, result);
                
                return result;
            }
            ENDCG
        }
    }
    
    FallBack "Mobile/Particles/Alpha Blended"
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