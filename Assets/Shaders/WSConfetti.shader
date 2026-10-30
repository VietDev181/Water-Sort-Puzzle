Shader "WaterSort/UI/Confetti"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0,1)) = 1
        _Start ("Start time", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_ws
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "WSCommon.cginc"

            float _Intensity, _Start;

            float3 Hue(float h)
            {
                float3 k = frac(h + float3(0.0, 0.6667, 0.3333));
                return saturate(abs(k * 6.0 - 3.0) - 1.0) * 0.75 + 0.25;
            }

            float4 frag(v2f_ws IN) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = float2(IN.uv.x * aspect, IN.uv.y);
                float t = max(_Time.y - _Start, 0.0);
                float4 col = float4(0, 0, 0, 0);

                for (int i = 0; i < 26; i++)
                {
                    float fi = (float)i;
                    float h1 = WS_Hash11(fi + 1.0);
                    float h2 = WS_Hash11(fi + 101.0);
                    float h3 = WS_Hash11(fi + 201.0);
                    float h4 = WS_Hash11(fi + 301.0);

                    float speed = 0.16 + 0.22 * h2;
                    float life = frac(t * speed + h3);
                    float launch = step(h3 * 0.0 + 0.0, t);          // all pieces start at once
                    float y = 1.15 - life * 1.4;
                    float x = h1 * aspect + sin(t * (1.5 + h4 * 2.0) + h3 * 6.28) * 0.04;
                    float2 c = float2(x, y);

                    float ang = t * (2.0 + h2 * 5.0) + h4 * 6.28;
                    float2 d = p - c;
                    float cs = cos(ang), sn = sin(ang);
                    d = float2(cs * d.x - sn * d.y, sn * d.x + cs * d.y);
                    d.y *= lerp(1.0, abs(sin(t * (3.0 + h1 * 4.0) + h2 * 6.28)) + 0.25, 0.8); // flutter
                    float size = 0.018 + 0.016 * h4;
                    float box = WS_RoundBox(d, float2(size, size * 0.55), size * 0.15);
                    float a = (1.0 - smoothstep(0.0, 0.003, box)) * launch;
                    col = WS_Over(col, Hue(h1 * 3.1 + h2), a);
                }

                col.a *= _Intensity;
                return WS_Finish(IN, col);
            }
            ENDCG
        }
    }
}
