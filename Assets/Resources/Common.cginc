#define MAX_SUBINSTANCE_SIZE 4096
	
CBUFFER_START(constantBuffer)		
uniform float _Scale;			
uniform int _EnableShadows;
CBUFFER_END		
		
uniform float3 _CameraForward;	
			
//uniform sampler2D_float _ShadowMap;
//uniform float3 _ShadowCameraWorldPos;
//uniform float4x4 _ShadowCameraViewMatrix;
//uniform float4x4 _ShadowCameraProjMatrix;
//uniform float4x4 _ShadowCameraViewProjMatrix;	
		
uniform int _FirstLevelBeingRange;
uniform float4x4 _LodLevelsInfos;

uniform	StructuredBuffer<int> _InstanceTypes;
uniform	StructuredBuffer<int> _InstanceStates;
uniform	StructuredBuffer<float4> _InstancePositions;
uniform	StructuredBuffer<float4> _InstanceRotations;
						
uniform StructuredBuffer<float4> _IngredientColors;	
uniform StructuredBuffer<float4> _ProteinAtomPositions;	
uniform StructuredBuffer<float4> _ProteinClusterPositions;	
uniform StructuredBuffer<float4> _ProteinSphereBatchInfos;	
					
uniform StructuredBuffer<float4> _LipidAtomPositions;		
uniform StructuredBuffer<float4> _LipidSphereBatchInfos;	
uniform StructuredBuffer<float4> _LipidInstancePositions;	

//--------------------------------------------------------------------------------------

struct LodInfo
{
			
	float radiusMin : FLOAT10;	
	float radiusMax : FLOAT11;	 		
	float radiusLerp : FLOAT12; 
};

struct vs2ds
{
	int id : INT0;
	int type : INT1;
	int state : INT2;	
	int sphereStart : INT3;	
	int sphereCount : INT4;	
	int lodLevel : INT5;	
	int decimationFactor : INT6;		
	float radiusScale : FLOAT0;
				
	float3 pos : FLOAT30;
	float3 color : FLOAT31;				
	float4 rot : FLOAT40;	
};

struct ds2gs
{
	int id : INT0;		
	int type : INT1;
	int state : INT2;								
	float radius : FLOAT0;	
		
	float3 pos : FLOAT30;	
	float3 color : FLOAT31;		
};

struct gs2fs
{
	uint id : UINT0;	
			
	nointerpolation float radius : FLOAT0;	
	nointerpolation float lambertFalloff : FLOAT1;
	nointerpolation float3 color : FLOAT30;		
				
	float2 uv: TEXCOORD0;	
	centroid float4 pos : SV_Position;	
};
	
//--------------------------------------------------------------------------------------

struct hsConst
{
	float tessFactor[2] : SV_TessFactor;
};

void HSConst(InputPatch<vs2ds, 1> input, uint patchID : SV_PrimitiveID, out hsConst output)
{
	output.tessFactor[0] = output.tessFactor[1] = ( input[0].sphereCount <= 0 ) ? 0 : ceil(sqrt(input[0].sphereCount));									
	return;
}

[domain("isoline")]
[partitioning("integer")]
[outputtopology("point")]
[outputcontrolpoints(1)]				
[patchconstantfunc("HSConst")]
vs2ds hs (InputPatch<vs2ds, 1> input, uint ID : SV_OutputControlPointID)
{
	return input[0];
} 

//--------------------------------------------------------------------------------------
