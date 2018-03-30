Shader"VJ/Channel18/Lattice/Line"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_Thickness ("Thickness", Range(0.0, 1.0)) = 1.0
	}

	SubShader
	{
		Tags { 
			"RenderType" = "Opaque" 
			"Queue" = "Transparent"
		}

		Pass
		{
			Blend One One
			ZWrite Off

			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "./Lattice.cginc"

			float4 _Color;
			fixed _Thickness;

			struct v2f {
				float4 position : POSITION;
			};

			v2f vert(appdata_full IN) 
			{
				v2f OUT;
				float3 position = lattice_position(IN.vertex.xyz);
				OUT.position = UnityObjectToClipPos(float4(position, 1));
				return OUT;
			}

			float4 frag(v2f IN) : COLOR {
				float alpha = saturate(_Thickness);
				clip(alpha - 0.01);
				return _Color * alpha;
			}

            ENDCG
        }


    }
}
