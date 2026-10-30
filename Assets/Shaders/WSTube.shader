Shader "WaterSort/UI/Tube"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _C0 ("Layer 0 (bottom)", Vector) = (0.9, 0.2, 0.2, 1)
        _C1 ("Layer 1", Vector) = (0.2, 0.4, 0.9, 1)
        _C2 ("Layer 2", Vector) = (0.95, 0.8, 0.2, 1)
        _C3 ("Layer 3 (top)", Vector) = (0.2, 0.7, 0.3, 1)
        _Amounts ("Layer amounts (units)", Vector) = (0, 0, 0, 0)
        _Size ("Size (px)", Vector) = (200, 540, 0, 0)
        _Pivot ("Pivot (uv)", Vector) = (0.5, 0.5, 0, 0)
        _Tilt ("Tilt (rad, ccw)", Float) = 0
        _Wobble ("Wobble", Float) = 0
        _Shadow ("Shadow", Range(0,1)) = 1
        _Glow ("Glow", Range(0,1)) = 0
        _GlowColor ("Glow colour", Vector) = (1, 1, 1, 1)
        _Done ("Done", Range(0,1)) = 0

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

            float4 _C0, _C1, _C2, _C3, _Amounts, _Size, _Pivot, _GlowColor;
            float _Tilt, _Wobble, _Shadow, _Glow, _Done;

            // Tube with a semicircular bottom. p.x centred, p.y absolute from the bottom of the rect.
            float sdTube(float2 p, float r, float yb, float yt)
            {
                float cy = yb + r;
                if (p.y < cy)
                    return length(p - float2(0, cy)) - r;
                return max(abs(p.x) - r, p.y - yt);
            }

            float4 frag(v2f_ws IN) : SV_Target
            {
                float W = _Size.x;
                float H = _Size.y;
                float2 p = float2((IN.uv.x - 0.5) * W, IN.uv.y * H);

                // Geometry (kept in sync with TubeView.cs).
                float hw = 0.30 * W;
                float yb = 0.30 * W;
                float yt = H - 0.10 * W;
                float lipH = 0.085 * W;
                float wall = 0.055 * W;
                float rIn = hw - wall;
                float lipMargin = 0.11 * W;
                float sh = (yt - yb - wall - lipMargin) / 4.0;
                float aa = 1.4;

                float dOut = sdTube(p, hw, yb, yt);
                float dIn = sdTube(p, rIn, yb + wall, yt);
                float inMask = 1.0 - smoothstep(-aa, aa, dIn);
                float outMask = 1.0 - smoothstep(-aa, aa, dOut);

                float4 col = float4(0, 0, 0, 0);

                // Ground shadow.
                float2 se = float2(p.x / (hw * 1.25), (p.y - yb * 0.62) / (yb * 0.5));
                float shadow = (1.0 - smoothstep(0.25, 1.0, length(se))) * 0.34 * _Shadow;
                col = WS_Over(col, float3(0.02, 0.05, 0.16), shadow);

                // Outer glow when selected.
                float glow = _Glow * exp(-max(dOut, 0.0) / (0.055 * W)) * (1.0 - outMask);
                col = WS_Over(col, _GlowColor.rgb, glow * 0.85);

                // Glass interior tint with depth.
                float depth = saturate((p.y - yb) / (yt - yb));
                float3 glassIn = lerp(float3(0.55, 0.78, 0.95), float3(0.80, 0.93, 1.0), depth);
                col = WS_Over(col, glassIn, inMask * 0.16);

                // Liquid in a frame that stays aligned with gravity when the tube tilts.
                float2 up = float2(sin(_Tilt), cos(_Tilt));
                float2 P = float2((_Pivot.x - 0.5) * W, _Pivot.y * H);
                float s = dot(p - P, up) + P.y;
                float2 cIn = float2(0, yb + wall + rIn);
                float sb = dot(cIn - P, up) + P.y - rIn;

                float4 A = _Amounts;
                float b1 = A.x;
                float b2 = A.x + A.y;
                float b3 = b2 + A.z;
                float b4 = b3 + A.w;

                float wobAmp = (_Wobble * 0.16 + 0.015) * sh;
                float wave = sin(p.x / rIn * 3.2 + _Time.y * 7.0) * wobAmp
                           + sin(p.x / rIn * 5.7 - _Time.y * 5.1) * wobAmp * 0.5;
                float u = (s - sb - wave) / sh;

                float liquid = (1.0 - smoothstep(-1.4 / sh, 1.4 / sh, u - b4)) * step(0.001, b4) * inMask;
                if (liquid > 0.001)
                {
                    float3 lc = _C0.rgb;
                    lc = lerp(lc, _C1.rgb, smoothstep(b1 - 0.02, b1 + 0.02, u) * step(0.001, A.y));
                    lc = lerp(lc, _C2.rgb, smoothstep(b2 - 0.02, b2 + 0.02, u) * step(0.001, A.z));
                    lc = lerp(lc, _C3.rgb, smoothstep(b3 - 0.02, b3 + 0.02, u) * step(0.001, A.w));

                    float xn = p.x / rIn;
                    float body = 0.74 + 0.26 * saturate(1.0 - xn * xn);
                    float3 lcol = lc * body;
                    // Soft inner highlight band on the left.
                    lcol += lc * pow(saturate(1.0 - abs(xn + 0.52) * 3.2), 2.0) * 0.28;
                    // Darker towards the bottom, lighter at the surface.
                    float depthL = saturate(u / max(b4, 0.001));
                    lcol *= 0.86 + 0.2 * depthL;
                    // Layer separators.
                    float sep = 0.0;
                    sep += smoothstep(0.035, 0.0, abs(u - b1)) * step(0.001, A.x) * step(0.001, A.y);
                    sep += smoothstep(0.035, 0.0, abs(u - b2)) * step(0.001, A.y) * step(0.001, A.z);
                    sep += smoothstep(0.035, 0.0, abs(u - b3)) * step(0.001, A.z) * step(0.001, A.w);
                    lcol *= 1.0 - sep * 0.12;
                    // Bright meniscus on the surface.
                    float top = (b4 - u) * sh;
                    lcol += smoothstep(0.045 * W, 0.0, top) * 0.28;
                    lcol += smoothstep(0.012 * W, 0.0, abs(top - 0.006 * W)) * 0.3;

                    col = WS_Over(col, lcol, liquid * 0.97);
                }

                // Glass wall.
                float band = outMask * smoothstep(-wall - aa, -wall + aa, dOut);
                float rim = smoothstep(2.6, 0.0, abs(dOut + 1.4));
                float leftLight = smoothstep(0.2, -0.9, p.x / hw);
                col = WS_Over(col, float3(0.78, 0.93, 1.0), band * (0.20 + 0.14 * leftLight));
                col = WS_Over(col, float3(0.95, 1.0, 1.0), rim * 0.55);

                // Specular streaks.
                float yLo = yb + rIn * 1.3;
                float yHi = yt - lipH * 2.2;
                float vfade = smoothstep(yLo - rIn * 0.2, yLo + rIn * 0.8, p.y) * (1.0 - smoothstep(yHi - rIn * 0.8, yHi, p.y));
                float streakL = smoothstep(rIn * 0.11, rIn * 0.03, abs(p.x + rIn * 0.58)) * vfade;
                float streakR = smoothstep(rIn * 0.07, rIn * 0.02, abs(p.x - rIn * 0.62)) * vfade * 0.6;
                float dot1 = smoothstep(rIn * 0.17, rIn * 0.06, length(p - float2(-rIn * 0.5, yb + rIn * 0.75)));
                col = WS_Over(col, float3(1, 1, 1), saturate(streakL * 0.5 + streakR * 0.3 + dot1 * 0.4) * inMask);

                // Rim of the opening.
                float2 lc2 = float2(0, yt - lipH * 0.5);
                float dLip = WS_RoundBox(p - lc2, float2(hw + 0.04 * W, lipH * 0.5), lipH * 0.5);
                float lipMask = 1.0 - smoothstep(-aa, aa, dLip);
                float lipEdge = smoothstep(2.4, 0.0, abs(dLip + 1.2));
                float lipTop = smoothstep(0.5, 1.0, (p.y - (yt - lipH)) / lipH);
                float3 lipCol = lerp(float3(0.62, 0.82, 0.95), float3(0.92, 0.99, 1.0), lipTop);
                col = WS_Over(col, lipCol, lipMask * 0.62);
                col = WS_Over(col, float3(1, 1, 1), lipEdge * 0.65);

                // Completed tube: a stopper drops into the opening.
                if (_Done > 0.001)
                {
                    float drop = (1.0 - saturate(_Done)) * 0.16 * W;
                    float a = saturate(_Done * 4.0);
                    float2 flangeC = float2(0, yt + 0.028 * W + drop);
                    float2 plugC = float2(0, yt - 0.03 * W + drop);
                    float dFlange = WS_RoundBox(p - flangeC, float2(hw + 0.05 * W, 0.034 * W), 0.03 * W);
                    float dPlug = WS_RoundBox(p - plugC, float2(rIn * 0.78, 0.055 * W), 0.02 * W);

                    float3 baseC = _C0.rgb;
                    float3 darkC = baseC * 0.55;
                    float plugMask = 1.0 - smoothstep(-aa, aa, dPlug);
                    float plugEdge = 1.0 - smoothstep(-aa, aa, dPlug + 2.0);
                    col = WS_Over(col, darkC * 0.6, plugMask * a);
                    col = WS_Over(col, baseC * 0.85, plugEdge * a);

                    float flMask = 1.0 - smoothstep(-aa, aa, dFlange);
                    float flIn = 1.0 - smoothstep(-aa, aa, dFlange + 2.5);
                    float fy = saturate((p.y - (flangeC.y - 0.034 * W)) / (0.068 * W));
                    col = WS_Over(col, darkC * 0.6, flMask * a);
                    col = WS_Over(col, lerp(baseC * 0.9, baseC * 1.25 + 0.12, fy), flIn * a);
                    float shine = smoothstep(0.03 * W, 0.0, abs(p.y - (flangeC.y + 0.016 * W))) * smoothstep(hw, hw * 0.5, abs(p.x));
                    col = WS_Over(col, float3(1, 1, 1), shine * flIn * a * 0.55);
                }

                return WS_Finish(IN, col);
            }
            ENDCG
        }
    }
}
