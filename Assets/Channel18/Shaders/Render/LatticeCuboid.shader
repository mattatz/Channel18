Shader"VJ/Channel18/Lattice/Cuboid"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}

        [Space]
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        [Gamma] _Metallic("Metallic", Range(0, 1)) = 0

        _Thickness ("Thickness", Range(0.0, 1.0)) = 0.1
		_Size ("Size", Range(0.0, 3.0)) = 1.0
		_Axis ("Axis", Vector) = (1, 1, 1, -1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
		Cull Front

        Pass
        {
            Tags { "LightMode"="Deferred" }
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment Fragment
            #pragma multi_compile_prepassfinal noshadowmask nodynlightmap nodirlightmap nolightmap
            #include "StandardLatticeGeometry.cginc"
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment Fragment
            #pragma multi_compile_shadowcaster noshadowmask nodynlightmap nodirlightmap nolightmap
            #define UNITY_PASS_SHADOWCASTER
            #include "StandardLatticeGeometry.cginc"
            ENDCG
        }
    }
}
