Shader"VJ/Channel18/Lattice/Line"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_Thickness ("Thickness", Range(0.0, 1.0)) = 1.0
		_Axis ("Axis", Vector) = (1, 1, 1, -1)
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
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "./Lattice.cginc"

			float4 _Color;
			fixed _Thickness;

			float3 _Axis;

			struct v2g {
				float4 position : POSITION;
				float3 local : NORMAL;
			};

			struct g2f {
				float4 position : POSITION;
				float thickness : NORMAL;
			};

			v2g vert(appdata_full IN) 
			{
				v2g OUT;
				float3 position = lattice_position(IN.vertex.xyz);
				OUT.position = UnityObjectToClipPos(float4(position, 1));
				OUT.local = IN.vertex.xyz;
				return OUT;
			}

			[maxvertexcount(24)]
			void geom(in line v2g IN[2], inout LineStream<g2f> OUT)
			{
				float3 dir = (IN[1].local.xyz - IN[0].local.xyz);
				float3 ndir = normalize(dir);
				float xaxis = step(0.999, abs(dot(ndir, float3(1, 0, 0))));
				float yaxis = step(0.999, abs(dot(ndir, float3(0, 1, 0))));
				float zaxis = step(0.999, abs(dot(ndir, float3(0, 0, 1))));

				float scale = max(max(_Axis.x * xaxis, _Axis.y * yaxis), _Axis.z * zaxis);
				float thickness = _Thickness * max(0, scale);

				g2f o;
				o.thickness = thickness;
				o.position = IN[0].position;
				OUT.Append(o);

				o.position = IN[1].position;
				OUT.Append(o);
			}

			float4 frag(g2f IN) : COLOR {
				float alpha = saturate(IN.thickness);
				clip(alpha - 0.01);
				return _Color * alpha;
			}

            ENDCG
        }


    }
}
