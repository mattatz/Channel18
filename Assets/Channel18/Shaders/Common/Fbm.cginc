#ifndef __FBM_INCLUDED__
#define __FBM_INCLUDED__

#ifndef FBM_ITERATIONS
#define FBM_ITERATIONS 6
#endif

float fbm(float2 P, float lacunarity, float gain)
{
    float sum = 0.0;
    float amp = 1.0;
    float2 pp = P;
    for (int i = 0; i < FBM_ITERATIONS; i += 1)
    {
        amp *= gain;
        sum += amp * noise2D(pp);
        pp *= lacunarity;
    }
    return sum;
}
 
float fbm_pattern(float2 p)
{
    float l = 2.5;
    float g = 0.4;
    float2 q = float2(fbm(p + float2(0.0, 0.0), l, g), fbm(p + float2(5.2, 1.3), l, g));
    float2 r = float2(fbm(p + 4.0 * q + float2(1.7, 9.2), l, g), fbm(p + 4.0 * q + float2(8.3, 2.8), l, g));
    return fbm(p + 4.0 * r, l, g);
}

#endif // __FBM_INCLUDED__
