Shader "Deltatime/World Time Emissive Scroll"
{
    Properties
    {
        _Emissive("Emissive", 2D) = "white" {}
        _Colour_01("Colour_01", Color) = (0,0.8694534,1,0)
        _Colour_02("Colour_02", Color) = (0,0.8694534,1,0)
        _Speed("Speed", Vector) = (-0.2,0,0,0)
        _LED_Mask_01("LED_Mask_01", 2D) = "white" {}
        [HideInInspector] _WorldElapsedTime("World Elapsed Time", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+0" "IsEmissive" = "true" }
        Cull Back

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard keepalpha addshadow fullforwardshadows

        struct Input
        {
            float2 uv_texcoord;
        };

        uniform float4 _Colour_02;
        uniform float4 _Colour_01;
        uniform sampler2D _Emissive;
        uniform float2 _Speed;
        uniform sampler2D _LED_Mask_01;
        uniform float4 _LED_Mask_01_ST;
        uniform float _WorldElapsedTime;

        void surf(Input i, inout SurfaceOutputStandard o)
        {
            float2 panner = _WorldElapsedTime * _Speed + i.uv_texcoord;
            float4 emissiveSample = tex2D(_Emissive, panner);
            float4 displayColor = lerp(
                _Colour_02,
                _Colour_01,
                emissiveSample);
            o.Albedo = (displayColor * emissiveSample).rgb;

            float2 maskUv = i.uv_texcoord * _LED_Mask_01_ST.xy +
                            _LED_Mask_01_ST.zw;
            o.Emission = lerp(
                displayColor,
                float4(0, 0, 0, 0),
                tex2D(_LED_Mask_01, maskUv).a).rgb;
            o.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
