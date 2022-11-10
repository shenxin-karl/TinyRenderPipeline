#ifndef __AMBIENT_LIGHTING_H__
#define __AMBIENT_LIGHTING_H__
#include "CookTorrance.cginc"

#define SH3_NUM_VECTOR 9

struct SH3 {
    float4 coefs[SH3_NUM_VECTOR]; 
};

float4 gAmbientDiffuseSH[SH3_NUM_VECTOR];
float3 AmbientDiffuse(float3 N) {
    float x = N.x;
    float y = N.y;
    float z = N.z;

    // l = 0
    float3 irradiance = gAmbientDiffuseSH[0].xyz * 0.2820948;
    // l = 1
    irradiance += gAmbientDiffuseSH[1].xyz * 0.4886025 * y;
    irradiance += gAmbientDiffuseSH[2].xyz * 0.4886025 * z;
    irradiance += gAmbientDiffuseSH[3].xyz * 0.4886025 * x;
    // l = 2
    irradiance += gAmbientDiffuseSH[4].xyz * 1.0925480 * x * y;
    irradiance += gAmbientDiffuseSH[5].xyz * 1.0925480 * y * z;
    irradiance += gAmbientDiffuseSH[6].xyz * 0.3153916 * ((3.0 * z * z) - 1.0);
    irradiance += gAmbientDiffuseSH[7].xyz * 1.0925480 * x * z;
    irradiance += gAmbientDiffuseSH[8].xyz * 0.5462742 * (x*x - y*y);
    return irradiance;
}

float3 FresnelSchlickRoughness(float cosTheta, MaterialData materialData) {
    float3 F0 = materialData.fresnelFactor;
    float roughness = materialData.roughness;
    float invCos = 1.0 - cosTheta;
    return F0 + (max(1.0 - roughness, F0) - F0) * (invCos * invCos * invCos * invCos * invCos);
}

float3 AmbientIBL(float3 N, float3 V, MaterialData materialData) {
    float3 Ks = FresnelSchlickRoughness(max(dot(N, V), 0.0), materialData);
    float3 Kd = 1.0 - Ks;
    float3 irradiance = AmbientDiffuse(N);
    float3 diffuse = Kd * irradiance * materialData.diffuseAlbedo;
    return diffuse;
}

#endif