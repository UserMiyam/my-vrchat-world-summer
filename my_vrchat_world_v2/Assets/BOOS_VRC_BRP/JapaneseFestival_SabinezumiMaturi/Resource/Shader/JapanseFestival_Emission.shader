Shader "JapaneseFestival/JapanseFestival_Emission"
{
    Properties
    {
        [Header(Parameters)] [Space(10)]
        _UVScale("UV Scale",float ) = 1
        _MetallicValue("MetallicValue",float) = 0
        _RoughnessAjustment("RoughnessAjustment",float) = 0
        [Space(20)]

        [Header(Base Maps)][Space(10)]
        [NoScaleOffset]_BasecolorMap("BasecolorMap",2D) = "white"{}
        [NoScaleOffset]_MetalicMap("MetallicMap",2D) = "black"{}
        [NoScaleOffset]_RoughnessMap("RoughnessMap",2D) = "white"{}
        [NoScaleOffset]_NormalMap("NormalMap",2D) = "bump"{}
        [NoScaleOffset]_EmissionMap("EmissionMap",2D) = "black"{}
        [Space(20)]

        [Header(Switch)][Space(10)]
        [KeywordEnum(Texture,Value)]_MetallicSource("MetallicSource",float) = 1

    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM


        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        #pragma shader_feature _METALLICSOURCE_TEXTURE _METALLICSOURCE_VALUE


        float _UVScale;
        half _MetallicValue;
        half _RoughnessAjustment;

        sampler2D _BasecolorMap;
        sampler2D _MetallicMap;
        sampler2D _RoughnessMap;
        sampler2D _NormalMap;
        sampler2D _EmissionMap;

        struct Input
        {
            float2 uv_BasecolorMap;
        };       


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_BasecolorMap * _UVScale;

            fixed4 color = tex2D(_BasecolorMap, uv);

            #ifdef _METALLICSOURCE_TEXTURE
                half metallic = tex2D(_MetallicMap, uv).r;
            #elif _METALLICSOURCE_VALUE
                half metallic = _MetallicValue;
            #endif

            half roughness = saturate(tex2D(_RoughnessMap, uv).r + _RoughnessAjustment);
            half3 normal = UnpackNormal(tex2D(_NormalMap, uv));


            fixed3 emission = tex2D(_EmissionMap, uv);



            o.Albedo = color;
            o.Metallic = metallic;
            o.Smoothness = 1 - roughness;
            o.Normal = normal;
            o.Emission = emission;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
