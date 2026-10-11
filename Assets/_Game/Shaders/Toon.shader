// BAIT & SWITCH illustrated toon (Cult of the Lamb lens): hard cel terminator with violet shadow tint, painted
// belly-to-back gradient, procedural patterns (stripes / spots / freckles / scars), fin ribs, fine body scales,
// soft gloss blob, rim light tinted by the water, ink hatching in the shadows and a whisper of paper grain;
// tinted ink outline (inverted hull).
// In the sea (globals set by DayNight; all zero in the menu so nothing changes there): surface caustics dapple
// anything near the top, objects sink into the water colour with depth, far layers (z > 0) dissolve into it,
// foreground layers (z < 0) darken into out-of-focus silhouettes, and _Sway makes kelp breathe with the current.
// SRP-batcher friendly. Materials are cached per look by ToonLibrary.
Shader "BaitSwitch/Toon"
{
    Properties
    {
        _BaseColor ("Base", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _ShadeStep ("Shade Step", Range(-1, 1)) = 0.08
        _ShadeSoft ("Shade Softness", Range(0.001, 0.3)) = 0.03
        _ShadeTint ("Shade Tint", Color) = (0.42, 0.33, 0.62, 1)
        _ShadeStrength ("Shade Strength", Range(0, 1)) = 0.45
        _Gradient ("Belly Gradient", Range(0, 1)) = 0
        _GradScale ("Gradient Height", Float) = 0.35
        _Gloss ("Gloss", Range(0, 1)) = 0.35
        _RimColor ("Rim", Color) = (1, 0.86, 0.66, 1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.5
        _Pattern ("Pattern 0 none 1 stripes 2 spots 3 freckles 4 scars", Float) = 0
        _PatternColor ("Pattern", Color) = (0.12, 0.08, 0.16, 1)
        _PatternScale ("Pattern Scale", Float) = 9
        _Ribs ("Fin Ribs", Range(0, 1)) = 0
        _Scales ("Body Scales", Range(0, 1)) = 0
        _Sway ("Current Sway (world units)", Range(0, 2)) = 0
        _Hatch ("Shadow Hatching", Range(0, 1)) = 0.35
        _Grain ("Paper Grain", Range(0, 0.2)) = 0.05
        _OutlineColor ("Outline", Color) = (0.118, 0.078, 0.157, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.06)) = 0.016
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _EmissionColor, _ShadeTint, _RimColor, _PatternColor, _OutlineColor;
            half _ShadeStep, _ShadeSoft, _ShadeStrength, _Gradient, _GradScale, _Gloss, _RimStrength;
            half _Pattern, _PatternScale, _Ribs, _Scales, _Sway, _Hatch, _Grain, _OutlineWidth;
        CBUFFER_END
        // sea globals (DayNight)
        float _BS_SurfaceY, _BS_SeabedY, _BS_FogAmount, _BS_Caustics;
        half4 _BS_FogTop, _BS_FogDeep;

        float3 SwayWS(float3 p)
        {
            if (_Sway <= 0) return p;
            float h = saturate((p.y - _BS_SeabedY) / 9.0);
            float t = _Time.y;
            p.x += (sin(t * 1.15 + p.x * 0.55 + p.z * 0.8) * 0.75 + sin(t * 2.3 + p.y * 0.9) * 0.25) * _Sway * h * h;
            return p;
        }

        // sink into the water: depth tint, far layers dissolve, foreground darkens; returns the fog amount used
        half3 SeaFog(half3 col, float3 ws, out half amount)
        {
            amount = 0;
            if (_BS_FogAmount <= 0) return col;
            float span = max(1.0, _BS_SurfaceY - _BS_SeabedY);
            float d = saturate((_BS_SurfaceY - ws.y) / span);
            half under = smoothstep(_BS_SurfaceY + 0.15, _BS_SurfaceY - 0.3, ws.y);
            half3 fogCol = lerp(_BS_FogTop.rgb, _BS_FogDeep.rgb, d);
            half far = saturate(ws.z / 22.0);
            amount = saturate(_BS_FogAmount * under * (0.12 + 0.55 * d) + far * (0.35 + 0.55 * under));
            col = lerp(col, fogCol, amount);
            half nearK = saturate(-ws.z / 5.0) * under;
            col = lerp(col, _BS_FogDeep.rgb * 0.35, nearK * 0.7);
            return col;
        }
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float3 ws = SwayWS(TransformObjectToWorld(i.positionOS.xyz));
                o.positionCS = TransformWorldToHClip(ws);
                o.positionWS = ws;
                o.positionOS = i.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }

            float hash31(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }

            // distance to the nearest random point in a 3D cell grid (for spots / freckles)
            float cells(float3 p)
            {
                float3 c = floor(p), f = frac(p);
                float d = 9;
                [unroll] for (int z = -1; z <= 1; z++)
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float3 o = float3(x, y, z);
                    float3 r = o + float3(hash31(c + o), hash31(c + o + 11.1), hash31(c + o + 23.7)) - f;
                    d = min(d, dot(r, r));
                }
                return sqrt(d);
            }

            half pattern(float3 p)
            {
                float k = _PatternScale;
                if (_Pattern < 0.5) return 0;
                if (_Pattern < 1.5)   // stripes: vertical bands over the back, fading toward the belly
                    return smoothstep(0.42, 0.5, abs(frac(p.x * k * 0.5 + sin(p.y * 6) * 0.08) - 0.5) * 2) * smoothstep(-0.05, 0.12, p.y);
                if (_Pattern < 2.5)   // spots
                    return smoothstep(0.32, 0.26, cells(p * k));
                if (_Pattern < 3.5)   // freckles: small dots on the face (head end of the body is −x)
                    return smoothstep(0.17, 0.12, cells(p * k * 2.2)) * smoothstep(0.05, -0.2, p.x);
                // scars: a few thin diagonal slashes
                float s = abs(frac((p.x + p.y * 0.8) * k * 0.35) - 0.5);
                return smoothstep(0.04, 0.0, s) * step(0.55, hash31(floor(float3(p.x * 3, p.y * 3, p.z * 3))));
            }

            // overlapping scale arcs, offset every other row
            half scales(float3 p)
            {
                float2 q = float2(p.x * 26.0, p.y * 30.0);
                q.x += step(1.0, fmod(floor(q.y) + 200.0, 2.0)) * 0.5;
                float2 f = frac(q) - float2(0.5, 0.15);
                float r = length(f);
                return smoothstep(0.42, 0.52, r) * smoothstep(0.75, 0.55, r);
            }

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
                Light light = GetMainLight();
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half nl = dot(n, light.direction);
                half lit = smoothstep(_ShadeStep - _ShadeSoft, _ShadeStep + _ShadeSoft, nl);

                half3 base = _BaseColor.rgb;
                // painted gradient: lighter cream belly, slightly deeper back
                half g = saturate(i.positionOS.y / max(0.01, _GradScale) * 0.5 + 0.5);
                half3 belly = lerp(base, half3(0.98, 0.94, 0.86), 0.55);
                half3 back = base * 0.82;
                base = lerp(base, lerp(belly, back, smoothstep(0.15, 0.85, g)), _Gradient);
                // pattern, scales, fin ribs
                base = lerp(base, _PatternColor.rgb, pattern(i.positionOS) * 0.85);
                if (_Scales > 0) base *= 1 - scales(i.positionOS) * 0.06 * _Scales * smoothstep(-0.25, 0.1, i.positionOS.y);
                if (_Ribs > 0)
                {
                    float a = atan2(i.positionOS.x, max(1e-3, i.positionOS.y + 0.02));
                    half rib = smoothstep(0.08, 0.0, abs(frac(a * 4.5) - 0.5)) * saturate(length(i.positionOS.xy) * 12);
                    base = lerp(base, base * 0.62, rib * _Ribs);
                    base = lerp(base, half3(1, 1, 1), 0.12 * _Ribs);
                }

                half3 shade = base * lerp(half3(1, 1, 1), _ShadeTint.rgb, _ShadeStrength);
                half3 lightCol = lerp(half3(1, 1, 1), light.color, 0.6);
                half3 col = lerp(shade, base, lit) * lightCol;
                col += SampleSH(n) * base * 0.28;

                // ink hatching in the shadow side (screen space, illustration feel)
                float2 sp = i.positionCS.xy;
                half hatch = step(0.5, frac((sp.x + sp.y) * 0.18)) * (1 - lit) * _Hatch;
                col *= 1 - hatch * 0.18;

                // dappled light from the waves above, strongest just under the surface
                half under = smoothstep(_BS_SurfaceY + 0.15, _BS_SurfaceY - 0.3, i.positionWS.y);
                if (_BS_Caustics > 0)
                {
                    float depth = saturate((_BS_SurfaceY - i.positionWS.y) / 9.0);
                    half c = smoothstep(0.4, 0.95, caustic(i.positionWS.xy * 1.6 + i.positionWS.z * 0.3, _Time.y));
                    col += _BS_FogTop.rgb * c * _BS_Caustics * (1 - depth) * (1 - depth) * (0.35 + 0.65 * lit) * under * 1.4;
                }

                // gloss blob + rim (warm in air, the colour of the water when submerged)
                float3 h = normalize(light.direction + v);
                col += smoothstep(0.986, 0.995, dot(n, h)) * _Gloss * lightCol * 1.2;      // a small wet glint, not a plastic patch
                col += base * saturate(dot(-n, light.direction)) * 0.4 * _Ribs;               // fin membranes glow when backlit
                half rim = pow(1 - saturate(dot(n, v)), 3.0) * _RimStrength;
                half3 rimCol = lerp(_RimColor.rgb, _BS_FogTop.rgb * 2.2 + 0.1, under * step(0.001, _BS_FogAmount) * 0.65);
                col += rimCol * rim * (0.35 + 0.65 * lit);

                col *= 1 + (hash21(floor(sp * 0.5)) - 0.5) * _Grain;
                half fog;
                col = SeaFog(col, i.positionWS, fog);
                col += _EmissionColor.rgb * (1 - fog * 0.6);   // glows pierce the murk
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ToonOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                float3 p = i.positionOS.xyz + normalize(i.normalOS) * _OutlineWidth;
                float3 ws = SwayWS(TransformObjectToWorld(p));
                o.positionCS = TransformWorldToHClip(ws);
                o.positionWS = ws;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 ink = lerp(_OutlineColor.rgb, _BaseColor.rgb * 0.25, 0.25);
                half fog;
                return half4(SeaFog(ink, i.positionWS, fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes i) { Varyings o; o.positionCS = TransformWorldToHClip(SwayWS(TransformObjectToWorld(i.positionOS.xyz))); return o; }
            half frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
