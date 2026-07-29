Shader "Deltatime/Vision Hidden Area"
{
    Properties
    {
        _HiddenColor ("Hidden Color", Color) = (0.035, 0.055, 0.09, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+110"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _HiddenColor;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _HiddenColor;
            }
            ENDCG
        }
    }
}
