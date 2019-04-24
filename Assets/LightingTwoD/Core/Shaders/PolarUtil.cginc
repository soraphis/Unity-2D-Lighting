// based of
// https://www.gamasutra.com/blogs/RobWare/20180226/313491/Fast_2D_shadows_in_Unity_using_1D_shadow_mapping.php

// line-line-intersection test
float Intersect(float2 lineOneStart, float2 lineOneEnd, float2 lineTwoStart, float2 lineTwoEnd)
{
    float2 line2Perp = float2(lineTwoEnd.y - lineTwoStart.y, lineTwoStart.x - lineTwoEnd.x);
    float line1Proj = dot(lineOneEnd - lineOneStart, line2Perp);

    if (abs(line1Proj) < 1e-10)
        return 0.0f;

    float t1 = dot(lineTwoStart-lineOneStart,line2Perp ) / line1Proj;
    return t1;
}

inline float ToPolarAngle(float2 cartesian, float2 center)
{
	float2 d = cartesian - center;
    return atan2(d.y, d.x);
}

inline float2 ToPolar(float2 cartesian, float2 center)
{
	float2 d = cartesian - center;
    return float2(atan2(d.y, d.x), length(d));
}


// convert from (-PI to +2PI) to (-1,+1)
// the 3PI range is the normal 2PI plus another PI to deal
// with wrap around e.g if a span goes from say 350 to 10 degrees
// (20 degrees shortest path) it would require splitting the span
// into 2 parts, 350-360 and 0-10, which is not possible in a vertex
// shader (maybe a geometry shader would be fine). Instead we make the
// span go from 350-370 and then when sampling from 0-PI you must
// also sample from 2PI to 3PI and take the min to resolve the
// wraparound.
inline float PolarAngleToClipSpace(float a)
{
	a += UNITY_PI;
	a *= 2.0f/(UNITY_PI*3.0f);
	a -= 1.0f;			
	return a;
}
// convert from (-PI to +PI) to (0,2/3)
// The final (1/3) is the wraparound as discussed above.
// if the returned angle is < 1/3 you should sample
// again with 2/3 added on and take the min.
inline float PolarAngleToShadowTextureLookup(float a)
{
	a += UNITY_PI; // a = (0, 2 PI)
	a *= 1.0f /(UNITY_PI*2.0f); // seemed wrong ... a = (0, 1)
	// a *= 1.0f / UNITY_PI / 3.0f; // a = (0, 2 / 3)
	return a;
}

// Takes a single sample from the shadow texture. Actually
// somtimes two samples are done internally to handle angle wrap around.
inline float SampleShadow1TapPreOptimise(sampler2D textureSampler, float u, float v)
{
	float sample = tex2D(textureSampler, float2(min(u,2.0f/3.0f), v)).r;
	if (u < 1.0f / 3.0f) 
	{
		sample = min(sample,tex2D(textureSampler, float2(u + (2.0f / 3.0f), v)).r);
	}
	return sample;
}


// Takes a single sample from the shadow texture.
inline float SampleShadow1Tap(sampler2D textureSampler, float u, float v)
{
	float sample = tex2D(textureSampler, float2(u, v)).r;
	return sample;
}

inline float SampleShadowTexture(sampler2D textureSampler, float angle, float v)
{
	return SampleShadow1Tap(textureSampler,PolarAngleToShadowTextureLookup(angle),v);
}

inline float SampleShadowTexturePCF(sampler2D textureSampler, float2 samplePos, float v)
{
	float u1 = PolarAngleToShadowTextureLookup(samplePos.x);
		
	//float u2 = u1-2*(1.0f/1024.0f);
	float u3 = u1-1*(1.0f/1024.0f);
	float u4 = u1+1*(1.0f/1024.0f);
	//float u5 = u1+2*(1.0f/1024.0f);

	float total = 0.0f;
	total += step(samplePos.y,SampleShadow1Tap(textureSampler,u1,v) * 10);
	//total += step(samplePos.y,SampleShadow1Tap(textureSampler,u2,v) * 10);
	total += step(samplePos.y,SampleShadow1Tap(textureSampler,u3,v) * 10);
	total += step(samplePos.y,SampleShadow1Tap(textureSampler,u4,v) * 10);
	//total += step(samplePos.y,SampleShadow1Tap(textureSampler,u5,v) * 10);
	return total / 3.0f;
}

// Returns the shortest angle arc between a and b (all angles in radians)
inline float AngleDiff(float a, float b)
{
	float diff = fmod(abs(a-b),2*UNITY_PI);
	if (diff > UNITY_PI)
	diff = 2 * UNITY_PI - diff;
	return diff;
}

inline float2 ClipSpaceToUV(float2 clipSpace)
{
	#if UNITY_UV_STARTS_AT_TOP
	float4 scale = float4(0.5f,0.5f,0.5f,0.5f);
	#else
	float4 scale = float4(0.5f,-0.5f,0.5f,0.5f);
	#endif
	return clipSpace * scale.xy + scale.zw;
}

