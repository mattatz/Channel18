Shader "VJ/Channel18/PostEffects/Blending"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_Color ("Color", Color) = (1, 1, 1, 1)
		_Gradient ("Gradient", 2D) = "-" {}
        _Scale ("Scale", Vector) = (0.25, 0.25, -1, -1)
        _Speed ("Speed", Range(0, 1)) = 0.25
        _T ("T", Range(0, 1)) = 1.0
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "../Common/Noise/SimplexNoise3D.cginc"
			#include "../Common/PhotoshopMath.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			float4 _Color;
			sampler2D _Gradient;
            half2 _Scale;
			half _Speed, _T;

			fixed4 frag (v2f i) : SV_Target
			{
                float2 uv = i.uv;
                fixed4 source = tex2D(_MainTex, uv);

                float r = _ScreenParams.x / _ScreenParams.y;
                float x = uv.x * r, y = uv.y;
                float2 displacement = (snoise(float3(float2(x, y) * _Scale.xy, _Time.y * _Speed)).xy - 0.5);
                fixed4 col = tex2D(_Gradient, uv + displacement);
                return fixed4(lerp(source.rgb, BlendLighten(source.rgb, col.rgb), _T), 1);
            }
			ENDCG
		}
	}
}
