
// This is port of below
// [Shadertoy] Pixelate video
// https://www.shadertoy.com/view/MslXRl

Shader "VJ/Channel18/PostEffects/Pixelate" {

	Properties {
		_MainTex ("-", 2D) = "" {}
	}
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	
	float2 _ScreenResolution;
	float _CellSize;
	
	fixed4 frag (v2f_img i) : SV_Target
	{
		float srX = _ScreenResolution.x;
		float srY = _ScreenResolution.y;
		float pitch = _CellSize;
		
		float2 divs = float2(srX / pitch, srY / pitch);

		float2 uv = i.uv.xy;
		uv = floor(uv * divs) / divs;
		return tex2D(_MainTex, uv);
	}
	ENDCG
	
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment frag
			ENDCG
		} 
	}

}