#ifndef __LATTICE_COMMON_INCLUDED__
#define __LATTICE_COMMON_INCLUDED__

#include "../Common/Noise/SimplexNoise3D.cginc"

float _NoiseOffset;
float3 _NoiseScale;
float _NoiseIntensity;

float3 lattice_position(float3 position) {
	float t = _NoiseOffset;
	float3 offset = float3(
		snoise((position * _NoiseScale + float3(t, 0, 0))).x,
		snoise((position * _NoiseScale + float3(0, t, 0))).x,
		snoise((position * _NoiseScale + float3(0, 0, t))).x
		) * _NoiseIntensity;
	return position + offset;
}

#endif

