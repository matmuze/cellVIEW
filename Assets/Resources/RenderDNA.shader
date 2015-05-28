// Render Lipids

CGINCLUDE

#include "UnityCG.cginc"
#include "Helper.cginc"		
	
#define NUM_STEPS_PER_SEGMENT_MAX 36
#define NUM_ROOT_POINTS_FLOAT (NUM_STEPS_PER_SEGMENT_MAX / 4)

uniform int _NumSteps;
uniform int _NumSegments;		
uniform int _EnableTwist;	

uniform float _Scale;
uniform float _TwistFactor;
uniform float _SegmentLength;

uniform	StructuredBuffer<float4> _DnaAtoms;
uniform	StructuredBuffer<float4> _DnaControlPoints;
//uniform	StructuredBuffer<float4> _DnaControlPointsNormal;

//--------------------------------------------------------------------------------------

struct vs2ds
{			
	int segmentId : INT0;			
	int numSteps : INT1;		
	int localSphereCount : INT2;		
	int globalSphereCount : INT3;		
		
	float3 pos0 : FLOAT30;
	float3 pos1 : FLOAT31;
	float3 pos2 : FLOAT32;
	float3 pos3 : FLOAT33;	
	
	float3 n1 : FLOAT34;
	float3 n2 : FLOAT35;	
							
	float4 rootPoints[NUM_ROOT_POINTS_FLOAT] : FLOAT4;
};

//--------------------------------------------------------------------------------------

void VS(uint id : SV_VertexID, out vs2ds output)
{			 
	int numLinearSeachStep = 4;
	int numBinarySearchStep = 8;			
	int numSteps = min(_NumSteps, NUM_STEPS_PER_SEGMENT_MAX);	
	
	float linearStepSize = 0.5f / numSteps;	
	float stepLength = _SegmentLength / (float)numSteps;

	float3 pos0 = _DnaControlPoints[id].xyz; // We skip the first segment
	float3 pos1 = _DnaControlPoints[id + 1].xyz;
	float3 pos2 = _DnaControlPoints[id + 2].xyz;
	float3 pos3 = _DnaControlPoints[id + 3].xyz;

	//output.n1 = _DnaControlPointsNormal[id + 1];
	//output.n2 = _DnaControlPointsNormal[id + 2];

	output.pos0 = pos0;
	output.pos1 = pos1;
	output.pos2 = pos2;
	output.pos3 = pos3;
	
	output.segmentId = id.x;
	output.numSteps = numSteps;
	output.rootPoints[0][0] = 0;
	output.localSphereCount = 20;
	output.globalSphereCount = numSteps * output.localSphereCount;

	/*****/

	// First pass to find out the size of the last interval 

	float3 current;	
	int stepsCount = 0;
	float3 previous = pos1;	
	float interpolationValue = linearStepSize;

	// Find the number of segments
	while(stepsCount < numSteps-1)
	{	
		// Linear search
		for( uint i = 0; i < numLinearSeachStep; i++ )
		{					
			current = CubicInterpolate(pos0, pos1, pos2, pos3, interpolationValue);						
			if(stepLength - distance(current, previous) < 0) break;		
			interpolationValue += linearStepSize;
		}
	
		float binaryStepSize = linearStepSize * 0.5;
		interpolationValue -= binaryStepSize;

		// Binary search
		for( uint i = 0; i < numBinarySearchStep; i++ )
		{
			current = CubicInterpolate(pos0, pos1, pos2, pos3, interpolationValue);	
			binaryStepSize *= 0.5;
			interpolationValue += (stepLength - distance(current, previous) < 0) ? -binaryStepSize : binaryStepSize;							
		}		
				
		stepsCount ++;				
		previous = current;
		interpolationValue += linearStepSize;			
	}
		
	// Second pass with corrected step length to normalize the spacing between each steps 
			
	stepsCount = 0;
	previous = pos1;	
	interpolationValue = linearStepSize;
	
	float stepLengthOffset = distance(current, pos2) - stepLength;
	stepLength += stepLengthOffset / (float)numSteps;	// Correct segment length to fill the last blank
	
	// Find the number of segments
	while(stepsCount < numSteps-1)
	{	
		// Linear search
		for( uint i = 0; i < numLinearSeachStep; i++ )
		{					
			current = CubicInterpolate(pos0, pos1, pos2, pos3, interpolationValue);						
			if(stepLength - distance(current, previous) < 0) break;		
			interpolationValue += linearStepSize;
		}
	
		float binaryStepSize = linearStepSize * 0.5;
		interpolationValue -= binaryStepSize;

		// Binary search
		for( uint i = 0; i < numBinarySearchStep; i++ )
		{
			current = CubicInterpolate(pos0, pos1, pos2, pos3, interpolationValue);	
			binaryStepSize *= 0.5;
			interpolationValue += (stepLength - distance(current, previous) < 0) ? -binaryStepSize : binaryStepSize;							
		}		
				
		stepsCount ++;		
		output.rootPoints[stepsCount/4][stepsCount%4] = interpolationValue;			
		
		previous = current;
		interpolationValue += linearStepSize;					
	}	
}	

