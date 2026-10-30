Shader "WaterSort/UI/Button"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ColorTop ("Top", Vector) = (0.5, 0.9, 0.5, 1)
        _ColorBottom ("Bottom", Vector) = (0.1, 0.6, 0.3, 1)
        _Size ("Size (px)", Vector) = (300, 120, 0, 0)
        _Pad ("Padding (px)", Float) = 16
        _Radius ("Radius (px)", Float) = 30
        _Press ("Press", Range(0,1)) = 0
        _Locked ("Locked", Range(0,1)) = 0
        _Shine ("Shine", Range(0,1)) = 1
        _Phase ("Phase", Float) = 0
        _Flat ("Flat panel", Range(0,1)) = 0

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

            float4 _ColorTop, _ColorBottom, _Size;
            float _Pad, _Radius, _Press, _Locked, _Shine, _Phase, _Flat;

            float4 frag(v2f_ws IN) : SV_Target
            {
                float2 p = (IN.uv - 0.5) * _Size.xy;
                float2 bhalf = _Size.xy * 0.5 - _Pad;
                float r = min(_Radius, min(bhalf.x, bhalf.y));

                float4 col = float4(0, 0, 0, 0);

                // Drop shadow.
                float2 so = float2(0, -_Pad * 0.45 * (1.0 - _Press * 0.6));
                float ds = WS_RoundBox(p - so, bhalf, r);
                float sa = (1.0 - smoothstep(-_Pad * 0.25, _Pad * 0.9, ds)) * 0.32 * (1.0 - _Flat);
                col = WS_Over(col, float3(0.02, 0.06, 0.18), sa);

                // 3D ledge below the face; shrinks while pressed.
                float ledge = lerp(0.075, 0.0, _Flat) * (bhalf.y * 2.0) * (1.0 - _Press * 0.75);
                float dBody = WS_RoundBox(p, bhalf, r);
                float3 dark = _ColorBottom.rgb * 0.55;
                float aaa = 1.5;

                // Outline + ledge.
                float outline = 1.0 - smoothstep(-aaa, aaa, dBody);
                col = WS_Over(col, dark * 0.55, outline);
                float ledgeMask = 1.0 - smoothstep(-aaa, aaa, dBody + 3.0);
                col = WS_Over(col, dark, ledgeMask);

                // Face.
                float2 fc = float2(0, ledge * 0.5 - _Press * 0.0);
                float2 fh = float2(bhalf.x - 3.0, bhalf.y - ledge * 0.5 - 3.0);
                float dFace = WS_RoundBox(p - fc, fh, max(r - 3.0, 2.0));
                float face = 1.0 - smoothstep(-aaa, aaa, dFace);

                float g = saturate((p.y - fc.y + fh.y) / (2.0 * fh.y));
                float3 fcol = lerp(_ColorBottom.rgb, _ColorTop.rgb, smoothstep(0.0, 1.0, g));

                // Glossy top highlight.
                float gloss = smoothstep(0.52, 0.95, g) * (1.0 - smoothstep(0.0, 0.8, abs(p.x) / fh.x * 0.5 + (1.0 - g) * 0.3));
                fcol += gloss * 0.18;
                // Inner top edge light.
                float inner = smoothstep(4.0, 0.0, -dFace) * smoothstep(0.55, 0.9, g);
                fcol += inner * 0.25;
                // Subtle darkening at the bottom of the face.
                fcol *= 1.0 - smoothstep(0.25, 0.0, g) * 0.12;

                // Animated shine sweep.
                float sweep = frac(_Time.y * 0.22 + _Phase) * 2.2 - 0.6;
                float cdiag = (p.x / _Size.x + 0.5) + (p.y / _Size.x) * 0.45;
                float band = smoothstep(0.1, 0.0, abs(cdiag - sweep)) * 0.32 * _Shine;
                fcol += band;

                // Locked state: desaturate and darken.
                float lum = dot(fcol, float3(0.299, 0.587, 0.114));
                fcol = lerp(fcol, float3(lum, lum, lum) * 0.62, _Locked);
                fcol *= 1.0 - _Press * 0.1;

                col = WS_Over(col, fcol, face);
                return WS_Finish(IN, col);
            }
            ENDCG
        }
    }
}
