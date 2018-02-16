Shader "VJ/Channel18/Grid" {

	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

        #include "UnityCG.cginc"
        #include "../Common/Quaternion.cginc"
        #include "../Common/Matrix.cginc"
        #include "../Common/ProceduralGrid.cginc"

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

        float4x4 _LocalToWorld, _WorldToLocal;

		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

        StructuredBuffer<Grid> _Grids;

        #endif

        void setup()
        {
            unity_ObjectToWorld = _LocalToWorld;
            unity_WorldToObject = _WorldToLocal;

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            uint iid = unity_InstanceID;
            Grid grid = _Grids[iid];
            unity_ObjectToWorld = mul(unity_ObjectToWorld, compose(grid.position.xyz, grid.rotation, grid.scale));
            unity_WorldToObject = inverse(unity_ObjectToWorld);
        #endif
        }

        void vert(inout appdata_full IN, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // float l = (sin(_Time.y) + 1.0) * 0.25;
            // IN.vertex.xyz += IN.tangent.xyz * l;
        }

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
