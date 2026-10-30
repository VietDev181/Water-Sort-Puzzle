Shader "WaterSort/UI/Transition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Progress (0 open, 1 covered)", Range(0,1)) = 0
        _Fill ("Fill colour", Vector) = (0.03, 0.09, 0.26, 1)
        _Edge ("Edge colour", Vector) = (0.45, 0.9, 1.0, 1)

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

            float4 _Fill, _Edge;
            float _Progress;

            float4 frag(v2f_ws IN) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = (IN.uv - 0.5) * float2(aspect, 1.0);
                float maxR = length(float2(aspect, 1.0)) * 0.5 + 0.08;
                float r = lerp(maxR, -0.05, _Progress);

                // Watery, wobbling iris edge.
                float ang = atan2(p.y, p.x);
                r += sin(ang * 7.0 + _Time.y * 4.0) * 0.012 * sin(_Progress * 3.14159);
                float d = length(p) - r;

                float cover = smoothstep(-0.004, 0.004, d);
                float edge = smoothstep(0.05, 0.0, abs(d - 0.01)) * 0.8;
                float3 rgb = lerp(_Fill.rgb, _Edge.rgb, edge * (1.0 - cover * 0.0));
                float a = saturate(cover + edge * 0.0);
                float ring = smoothstep(0.03, 0.0, abs(d + 0.012)) * (1.0 - cover);
                float4 col = float4(rgb, cover);
                col = WS_Over(col, _Edge.rgb, ring * 0.9);
                return WS_Finish(IN, col);
            }
            ENDCG
        }
    }
}
