Shader"VJ/Channel18/Lattice/Line"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }

		Pass
		{
			ZWrite On

			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "./Lattice.cginc"

			float4 _Color;

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
				return _Color;
			}

            ENDCG
        }


    }
}
