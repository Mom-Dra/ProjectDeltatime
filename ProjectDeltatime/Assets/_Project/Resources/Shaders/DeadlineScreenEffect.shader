Shader "Hidden/Deltatime/Deadline Screen Effect"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Blend;
            float _Saturation;
            float4 _TintColor;
            float _TintStrength;
            float _VignetteStrength;
            float _GrainStrength;
            float2 _EffectCenter;
            float2 _AimCenter;
            float _ScreenAspect;
            float _RingRadius;
            float _RingStrength;
            float _FlashStrength;
            float _UnscaledTime;

            float Random(float2 position)
            {
                return frac(sin(dot(position, float2(12.9898, 78.233))) *
                    43758.5453);
            }

            fixed4 frag(v2f_img input) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, input.uv);
                float2 offset = input.uv - _EffectCenter;
                offset.x *= max(0.0001, _ScreenAspect);
                float distanceFromPlayer = length(offset);

                float2 aimOffset = input.uv - _AimCenter;
                aimOffset.x *= max(0.0001, _ScreenAspect);
                float distanceFromAim = length(aimOffset);
                float distanceFromFocus = min(
                    distanceFromPlayer,
                    distanceFromAim);

                float edgeFocus = smoothstep(0.08, 0.72, distanceFromFocus);
                float localBlend = saturate(_Blend) * lerp(0.28, 1.0, edgeFocus);
                float luminance = dot(source.rgb, float3(0.2126, 0.7152, 0.0722));
                fixed3 color = lerp(
                    source.rgb,
                    luminance.xxx,
                    (1.0 - _Saturation) * localBlend);

                fixed3 tintedColor = color * _TintColor.rgb +
                    _TintColor.rgb * 0.025;
                color = lerp(
                    color,
                    tintedColor,
                    _TintStrength * localBlend);

                float2 screenOffset = input.uv - float2(0.5, 0.5);
                screenOffset.x *= max(0.0001, _ScreenAspect);
                float vignette = smoothstep(0.34, 1.02, length(screenOffset));
                color *= 1.0 - vignette * _VignetteStrength * localBlend;

                float2 grainCell = floor(
                    input.uv * _ScreenParams.xy * 0.5 +
                    floor(_UnscaledTime * 6.0));
                float grain = Random(grainCell) - 0.5;
                color += grain * _GrainStrength * localBlend;

                float ringDistance = abs(distanceFromPlayer - _RingRadius);
                float ring = 1.0 - smoothstep(0.012, 0.035, ringDistance);
                color += ring * _RingStrength *
                    float3(0.52, 0.95, 1.0) * 0.56;
                color += _FlashStrength * float3(0.72, 0.96, 1.0);

                return fixed4(saturate(color), source.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
