Shader "Custom/FestivalBubblePop"
{
    Properties
    {
        [Header(Color Settings)]
        _RiseColor ("Rise Bubble Color", Color) = (1, 1, 1, 0.8)
        _SurfaceColor ("Surface Bubble Color", Color) = (0.7, 0.9, 1.0, 0.8)
        _RimPower ("Rim Power (Thickness)", Range(0.1, 10.0)) = 3.0
        
        _BubbleRadius ("Bubble Radius (Base Scale)", Float) = 0.002

        [Header(Spawn Settings)]
        _SpawnOrigin ("Spawn Origin (Local X, Z)", Vector) = (0, 0, 0, 0)

        [Header(Rise Settings)]
        _RiseSpeed ("Base Rise Speed", Float) = 0.4
        _RiseSpeedRandom ("Rise Speed Randomness", Range(0, 1)) = 0.5
        _RiseSpread ("Rise Spread (Cone width)", Float) = 0.3
        _Wobble ("Wobble Strength", Float) = 0.02

        [Header(Surface Settings)]
        _WaterLevel ("Water Level (Local Y -0.5 to 0.5)", Range(-0.5, 0.5)) = 0.3
        _SurfaceSpreadSpeed ("Surface Spread Speed", Float) = 0.5
        _SurfaceSpeedRandom ("Surface Speed Randomness", Range(0, 1)) = 0.5
        
        [Header(System)]
        _CycleLength ("Total Respawn Cycle Time", Float) = 4.0
        _BubbleCount ("Bubble Count (Max 2000)", Range(1, 2000)) = 2000 
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0 
            #include "UnityCG.cginc"

            struct appdata { 
                float4 vertex : POSITION; 
                uint vid : SV_VertexID; 
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };
            
            struct v2g { 
                float3 worldCenter : TEXCOORD0;
                float fade : TEXCOORD1;
                uint bubbleID : TEXCOORD2;
                float bubbleScale : TEXCOORD3;
                float4 color : COLOR; 
                UNITY_VERTEX_INPUT_INSTANCE_ID 
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            struct v2f { 
                float4 pos : SV_POSITION; 
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
                float4 color : COLOR; 
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            float4 _RiseColor;
            float4 _SurfaceColor;
            float _RimPower, _BubbleRadius, _WaterLevel;
            float4 _SpawnOrigin;
            float _RiseSpeed, _RiseSpeedRandom, _RiseSpread, _Wobble;
            float _SurfaceSpreadSpeed, _SurfaceSpeedRandom, _CycleLength;
            int _BubbleCount;

            float4 hash41(float p)
            {
                float4 p4 = frac(float4(p, p, p, p) * float4(.1031, .1030, .0973, .1099));
                p4 += dot(p4, p4.wzxy + 33.33);
                return frac((p4.xxyz + p4.yzzw) * p4.zywx);
            }

            v2g vert (appdata v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 

                o.bubbleID = v.vid / 3;
                
                if (o.bubbleID >= (uint)_BubbleCount) {
                    o.worldCenter = float3(0, 0, 0);
                    o.fade = 0;
                    o.bubbleScale = 0;
                    o.color = float4(0,0,0,0);
                    return o;
                }

                float3 objScale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22)
                );
                float avgScale = (objScale.x + objScale.y + objScale.z) / 3.0;
                o.bubbleScale = _BubbleRadius * avgScale;

                float startY = -0.5;
                float baseWaterDist = _WaterLevel - startY;

                float4 rand = hash41((float)o.bubbleID);
                float r1 = rand.x; float r2 = rand.y; float r3 = rand.z; float r4 = rand.w;

                float localRiseSpeed = _RiseSpeed * (1.0 + (r1 - 0.5) * 2.0 * _RiseSpeedRandom);
                float localSurfaceSpeed = _SurfaceSpreadSpeed * (1.0 + (r2 - 0.5) * 2.0 * _SurfaceSpeedRandom);
                
                float timeOffset = r3 * 100.0;
                float cycleProg = fmod(_Time.y * 1.0 + timeOffset, _CycleLength);

                float virtualY = startY + cycleProg * localRiseSpeed;
                
                float isRising = step(virtualY, _WaterLevel);
                float currentY = min(virtualY, _WaterLevel);

                o.color = lerp(_SurfaceColor, _RiseColor, isRising);

                float timeToReach = baseWaterDist / localRiseSpeed;
                float timeAfterReach = max(0.0, cycleProg - timeToReach);
                
                float radiusFactor = sqrt(r1);
                float angle = r4 * 6.283185;

                float riseSpreadAmount = (currentY - startY) * _RiseSpread * radiusFactor;
                float surfaceSpreadAmount = timeAfterReach * localSurfaceSpeed;
                float totalSpread = riseSpreadAmount + surfaceSpreadAmount;
                
                float3 localCenter;
                localCenter.y = currentY;
                localCenter.x = _SpawnOrigin.x + cos(angle) * totalSpread;
                localCenter.z = _SpawnOrigin.y + sin(angle) * totalSpread;
                
                localCenter.x += sin(_Time.y * 3.0 + timeOffset) * _Wobble * isRising;
                localCenter.z += cos(_Time.y * 2.5 + timeOffset) * _Wobble * isRising;

                o.worldCenter = mul(unity_ObjectToWorld, float4(localCenter, 1.0)).xyz;
                o.fade = 1.0 - saturate((cycleProg - (_CycleLength - 0.5)) / 0.5);

                return o;
            }

            [maxvertexcount(4)]
            void geom(triangle v2g IN[3], inout TriangleStream<v2f> TRI_STREAM)
            {
                if (IN[0].bubbleID >= (uint)_BubbleCount) return;

                UNITY_SETUP_INSTANCE_ID(IN[0]);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN[0]); 

                float3 camRight = normalize(unity_CameraToWorld._m00_m10_m20);
                float3 camUp = normalize(unity_CameraToWorld._m01_m11_m21);

                float2 quadOffsets[4] = { float2(-1,-1), float2(1,-1), float2(-1,1), float2(1,1) };
                float2 quadUVs[4] = { float2(0,0), float2(1,0), float2(0,1), float2(1,1) };

                float currentRadius = IN[0].bubbleScale;

                for (int i = 0; i < 4; i++)
                {
                    v2f OUT;
                    UNITY_INITIALIZE_OUTPUT(v2f, OUT);
                    UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], OUT);

                    float3 finalWorldPos = IN[0].worldCenter + (camRight * quadOffsets[i].x + camUp * quadOffsets[i].y) * currentRadius;
                    OUT.pos = UnityWorldToClipPos(finalWorldPos);
                    OUT.uv = quadUVs[i];
                    OUT.fade = IN[0].fade;
                    OUT.color = IN[0].color; 
                    
                    TRI_STREAM.Append(OUT);
                }
                TRI_STREAM.RestartStrip();
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centeredUV = i.uv * 2.0 - 1.0;
                float dist = length(centeredUV);
                
                if(dist > 1.0) discard;

                float z = sqrt(1.0 - dist * dist);
                float rim = pow(1.0 - z, _RimPower);

                float alpha = rim * i.fade * i.color.a;
                return fixed4(i.color.rgb, alpha);
            }
            ENDCG
        }
    }
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