Shader "Unlit/LightingPassPS"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Off
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "CookTorrance.cginc"

            struct VertexIn {
                float3 pos : POSITION;
                float2 uv  : TEXCOORD0;
            };

            struct VertexOut {
                float4 SVPosition : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 ray        : TEXCOORD1;
            };

            sampler2D gBuffer0;
            sampler2D gBuffer1;
            sampler2D gBuffer2;
            sampler2D gDepthMap;
            float4x4  gMatInvViewProj;
            float4    gFrustumCorners[4]; 
            
            VertexOut vert(VertexIn vin) {
                VertexOut vout;
                vout.SVPosition = UnityObjectToClipPos(vin.pos);
                vout.uv = vin.uv;
                vout.ray = gFrustumCorners[int(vin.uv.x + (2 * vin.uv.y))];
                return vout;
            }
            
            float3 GetWorldPosition(VertexOut pin) {
                float d = UNITY_SAMPLE_DEPTH(tex2D(gDepthMap, pin.uv));
                float linearDepth = Linear01Depth(d);
                return _WorldSpaceCameraPos.xyz + (linearDepth * pin.ray);
            }

            float3 GetAlbedo(VertexOut pin) {
                return tex2D(gBuffer0, pin.uv).rgb;
            }
            
            float3 GetNormal(VertexOut pin) {
                float3 normal = tex2D(gBuffer1, pin.uv).xyz;
                return normalize(normal * 2.0 - 1.0);
            }

            float3 GetAoRoughnessMetallic(VertexOut pin) {
                return tex2D(gBuffer2, pin.uv).xyz;
            }
            
            float4 frag(VertexOut pin) : SV_Target {
                float3 N = GetNormal(pin);
                float3 albedo = GetAlbedo(pin);
                float3 worldPos = GetWorldPosition(pin);
                
                float3 aoRoughnessMetallic = GetAoRoughnessMetallic(pin);
                float roughness = aoRoughnessMetallic.y;
                float metallic = aoRoughnessMetallic.z;
                MaterialData materialData = CalcMaterialData(float4(albedo, 1.0), roughness, metallic);

                DirectionalLight light;
                light.direction = normalize(UnityWorldSpaceLightDir(worldPos));
                
                light.radiance = _LightColor0.rgb;

                float3 V = normalize(UnityWorldSpaceViewDir(worldPos));
                float3 radiance = ComputeDirectionLight(light, materialData, N, V);
                return float4(radiance, 1.0);
            }
            ENDCG
        }
    }
}
