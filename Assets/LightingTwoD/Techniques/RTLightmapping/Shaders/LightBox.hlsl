// based of
// https://www.gamasutra.com/blogs/RobWare/20180226/313491/Fast_2D_shadows_in_Unity_using_1D_shadow_mapping.php

#ifndef RTLM_BOX_INCLUDED
#define RTLM_BOX_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Assets/LightingTwoD/Core/Shaders/PolarUtil.hlsl"

CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
CBUFFER_END

CBUFFER_START(UnityPerFrame)
    sampler2D   _ShadowTex;    
	float4x4    unity_MatrixVP;
    float4 		_LightPosition;
    float4 		_ShadowMapParams; // (slot uv space, slot clip space, 0, 0)
    float4 		_SpotDirection;
    float4 		_LightAttenuation;
    float4 		_Color;	
CBUFFER_END

struct VertexInput
{
    float4 vertex   : POSITION;
};

struct VertexOutput
{
    float4 vertex   : SV_POSITION;
    float4 modelPos : TEXCOORD1;
    float4 worldPos : TEXCOORD2;
};


//-------------------------------------------------------------------------------------
//                                      Point and Spotlights
//-------------------------------------------------------------------------------------


VertexOutput PassVertex(VertexInput input){
    VertexOutput output;
	
	output.modelPos = input.vertex;
	output.vertex = mul(mul(unity_MatrixVP, unity_ObjectToWorld), input.vertex);
	output.worldPos =  mul(unity_ObjectToWorld, input.vertex);
	
	return output;
}
float4 PassFragment(VertexOutput input) : SV_Target{
    float2 worldPos = input.worldPos.xy;
    
    float2 polar = ToPolar(worldPos, _LightPosition.xy);
    float shadowAttenuation = SampleShadowTexture(_ShadowTex, polar.x, _ShadowMapParams.x) * _LightAttenuation.y; 
    shadowAttenuation = shadowAttenuation - polar.y;
    shadowAttenuation = saturate(shadowAttenuation + step(0.00390625f, shadowAttenuation) * 1);    
    //clip(shadowAttenuation);
    
    float rangeFade = polar.y * polar.y * _LightAttenuation.x;
    rangeFade = saturate(1 - rangeFade);
    
    float2 lightDirection = normalize(_LightPosition.xy - worldPos);
    float spotFade = dot(_SpotDirection.xy, lightDirection);
    spotFade = saturate(spotFade * _LightAttenuation.z + _LightAttenuation.w);
    
    float distanceSqr = max(1 + polar.y * polar.y, 0.00001);
    
    float diffuse = shadowAttenuation * spotFade * rangeFade / distanceSqr;
    
    
    
    return _Color * diffuse; 
}

//-------------------------------------------------------------------------------------
//                                      Line Lights
//-------------------------------------------------------------------------------------


VertexOutput LinePassVertex(VertexInput input){
    VertexOutput output;
	output.modelPos = input.vertex;
	output.vertex = mul(mul(unity_MatrixVP, unity_ObjectToWorld), input.vertex);
	output.worldPos =  mul(unity_ObjectToWorld, input.vertex);
	return output;
}

float4 LinePassFragment(VertexOutput input) : SV_Target{
    float2 worldPos = input.worldPos.xy;
    
    // input.modelPos is clipSpace -> to uvspace:
    float2 uv = (input.modelPos.xy + 1) * 0.5f;
    uv = float2(1-uv.y, uv.x);
    float worldScaleY = uv.y * _LightAttenuation.y;
    
    float br = SampleShadow1Tap(_ShadowTex, uv.x, _ShadowMapParams.x);
    br *= _LightAttenuation.y;
    clip(-(br < worldScaleY));
    
    float rangeFade = worldScaleY*worldScaleY * _LightAttenuation.x;
    rangeFade = saturate(1 - rangeFade);
    
   /* 
    float2 lightDirection = normalize(_LightPosition.xy - worldPos);
    float spotFade = dot(_SpotDirection.xy, lightDirection);
    spotFade = saturate(spotFade * _LightAttenuation.z + _LightAttenuation.w);
    */
    float distanceSqr = max(1 + worldScaleY*worldScaleY, 0.00001);
    float diffuse = rangeFade / distanceSqr;
    return _Color * diffuse; 
}



#endif // RTLM_BOX_INCLUDED