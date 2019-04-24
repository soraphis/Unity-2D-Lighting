Shader "Soraphis/RTLM/Mapping"
{
	SubShader
	{
	
        Cull Off 
        ZWrite Off 
        ZTest Always 
		Blend One One
		BlendOp Min
		
        Pass
        {
            HLSLPROGRAM
			
			#pragma target 3.5
			
			#pragma vertex PassVertex
			#pragma fragment PassFragment
			
			#include "Mapping.hlsl"
			
			ENDHLSL
        }
        
        Pass
        {
            HLSLPROGRAM
			
			#pragma target 3.5
			
			#pragma vertex PassVertex
			#pragma fragment PassFragment
			
			#include "Refit.hlsl"
			
			ENDHLSL
        }
        
	}
	

}