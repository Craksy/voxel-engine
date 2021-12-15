Shader "Unlit/UnlitOcclusionTiling"
{
    Properties
    {
        _Textures ("Textures", 2DArray) = "white" {}
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
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #pragma instancing_options procedural:setup

            struct appdata
            {
                float4 vertex : POSITION;
                fixed2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct InstanceData {
                float3 position;
                uint type;
            };

            UNITY_DECLARE_TEX2DARRAY(_Textures);
            float4 _Textures_ST;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<InstanceData> InstanceDataBuffer;
            #endif

            void setup(){
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint id = unity_InstanceID;
                    unity_ObjectToWorld._11_21_31_41 = float4(1,0,0,0);
                    unity_ObjectToWorld._12_22_32_42 = float4(0,1,0,0);
                    unity_ObjectToWorld._13_23_33_43 = float4(0,0,1,0);
                    unity_ObjectToWorld._14_24_34_44 = float4(InstanceDataBuffer[id].position, 1);
                #endif
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Textures);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 c;
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    c = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(i.uv, InstanceDataBuffer[unity_InstanceID].type));
                #else
                    c = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(i.uv, 5));
                    // c = fixed4(1,0,0,1);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
