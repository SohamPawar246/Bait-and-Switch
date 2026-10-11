// Main-menu water seen from above, in world units: concentric ripple rings breathing out from the boat, a gentle swell,
// a shimmering moon/sun glint path toward the camera, and a huge eye that opens beneath the boat (_EyeOpen 0..1).
// The left screen column stays calm for the menu. Output is luminance-rich; the dither pass maps it to the phase ramp.
Shader "BaitSwitch/MenuWater"
{
    Properties
    {
        _Shallow ("Light Water", Color) = (0.55, 0.62, 0.6, 1)
        _Deep ("Dark Water", Color) = (0.08, 0.1, 0.12, 1)
        _RingColor ("Ring", Color) = (0.95, 0.95, 0.9, 1)
        _CenterWS ("Boat (world xz)", Vector) = (1000, 0, 0, 0)
        _RingSpacing ("Ring Spacing (m)", Float) = 1.25
        _RingSpeed ("Ring Speed", Float) = 1.3
        _RingStrength ("Ring Strength", Range(0, 2)) = 0.9
        _RingReach ("Ring Reach (m)", Float) = 9
        _GlintWidth ("Glint Width (m)", Float) = 1.6
        _GlintStrength ("Glint Strength", Range(0, 2)) = 0
        _EyeOpen ("Eye Open", Range(0, 1)) = 0
        _EyeRadius ("Eye Radius (m)", Float) = 1.9
        _Calm ("Calm Left Column (screen x)", Float) = 0.36
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Shallow, _Deep, _RingColor;
                float4 _CenterWS;
                half _RingSpacing, _RingSpeed, _RingStrength, _RingReach, _GlintWidth, _GlintStrength, _EyeOpen, _EyeRadius, _Calm;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 ws : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(w);
                o.ws = w.xz;
                return o;
            }

            float swell(float2 p, float t)
            {
                return sin(p.x * 0.55 + t * 0.4) * 0.5 + sin(p.y * 0.8 - t * 0.33 + p.x * 0.2) * 0.35 + sin((p.x + p.y) * 1.7 + t * 0.7) * 0.15;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 p = i.ws - _CenterWS.xy;                 // metres from the boat
                float n = swell(i.ws, t);
                half3 col = lerp(_Deep.rgb, _Shallow.rgb, 0.40 + n * 0.06);
                // fine wavelets: the grainy surface texture of the reference stills
                float w = sin(i.ws.x * 7.3 + sin(i.ws.y * 5.1 + t) * 1.3 + t * 1.7) * sin(i.ws.y * 6.1 - t * 1.3 + i.ws.x * 0.7);
                col += (w * 0.5 + 0.5) * 0.06 - 0.03;

                float sx = i.positionCS.x / _ScreenParams.x;   // 0..1 across the screen
                float calm = smoothstep(_Calm - 0.04, _Calm + 0.1, sx);

                // concentric rings breathing out from the hull (slightly wobbly)
                float r = length(p * float2(1.0, 1.1)) + n * 0.05;
                float ring = sin(r * 6.2831 / _RingSpacing - t * _RingSpeed);
                ring = smoothstep(0.2, 1.0, ring) * (0.75 + 0.25 * sin(atan2(p.y, p.x) * 3 + t * 0.3));
                float fall = saturate(1 - r / _RingReach);
                fall *= fall;
                col = lerp(col, _RingColor.rgb, ring * fall * _RingStrength * calm);
                col *= 1 - 0.4 * exp(-r * 1.4);                 // dark water hugging the hull

                // glint path: shimmering dashes in a column toward the camera (−z)
                // moon/sun path: broken horizontal streaks in a column through the boat (above and below it)
                float g = exp(-pow((p.x + sin(p.y * 1.7 + t * 0.6) * 0.25) / _GlintWidth, 2));
                float dash = smoothstep(0.55, 0.85, sin(p.y * 7.0 + sin(p.x * 4.0 + t * 1.8) * 1.6 + t * 1.2));
                dash *= smoothstep(0.2, 0.6, sin(p.x * 3.1 + p.y * 1.3 + t));
                col += _RingColor.rgb * g * dash * _GlintStrength * calm * saturate(1 - abs(p.y) * 0.06);

                // the eye beneath the boat
                float2 e = p - float2(0.0, -0.4);
                float lid = abs(e.y) < _EyeRadius * 0.42 * _EyeOpen * sqrt(saturate(1 - pow(e.x / _EyeRadius, 2))) ? 1 : 0;
                float iris = smoothstep(_EyeRadius * 0.34, _EyeRadius * 0.31, length(e));
                float pupil = smoothstep(_EyeRadius * 0.08, _EyeRadius * 0.06, abs(e.x)) * iris;
                col = lerp(col, _Deep.rgb * 0.35, lid * 0.8);
                col = lerp(col, _RingColor.rgb * 0.8, iris * lid * 0.85);
                col = lerp(col, _Deep.rgb * 0.1, pupil * lid);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
