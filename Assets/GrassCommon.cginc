
#ifndef GRASS_COMMON
#define GRASS_COMMON

#include "QuaternionUtils.cginc"
#include "GrassVariables.cginc"
#include "Assets\DistributionModule\Resources\GrassUtils.cginc"

UNITY_DECLARE_TEX2DARRAY(_grassAlbedo);
UNITY_DECLARE_TEX2DARRAY(_grassNormal);

StructuredBuffer<float4> _positionsBuffer;
StructuredBuffer<RotatedPosition> _positionsBufferRotated;


appdata_full VertexAnimation(appdata_full v, float3 worldPos, float scale)
{
	//return v;

    float distCam = length(worldPos - _WorldSpaceCameraPos.xyz);
	
	float3 axis = float3(1, 0, 1);

    //if (_animationEnabled == 1 && distCam < _animationRange)
    {
        float h = clamp(v.vertex.y, 0, 10);
        
        //remove the animation for far objects - to reduce the aliasing
        float dist = 1 - Remap(distCam, 0, _animationRange, 0, 1);

        float delta = _animationFactor;

        if (_interpolationHeightAnimation == 1)
        {
            float movement = abs(_animationFactor - 10); //lower number = more movement
					
            delta = 4 - pow((h - 2), 2);
            delta = pow(delta, 0.5) / movement;

            v.vertex.xyz += axis * delta * h * sin(_Time) * _animationSpeed * dist;
        }

        //v.vertex.xyz += axis * h * sin(_Time) * _animationSpeed * 1 * dist;
        
    }
    
	v.vertex.xyz += axis * v.vertex.y * sin(_Time ) * 0.05 * scale;// * nrand(worldPos.xz, 1);

    return v;
}


appdata_full VertexRotation(appdata_full v)
{
    v.vertex.xyz = QuaternionRotatePoint(v.vertex.xyz, qBaseRotation);
    v.normal = QuaternionRotatePoint(v.normal, qBaseRotation);
    v.tangent.xyz = QuaternionRotatePoint(v.tangent.xyz, qBaseRotation);
    
    return v;
}


inline int GetMaterialID(float2 uv)
{
    return CustomRandInt(uv % 5000, 0, _materialsCount);
}


inline void CreateQuaternionBaseRotation(float2 uv, float4 rotationAxis)
{
	float4 bendXZ = QuaternionFromToRotationNormalized(float3(0,1,0), normalize(rotationAxis.xyz));
	//float4 bendXZ = QuaternionFromToRotationNormalized(float3(0,1,0), float3(0,1,0));

	float4 randomYaw = QuaternionFromNormalizedDirection(float3(0, 1, 0), (uv.x + uv.y) * 360);

	qBaseRotation = normalize(QuaternionMultiplication(bendXZ, randomYaw));
}



inline void SetMatrix(float3 worldPos, float scale)
{
    unity_ObjectToWorld._11_21_31_41 = float4(scale, 0, 0, 0);
    unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
    unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale, 0);
    unity_ObjectToWorld._14_24_34_44 = float4(worldPos, 1);

    unity_WorldToObject = unity_ObjectToWorld;
    unity_WorldToObject._14_24_34 *= -1;
    unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
}


void setup()
{
	#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED 

        float4 samplePosition, sampleRotation;

        if(_isCurvedPass)
        {
            samplePosition = _positionsBufferRotated[unity_InstanceID].position;
            sampleRotation = _positionsBufferRotated[unity_InstanceID].rotation;

            sampleRotation.w = 1;

            collisionSampleDebug = sampleRotation;
        }
        else
        {
            samplePosition = _positionsBuffer[unity_InstanceID];
            sampleRotation = float4(0, 1, 0, 0);
        }
        
		float distance2Cam = length(samplePosition.xyz - _WorldSpaceCameraPos);

		if(samplePosition.a < SCALE_CUTOFF || distance2Cam > _LODRanges[_LODCount - 1])
		{
			setCulled = true;
			return;
		}

		setCulled = false;
        
		CreateQuaternionBaseRotation(samplePosition.xz, sampleRotation);

        SetMatrix(samplePosition.xyz, samplePosition.a);
    #endif
}




#endif

