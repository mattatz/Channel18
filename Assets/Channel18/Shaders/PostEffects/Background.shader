Shader "VJ/Channel18/PostEffects/Background"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_Color ("Color", Color) = (1, 1, 1, 1)
		_Gradient ("Gradient", 2D) = "-" {}
        _Scale ("Scale", Range(0, 1)) = 0.25
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
            fixed _Scale, _Speed, _T;

			fixed4 frag (v2f i) : SV_Target
			{
                float r = _ScreenParams.x / _ScreenParams.y;
                float2 uv = i.uv;
                float x = uv.x * r, y = uv.y;
                float2 displacement = snoise(float3(float2(x, y) * _Scale, _Time.y * _Speed)).xy;
                fixed4 col = tex2D(_Gradient, uv + displacement);
                return col;
            }
			ENDCG
		}
	}
}
