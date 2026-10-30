Shader "WaterSort/UI/Background"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TopColor ("Top", Vector) = (0.30, 0.78, 0.92, 1)
        _MidColor ("Middle", Vector) = (0.16, 0.45, 0.82, 1)
        _BottomColor ("Bottom", Vector) = (0.07, 0.16, 0.44, 1)

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

            float4 _TopColor, _MidColor, _BottomColor;

            // Bubble field: one optional bubble per grid cell, drifting upwards.
            float3 Bubbles(float2 uv, float aspect, float scale, float speed, float seed)
            {
                float2 g = float2(uv.x * aspect, uv.y - _Time.y * speed) * scale;
                float2 id = floor(g);
                float2 f = frac(g) - 0.5;
                float h = WS_Hash21(id + seed);
                float present = step(0.55, h);
                float2 c = float2(frac(h * 13.7) - 0.5, frac(h * 91.3) - 0.5) * 0.45;
                c.x += sin(_Time.y * (0.6 + h) + h * 40.0) * 0.08;
                float r = lerp(0.07, 0.2, frac(h * 7.7));
                float d = length(f - c);
                float fill = smoothstep(r, r - 0.04, d) * 0.10;
                float ring = smoothstep(0.035, 0.0, abs(d - r)) * 0.45;
                float2 hl = f - c - float2(-r * 0.35, r * 0.35);
                float glint = smoothstep(r * 0.28, 0.0, length(hl)) * 0.55;
                return float3(fill + ring + glint, 0, 0) * present;
            }

            float4 frag(v2f_ws IN) : SV_Target
            {
                float2 uv = IN.uv;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float t = _Time.y;

                float3 col = lerp(_BottomColor.rgb, _MidColor.rgb, smoothstep(0.0, 0.55, uv.y));
                col = lerp(col, _TopColor.rgb, smoothstep(0.45, 1.0, uv.y));

                // Soft caustic light bands.
                float2 w = float2(uv.x * aspect, uv.y);
                float caustic = sin(w.x * 5.0 + sin(w.y * 4.0 + t * 0.6) * 1.8 + t * 0.5)
                              * sin(w.y * 6.0 - t * 0.35);
                caustic = smoothstep(0.35, 1.0, caustic);
                col += caustic * 0.07 * float3(0.8, 1.0, 1.0);


                // Bubbles in two parallax layers.
                float b = Bubbles(uv, aspect, 7.0, 0.05, 1.0).x + Bubbles(uv, aspect, 13.0, 0.09, 7.0).x * 0.7;
                col += b * float3(0.9, 1.0, 1.0);

                // Vignette.
                float2 vc = (uv - 0.5) * float2(aspect, 1.0);
                col *= 1.0 - smoothstep(0.35, 1.25, length(vc)) * 0.38;

                return WS_Finish(IN, float4(col, 1.0));
            }
            ENDCG
        }
    }
}
