#ifndef __AMBIENT_LIGHTING_H__
#define __AMBIENT_LIGHTING_H__

#define SH3_NUM_VECTOR 9

struct SH3 {
    float4 coefs[SH3_NUM_VECTOR]; 
};


float3 AmbientDiffuse(float4 coefs[SH3_NUM_VECTOR], float3 N) {
    float x = N.x;
    float y = N.y;
    float z = N.z;

    // l = 0
    float3 result = coefs[0] * 0.2820948;
    // l = 1
    result += coefs[1] * 0.4886025 * y;
    result += coefs[2] * 0.4886025 * z;
    result += coefs[3] * 0.4886025 * x;
    // l = 2
    result += coefs[4] * 1.0925480 * x * y;
    result += coefs[5] * 1.0925480 * y * z;
    result += coefs[6] * 0.3153916 * ((3.0 * z * z) - 1.0);
    result += coefs[7] * 1.0925480 * x * z;
    result += coefs[8] * 0.5462742 * (x*x - y*y);
    return result;
}

#endif