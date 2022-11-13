#ifndef __AMBIENT_LIGHTING_H__
#define __AMBIENT_LIGHTING_H__

#pragma shader_feature _ _ENABLE_IBL_DIFFUSE
#pragma shader_feature _ _ENABLE_IBL_SPECULAR

#include "CookTorrance.cginc"

float3 FresnelSchlickRoughness(float cosTheta, MaterialData materialData) {
    float3 F0 = materialData.fresnelFactor;
    float roughness = materialData.roughness;
    float invCos = 1.0 - cosTheta;
    return F0 + (max(1.0 - roughness, F0) - F0) * (invCos * invCos * invCos * invCos * invCos);
}

samplerCUBE gIrradianceMap;
samplerCUBE gPrefilterMap;
sampler2D   gBrdfLutMap;
float3 AmbientIBL(float3 N, float3 V, MaterialData materialData) {
    float3 Ks = FresnelSchlickRoughness(max(dot(N, V), 0.0), materialData);
    float3 Kd = 1.0 - Ks;
    float3 F = Ks;

    float3 diffuse = 0.0;
    float3 specular = 0.0;
    #if defined(_ENABLE_IBL_DIFFUSE)
        float3 irradiance = texCUBE(gIrradianceMap, N);
        diffuse = Kd * irradiance * materialData.diffuseAlbedo;
    #endif

    #if defined(_ENABLE_IBL_SPECULAR)
        const float kMaxLodLevel = 6.0;
        float3 R = reflect(-V, N);
        float lod = materialData.roughness * kMaxLodLevel;
        float3 prefilteredColor = texCUBElod(gPrefilterMap, float4(R, lod)).rgb;
        float2 brdf = tex2Dlod(gBrdfLutMap, float4(max(dot(N, V), 0.0), materialData.roughness, 0, 0)).rg;
        specular = prefilteredColor * (F * brdf.x + brdf.y);
    #endif
    
    return diffuse + specular;
}

#endif