Shader "JapaneseFestival/JapanseFestival_StallBanner"
{
    Properties
    {
        [Header(Parameters)] [Space(10)]
        _ColorTint("ColorTint",Color) = (1,1,1,1)
        _RoughnessAjustment("RoughnessAjustment",float) = 0
        [Space(20)]

        [Header(Base Maps)][Space(10)]
        [NoScaleOffset]_BasecolorMap("BasecolorMap",2D) = "white"{}
        [NoScaleOffset]_RoughnessMap("RoughnessMap",2D) = "white"{}
        [NoScaleOffset]_NormalMap("NormalMap",2D) = "bump"{}
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM


        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _ColorTint;
        half _RoughnessAjustment;

        sampler2D _BasecolorMap; 
        sampler2D _RoughnessMap;
        sampler2D _NormalMap;

        struct Input
        {
            float2 uv_BasecolorMap;
        };       


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_BasecolorMap;

            fixed4 color = tex2D(_BasecolorMap, uv);

            half roughness = saturate(tex2D(_RoughnessMap, uv).r + _RoughnessAjustment);
            half3 normal = UnpackNormal(tex2D(_NormalMap, uv));


            o.Albedo = color * _ColorTint;
            o.Metallic = 0;
            o.Smoothness = 1 - roughness;
            o.Normal = normal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
