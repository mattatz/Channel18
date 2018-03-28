Shader "VJ/Channel18/Bubble"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Size ("Size", Range(0.0, 5.0)) = 2.0
		_SizeCurve ("SizeCurve", 2D) = "white" {}

        _Displacement ("Displacement", Range(0.0, 500.0)) = 100.0
        _Curvature ("Curvature", Range(0.0, 5.0)) = 2.0

		_Gradient ("Gradient", 2D) = "white" {}
        [Toggle] _Rim ("Rim", Range(0.0, 1.0)) = 0.0
        [Toggle] _Mono ("Mono", Range(0.0, 1.0)) = 0.0
	}

	SubShader
	{
		Tags { 
            "RenderType"="Opaque" 
            "Queue"="Transparent+1" 
        }
		LOD 100
        GrabPass {}
		ZWrite On
		Blend Off
		ZTest LEqual

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
			
			#include "UnityCG.cginc"
            #include "../Common/Random.cginc"
            #include "../Common/Quaternion.cginc"
            #include "../Common/Matrix.cginc"
            #include "../Common/Bubble.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            half4 _Color;
			sampler2D _Gradient;
            float4x4 _LocalToWorld, _WorldToLocal;

            StructuredBuffer<Bubble> _Bubbles;
            half _Size;
            sampler2D _SizeCurve;

            float _Displacement, _Curvature;
			float _Rim, _Mono;

            sampler2D _GrabTexture;
            float4 _GrabTexture_ST;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            void setup()
            {
                unity_ObjectToWorld = _LocalToWorld;
                unity_WorldToObject = _WorldToLocal;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                uint iid = unity_InstanceID;
                Bubble bub = _Bubbles[iid];
                unity_ObjectToWorld = mul(unity_ObjectToWorld, compose(bub.position.xyz, QUATERNION_IDENTITY, float3(1, 1, 1)));
                unity_WorldToObject = inverse(unity_ObjectToWorld);
            #endif
            }
			
			v2f vert (appdata IN)
			{
				v2f OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float4 world = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
                float4 center = mul(UNITY_MATRIX_V, world);

                half size = _Size;
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                uint iid = unity_InstanceID;
                size *= tex2Dlod(_SizeCurve, float4(_Bubbles[iid].lifetime, 0, 0, 0)).x;
            #endif
                center.xy += (IN.uv - 0.5) * saturate(world.w) * max(0, size);
				OUT.vertex = mul(UNITY_MATRIX_P, center);
				OUT.uv = IN.uv;
                OUT.screenPos = ComputeGrabScreenPos(OUT.vertex);
				return OUT;
			}

			fixed4 frag (v2f IN) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 normal = float3((IN.uv - 0.5) * 2.0, 0);
                float r2 = dot(normal.xy, normal.xy);
                clip(1.0 - r2);

                normal.z = sqrt(1.0 - r2);
                // return fixed4(IN.uv, 0, 1);
                // return fixed4((normal + 1.0) * 0.5, 1);
                // return fixed4((normal.xy + 1.0) * 0.5, 0, 1);

                float nh = max(0, dot(normal, float3(0, 0, 1)));
                float rim = 1.0 - saturate(nh);
                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                float f = pow(rim, _Curvature);
                half4 grab = tex2D(_GrabTexture, uv + normal.xy * _Displacement * (_ScreenParams.zw - 1) * f);

				float4 color = _Color;

				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
					uint iid = unity_InstanceID;
					float u = nrand(float2(iid, 0));
					color = color * tex2D(_Gradient, float2(u, 0));
				#endif

                // return grab;
                return lerp(lerp(grab, color, rim * _Rim), color, _Mono);
            }

			ENDCG
		}
	}
}
