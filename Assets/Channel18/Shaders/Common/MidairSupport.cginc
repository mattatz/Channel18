#ifndef __MIDAIR_SUPPORT_COMMON_INCLUDED__

#define __MIDAIR_SUPPORT_COMMON_INCLUDED__

struct Support
{
    float extrusion, thickness;
    float4 prevRotation, toRotation;
	float2 prevScale, toScale;
    float time;
    float offset;
    int flag;
};

#endif

