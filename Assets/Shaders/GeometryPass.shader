Shader "Unlit/GeometryPass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "LightMode" = "GeometryPass" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct VertexIn {
                float3 pos : POSITION;
                float2 uv  : TEXCOORD0;
            };

            struct VertexOut {
                float4 SVPosition : SV_POSITION;
                float3 pos        : TEXCOORD0;
                float2 uv         : TEXCOORD1;           
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            VertexOut vert (VertexIn vin)
            {
                VertexOut vout;
                vout.SVPosition = UnityObjectToClipPos(float4(vin.pos, 1.0));
                vout.pos = mul(UNITY_MATRIX_M, float4(vin.pos, 1.0));
                vout.uv = TRANSFORM_TEX(vin.uv, _MainTex);
                return vout;
            }


            struct PixelOut {
                fixed4 gBuffer0 : SV_Target0;
                fixed4 gBuffer1 : SV_Target1;
                fixed4 gBuffer2 : SV_Target2;
            };
            
            PixelOut frag (VertexOut pin) : SV_Target
            {
                PixelOut pout;
                fixed4 col = tex2D(_MainTex, pin.uv);
                pout.gBuffer0 = col;
                pout.gBuffer1 = float4(1.0, 0.0, 0.0, 1.0);
                pout.gBuffer2 = float4(0.0, 1.0, 0.0, 1.0);
                return pout;
            }
            ENDCG
        }
    }
}
