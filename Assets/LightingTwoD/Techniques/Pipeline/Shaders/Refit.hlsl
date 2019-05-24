// based of
// https://www.gamasutra.com/blogs/RobWare/20180226/313491/Fast_2D_shadows_in_Unity_using_1D_shadow_mapping.php

#ifndef RTLM_REFIT_INCLUDED
#define RTLM_REFIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Assets/LightingTwoD/Core/Shaders/PolarUtil.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld

CBUFFER_START(UnityPerFrame)
    sampler2D   _MainTex;
    float4x4    unity_MatrixVP;    
CBUFFER_END

struct Appdata
{
    float4 vertex : POSITION;
    float2 texcoords : TEXCOORD0;
};

Appdata PassVertex(Appdata input){
    input.vertex = mul(unity_MatrixVP, input.vertex);
    return input;
}

float4 PassFragment(Appdata input) : SV_Target{
    float u = input.texcoords.x * 2.0f / 3.0f;
    float v = input.texcoords.y;
    float s = tex2D(_MainTex, float2(u,v)).r;
    if (u < 1.0f / 3.0f){
        s = min(s,tex2D(_MainTex, float2(u + (2.0f / 3.0f), v)).r);
    }
    return s;
}

#endif // RTLM_REFIT_INCLUDED