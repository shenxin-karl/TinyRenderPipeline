#ifndef __AMBIENT_LIGHTING_H__
#define __AMBIENT_LIGHTING_H__
#include "CookTorrance.cginc"

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

float3 FresnelSchlickRoughness(float cosTheta, MaterialData materialData) {
    float3 F0 = materialData.fresnelFactor;
    float roughness = materialData.roughness;
    float invCos = 1.0 - cosTheta;
    return F0 + (max(1.0 - roughness, F0) - F0) * (invCos * invCos * invCos * invCos * invCos);
}

float AmbientIBL(float3 N, float3 V, float4 coefs[SH3_NUM_VECTOR], MaterialData materialData) {
    float3 Ks = FresnelSchlickRoughness(max(dot(N, V), 0.0), materialData);
    float3 Kd = 1.0 - Ks;
    float3 irradiance = AmbientDiffuse(coefs, N);
    float3 diffuse = Kd * irradiance * materialData.diffuseAlbedo;
    return irradiance;
}

#endif