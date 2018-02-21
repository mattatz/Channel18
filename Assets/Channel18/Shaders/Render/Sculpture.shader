Shader"VJ/Channel18/Sculpture"
{

	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        [Space] _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        [Gamma] _Metallic ("Metallic", Range(0, 1)) = 0

        _Scale ("Scale", Vector) = (1, 1, 1, -1)
        _Step ("Step", Vector) = (1, 2, 4, 8)
        _Noise ("Noise", Vector) = (0.25, 4.0, -1, -1)
        _CutoutDistance ("Cutout Distance", Float) = 0.01
	}

    CGINCLUDE

    #include "UnityCG.cginc"
    #include "UnityStandardCore.cginc"

    float3 _Scale;
    float3 _Noise;
    float4 _Step;
    float _CutoutDistance;

    struct appdata
    {
        float4 vertex : POSITION;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float3 world : NORMAL;
    };
    
    v2f vert (appdata IN)
    {
        v2f OUT;
        OUT.vertex = UnityObjectToClipPos(IN.vertex);
        OUT.world = mul(unity_ObjectToWorld, IN.vertex).xyz;
        return OUT;
    }

    float sdBox(float3 p, float3 b)
    {
        float3 d = abs(p) - b;
        return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
    }

    struct gbuffer_out
    {
        half4 diffuse : SV_Target0; // RT0: diffuse color (rgb), occlusion (a)
        half4 spec_smoothness : SV_Target1; // RT1: spec color (rgb), smoothness (a)
        half4 normal : SV_Target2; // RT2: normal (rgb), --unused, very low precision-- (a) 
        half4 emission : SV_Target3; // RT3: emission (rgb), --unused-- (a)
        float depth : SV_Depth;
    };

    float3 GetCameraPosition() {
        return _WorldSpaceCameraPos;
    }

    float3 GetCameraForward() {
        return -UNITY_MATRIX_V[2].xyz;
    }

    float3 localize(float3 p)
    {
        p = mul(unity_WorldToObject, float4(p, 1)).xyz * _Scale.xyz;
        return p;
    }

    // https://www.shadertoy.com/view/XtjSDK
    float4 grow = float4(1.0, 1.0, 1.0, 1.0);

    float3 vsin(float3 seed, float s)
    {
        // return float3(sin(seed.x * s), sin(seed.y * s), sin(seed.z * s));
        return s * sin(seed);
    }

    float3 mapP(float3 p)
    {
        float3 seed = p;
        seed.xyz *= _Noise.x;

        float4 scales = float4(1.0, 0.5, 0.25, 0.05) * _Noise.y;
        p.xyz += scales.x * vsin(seed.yzx, _Step.x) * grow.x;
        p.xyz += scales.y * vsin(seed.yzx, _Step.y) * grow.y;
        p.xyz += scales.z * vsin(seed.yzx, _Step.z) * grow.z;
        p.xyz += scales.w * vsin(seed.yzx, _Step.w) * grow.w;
        return p;
    }

    float map(float3 q)
    {
        q = localize(q);
        float3 p = mapP(q);
        float d = length(p) - 0.35 * _Scale.x;
        return d * _Noise.z;
    }

    float3 guess_normal(float3 p)
    {
        const float d = 0.001;
        return normalize(float3(
            map(p + float3(d, 0.0, 0.0)) - map(p + float3(-d, 0.0, 0.0)),
            map(p + float3(0.0, d, 0.0)) - map(p + float3(0.0, -d, 0.0)),
            map(p + float3(0.0, 0.0, d)) - map(p + float3(0.0, 0.0, -d))));
    }

    void raymarching(float3 world, const int num_steps, inout float o_total_distance, out float o_num_steps, out float o_last_distance, out float3 o_raypos)
    {
        float3 cam_pos = GetCameraPosition();

        float3 ray_dir = normalize(world - GetCameraPosition());
        o_raypos = world;

        o_num_steps = 0.0;
        o_last_distance = 0.0;
        for (int i = 0; i < num_steps; i++)
        {
            o_last_distance = map(o_raypos);
            o_total_distance += o_last_distance;
            o_raypos += ray_dir * o_last_distance;
            o_num_steps += 1.0;
            if (o_last_distance < 0.001)
            {
                break;
            }
        }

        float3 pl = localize(o_raypos);
        float d = sdBox(pl, _Scale * 0.5);
        clip(_CutoutDistance - d);
    }

    float ComputeDepth(float4 clippos)
    {
    #if defined(SHADER_TARGET_GLSL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
        return (clippos.z / clippos.w) * 0.5 + 0.5;
    #else
        return clippos.z / clippos.w;
    #endif
    }
                            
    gbuffer_out frag (v2f IN)
    {
        grow = smoothstep(0.0, 1.0, (_Time.y - float4(0, 1, 2, 3)) / 3.0);

        float num_steps = 1.0;
        float last_distance = 0.0;
        float total_distance = _ProjectionParams.y;
        float3 ray_pos;
        raymarching(IN.world, 60, total_distance, num_steps, last_distance, ray_pos);
        float3 normal = guess_normal(ray_pos);

        gbuffer_out OUT;
        OUT.diffuse = _Color * float4(0.75, 0.75, 0.80, 1.0);
        OUT.spec_smoothness = float4(0.2, 0.2, 0.2, _Glossiness);
        OUT.normal = float4(normal * 0.5 + 0.5, 1.0);

        float glow = max(1.0 - abs(dot(-GetCameraForward(), normal)) - 0.4, 0.0) * 1.0;
        float3 emission = float3(0.7, 0.7, 1.0) * glow * 0.6;
        OUT.emission = float4(emission, 1.0);
        OUT.depth = ComputeDepth(mul(UNITY_MATRIX_VP, float4(ray_pos, 1.0)));

        return OUT;
    }

    ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
        LOD 100

        /*
        Pass{
            Tags{ "LightMode" = "ShadowCaster" }
            Cull Front
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert_shadow
            #pragma fragment frag_shadow

            struct v2f_shadow
            {
                float4 pos : SV_POSITION;
                float3 world : NORMAL;
                LIGHTING_COORDS(0, 1)
            };

            v2f_shadow vert_shadow(appdata_full IN)
            {
                v2f_shadow OUT;
                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.world = mul(unity_ObjectToWorld, IN.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(OUT);
                return OUT;
            }

            half4 frag_shadow(v2f_shadow IN) : SV_Target
            {
                float num_steps = 1.0;
                float last_distance = 0.0;
                float total_distance = _ProjectionParams.y;
                float3 ray_pos;
                raymarching(IN.world, 30, total_distance, num_steps, last_distance, ray_pos);
                return 0.0;
            }

            ENDCG
        }
        */

		Pass
		{
            Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
