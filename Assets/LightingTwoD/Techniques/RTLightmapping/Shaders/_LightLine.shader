Shader "Soraphis/RTLM/LightLine"
{
	/*Properties
	{
		[PerRendererData] _ShadowTex ("Texture", 2D) = "white" {}
		[PerRendererData] _Color ("Color", Color) = (1,1,1,1)
		[PerRendererData] _LightPosition("LightPosition", Vector) = (0,0,1,0)
		[PerRendererData] _ShadowMapParams("ShadowMapParams", Vector) = (0,0,0,0)
		[PerRendererData] _Params2("Params2", Vector) = (0,0,0,0)
	}*/
	
	SubShader
	{
	
        Cull Off 
		Lighting Off 
		ZWrite Off 
		ZTest Always
		
        Tags { 
            "IgnoreProjector"="True"
            "Queue"="Overlay" 
            "RenderType"="Transparent" // Transparent/Sprite?
            "CanUseSpriteAtlas"="False" // would have no effect 
            "PreviewType"="Plane" // has no effect
        }
	
        Pass
        {
            Blend One One
            HLSLPROGRAM
			
			#pragma target 3.5
			
            #pragma multi_compile_instancing
			
			#pragma vertex LinePassVertex
			#pragma fragment LinePassFragment
			
			#include "LightBox.hlsl"
			
			ENDHLSL
        }
	}
	

}