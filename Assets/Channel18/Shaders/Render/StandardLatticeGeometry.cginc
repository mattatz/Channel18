
#include "UnityCG.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardUtils.cginc"

#if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
#define PASS_CUBE_SHADOWCASTER
#endif

half4 _Color;
sampler2D _MainTex;
float4 _MainTex_ST;

float _Glossiness;
float _Metallic;
float _Thickness, _Size;
float3 _Axis;

#include "../Common/Quaternion.cginc"
#include "../Common/Matrix.cginc"
#include "./Lattice.cginc"

struct Attributes
{
    float4 position : POSITION;
	float3 local : NORMAL;
};

struct Varyings
{
    float4 position : SV_POSITION;

#if defined(PASS_CUBE_SHADOWCASTER)
    float3 shadow : TEXCOORD0;
#elif defined(UNITY_PASS_SHADOWCASTER)
#else
    float3 normal : NORMAL;
    half3 ambient : TEXCOORD1;
    float3 wpos : TEXCOORD2;
#endif
};

Attributes Vertex(Attributes input, uint vid : SV_VertexID)
{
	float3 local = lattice_position(input.position.xyz);
	input.local.xyz = input.position.xyz;
    input.position.xyz = mul(unity_ObjectToWorld, float4(local, 1)).xyz;
    return input;
}

Varyings VertexOutput(in Varyings o, float4 wpos, float3 wnrm)
{
    // float3 wpos = mul(unity_ObjectToWorld, pos).xyz;

#if defined(PASS_CUBE_SHADOWCASTER)
    // Cube map shadow caster pass: Transfer the shadow vector.
    o.position = UnityWorldToClipPos(float4(wpos.xyz, 1));
    o.shadow = wpos.xyz - _LightPositionRange.xyz;

#elif defined(UNITY_PASS_SHADOWCASTER)
    // Default shadow caster pass: Apply the shadow bias.
    float scos = dot(wnrm, normalize(UnityWorldSpaceLightDir(wpos.xyz)));
    wpos.xyz -= wnrm * unity_LightShadowBias.z * sqrt(1 - scos * scos);
    o.position = UnityApplyLinearShadowBias(UnityWorldToClipPos(float4(wpos.xyz, 1)));

#else
    // GBuffer construction pass
    o.position = UnityWorldToClipPos(float4(wpos.xyz, 1));
    o.normal = wnrm;
    o.ambient = ShadeSHPerVertex(wnrm, 0);
    o.wpos = wpos.xyz;
#endif

    return o;
}

void addFace(inout TriangleStream<Varyings> OUT, float4 p[4], float3 wnrm)
{
    Varyings o = VertexOutput(o, p[0], wnrm);
    OUT.Append(o);

    o = VertexOutput(o, p[1], wnrm);
    OUT.Append(o);

    o = VertexOutput(o, p[2], wnrm);
    OUT.Append(o);

    o = VertexOutput(o, p[3], wnrm);
    OUT.Append(o);

    OUT.RestartStrip();
}

[maxvertexcount(72)]
void Geometry (in line Attributes IN[2], inout TriangleStream<Varyings> OUT) {
    float3 pos = (IN[0].position.xyz + IN[1].position.xyz) * 0.5;

    float3 tangent = (IN[1].position.xyz - pos);

    float3 dir = (IN[1].local.xyz - IN[0].local.xyz);
	float3 ndir = normalize(dir);
	float xaxis = step(0.999, abs(dot(ndir, float3(1, 0, 0))));
	float yaxis = step(0.999, abs(dot(ndir, float3(0, 1, 0))));
	float zaxis = step(0.999, abs(dot(ndir, float3(0, 0, 1))));

	float scale = max(max(_Axis.x * xaxis, _Axis.y * yaxis), _Axis.z * zaxis);
	float thickness = _Thickness * _Size * max(0, scale);

    float3 forward = normalize(tangent) * (length(tangent) + thickness);
    float3 nforward = normalize(forward);

    float3 ntmp = cross(nforward, lerp(float3(0, 1, 0), float3(1, 0, 0), yaxis));
    // float3 ntmp = cross(nforward, float3(0, 1, 0));
    float3 up = (cross(ntmp, nforward));
    float3 nup = normalize(up);
    float3 right = (cross(nforward, nup));
    float3 nright = normalize(right);

    up = nup * thickness;
    right = nright * thickness;

    float4 v[4];

    // forward
    v[0] = float4(pos + forward + right - up, 1.0f);
    v[1] = float4(pos + forward + right + up, 1.0f);
    v[2] = float4(pos + forward - right - up, 1.0f);
    v[3] = float4(pos + forward - right + up, 1.0f);
    addFace(OUT, v, nforward);

    // back
    v[0] = float4(pos - forward - right - up, 1.0f);
    v[1] = float4(pos - forward - right + up, 1.0f);
    v[2] = float4(pos - forward + right - up, 1.0f);
    v[3] = float4(pos - forward + right + up, 1.0f);
    addFace(OUT, v, -nforward);

    // up
    v[0] = float4(pos - forward + right + up, 1.0f);
    v[1] = float4(pos - forward - right + up, 1.0f);
    v[2] = float4(pos + forward + right + up, 1.0f);
    v[3] = float4(pos + forward - right + up, 1.0f);
    addFace(OUT, v, nup);

    // down
    v[0] = float4(pos + forward + right - up, 1.0f);
    v[1] = float4(pos + forward - right - up, 1.0f);
    v[2] = float4(pos - forward + right - up, 1.0f);
    v[3] = float4(pos - forward - right - up, 1.0f);
    addFace(OUT, v, -nup);

    // left
    v[0] = float4(pos + forward - right - up, 1.0f);
    v[1] = float4(pos + forward - right + up, 1.0f);
    v[2] = float4(pos - forward - right - up, 1.0f);
    v[3] = float4(pos - forward - right + up, 1.0f);
    addFace(OUT, v, -nright);

    // right
    v[0] = float4(pos - forward + right + up, 1.0f);
    v[1] = float4(pos + forward + right + up, 1.0f);
    v[2] = float4(pos - forward + right - up, 1.0f);
    v[3] = float4(pos + forward + right - up, 1.0f);
    addFace(OUT, v, nright);
};

//
// Fragment phase
//

#if defined(PASS_CUBE_SHADOWCASTER)

// Cube map shadow caster pass
half4 Fragment(Varyings input) : SV_Target
{
    float depth = length(input.shadow) + unity_LightShadowBias.x;
    return UnityEncodeCubeShadowDepth(depth * _LightPositionRange.w);
}

#elif defined(UNITY_PASS_SHADOWCASTER)

half4 Fragment() : SV_Target { return 0; }

#else

void Fragment (Varyings input, out half4 outGBuffer0 : SV_Target0, out half4 outGBuffer1 : SV_Target1, out half4 outGBuffer2 : SV_Target2, out half4 outEmission : SV_Target3) {
    half3 albedo = _Color.rgb;

    half3 c_diff, c_spec;
    half refl10;
    c_diff = DiffuseAndSpecularFromMetallic(
        albedo, _Metallic, // input
        c_spec, refl10 // output
    );

    UnityStandardData data;
    data.diffuseColor = c_diff;
    data.occlusion = 1.0;
    data.specularColor = c_spec;
    data.smoothness = _Glossiness;
    data.normalWorld = input.normal;
    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    half3 sh = ShadeSHPerPixel(data.normalWorld, input.ambient, input.wpos);
    outEmission = half4(sh * c_diff, 1);
}

#endif
