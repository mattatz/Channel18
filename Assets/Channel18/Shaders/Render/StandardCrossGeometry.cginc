
#include "UnityCG.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardUtils.cginc"

#if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
#define PASS_CUBE_SHADOWCASTER
#endif

half4 _Color, _Emission;
sampler2D _MainTex;
float4 _MainTex_ST;

half _Glossiness;
half _Metallic;

half _Extrusion, _Thickness;

float _Size, _Distance;

#include "../Common/Quaternion.cginc"
#include "../Common/Matrix.cginc"
#include "../Common/ProceduralMidairGrid.cginc"
#include "./Lattice.cginc"

StructuredBuffer<Grid> _Grids;

struct Attributes
{
    float4 position : POSITION;
    float distance : TEXCOORD0;
    float4 rotation : TANGENT;
    float2 scale : NORMAL;
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
    Grid grid = _Grids[vid];

    // input.position.xyz = grid.position.xyz;
    // input.position.xyz = lattice_position(grid.position.xyz);
	float3 local = lattice_position(grid.position.xyz);
    input.position.xyz = mul(unity_ObjectToWorld, float4(local, 1)).xyz;

    input.rotation = grid.rotation;
    input.scale.xy = grid.scale.xy;

    float3 vp = UnityObjectToViewPos(float4(local, 1)).xyz;
    input.distance = length(vp) / _Distance;

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

void addFace (inout TriangleStream<Varyings> OUT, float4 p[4], float3 wnrm)
{
    // float3 wnrm = UnityObjectToWorldNormal(normal);
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

void addCube (float3 pos, float3 right, float3 up, float3 forward, inout TriangleStream<Varyings> OUT)
{
    float4 v[4];

	// forward
    v[0] = float4(pos + forward + right - up, 1.0f);
    v[1] = float4(pos + forward + right + up, 1.0f);
    v[2] = float4(pos + forward - right - up, 1.0f);
    v[3] = float4(pos + forward - right + up, 1.0f);
    addFace(OUT, v, normalize(forward));

	// back
    v[0] = float4(pos - forward - right - up, 1.0f);
    v[1] = float4(pos - forward - right + up, 1.0f);
    v[2] = float4(pos - forward + right - up, 1.0f);
    v[3] = float4(pos - forward + right + up, 1.0f);
    addFace(OUT, v, -normalize(forward));

	// up
    v[0] = float4(pos - forward + right + up, 1.0f);
    v[1] = float4(pos - forward - right + up, 1.0f);
    v[2] = float4(pos + forward + right + up, 1.0f);
    v[3] = float4(pos + forward - right + up, 1.0f);
    addFace(OUT, v, normalize(up));

	// down
    v[0] = float4(pos + forward + right - up, 1.0f);
    v[1] = float4(pos + forward - right - up, 1.0f);
    v[2] = float4(pos - forward + right - up, 1.0f);
    v[3] = float4(pos - forward - right - up, 1.0f);
    addFace(OUT, v, -normalize(up));

	// left
    v[0] = float4(pos + forward - right - up, 1.0f);
    v[1] = float4(pos + forward - right + up, 1.0f);
    v[2] = float4(pos - forward - right - up, 1.0f);
    v[3] = float4(pos - forward - right + up, 1.0f);
    addFace(OUT, v, -normalize(right));

	// right
    v[0] = float4(pos - forward + right + up, 1.0f);
    v[1] = float4(pos + forward + right + up, 1.0f);
    v[2] = float4(pos - forward + right - up, 1.0f);
    v[3] = float4(pos + forward + right - up, 1.0f);
    addFace(OUT, v, normalize(right));
}

[maxvertexcount(72)]
void Geometry (point Attributes IN[1], inout TriangleStream<Varyings> OUT) {
    float3 pos = IN[0].position.xyz;
    float hs = _Size * 0.5f * saturate(IN[0].distance);
    float3 right = rotate_vector(float3(1, 0, 0), IN[0].rotation) * hs;
    float3 up = rotate_vector(float3(0, 1, 0), IN[0].rotation) * hs;
    float3 forward = rotate_vector(float3(0, 0, 1), IN[0].rotation) * hs;

    float extrusion = lerp(_Thickness, _Extrusion, IN[0].scale.x);
    float thickness = min(_Thickness, _Extrusion) * IN[0].scale.y;

    addCube(pos, right * thickness, up * thickness, forward * extrusion, OUT);
    addCube(pos, right * extrusion, up * thickness, forward * thickness, OUT);
    addCube(pos, right * thickness, up * extrusion, forward * thickness, OUT);
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
    outEmission = _Emission + half4(sh * c_diff, 1);
}

#endif
