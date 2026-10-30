Shader "WaterSort/UI/Icon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Kind ("Kind", Float) = 0
        _IconColor ("Icon colour", Vector) = (1, 1, 1, 1)
        _Outline ("Outline colour", Vector) = (0.1, 0.2, 0.4, 1)
        _Stars ("Filled stars", Float) = 3
        _Anim ("Star pop-in (0..3)", Float) = 3
        _Dim ("Dim", Range(0,1)) = 0

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

            float4 _IconColor, _Outline;
            float _Kind, _Stars, _Anim, _Dim;

            static const float PI = 3.14159265;

            float sdSeg(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
            }

            float sdTri(float2 p, float2 a, float2 b, float2 c)
            {
                float2 e0 = b - a, e1 = c - b, e2 = a - c;
                float2 v0 = p - a, v1 = p - b, v2 = p - c;
                float2 pq0 = v0 - e0 * saturate(dot(v0, e0) / dot(e0, e0));
                float2 pq1 = v1 - e1 * saturate(dot(v1, e1) / dot(e1, e1));
                float2 pq2 = v2 - e2 * saturate(dot(v2, e2) / dot(e2, e2));
                float s = sign(e0.x * e2.y - e0.y * e2.x);
                float2 d = min(min(float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                                   float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                               float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
                return -sqrt(d.x) * sign(d.y);
            }

            float sdStar5(float2 p, float r, float rf)
            {
                const float2 k1 = float2(0.809016994375, -0.587785252292);
                const float2 k2 = float2(-k1.x, k1.y);
                p.x = abs(p.x);
                p -= 2.0 * max(dot(k1, p), 0.0) * k1;
                p -= 2.0 * max(dot(k2, p), 0.0) * k2;
                p.x = abs(p.x);
                p.y -= r;
                float2 ba = rf * float2(-k1.y, k1.x) - float2(0, 1);
                float h = clamp(dot(p, ba) / dot(ba, ba), 0.0, r);
                return length(p - ba * h) * sign(p.y * ba.x - p.x * ba.y);
            }

            // Ring of given radius/half-thickness restricted to an angular window (centre angle, half width).
            float sdArcRing(float2 p, float R, float t, float centre, float halfWidth)
            {
                float ang = atan2(p.y, p.x);
                float da = abs(fmod(ang - centre + PI * 3.0, PI * 2.0) - PI);
                float ring = abs(length(p) - R) - t;
                float2 e1 = R * float2(cos(centre + halfWidth), sin(centre + halfWidth));
                float2 e2 = R * float2(cos(centre - halfWidth), sin(centre - halfWidth));
                float caps = min(length(p - e1), length(p - e2)) - t;
                return da <= halfWidth ? ring : caps;
            }

            // Circular arrow. dir = +1 clockwise (restart), -1 counter clockwise (undo).
            float sdCircArrow(float2 p, float dir)
            {
                p.x *= dir;
                float gapC = PI * 0.30;
                float gapH = PI * 0.22;
                float R = 0.5;
                float t = 0.10;
                // Arc occupies everything except the gap window.
                float d = sdArcRing(p, R, t, gapC + PI, PI - gapH);
                float phi = gapC + gapH;                         // head sits on the gap edge
                float2 pos = R * float2(cos(phi), sin(phi));
                float2 tg = float2(sin(phi), -cos(phi));        // clockwise travel direction
                float2 rad = float2(cos(phi), sin(phi));
                float2 tip = pos + tg * 0.34;
                float2 b1 = pos + rad * 0.27 - tg * 0.04;
                float2 b2 = pos - rad * 0.27 - tg * 0.04;
                return min(d, sdTri(p, tip, b1, b2) - 0.02);
            }

            float sdSpeaker(float2 p, float on)
            {
                float d = WS_RoundBox(p - float2(-0.5, 0.0), float2(0.14, 0.2), 0.04);
                float2 a = float2(-0.42, 0.2), b = float2(0.02, 0.58), c = float2(0.02, -0.58), e = float2(-0.42, -0.2);
                d = min(d, sdTri(p, a, b, c));
                d = min(d, sdTri(p, a, c, e));
                if (on > 0.5)
                {
                    float2 q = p - float2(0.0, 0.0);
                    float w = 0.62;
                    float ang = abs(atan2(q.y, q.x));
                    float r1 = abs(length(q) - 0.40) - 0.065;
                    float r2 = abs(length(q) - 0.68) - 0.065;
                    d = min(d, max(r1, ang - w));
                    d = min(d, max(r2, ang - w));
                }
                else
                {
                    d = min(d, sdSeg(p, float2(0.2, 0.26), float2(0.72, -0.26)) - 0.07);
                    d = min(d, sdSeg(p, float2(0.2, -0.26), float2(0.72, 0.26)) - 0.07);
                }
                return d;
            }

            // Returns distance (negative inside). Also reports a secondary mask for details.
            float shapeDist(float2 p, out float detail)
            {
                detail = 0.0;
                int k = (int)(_Kind + 0.5);
                if (k == 1) // lock
                {
                    float body = WS_RoundBox(p - float2(0, -0.2), float2(0.5, 0.38), 0.1);
                    float ang = p.y - 0.14;
                    float arc = max(abs(length(p - float2(0, 0.14)) - 0.3) - 0.09, -ang);
                    float legs = max(abs(abs(p.x) - 0.3) - 0.09, max(p.y - 0.14, -0.1 - p.y));
                    float hole = min(length(p - float2(0, -0.14)) - 0.1, WS_RoundBox(p - float2(0, -0.28), float2(0.04, 0.12), 0.02));
                    detail = hole;
                    return min(body, min(arc, legs));
                }
                if (k == 2) // chevron left
                {
                    float2 a = float2(0.22, 0.52), b = float2(-0.24, 0.0), c = float2(0.22, -0.52);
                    return min(sdSeg(p, a, b), sdSeg(p, b, c)) - 0.13;
                }
                if (k == 3) return sdCircArrow(p, -1.0);   // undo
                if (k == 4) return sdCircArrow(p, 1.0);    // restart
                if (k == 5) // plus
                {
                    return min(WS_RoundBox(p, float2(0.52, 0.14), 0.1), WS_RoundBox(p, float2(0.14, 0.52), 0.1));
                }
                if (k == 6) return sdSpeaker(p, 1.0);
                if (k == 7) return sdSpeaker(p, 0.0);
                if (k == 8) return sdStar5(p, 0.95, 0.38);
                return 1.0;
            }

            float4 frag(v2f_ws IN) : SV_Target
            {
                float4 col = float4(0, 0, 0, 0);
                int k = (int)(_Kind + 0.5);

                if (k == 0)
                {
                    // Three stars in a row.
                    float x3 = IN.uv.x * 3.0;
                    float cell = floor(x3);
                    float2 p = float2(frac(x3) - 0.5, IN.uv.y - 0.5) * 2.0;
                    p.y -= 0.05;
                    float filled = step(cell + 0.5, _Stars);
                    float pop = saturate(_Anim - cell);
                    float sc = lerp(0.001, 1.0, pop) * (1.0 + sin(pop * 3.14159) * 0.35);
                    p /= max(sc, 0.001);

                    float d = sdStar5(p, 0.82, 0.42);
                    float aaw = max(fwidth(d), 0.002) * 1.2;
                    float g = saturate(p.y * 0.5 + 0.5);
                    float3 fillCol = lerp(float3(1.0, 0.62, 0.12), float3(1.0, 0.93, 0.35), g);
                    fillCol += smoothstep(0.1, 0.0, abs(d + 0.14)) * 0.12;
                    float3 emptyCol = float3(0.12, 0.2, 0.38);
                    float3 inner = lerp(emptyCol, fillCol, filled);

                    float outline = 1.0 - smoothstep(0.07 - aaw, 0.07 + aaw, d);
                    float body = 1.0 - smoothstep(-aaw, aaw, d);
                    float3 outCol = lerp(float3(0.05, 0.1, 0.25), float3(0.62, 0.3, 0.05), filled);
                    col = WS_Over(col, outCol, outline * (filled > 0.5 ? 1.0 : 0.55) * step(0.001, pop));
                    col = WS_Over(col, inner, body * (filled > 0.5 ? 1.0 : 0.75) * step(0.001, pop));
                    return WS_Finish(IN, col);
                }

                float2 p = (IN.uv - 0.5) * 2.0 * 1.12;
                float detail;
                float d = shapeDist(p, detail);
                float aaw = max(fwidth(d), 0.002) * 1.2;

                float outline = 1.0 - smoothstep(0.08 - aaw, 0.08 + aaw, d);
                float body = 1.0 - smoothstep(-aaw, aaw, d);
                float g = saturate(p.y * 0.5 + 0.5);
                float3 ic = _IconColor.rgb * (0.88 + 0.16 * g);
                ic = lerp(ic, ic * 0.55, _Dim);

                col = WS_Over(col, _Outline.rgb, outline);
                col = WS_Over(col, ic, body);
                if (k == 1)
                {
                    float hm = 1.0 - smoothstep(-aaw, aaw, detail);
                    col = WS_Over(col, _Outline.rgb, hm * body);
                }
                return WS_Finish(IN, col);
            }
            ENDCG
        }
    }
}