//--------------------------------------------------------------------------------------

struct hsConst
{
	float tessFactor[2] : SV_TessFactor;
};

void HSConst(InputPatch<vs2ds, 1> input, uint patchID : SV_PrimitiveID, out hsConst output)
{
	output.tessFactor[0] = output.tessFactor[1] = ( input[0].globalSphereCount <= 0 ) ? 0 : ceil(sqrt(input[0].globalSphereCount));									
	return;
}

[domain("isoline")]
[partitioning("integer")]
[outputtopology("point")]
[outputcontrolpoints(1)]				
[patchconstantfunc("HSConst")]
vs2ds HS (InputPatch<vs2ds, 1> input, uint ID : SV_OutputControlPointID)
{
	return input[0];
} 

//--------------------------------------------------------------------------------------

struct ds2gs
{	
	int id : INT0;
	float radius : FLOAT0;
	float3 color : FLOAT30;		
	float3 position : FLOAT31;
};

[domain("isoline")]
void DS(hsConst input, const OutputPatch<vs2ds, 1> op, float2 uv : SV_DomainLocation, out ds2gs output)
{
	int x = round(uv.y * input.tessFactor[0]);
	int y = round(uv.x * input.tessFactor[0]);		
	int sphereId = x + y * input.tessFactor[0];	
			
	int atomId = sphereId / op[0].numSteps;				
	int stepId = (sphereId % op[0].numSteps);	

	// Find normal at control points
	//float3 n1 = op[0].n1; 
	//float3 n2 = op[0].n2; 

	float3 n1 = normalize(cross(op[0].pos0 - op[0].pos1, op[0].pos2 - op[0].pos1));	
	float3 n2 = normalize(cross(op[0].pos1 - op[0].pos2,  op[0].pos3 - op[0].pos2));	

	// Find begin step pos	
	int beingStepId = stepId;	
	float beingStepLerp =  op[0].rootPoints[beingStepId / 4][beingStepId % 4];
	float3 beginStepPos = CubicInterpolate(op[0].pos0, op[0].pos1, op[0].pos2, op[0].pos3, beingStepLerp); 		
	
	// Find end step pos
	int endStepId = beingStepId + 1;	
	float endStepLerp =  (endStepId < op[0].numSteps) ? op[0].rootPoints[endStepId / 4][endStepId % 4] : 1; // if this is the last step use 1 for the lerp value
	float3 endStepPos = CubicInterpolate(op[0].pos0, op[0].pos1, op[0].pos2, op[0].pos3, endStepLerp);
	
	// Find mid step pos
	float3 diff = endStepPos - beginStepPos;	
	float3 tangent = normalize(diff);
	float3 sphereOffset = beginStepPos + diff * 0.5;	
	float midStepLerp = beingStepLerp + (endStepLerp - beingStepLerp) * 0.5;
	
	// Find binormal
	float3 crossDirection = float3(0,1,0);	
	float3 binormal = normalize(lerp(n1, n2, midStepLerp));		
	//float3 binormal = normalize(cross(tangent, crossDirection));		
		
	// Do helix rotation of the binormal arround the tangent
	float angleStep = _TwistFactor * (3.14 / 180);
	float angleStart = op[0].segmentId * op[0].numSteps * angleStep;
	float rotationAngle = angleStart + stepId * angleStep; 
	float4 q = QuaternionFromAxisAngle(tangent, (_EnableTwist == 1) ? rotationAngle : 0 );		
	binormal = QuaternionTransform(q, binormal);	
		
	// Find normal
	float3 normal = normalize(cross(tangent, binormal));

	// Get rotation to align with the normal
	float3 from = float3(0,1,0);	// Assuming that the nucleotide is pointing in the up direction
	float3 to = normal;	
    float3 axis = -normalize(cross(from, to));
	float cos_theta = dot(normalize(from), normalize(to));
    float angle = acos(cos_theta);
    float4 quat = QuaternionFromAxisAngle(axis, angle);
	
	// Get rotation to align with the binormal
	float3 from2 = QuaternionTransform(quat, float3(1,0,0));	
	float3 to2 = binormal;	
    float3 axis2 = -normalize(cross(from2, to2));
	float cos_theta2 = dot(normalize(from2), normalize(to2));
    float angle2 = acos(cos_theta2);
    float4 quat2 = QuaternionFromAxisAngle(axis2, angle2);
	
	// Fetch nucleotid atoms
	float4 sphereCenter = _DnaAtoms[atomId];	
	sphereCenter.xyz = QuaternionTransform(quat, sphereCenter.xyz);
	sphereCenter.xyz = QuaternionTransform(quat2, sphereCenter.xyz);
	
	//// Use this to draw the coordinate frame for debug
	//float4 sphereCenter;	
	//int halfSphereCount = op[0].localSphereCount * 0.5;
	//float hdd = atomId - halfSphereCount;
			
	//if(hdd < 0)
	//{
	//	float t = abs(hdd) / halfSphereCount;
	//	sphereCenter = float4(normal * t  * 5, 1); //float3(1,0,0)  * 1;
	//}
	//else
	//{
	//	float t = abs(hdd) / halfSphereCount;
	//	sphereCenter = float4(binormal * t * 5, 1); //float3(0,1,0) * t * 1;
	//}

	//sphereCenter = float4(0,0,0,1);
	//sphereOffset = lerp(op[0].pos1, op[0].pos2, midsegmentLerp);
	
	//if(beingSegmentLerp == 0) 
	//{
	//	sphereOffset = op[0].pos1;
	//	sphereCenter.xyz = atomId * n1;
	//	float3 axis = normalize(op[0].pos0 - op[0].pos2);
	//	float4 quat2 = QuaternionFromAxisAngle(axis, -90 * 3.12 / 180);
	//	sphereCenter.xyz = QuaternionTransform(quat2, sphereCenter.xyz);
	//}
	
	//if(endSegmentLerp == 1)
	//{
	//	sphereCenter.xyz = atomId * n2;
	//	float3 axis = normalize(op[0].pos1 - op[0].pos3);
	//	float4 quat2 = QuaternionFromAxisAngle(axis,  90 * 3.12 / 180);
	//	sphereCenter.xyz = QuaternionTransform(quat2, sphereCenter.xyz);
	//}

	output.position = sphereOffset * _Scale + sphereCenter * _Scale; 
	output.radius = (y >= input.tessFactor[0] || sphereId >= op[0].globalSphereCount) ? 0 : sphereCenter.w * _Scale * 1; // Discard unwanted spheres	
	//output.color = float3(1, midStepLerp, ((float)atomId / 41.0f));	// Debug colors		
	output.color = float3(1,0, 0);	// Debug colors		
	output.id = op[0].segmentId * op[0].numSteps + stepId;
}

