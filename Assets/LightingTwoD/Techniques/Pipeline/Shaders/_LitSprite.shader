Shader "Soraphis/Pipeline/LitSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RendererColor ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
    
     Tags { 
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "Queue"="Overlay" 
            "RenderType"="Sprite" // Transparent/Sprite?
        }

        // AlphaTest NotEqual 0.0
        Pass
        {
            Cull Off
            
            HLSLPROGRAM
			
			#pragma target 3.5
			
//            #pragma multi_compile_instancing
			
			#pragma vertex   LitPassVertex
			#pragma fragment LitPassFragment
			
			#include "./LitSprite.hlsl"
			
			ENDHLSL
        }
    }
}