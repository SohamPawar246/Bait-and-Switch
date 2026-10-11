// The sea behind everything. The quad sits far back (z = 40) but every pixel is mapped through the camera ray onto
// the gameplay plane (z = 0), so the surface line always lines up with the boats however the perspective camera
// moves, while the parallax layers in between (rocks, kelp, drifting giants) sit in front of it.
// Depth gradient, a bright band under the surface, slanted light rays, caustics, a wavy foamy surface, and a sky
// with sun/moon and stars above it. Colours are driven by DayNight; surface/seabed come from its globals.
Shader "BaitSwitch/SeaBackdrop"
{
    Properties
    {
        _WaterTop ("Water Top", Color) = (0.16, 0.42, 0.45, 1)
        _WaterBottom ("Water Bottom", Color) = (0.03, 0.06, 0.12, 1)
        _SkyTop ("Sky Top", Color) = (0.25, 0.45, 0.6, 1)
        _SkyHorizon ("Sky Horizon", Color) = (0.85, 0.85, 0.75, 1)
        _FoamColor ("Foam", Color) = (0.9, 0.95, 0.9, 1)
        _RayColor ("Ray", Color) = (0.7, 0.95, 0.9, 1)
        _RayStrength ("Ray Strength", Range(0, 1)) = 0.35
        _CausticStrength ("Caustic Strength", Range(0, 1)) = 0.35
        _MoonColor ("Sun/Moon", Color) = (1, 0.95, 0.8, 1)
        _MoonPos ("Sun/Moon Pos (0..1 across, 0..1 up)", Vector) = (0.78, 0.94, 0, 0)
        _MoonSize ("Sun/Moon Radius (world)", Range(0, 3)) = 0.9
        _Stars ("Stars", Range(0, 1)) = 0
        _SurfaceV ("(unused) Surface uv", Range(0, 1)) = 0.86
        _Aspect ("(unused) Aspect", Float) = 1.7
        _WaveAmp ("Wave Amp (world)", Float) = 0.07
        _WaveFreq ("Wave Freq (per unit)", Float) = 0.9
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry-50" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterTop, _WaterBottom, _SkyTop, _SkyHorizon, _FoamColor, _RayColor, _MoonColor;
                half _RayStrength, _CausticStrength, _MoonSize, _Stars, _SurfaceV, _Aspect, _WaveAmp, _WaveFreq;
                float4 _MoonPos;
            CBUFFER_END
            float _BS_SurfaceY, _BS_SeabedY;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }

            float caustic(float2 p, float t)
            {
                float c = 0;
                float2 q = p;
                [unroll] for (int k = 0; k < 3; k++)
                {
                    q += float2(sin(q.y * 1.7 + t * 0.6 + k), cos(q.x * 1.3 - t * 0.5 + k * 1.7)) * 0.55;
                    c += 1.0 / (1.0 + 30.0 * abs(sin(q.x) * sin(q.y)));
                }
                return c / 3.0;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                // where this pixel's view ray crosses the gameplay plane
                float3 cam = _WorldSpaceCameraPos;
                float3 dir = normalize(i.positionWS - cam);
                float k = -cam.z / (abs(dir.z) > 1e-4 ? dir.z : 1e-4);
                float2 p = (cam + dir * max(0.0, k)).xy;
                float surf = _BS_SurfaceY;
                float span = _BS_SurfaceY - _BS_SeabedY;
                span = span < 1.0 ? 20.0 : span;

                float wave = surf + sin(p.x * _WaveFreq + t * 1.1) * _WaveAmp + sin(p.x * _WaveFreq * 2.3 - t * 1.7) * _WaveAmp * 0.5;
                float d = p.y - wave;

                if (d > 0)
                {
                    // sky
                    float sv = saturate(d / 7.0);
                    half3 sky = lerp(_SkyHorizon.rgb, _SkyTop.rgb, pow(sv, 0.6));
                    float2 moon = float2((_MoonPos.x - 0.5) * 34.0, surf + 1.2 + saturate((_MoonPos.y - 0.8) * 5.0) * 4.0);
                    float md = length(p - moon);
                    sky += _MoonColor.rgb * (smoothstep(_MoonSize, _MoonSize * 0.85, md) + 0.3 * exp(-md * 0.9));
                    float2 sp = floor(p * 7.0);
                    float star = step(0.993, hash21(sp)) * (0.6 + 0.4 * sin(t * 2 + hash21(sp + 7) * 6.28));
                    sky += star * _Stars * smoothstep(0.3, 2.0, d);
                    half foam = smoothstep(0.07, 0.0, d);
                    return half4(lerp(sky, _FoamColor.rgb, foam), 1);
                }

                // water
                float depth = saturate(-d / span);                 // 0 at the surface, 1 at the seabed
                half3 col = lerp(_WaterTop.rgb, _WaterBottom.rgb, pow(depth, 0.75));
                col += _RayColor.rgb * exp(d * 1.2) * 0.22;         // the bright band just under the waves
                float rx = p.x + (-d) * 0.42;
                float rays = pow(saturate((sin(rx * 0.62 + t * 0.25) * 0.5 + 0.5) * (sin(rx * 0.27 - t * 0.13) * 0.5 + 0.5)), 3.0);
                col += _RayColor.rgb * rays * _RayStrength * pow(1 - depth, 2.0);
                float c = caustic(p * 0.95, t);
                col += _RayColor.rgb * smoothstep(0.35, 0.9, c) * _CausticStrength * pow(1 - depth, 4.0);
                half foam = smoothstep(-0.09, 0.0, d);
                col = lerp(col, _FoamColor.rgb, foam * 0.8);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
