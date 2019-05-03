Shader "Soraphis/MGen/Light"
{
    Properties
    {
		_MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (0, 0, 0, 0)
		_Intensity("Intensity", Range(0.0,10.)) = 1.0        
    }
    SubShader
    {
		Cull Off ZWrite Off ZTest Always
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Blend DstColor SrcAlpha
            
            HLSLPROGRAM
			
			#pragma target 3.5
			
            #pragma multi_compile_instancing
			
			#pragma vertex PassVertex
			#pragma fragment PassFragment
			
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			
			CBUFFER_START(UnityPerDraw)
			    sampler2D _MainTex;
			    float4 _Color;
			    float _Intensity;
                float4x4 unity_ObjectToWorld;
            CBUFFER_END
			
			CBUFFER_START(UnityPerFrame)
                float4x4    unity_MatrixVP;
            CBUFFER_END
			
			struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };
            
            struct VertexOutput
            {
                float2 uv       : TEXCOORD0;	
                float4 vertex	: SV_POSITION;		
            };
			
            VertexOutput PassVertex(VertexInput input){
                VertexOutput output;
                // output.modelPos = input.vertex;
                output.vertex = mul(mul(unity_MatrixVP, unity_ObjectToWorld), input.vertex);
                output.uv = input.uv;
                return output;
            }
            
            float4 PassFragment(VertexOutput input) : SV_Target{
				float4 col = tex2D(_MainTex, input.uv);
				col.a = min(1, col.a * _Intensity);
				col.rgb = col.rgb * (1 - col.a) + _Color.rgb * col.a;
				return col;                
            }
			
			ENDHLSL
        }
    }
}