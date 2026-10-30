Shader "WaterSort/UI/Rays"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RayColor ("Ray colour", Vector) = (1, 0.9, 0.45, 1)
        _Count ("Ray count", Float) = 12
        _Speed ("Rotation speed", Float) = 0.12

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

            float4 _RayColor;
            float _Count, _Speed;

            float4 frag(v2f_ws IN) : SV_Target
            {
                float2 p = (IN.uv - 0.5) * 2.0;
                float r = length(p);
                float ang = atan2(p.y, p.x) + _Time.y * _Speed * 6.2831853;
                float wedge = smoothstep(0.35, 0.65, abs(frac(ang / 6.2831853 * _Count) - 0.5) * 2.0);
                float radial = smoothstep(1.0, 0.15, r) * smoothstep(0.0, 0.12, r);
                float glow = smoothstep(0.55, 0.0, r) * 0.5;
                float a = saturate(wedge * radial * 0.55 + glow);
                return WS_Finish(IN, float4(_RayColor.rgb, a));
            }
            ENDCG
        }
    }
}
