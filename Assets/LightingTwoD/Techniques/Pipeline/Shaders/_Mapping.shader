Shader "Soraphis/Pipeline/Mapping"
{
	SubShader
	{
	
        Cull Off 
        ZWrite Off 
        ZTest Always 
		Blend One One
		BlendOp Min
		
        Pass // Mapping Non-line
        {
            HLSLPROGRAM
			
			#pragma target 3.5
			
			#pragma vertex PassVertex
			#pragma fragment PassFragment
			
			#include "./Mapping.hlsl"
			
			ENDHLSL
        }
        
        Pass // Mapping refit
        {
            HLSLPROGRAM
			
			#pragma target 3.5
			
			#pragma vertex PassVertex
			#pragma fragment PassFragment
			
			#include "./Refit.hlsl"
			
			ENDHLSL
        }
        
	}
	

}