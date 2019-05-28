
#define INFINITY 999999

bool PointInsideArbitraryQuad(float3 p, float4 quad[4])
{
        float3 qAresta1 = normalize(quad[1].xyz - quad[0].xyz);
        float3 qAresta2 = normalize(quad[2].xyz - quad[1].xyz);
        float3 qAresta3 = normalize(quad[3].xyz - quad[2].xyz);
        float3 qAresta4 = normalize(quad[0].xyz - quad[3].xyz);

        float3 d1 = normalize(p - quad[0].xyz);
        float3 d2 = normalize(p - quad[1].xyz);
        float3 d3 = normalize(p - quad[2].xyz);
        float3 d4 = normalize(p - quad[3].xyz);
        
        return dot(qAresta1, d1) > 0 &&
               dot(qAresta2, d2) > 0 &&
               dot(qAresta3, d3) > 0 &&
               dot(qAresta4, d4) > 0;
}

bool IsPointInsideSphere(float3 pos, float3 center, float radius)
{
	return length(pos - center) < radius;
}


bool IsPointInsideCircle(float2 pos, float2 center, float radius)
{
	return length(pos - center) < radius;
}



float RayPlaneIntersect(float3 ro, float3 rDir, float3 po, float3 pNormal)
{
	float d = dot(rDir, pNormal);

	if (abs(d) > 0.0001)
	{
		float t = ( dot(pNormal, po - ro)) / dot(pNormal, rDir);

        if (t >= 0)
			return t;
    }
        
    return INFINITY;
}


float RaySphereIntersect(float3 ro, float3 rDir, float3 sCenter, float sRadius)
{
        if (dot(rDir, normalize(sCenter - ro)) < 0.0f)
            return INFINITY;

        float3 dir = ro - sCenter;
        float b = dot(rDir, dir);
        float c = dot(dir, dir) - sRadius * sRadius;

        float delta = b * b - c;

        if (delta < 0.0f)
            return INFINITY;

        delta = sqrt(delta);

        float r1 = -b - delta;
        float r2 = -b + delta;

        if (r1 < 0 && r2 > 0) return r2;
        if (r1 > 0 && r2 < 0) return r1;
        
        return min(r1, r2);
}

