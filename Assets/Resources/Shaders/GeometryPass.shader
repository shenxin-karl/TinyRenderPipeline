Shader "Unlit/GeometryPass"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _NormalTex ("NormalTexture", 2D) = "bump" {}
        [NoScaleOffset] _MetallicTex ("MetallicTexture", 2D) = "white" {}
        [NoScaleOffset] _RoughnessTex ("RoughnessTex", 2D) = "white" {}
        _BumpScale ("BumpScale", float) = 1.0
        _AlphaCutoff ("AlphaCutoff", Range(0, 1)) = 0.5
        [gamma] _Metallic ("Metallic", Range(0, 1)) = 0.5
        _Roughness ("Roughness", Range(0, 1)) = 0.5
    }
    CustomEditor "GeometryShaderGUI"
    
    SubShader
    {
        Tags { "LightMode" = "GeometryPass" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _ENABLE_ALBEDO_MAP
            #pragma shader_feature _ _ENABLE_NORMAL_MAP
            #pragma shader_feature _ _ENABLE_METALLIC_TEX
            #pragma shader_feature _ _ENABLE_ROUGHNESS_TEX
            #pragma multi_compile  _ _ENABLE_ALPHA_TEST
            #include "UnityCG.cginc"
            #include "PackedVelocity.cginc"

            #if defined(_ENABLE_ALBEDO_MAP) || defined(_ENABLE_NORMAL_MAP) || defined(_ENABLE_METALLIC_TEX) || defined(_ENABLE_ROUGHNESS_TEX)
                #define NEED_UV 1
            #endif

            #if defined(_ENABLE_NORMAL_MAP)
                #define NEED_TANGENT 1
            #endif

            struct VertexIn {
                float3 pos     : POSITION;
                float3 normal  : NORMAL;
                float2 uv      : TEXCOORD0;
                float4 tangent : TANGENT;
            };

            struct VertexOut {
                float4 SVPosition : SV_POSITION;
                float3 normal     : TEXCOORD2;
                
                #if NEED_UV
                    float2 uv         : TEXCOORD1;
                #endif
                #if NEED_TANGENT
                    float4 tangent    : TEXCOORD3;
                #endif
                float4 currFrameWorldPos    : TEXCOORD4;
                float4 prevFrameWorldPos    : TEXCOORD5;   
            };

            sampler2D _MainTex;
            sampler2D _NormalTex;
            sampler2D _MetallicTex;
            sampler2D _RoughnessTex;
            float4    _MainTex_ST;
            float4    _Color;
            float     _BumpScale;
            float     _Metallic;
            float     _Roughness;
            float     _AlphaCutoff;

            void InitUV(VertexIn vin, inout VertexOut vout) {
                #if NEED_UV
                    vout.uv = TRANSFORM_TEX(vin.uv, _MainTex);
                #endif
            }
            
            void InitTangent(VertexIn vin, inout VertexOut vout) {
                #if NEED_TANGENT
                    vout.tangent = float4(UnityObjectToWorldDir(vin.tangent.xyz), vin.tangent.w);
                #endif
            }

            VertexOut vert (VertexIn vin) {
                VertexOut vout = (VertexOut)0;
                vout.SVPosition = UnityObjectToClipPos(vin.pos);
                vout.normal = UnityObjectToWorldNormal(vin.normal);
                InitUV(vin, vout);
                InitTangent(vin, vout);
                return vout;
            }

            struct PixelOut {
                fixed4 gBuffer0 : SV_Target0;
                fixed4 gBuffer1 : SV_Target1;
                fixed4 gBuffer2 : SV_Target2;
                packed_velocity_t velocityMap : SV_Target3;
            };

            float4 GetAlbedo(VertexOut pin) {
                float4 albedo = _Color;
                #if defined(_ENABLE_ALBEDO_MAP)
                    albedo *= tex2D(_MainTex, pin.uv);
                #endif

                #if defined(_ENABLE_ALPHA_TEST)
                    clip(albedo.a - _AlphaCutoff);
                #endif
                return albedo;
            }

            float3 GetNormal(VertexOut pin) {
                float3 N = normalize(pin.normal);
                #if defined(_ENABLE_NORMAL_MAP)
                    float3 T = normalize(pin.tangent.xyz);
                    float3 B = cross(N, T) * pin.tangent.w;
                    float3 sampleNormal = tex2D(_NormalTex, pin.uv).rgb * 2.0 - 1.0;
                    N = sampleNormal.x * T +
                        sampleNormal.y * B +
                        sampleNormal.z * N ;
                #endif
                return N;
            }

            float4 PackNormal(float3 N) {
                return float4(N * 0.5 + 0.5, 0.0);
            }
            
            float4 GetAoRoughnessMetallic(VertexOut pin) {
                float4 result = 0.0;
                result.z = _Metallic;
                result.y = _Roughness;
                #if defined(_ENABLE_ROUGHNESS_TEX)
                    result.y *= tex2D(_RoughnessTex, pin.uv).r;
                #endif
                #if defined(_ENABLE_METALLIC_TEX)
                    result.z *= tex2D(_MetallicTex, pin.uv).r;
                #endif

                            
                return result;
            }
            
            PixelOut frag (VertexOut pin) : SV_Target {
                PixelOut pout;
                pout.gBuffer0 = GetAlbedo(pin);
                pout.gBuffer1 = PackNormal(GetNormal(pin));
                pout.gBuffer2 = GetAoRoughnessMetallic(pin);
                return pout;
            }
            ENDCG
        }
    }
}
