Shader "CuboidLine/Color"
{

	Properties
	{
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _Thickness ("Thickness", Float) = 0.1
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM

			#include "CuboidLine.cginc"

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			float4 frag (g2f i) : SV_Target
			{
				return _Color;
			}

			ENDCG
		}
	}
}
