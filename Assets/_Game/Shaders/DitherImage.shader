// UI RawImage shader: ordered dither (Bayer 8x8) of a render texture onto a 4-colour ramp, chunky pixels,
// plus a dither-threshold dissolve toward ramp colour 0 (_Dissolve). Used for the main menu sea.
Shader "BaitSwitch/DitherImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _C0 ("Ramp 0 (darkest)", Color) = (0.105, 0.141, 0.251, 1)
        _C1 ("Ramp 1", Color) = (0.122, 0.373, 0.478, 1)
        _C2 ("Ramp 2", Color) = (0.247, 0.655, 0.639, 1)
        _C3 ("Ramp 3 (lightest)", Color) = (0.957, 0.914, 0.847, 1)
        _PixelSize ("Pixel Size (screen px)", Float) = 3
        _Contrast ("Contrast", Float) = 1.25
        _Bias ("Bias", Float) = 0
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Smooth ("Smooth Tone Mix", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color, _C0, _C1, _C2, _C3;
            float _PixelSize, _Contrast, _Bias, _Dissolve, _Smooth;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float4 screen : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.screen = ComputeScreenPos(o.pos);
                return o;
            }

            float bayer8(float2 p)
            {
                uint x = (uint)p.x & 7u;
                uint y = (uint)p.y & 7u;
                uint v = x ^ y;
                uint m = ((v & 1u) << 5) | ((x & 1u) << 4) | ((v & 2u) << 2) | ((x & 2u) << 1) | ((v & 4u) >> 1) | ((x & 4u) >> 2);
                return (m + 0.5) / 64.0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 screenPx = i.screen.xy / i.screen.w * _ScreenParams.xy;
                float2 cell = floor(screenPx / _PixelSize);
                // sample the scene at the centre of each chunky pixel
                float2 uv = i.uv + ((cell + 0.5) * _PixelSize - screenPx) / _ScreenParams.xy;
                fixed3 c = tex2D(_MainTex, uv).rgb;
                float lum = dot(c, float3(0.299, 0.587, 0.114));
                lum = saturate((lum - 0.5) * _Contrast + 0.5 + _Bias);
                float th = bayer8(cell);
                float v = lum * 3.0;
                float idx = floor(v) + step(th, frac(v));
                fixed3 col = idx < 0.5 ? _C0.rgb : idx < 1.5 ? _C1.rgb : idx < 2.5 ? _C2.rgb : _C3.rgb;
                // keep soft tones under the halftone (the references are dithered, not posterized)
                fixed3 softTone = v < 1 ? lerp(_C0.rgb, _C1.rgb, saturate(v)) : v < 2 ? lerp(_C1.rgb, _C2.rgb, v - 1) : lerp(_C2.rgb, _C3.rgb, saturate(v - 2));
                col = lerp(col, softTone, _Smooth);
                if (th < _Dissolve) col = _C0.rgb;
                return fixed4(col, i.color.a);
            }
            ENDCG
        }
    }
}
