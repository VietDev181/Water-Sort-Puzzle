#ifndef WS_COMMON_INCLUDED
#define WS_COMMON_INCLUDED

#include "UnityCG.cginc"
#include "UnityUI.cginc"

// Shared plumbing for all Water Sort UI shaders.
// Colours are authored in gamma space (as Vector properties) and converted on output.

struct appdata_ws
{
    float4 vertex : POSITION;
    float4 color  : COLOR;
    float2 uv     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f_ws
{
    float4 pos   : SV_POSITION;
    fixed4 color : COLOR;
    float2 uv    : TEXCOORD0;
    float4 local : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

fixed4 _Color;
float4 _ClipRect;

v2f_ws vert_ws(appdata_ws v)
{
    v2f_ws o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(v2f_ws, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.local = v.vertex;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    o.color = v.color * _Color;
    return o;
}

float3 WS_Out(float3 c)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return c;
#else
    return GammaToLinearSpace(saturate(c));
#endif
}

float4 WS_Finish(v2f_ws IN, float4 c)
{
    c.a *= IN.color.a;
#ifdef UNITY_UI_CLIP_RECT
    c.a *= UnityGet2DClipping(IN.local.xy, _ClipRect);
#endif
    c.rgb = WS_Out(c.rgb);
    return c;
}

// Straight-alpha "over" compositing.
float4 WS_Over(float4 dst, float3 c, float a)
{
    float na = a + dst.a * (1.0 - a);
    float3 rgb = (c * a + dst.rgb * dst.a * (1.0 - a)) / max(na, 1e-4);
    return float4(rgb, na);
}

float WS_Hash11(float n) { return frac(sin(n * 127.1) * 43758.5453); }
float WS_Hash21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

float WS_RoundBox(float2 p, float2 b, float r)
{
    float2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

#endif
