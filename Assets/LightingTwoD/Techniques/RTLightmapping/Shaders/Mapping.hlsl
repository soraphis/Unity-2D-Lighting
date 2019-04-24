// based of
// https://www.gamasutra.com/blogs/RobWare/20180226/313491/Fast_2D_shadows_in_Unity_using_1D_shadow_mapping.php

#ifndef RTLM_MAP_INCLUDED
#define RTLM_MAP_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Assets/LightingTwoD/Core/Shaders/PolarUtil.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld

CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
CBUFFER_END

CBUFFER_START(UnityPerFrame)
    sampler2D   _ShadowTex;
	float4x4    unity_MatrixVP;
    float4 		_LightPosition;
    float4 		_ShadowMapParams; // (slot uv space, slot clip space, 0, 0)
    float4 		_Color;	
CBUFFER_END

struct VertexInput
{
    float3 vertex1 : POSITION;
    float2 vertex2 : TEXCOORD0;
    float4 color: COLOR;
};

struct VertexOutput
{
    float4 vertex : SV_POSITION;	
    float4 edge   : TEXCOORD0;		// xy=edgeVertex1,zw=edgeVertex2
    float2 data	  : TEXCOORD1;		// x=angle,y=layer
};

int bit_set(float num, float bit){
    float m = pow(2, bit+1); // 1 << (bit+1)
    return int(fmod(num, m) >= m*0.5f);
}

VertexOutput PassVertex(VertexInput input){
    VertexOutput output;
    float polar1 = ToPolarAngle(input.vertex1.xy,_LightPosition.xy);
    float polar2 = ToPolarAngle(input.vertex2.xy,_LightPosition.xy);
    
    output.edge = float4(input.vertex1.xy,input.vertex2.xy);
    output.edge = lerp(output.edge,output.edge.zwxy,step(polar1,polar2));
    
    float diff = abs(polar1-polar2);
    if (diff >= UNITY_PI)
    { 
        float maxAngle = max(polar1,polar2);
        if (polar1 == maxAngle)
        {
            polar1 = maxAngle + 2 * UNITY_PI - diff;
        }
        else
        {
            polar1 = maxAngle;
        }
    }
    
    output.vertex = float4(PolarAngleToClipSpace(polar1), _ShadowMapParams.y, 0.0f, 1.0f);
    
    int s = bit_set(_ShadowMapParams.z, input.color.r);
    
    output.data = float2(polar1, s); 
	return output;
}

float4 PassFragment(VertexOutput input) : SV_Target{
    int s = input.data.y;
    
    //clip((s & _ShadowMapParams.z) -1); // check for cliping mask
    clip(s - 0.5f);
     
    float angle = input.data.x;
    // if (AngleDiff(angle,_LightPosition.z) > _LightPosition.w)
    //     return float4(0,0,0,0);
    
    float2 realEnd = _LightPosition.xy + float2(cos(angle) * 10, sin(angle) * 10);
    
    float t = Intersect(_LightPosition.xy, realEnd, input.edge.xy, input.edge.zw);
    return t;
}

#endif // RTLM_MAP_INCLUDED