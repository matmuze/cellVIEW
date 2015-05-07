#define MAX_SUBINSTANCE_SIZE 4096
	
CBUFFER_START(constantBuffer)		
uniform float _Scale;		
uniform int _EnableBrownianMotion;
uniform int _EnableCrossSection;		
uniform float4 _CrossSectionPlane;	
uniform int _EnableLod;	
uniform float _DistanceLod0;			
uniform float _DistanceLod1;	
uniform float _MaxAtomRadiusLod0;		
uniform float _MinAtomRadiusLod1;
uniform float _DecimationFactorLod0;	
uniform float _DecimationFactorLod1;
CBUFFER_END		

uniform int _EnableObjectCulling;
uniform int _SubInstanceStart;

uniform int _EnableShadows;			
uniform sampler2D_float _ShadowMap;
uniform float3 _ShadowCameraWorldPos;
uniform float4x4 _ShadowCameraViewMatrix;
uniform float4x4 _ShadowCameraViewProjMatrix;	

uniform float4 _FrustrumPlane_0;
uniform float4 _FrustrumPlane_1;
uniform float4 _FrustrumPlane_2;
uniform float4 _FrustrumPlane_3;
uniform float4 _FrustrumPlane_4;
uniform float4 _FrustrumPlane_5;
		
uniform	StructuredBuffer<float> _AtomRadii;
uniform StructuredBuffer<float4> _AtomPositions;							
		
uniform	StructuredBuffer<int> _InstanceTypes;
uniform	StructuredBuffer<int> _InstanceStates;
uniform	StructuredBuffer<float4> _InstancePositions;
uniform	StructuredBuffer<float4> _InstanceRotations;
	
uniform	StructuredBuffer<int> _SubInstanceCullFlags;			
uniform StructuredBuffer<float4> _SubInstanceInformations;	
	
uniform StructuredBuffer<float4> _IngredientColors;				
uniform StructuredBuffer<int> _IngredientToggle;	
uniform StructuredBuffer<int> _IngredientAtomCount;										
uniform StructuredBuffer<int> _IngredientAtomStart;	
uniform StructuredBuffer<float>_IngredientBoundingSphere;

//--------------------------------------------------------------------------------------

struct LodInfo
{
	int level : INT10;					
	int decimationFactor : INT11;		

	float atomRadiusMin : FLOAT10;	
	float atomRadiusMax : FLOAT11;	 		
	float atomRadiusLerp : FLOAT12; 
};

struct vs2hs
{
	uint id : UINT0;
	uint type : UINT1;
	uint state : UINT2;	
	uint atomCount : UINT3;
	uint atomStart : UINT4;	
	uint subIntanceAtomCount : UINT5;
	uint subIntanceAtomStart : UINT6;	
	uint subInstanceId : UINT7;

	float3 pos : FLOAT30;
	float4 rot : FLOAT40;	
		
	LodInfo lodInfo;	
};
        	
struct hs2ds
{
	uint id : UINT0;		
	uint type : UINT1;
	uint state : UINT2;	
	uint atomCount : UINT3;
	uint atomStart : UINT4;	
	uint subIntanceAtomCount : UINT5;
	uint subIntanceAtomStart : UINT6;	
	uint subInstanceId : UINT7;

	float3 pos : FLOAT30;
	float4 rot : FLOAT40;

	LodInfo lodInfo; 
};

struct hsConst
{
	float tessFactor[2] : SV_TessFactor;
};

struct ds2gs
{
	uint id : UINT0;		
	uint type : UINT1;
	uint state : UINT2;		
	uint subInstanceId : UINT7;
				
	int atomType : INT0;		
	float3 pos : FLOAT30;			
		
	LodInfo lodInfo;	
};
	
//--------------------------------------------------------------------------------------

void HSConst(InputPatch<vs2hs, 1> input, uint patchID : SV_PrimitiveID, out hsConst output)
{
	output.tessFactor[0] = output.tessFactor[1] = ( input[0].subIntanceAtomCount == 0 ) ? 0 : ceil(sqrt(input[0].subIntanceAtomCount));									
	return;
}

//--------------------------------------------------------------------------------------

bool PlaneTest( float4 plane, float3 center, float offset)
{
	return dot(plane.xyz, center - plane.xyz * -plane.w) + offset > 0;
}

bool SphereInFrustum( float3 center, float radius )
{
	bool inFrustrum = true;

	inFrustrum = inFrustrum & PlaneTest(_FrustrumPlane_0, center, radius);
	inFrustrum = inFrustrum & PlaneTest(_FrustrumPlane_1, center, radius);
	inFrustrum = inFrustrum & PlaneTest(_FrustrumPlane_2, center, radius);
	inFrustrum = inFrustrum & PlaneTest(_FrustrumPlane_3, center, radius);
	inFrustrum = inFrustrum & PlaneTest(_FrustrumPlane_4, center, radius);
	inFrustrum = inFrustrum & PlaneTest(_FrustrumPlane_5, center, radius);	
		
	return inFrustrum;
}				
