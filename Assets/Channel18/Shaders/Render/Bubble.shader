Shader "VJ/Channel18/Bubble"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Size ("Size", Range(0.0, 5.0)) = 2.0
		_SizeCurve ("SizeCurve", 2D) = "white" {}

        _Displacement ("Displacement", Range(0.0, 0.2)) = 0.05
		_RefractionRatio ("Refraction ratio", Range(0.0, 1.0)) = 0.85
		_FresnelBias ("Fresnel bias", Float) = 0.113
		_FresnelScale ("Fresnel scale", Float) = 2.5
		_FresnelPower ("Fresnel power", Float) = 0.68
	}

	SubShader
	{
		Tags { 
            "RenderType"="Opaque" 
            "Queue"="Transparent" 
        }
		LOD 100
        GrabPass {}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
			
			#include "UnityCG.cginc"
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
                float3 world : TANGENT;
				float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4x4 _LocalToWorld, _WorldToLocal;

            StructuredBuffer<Bubble> _Bubbles;
            half _Size;
            sampler2D _SizeCurve;

            float _Displacement;
            float _RefractionRatio;
            float _FresnelBias, _FresnelScale, _FresnelPower;

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
                center.xy += (IN.uv - 0.5) * world.w * size;
				OUT.vertex = mul(UNITY_MATRIX_P, center);
                OUT.world = mul(inverse(UNITY_MATRIX_V), center).xyz;
				OUT.uv = IN.uv;
                OUT.screenPos = ComputeGrabScreenPos(OUT.vertex);
				return OUT;
			}

            fixed4 sample(float3 v, float2 uv)
            {
                return tex2D(_GrabTexture, uv + v.xy * _Displacement);
            }
                        
			fixed4 frag (v2f IN) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 normal = float3((IN.uv - 0.5) * 2.0, 0);
                float r2 = dot(normal.xy, normal.xy);
                if (r2 > 1.0) {
                    discard;
                }
                normal.z = sqrt(1.0 - r2);

                // return fixed4(IN.uv, 0, 1);
                // return fixed4((normal + 1.0) * 0.5, 1);

                float3 I = IN.world - _WorldSpaceCameraPos;
                float3 ni = normalize(I);
                float3 worldNormal = UnityObjectToWorldNormal(normal);
                float3 reflection = reflect(ni, worldNormal);
                float fresnel = _FresnelBias + _FresnelScale * pow(1.0 + dot(ni, worldNormal), _FresnelPower);

                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(IN.world));
                float rim = saturate(dot(worldViewDir, worldNormal));

                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                float4 reflectedColor = sample(reflection, uv);
                float4 refractedColor = sample(refract(ni, worldNormal, _RefractionRatio), uv);
                float4 color = lerp(refractedColor, reflectedColor, saturate(fresnel));
                return color;
            }

			ENDCG
		}
	}
}
