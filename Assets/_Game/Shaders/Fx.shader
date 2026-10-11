// Particles and FX quads: texture x vertex colour x tint, alpha or additive (set _SrcBlend/_DstBlend), soft depth fade
// toward the water colour so bubbles and glows sit in the same sea as the toon models.
Shader "BaitSwitch/Fx"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SrcBlend ("Src", Float) = 5
        [HideInInspector] _DstBlend ("Dst", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
            CBUFFER_END
            float _BS_SurfaceY, _BS_SeabedY, _BS_FogAmount;
            half4 _BS_FogTop, _BS_FogDeep;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float3 positionWS : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color * _Tint;
                float d = saturate((_BS_SurfaceY - i.positionWS.y) / max(1.0, _BS_SurfaceY - _BS_SeabedY));
                half under = smoothstep(_BS_SurfaceY + 0.1, _BS_SurfaceY - 0.3, i.positionWS.y);
                half3 fogCol = lerp(_BS_FogTop.rgb, _BS_FogDeep.rgb, d);
                c.rgb = lerp(c.rgb, fogCol * c.a, saturate(_BS_FogAmount * d * 0.5 * under));
                return c;
            }
            ENDHLSL
        }
    }
}
