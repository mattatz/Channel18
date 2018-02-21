#ifndef __MIDAIR_SUPPORT_COMMON_INCLUDED__

#define __MIDAIR_SUPPORT_COMMON_INCLUDED__

struct Support
{
    float extrusion, thickness;
    float4 prevRotation, toRotation;
    float time;
    int flag;
};

#endif

