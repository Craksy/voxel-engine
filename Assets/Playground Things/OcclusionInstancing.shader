Shader "Custom/OcclusionInstancing"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Textures ("Texture Array", 2DArray) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Lambert
        #pragma multi_compile_instancing
        #include "UnityCG.cginc"
        #pragma instancing_options procedural:setup

        UNITY_DECLARE_TEX2DARRAY(_Textures);

        struct Input
        {
            float2 uv_Textures;
        };

        struct InstanceData {
            float3 position;
            uint type;
        };

        // half _Glossiness;
        // half _Metallic;
        fixed4 _Color;
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

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c;
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                c = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(IN.uv_Textures, InstanceDataBuffer[unity_InstanceID].type)) * _Color;
                // c = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(IN.uv_Textures, 2)) * _Color;
            #else
                c = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(IN.uv_Textures, 5)) * _Color;
            #endif
            o.Albedo = c.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
