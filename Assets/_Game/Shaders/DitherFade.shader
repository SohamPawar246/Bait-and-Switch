// Full-screen transition overlay: covers the screen through a Bayer 8x8 threshold (_Amount 0 = clear, 1 = solid).
// The "dither dissolve" used by every screen change (docs/12-screens-ui-audio.md).
Shader "BaitSwitch/DitherFade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Colour", Color) = (0.118, 0.078, 0.157, 1)
        _Amount ("Amount", Range(0, 1)) = 0
        _PixelSize ("Pixel Size", Float) = 4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
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

            fixed4 _Color;
            float _Amount, _PixelSize;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float4 screen : TEXCOORD0; float4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screen = ComputeScreenPos(o.pos);
                o.color = v.color;
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
                float2 px = floor(i.screen.xy / i.screen.w * _ScreenParams.xy / _PixelSize);
                float a = step(bayer8(px), _Amount);
                return fixed4(_Color.rgb, a * _Color.a * i.color.a);
            }
            ENDCG
        }
    }
}
