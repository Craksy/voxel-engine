Shader "Custom/InstanceSurfaceShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        //_MainTex ("Albedo (RGB)", 2D) = "white" {}
        //_MainTex_ST ("Texture", 2D) = "white" {}
        _Textures ("Texture Array", 2DArray) = "" {}
        //_Glossiness ("Smoothness", Range(0,1)) = 0.5
        //_Metallic ("Metallic", Range(0,1)) = 0.0
        //_TileWidth ("TileWidth", Float) = 0.1
        //_TileHeight ("TileHeight", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf Standard fullforwardshadows vertex:vert
        //#pragma surface surf Standard fullforwardshadows
        #pragma surface surf Lambert
        #pragma multi_compile_instancing
        #include "UnityCG.cginc"
        #pragma instancing_options procedural:setup

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        UNITY_DECLARE_TEX2DARRAY(_Textures);

        struct Input
        {
            fixed2 uv_Textures;
        };

        //half _Glossiness;
        //half _Metallic;
        fixed4 _Color;
        //sampler2D _MainTex;

        //float _TileWidth;
        //float _TileHeight;

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<float3> PositionBuffer;
            //StructuredBuffer<float2> TexBuffer;
            StructuredBuffer<uint> TypeBuffer;
        #endif

        void setup(){
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                uint id = unity_InstanceID;
                unity_ObjectToWorld._11_21_31_41 = float4(1,0,0,0);
                unity_ObjectToWorld._12_22_32_42 = float4(0,1,0,0);
                unity_ObjectToWorld._13_23_33_43 = float4(0,0,1,0);
                unity_ObjectToWorld._14_24_34_44 = float4(PositionBuffer[id], 1);
            #endif
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c;
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                c = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(IN.uv_Textures, TypeBuffer[unity_InstanceID])) * _Color;
            #else
                c = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(IN.uv_Textures, 5)) * _Color;
            #endif
            o.Albedo = c.rgb;
            //o.Metallic = _Metallic;
            //o.Smoothness = _Glossiness;
            //o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
