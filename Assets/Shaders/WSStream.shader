Shader "WaterSort/UI/Stream"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Liquid ("Liquid colour", Vector) = (0.2, 0.5, 0.9, 1)
        _Size ("Size (px)", Vector) = (40, 300, 0, 0)
        _Grow ("Head (0..1 from top)", Range(0,1)) = 1
        _Tail ("Tail (0..1 from top)", Range(0,1)) = 0

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

            float4 _Liquid, _Size;
            float _Grow, _Tail;

            float4 frag(v2f_ws IN) : SV_Target
            {
                float x = IN.uv.x * 2.0 - 1.0;
                float t = 1.0 - IN.uv.y;                 // 0 at the lip, 1 at the target
                float len = _Size.y;

                float wob = sin(t * len / 38.0 - _Time.y * 22.0) * 0.07 * smoothstep(0.0, 0.3, t);
                float halfW = lerp(0.9, 0.62, t);
                float d = abs(x - wob) - halfW;
                float a = 1.0 - smoothstep(-0.04, 0.06, d);

                // Head (rounded) and tail.
                float headPx = (_Grow - t) * len;
                float tailPx = (t - _Tail) * len;
                a *= smoothstep(0.0, 14.0, headPx) * smoothstep(0.0, 6.0, tailPx);
                a *= step(0.001, _Grow);

                float core = exp(-pow((x + 0.28 - wob) / 0.3, 2.0));
                float3 c = _Liquid.rgb * (0.82 + 0.24 * core);
                c += smoothstep(0.05, 0.0, abs(x + 0.5 - wob)) * 0.22;

                // Flowing streaks.
                float streak = frac(t * len / 70.0 - _Time.y * 2.6 + x * 0.15);
                c += smoothstep(0.88, 1.0, streak) * 0.18;
                c *= 1.0 - smoothstep(0.55, 1.0, abs(x - wob)) * 0.22;

                return WS_Finish(IN, float4(c, a * 0.96));
            }
            ENDCG
        }
    }
}
