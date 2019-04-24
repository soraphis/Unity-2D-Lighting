Shader "SLight/SmoothLight"
{
	Properties
	{
		[PerRendererData] _ShadowTex ("Texture", 2D) = "white" {}
		[PerRendererData] _Color ("Color", Color) = (1,1,1,1)
		[PerRendererData] _LightPosition("LightPosition", Vector) = (0,0,1,0)
		[PerRendererData] _ShadowMapParams("ShadowMapParams", Vector) = (0,0,0,0)
		[PerRendererData] _Params2("Params2", Vector) = (0,0,0,0)
	}

	SubShader
	{
		Cull Off 
		Lighting Off 
		ZWrite Off 
		ZTest Always
		
        Tags { 
            // "AlphaDepth"="False"
            "CanUseSpriteAtlas"="False" // would have no effect 
            "IgnoreProjector"="True"
            "PreviewType"="Plane" // has no effect
            "Queue"="Overlay" 
            "RenderType"="Transparent" // Transparent/Sprite?
        }


		Pass
		{
		
		Blend One One
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
            #include "Assets/LightingTwoD/Core/Shaders/PolarUtil.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float4 modelPos : TEXCOORD1;
				float4 worldPos : TEXCOORD2;
			};
			
			v2f vert(appdata_t IN)
			{
				v2f OUT;
				//OUT.modelPos = IN.vertex;
				//OUT.vertex = UnityObjectToClipPos(IN.vertex);
				//OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
				
                OUT.modelPos = IN.vertex;
                OUT.vertex = mul(unity_MatrixMVP, IN.vertex);
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
				
				return OUT;
			}

			sampler2D 	_ShadowTex;
			float4 		_LightPosition;
			float4 		_ShadowMapParams; // (slot uv space, slot clip space, 0, 0)
			float4 		_Params2;
			fixed4 		_Color;
			
			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = _Color;

				float2 polar = ToPolar(IN.worldPos.xy,_LightPosition.xy);

				float shadow = SampleShadowTexturePCF(_ShadowTex,polar,_ShadowMapParams.x);
				// return float4((UNITY_PI + polar.x) / UNITY_PI / 2, 1-polar.y / 10, 0, 1);
				
				clip( shadow - 0.5f);
				// branched version but with early exit, don't know whats faster
				//if (shadow < 0.5f) {
				//	clip( -1.0 );
				//	return c;
				//}
				
				float distFalloff = max(0.0f,length(IN.worldPos.xy-_LightPosition.xy) - _Params2.w) * _Params2.z;
				distFalloff = 1.0f - clamp(distFalloff,0.0f,1.0f);
// 				distFalloff = pow(distFalloff,_LightPosition.z);
//
				float angleFalloff = AngleDiff(polar.x, _Params2.x) / _Params2.y;
				angleFalloff = step(AngleDiff(polar.x, _Params2.x), _Params2.y);
				
//				angleFalloff = (1.0f - clamp(angleFalloff, 0.8f, 1.0f) ) / 0.2f;

				//angleFalloff = (1.0f - angleFalloff);
                
//				angleFalloff = pow(angleFalloff, _LightPosition.w);

				c.rgb *= distFalloff * angleFalloff;

				return c;
			}
		ENDCG
		}
	}
}
