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
    float4 		_LightAttenuation;
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

//-------------------------------------------------------------------------------------
//                                      Point and Spotlights
//-------------------------------------------------------------------------------------

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
    clip(s - 0.5f);
     
    float angle = input.data.x;
    
    float range = _LightAttenuation.y;
    float2 realEnd = _LightPosition.xy + float2(cos(angle) * range, sin(angle) * range);
    
    float t = Intersect(_LightPosition.xy, realEnd, input.edge.xy, input.edge.zw);
    return t;
}

//-------------------------------------------------------------------------------------
//                                      Line Lights
//-------------------------------------------------------------------------------------

VertexOutput LinePassVertex(VertexInput input){
    VertexOutput output;
    float2 lightLine = normalize(_LightPosition.zw - _LightPosition.xy);
    
    // float t1 = Intersect(_LightPosition.xy, _LightPosition.xy + lightNormal * 10, output.edge.xy, output.edge.zw) //fixme: 10 should be replaced with Light range
    float start = dot(lightLine, _LightPosition.xy);
    float end = dot(lightLine, _LightPosition.zw);
    
    output.edge = float4(input.vertex1.xy,input.vertex2.xy);
    output.edge = lerp(output.edge,output.edge.zwxy,step(start,end));
    
    float t1 = (dot(lightLine, input.vertex1.xy) - start) / (end - start);
    
    // t1 is uv-space -> to clipspace:
    float t2 = t1 * 2.0f/3; //for refit.hlsl
    t2 = (t2 * 2) - 1; 
        
    output.vertex = float4(t2, _ShadowMapParams.y, 0.0f, 1.0f);
    
    int s = bit_set(_ShadowMapParams.z, input.color.r);
    output.data = float2(t1, s); 
	return output;
}

float4 LinePassFragment(VertexOutput input) : SV_Target{
    int s = input.data.y;
    //clip(s - 0.5f);
    
    float t1 = (input.data.x + 1) * 0.5f;
    t1 *= 3.0f/2;
    
    clip(-(input.data.x > 1));
    clip(-(input.data.x < 0));
    
    
    float2 lightLine = _LightPosition.zw - _LightPosition.xy;
    float2 lightNormal = normalize(float2(-lightLine.y, lightLine.x)); 
    
    float range = _LightAttenuation.y;
    
    float2 start = _LightPosition.xy + lightLine * t1;
    float2 end = start + lightNormal * range; //fixme: 10 should be replaced with Light range
    
    float t = Intersect(start, end, input.edge.xy, input.edge.zw);
    return t;
}

#endif // RTLM_MAP_INCLUDED