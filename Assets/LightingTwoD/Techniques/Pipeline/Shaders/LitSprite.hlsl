#ifndef PIPELINE_UNLIT_INCLUDED
#define PIPELINE_UNLIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Assets/LightingTwoD/Core/Shaders/PolarUtil.hlsl"

CBUFFER_START(UnityPerFrame)
	float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
	float4 _LightData;
	float4 _LightIndices[2];
CBUFFER_END

CBUFFER_START(UnityPerMaterial)
    // sampler2D _MainTex;
	//float4 _Color;
CBUFFER_END

#define MAX_VISIBLE_LIGHTS 16

CBUFFER_START(_LightBuffer)
	float4 _VisibleLightColors[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightDirectionsOrPositions[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightAttenuations[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightSpotDirections[MAX_VISIBLE_LIGHTS];
CBUFFER_END

CBUFFER_START(_ShadowBuffer)
    float4 _WorldToShadowMatrices[MAX_VISIBLE_LIGHTS];
    float4 _ShadowMapParams[MAX_VISIBLE_LIGHTS];
    
    TEXTURE2D(_MainTex);
    SAMPLER(sampler__MainTex);
    sampler2D   _ShadowTex; 
CBUFFER_END



float ShadowAttenuation(int index, float3 worldPos){
	float4 lightAttenuation = _VisibleLightAttenuations[index];
    
    float2 polar = ToPolar(worldPos.xy, _WorldToShadowMatrices[index].xy);
    
    float offset = 1*(1.0f/512.0f);
    
    float shadowAttenuation = SampleShadowTexture(_ShadowTex, polar.x, _ShadowMapParams[index].x) * lightAttenuation.y; 
    shadowAttenuation += SampleShadowTexture(_ShadowTex, polar.x + offset, _ShadowMapParams[index].x) * lightAttenuation.y; 
    shadowAttenuation += SampleShadowTexture(_ShadowTex, polar.x - offset, _ShadowMapParams[index].x) * lightAttenuation.y;
    shadowAttenuation /= 3;
    
    shadowAttenuation = shadowAttenuation - polar.y;
    
    return saturate(shadowAttenuation + step(0.00390625f, shadowAttenuation) * 1);
}

float3 DiffuseLight(int index, float3 normal, float3 worldPos, float shadowAttenuation){
    float3 lightColor = _VisibleLightColors[index].rgb;
	float4 lightPositionOrDirection = _VisibleLightDirectionsOrPositions[index];
	float4 lightAttenuation = _VisibleLightAttenuations[index];
	float3 spotDirection = _VisibleLightSpotDirections[index].xyz;
	
	float local = lightPositionOrDirection.w;
	
	float3 lightVector = lightPositionOrDirection.xyz - worldPos * local;
	
	//lightVector.z = 0;
	float3 lightDirection = float3(normalize(lightVector.xy),-0.5f) * local 
	                        + normalize(lightVector.xyz) * (1 - local);
	float diffuse = saturate(dot(normal, lightDirection));
	
	float rangeFade = dot(lightVector, lightVector) * lightAttenuation.x;
	rangeFade = saturate(1.0 - rangeFade); //saturate(1.0 - rangeFade * rangeFade);
	// rangeFade *= rangeFade;
	
	
	float spotFade = dot(spotDirection, lightDirection);
	spotFade = saturate(spotFade * lightAttenuation.z + lightAttenuation.w);
	//spotFade *= spotFade;
	
	float distanceSqr = max(1 + dot(lightVector, lightVector), 0.00001);
	// diffuse *= spotFade * rangeFade / distanceSqr;
	diffuse *= shadowAttenuation * spotFade * rangeFade / distanceSqr;
	
	// return lightColor * rangeFade / distanceSqr;
	// return shadowAttenuation;
	return diffuse * lightColor * local + lightColor * (1 - local);
}

#define UNITY_MATRIX_M unity_ObjectToWorld

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

#ifdef UNITY_INSTANCING_ENABLED
    UNITY_INSTANCING_BUFFER_START(PerInstance)
        UNITY_DEFINE_INSTANCED_PROP(float4, _RendererColorArray)
    UNITY_INSTANCING_BUFFER_END(PerInstance)

    #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, _RendererColorArray)
#else
    CBUFFER_START(PerInstance)
        UNITY_DEFINE_INSTANCED_PROP(float4, _RendererColor)
    CBUFFER_END
#endif

struct VertexInput {
	float4 pos : POSITION;
	float4 color : COLOR;
	float3 normal : NORMAL;
	float4 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput {
	float4 clipPos : SV_POSITION;
	float4 color : COLOR;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutput LitPassVertex (VertexInput input) {
	VertexOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xy, 0, 1.0));
	output.clipPos = mul(unity_MatrixVP, worldPos);
	
	output.normal = mul((float3x3)UNITY_MATRIX_M, input.normal);
	output.uv = float2(input.uv.xy);
	output.worldPos = worldPos.xyz;
	output.color = input.color;
	return output;
}

float4 LitPassFragment (VertexOutput input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);
    input.normal = normalize(input.normal);
    float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler__MainTex, input.uv);
    float4 color = input.color * UNITY_ACCESS_INSTANCED_PROP(PerInstance, _RendererColor);

    float3 diffuseLight = float3(0.001f, 0.001f, 0.001f); // ambient
    for(int i = 0; i < _LightData.y; ++i){
        float shadowAttenuation = ShadowAttenuation(i, input.worldPos);
        diffuseLight += DiffuseLight(i, input.normal, input.worldPos, shadowAttenuation);
    }
    //return float4(diffuseLight, 1);
    
    float4 output = float4(diffuseLight,1) * albedo * color;
    clip(output.a - 0.00390625f); // 1/256
    
	return output;
}



#endif // PIPELINE_UNLIT_INCLUDED