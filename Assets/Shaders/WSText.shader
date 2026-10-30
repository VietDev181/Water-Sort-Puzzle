Shader "WaterSort/UI/Text"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Top ("Top colour", Vector) = (1, 1, 1, 1)
        _Bottom ("Bottom colour", Vector) = (0.8, 0.9, 1, 1)
        _Rect ("Gradient rect (xmin,xmax,ymin,ymax)", Vector) = (-100, 100, -30, 30)
        _Shine ("Shine", Range(0,1)) = 0
        _Phase ("Phase", Float) = 0

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

            sampler2D _MainTex;
            float4 _Top, _Bottom, _Rect;
            float _Shine, _Phase;

            float4 frag(v2f_ws IN) : SV_Target
            {
                float coverage = tex2D(_MainTex, IN.uv).a;

                float gy = saturate((IN.local.y - _Rect.z) / max(_Rect.w - _Rect.z, 1.0));
                float3 grad = lerp(_Bottom.rgb, _Top.rgb, smoothstep(0.0, 1.0, gy));

                float gx = (IN.local.x - _Rect.x) / max(_Rect.y - _Rect.x, 1.0);
                float sweep = frac(_Time.y * 0.28 + _Phase) * 2.0 - 0.5;
                float band = smoothstep(0.12, 0.0, abs(gx + gy * 0.25 - sweep)) * 0.55 * _Shine;
                grad += band;

                // The vertex colour tints (white keeps the gradient, black gives outlines/shadows).
                float3 rgb = grad * IN.color.rgb;
                return WS_Finish(IN, float4(rgb, coverage));
            }
            ENDCG
        }
    }
}
