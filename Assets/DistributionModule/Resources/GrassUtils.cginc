#ifndef GRASS_UTILS

#define PI 3.1416

inline float Remap(float x, float in_min, float in_max, float out_min, float out_max)
{
    return clamp(((x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min), out_min, out_max);
}


float nrand(float2 uv, float salt)
{
    uv *= (salt + 1);
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}


inline float2 RandomPosInsideSquere(float2 uv, int salt)
{
    return float2(nrand(uv.xy, salt), nrand(uv.yx, salt));
}
	

int CustomRandInt(float2 seed, int min, int max)
{
    float rand = frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);

    return (int) (rand * (max - min) + min);
}

float CustomRandFloat(float2 seed, int min, int max)
{
    return (nrand(seed, 1) * (max - min) + min);
}


#endif