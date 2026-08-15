Shader "Deltatime/Weapon Pickup Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.55, 0.035, 1)
        _OutlinePixels ("Outline Width (Pixels)", Range(0, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+50"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "WeaponPickupOutline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Offset 1, 1

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _OutlineColor;
            float _OutlinePixels;

            v2f vert(appdata input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                v2f output;
                UNITY_INITIALIZE_OUTPUT(v2f, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 clipPosition = UnityObjectToClipPos(input.vertex);
                float3 viewNormal = mul(
                    (float3x3)UNITY_MATRIX_IT_MV,
                    input.normal);
                float2 projectedNormal = float2(
                    UNITY_MATRIX_P[0][0] * viewNormal.x,
                    UNITY_MATRIX_P[1][1] * viewNormal.y);
                float projectedLength = max(
                    length(projectedNormal),
                    0.00001);
                float2 pixelOffset =
                    projectedNormal / projectedLength *
                    (_OutlinePixels * 2.0) /
                    _ScreenParams.xy;
                clipPosition.xy += pixelOffset * clipPosition.w;
                output.vertex = clipPosition;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
