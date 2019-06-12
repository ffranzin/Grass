
#ifndef GRASS_VARIABLES
#define GRASS_VARIABLES

#define NaN 0.0/0.0
#define SCALE_CUTOFF 0.15
#define ALPHA_CUTOFF 0.15

#define true 1
#define false 0

//----------------------------------------------------------------------------------------
//GRASS PARAMETERS - ANIMATION
//----------------------------------------------------------------------------------------
float _animationRange;
float _animationFactor;
float _animationSpeed;
int _interpolationHeightAnimation;
int _animationEnabled;
float4 qBaseRotation;

struct RotatedPosition
{
	float4 position;
	float4 rotation;
};


//----------------------------------------------------------------------------------------
//GRASS PARAMETERS - SCALE
//----------------------------------------------------------------------------------------
float _minScaleGrass;
float _maxScaleGrass;



//----------------------------------------------------------------------------------------
//NODE DESCRIPTIONS
//----------------------------------------------------------------------------------------
float4 _cellWorldDesc;
float4 _positionsBufferAtlasDesc;
float4 _heightmapAtlasDesc;
float4 _vectorFeaturemapAtlasDesc;
float4 _collisionmapDesc;

float _bufferSize1D;
float _cellSubdivision;

int _isCurvedPass;

//----------------------------------------------------------------------------------------
// WORLD PARAMETERS
//----------------------------------------------------------------------------------------

int _materialsCount;
float3 _cameraPosition;

int setCulled;


//----------------------------------------------------------------------------------------
//LOD
//----------------------------------------------------------------------------------------
int _LODCount;
int _CurrentLOD;
StructuredBuffer<float> _LODRanges;




//----------------------------------------------------------------------------------------
//GRASS RENDERING VARIABLE
//----------------------------------------------------------------------------------------
float2 seed;
float4 collisionSampleDebug;



#endif

