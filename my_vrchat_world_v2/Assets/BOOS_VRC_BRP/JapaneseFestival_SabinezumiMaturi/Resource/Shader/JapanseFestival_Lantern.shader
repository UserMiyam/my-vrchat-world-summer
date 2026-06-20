Shader "JapaneseFestival/JapanseFestival_Lantern"
{
    Properties
    {
        [Header(Parameters)] [Space(10)]
        _CustomColor1("CustomColor1",Color) = (1,1,1,1)
        _CustomColor2("CustomColor2",Color) = (1,0.5,0,1)
        [Space(5)]
        _MetallicValue("MetallicValue",float) = 0
        _RoughnessAjustment("RoughnessAjustment",float) = 0
        [Space(5)]
        _EmissionColorTint("EmissionColorTint",Color) = (1,1,1,1)
        _EmissionIntensity("EmissionIntensity",float) = 1
        _FresnelPow("FresnelPow",float) = 3
        [Space(20)]

        [Header(Base Maps)][Space(10)]
        [NoScaleOffset]_BasecolorMap("BasecolorMap",2D) = "white"{}
        [NoScaleOffset]_MetalicMap("MetallicMap",2D) = "black"{}
        [NoScaleOffset]_RoughnessMap("RoughnessMap",2D) = "white"{}
        [NoScaleOffset]_NormalMap("NormalMap",2D) = "bump"{}
        [NoScaleOffset]_MaskMap("MaskMap",2D) = "black"{}
        [NoScaleOffset]_CharactorMap("CharactorMap",2D) = "black"{}
        [Space(20)]

        [Header(Switch)][Space(10)]
        [Toggle(USECUSTOMCOLOR)]_UseCustomColor("UseCustomColor",int) = 0
        [Toggle(CHARACTOR)]_UseCharactor("Charctor",int) = 0
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
        #pragma shader_feature USECUSTOMCOLOR
        #pragma shader_feature CHARACTOR
        


        fixed4 _CustomColor1;
        fixed4 _CustomColor2;
        half _MetallicValue;
        half _RoughnessAjustment;
        fixed4 _EmissionColorTint;
        float _EmissionIntensity;
        float _FresnelPow;

        sampler2D _BasecolorMap;
        sampler2D _MetallicMap;
        sampler2D _RoughnessMap;
        sampler2D _NormalMap;
        sampler2D _MaskMap;
        sampler2D _CharactorMap;

        struct Input
        {
            float2 uv_BasecolorMap;
            float3 worldNormal;
            float3 viewDir;
        };       


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_BasecolorMap;
            fixed4 masks = tex2D(_MaskMap, uv);


            fixed4 color = tex2D(_BasecolorMap, uv);

            #ifdef USECUSTOMCOLOR
                fixed4 customColor = lerp(_CustomColor1, _CustomColor2, masks.g);
                color = lerp(color, customColor, masks.r);
            #endif


            #ifdef _METALLICSOURCE_TEXTURE
                half metallic = tex2D(_MetallicMap, uv).r;
            #elif _METALLICSOURCE_VALUE
                half metallic = _MetallicValue;
            #endif


            half roughness = saturate(tex2D(_RoughnessMap, uv).r + _RoughnessAjustment);


            half3 normal = UnpackNormal(tex2D(_NormalMap, uv));


            #ifdef CHARACTOR
                fixed4 char = tex2D(_CharactorMap, uv);
                color = lerp(color, char, char.a);
            #endif


            float fresnel = saturate(dot(IN.viewDir, o.Normal));
            fresnel = pow(fresnel, _FresnelPow);
            fresnel *= masks.r;

            fixed3 emission = color * fresnel;           
            emission *= _EmissionColorTint * _EmissionIntensity;

 

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