//--------------------------------------------------------------------------------------

struct gs2fs
{				
	int id : INT0;
	float2 uv: TEXCOORD0;	
	nointerpolation float radius : FLOAT0;	
	nointerpolation float3 color : FLOAT30;		
	centroid float4 position : SV_Position;	
};
							
[maxvertexcount(3)]
void GS(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
{	
	if(input[0].radius <= 0) return;	
		
	float radius = input[0].radius;

	float4 position = mul(UNITY_MATRIX_MVP, float4(input[0].position, 1));
	float4 offset = mul(UNITY_MATRIX_P, float4(radius, radius, 0, 0));

	//*****//
		
	float triBase = 3.464;
	float triHeigth = 3;
	float triBaseHalf = triBase * 0.5;
	float2 triOffset = float2(triBaseHalf, 1.0);

	gs2fs output;
	output.color = input[0].color;
	output.radius = radius;
	output.id = input[0].id;
	output.uv = float2(0, 0) - triOffset;
	output.position = position + float4(output.uv * offset.xy, 0, 0);
	triangleStream.Append(output);

	output.uv = float2(triBaseHalf, triHeigth) - triOffset;
	output.position = position + float4(output.uv * offset.xy, 0, 0);
	triangleStream.Append(output);	
								
	output.uv = float2(triBase,0) - triOffset;
	output.position = position + float4(output.uv * offset.xy, 0, 0);
	triangleStream.Append(output);
}

//--------------------------------------------------------------------------------------
	
void FS (gs2fs input, out float4 color : COLOR0, out float4 id : COLOR1, out float depth : sv_depthgreaterequal) 
{					
	float lensqr = dot(input.uv, input.uv);   
	if(lensqr > 1) discard;

	// Find normal
	float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));		
		
	// Find color
	//float ndotl = pow(max( 0.0, dot(float3(0,0,1), normal)),0.1); //, 0.15 * input.lambertFalloff);						
	color = float4(ColorCorrection(input.color), 1);				

	// Find id color
	uint t1 = input.id / 256;
	uint t2 = t1 / 256;
	float3 colorId = float3(t2 % 256, t1 % 256, input.id % 256) * 1/255;	
	id = float4(colorId, 1);	

	// Find depth
	float eyeDepth = LinearEyeDepth(input.position.z) + input.radius * (1-normal.z);
	depth = 1 / (eyeDepth * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;			
}

ENDCG

//--------------------------------------------------------------------------------------

Shader "Custom/RenderDNA" 
{	
	SubShader 
	{	
		Pass 
	    {
			ZWrite On

	    	CGPROGRAM				    		
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex VS
			#pragma hull HS
			#pragma domain DS				
			#pragma geometry GS			
			#pragma fragment FS
						
			ENDCG
		}			
	}
	Fallback Off
}	