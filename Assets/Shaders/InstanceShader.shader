Shader "Unlit/InstanceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TileWidth ("TileWidth", Float) = 0.1
        _TileHeight ("TileHeight", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #pragma instancing_options procedural:setup



            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<float3> PositionBuffer;
                StructuredBuffer<float2> UvBuffer;
                StructuredBuffer<uint> TypeBuffer;
            #endif

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _TileWidth;
            float _TileHeight;

            void setup(){
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint id = unity_InstanceID;
                    unity_ObjectToWorld._11_21_31_41 = float4(1,0,0,0);
                    unity_ObjectToWorld._12_22_32_42 = float4(0,1,0,0);
                    unity_ObjectToWorld._13_23_33_43 = float4(0,0,1,0);
                    unity_ObjectToWorld._14_24_34_44 = float4(PositionBuffer[id], 1);
                #endif
            }

            v2f vert (appdata v, float3 normal : NORMAL, uint instanceId : SV_INSTANCEID)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    float2 tileSize = float2(_TileWidth, _TileHeight);
                    float3 modifier = float3(2,4,0); //how much to increase the value of each axis
                    float3 shift = (normal + (3*abs(normal)))*0.5; //normals
                    shift += modifier*step(1,shift);
                    uint idx = dot(shift, 1)-1;
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex)*tileSize + UvBuffer[TypeBuffer[instanceId]*6+idx];
                #else
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                #endif

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
