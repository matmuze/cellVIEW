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

//--------------------------------------------------------------------------------------

float3 QuaternionTransform( float4 q, float3 v )
{ 
	return v + 2.0 * cross(cross(v, q.xyz ) + q.w * v, q.xyz);
}

float4 QuaternionFromAxisAngle(float3 axis, float angle)
{
	float4 q;
	q.x = axis.x * sin(angle/2);
	q.y = axis.y * sin(angle/2);
	q.z = axis.z * sin(angle/2);
	q.w = cos(angle/2);
	return q;
}

float3 CubicInterpolate(float3 y0, float3 y1, float3 y2,float3 y3, float3 mu)
{
   float mu2 = mu*mu;
   float3 a0,a1,a2,a3;

   //a0 = -0.5*y0 + 1.5*y1 - 1.5*y2 + 0.5*y3;
   //a1 = y0 - 2.5*y1 + 2*y2 - 0.5*y3;
   //a2 = -0.5*y0 + 0.5*y2;
   //a3 = y1;

   a0 = y3 - y2 - y0 + y1;
   a1 = y0 - y1 - a0;
   a2 = y2 - y0;
   a3 = y1;

   return(a0*mu*mu2 + a1*mu2+a2 * mu+a3);
}

//--------------------------------------------------------------------------------------

struct vs2ds
{					
	int segmentCount : INT0;		
	int localSphereCount : INT1;		
	int globalSphereCount : INT2;		
		
	float3 pos0 : FLOAT30;
	float3 pos1 : FLOAT31;
	float3 pos2 : FLOAT32;
	float3 pos3 : FLOAT33;						
	float4 rootPoints[NUM_ROOT_POINTS_FLOAT] : FLOAT4;
};

//--------------------------------------------------------------------------------------

void VS(uint id : SV_VertexID, out vs2ds output)
{			 
	int numLinearSeachStep = 4;
	int numBinarySearchStep = 4;			
	int numStepsMax = min(_NumSteps, NUM_STEPS_PER_SEGMENT_MAX);	
	
	float linearStepSize = 0.5f / numStepsMax;	
	float stepLength = _SegmentLength / (float)numStepsMax;
		
	int controlPointId = id + 1; // The first segment is skipped

	float3 pos0 = _DnaControlPoints[controlPointId -1].xyz;
	float3 pos1 = _DnaControlPoints[controlPointId].xyz;
	float3 pos2 = _DnaControlPoints[controlPointId + 1].xyz;
	float3 pos3 = _DnaControlPoints[controlPointId + 2].xyz;

	output.pos0 = pos0;
	output.pos1 = pos1;
	output.pos2 = pos2;
	output.pos3 = pos3;
	
	output.rootPoints[0][0] = 0;
	output.segmentCount = numStepsMax;
	output.localSphereCount = 41;
	output.globalSphereCount = numStepsMax * output.localSphereCount;

	/*****/

	// First pass to find out the size of the last interval 

	float3 current;	
	int stepsCount = 0;
	float3 previous = pos1;	
	float interpolationValue = linearStepSize;

	// Find the number of segments
	while(stepsCount < numStepsMax-1)
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
	stepLength += stepLengthOffset / (float)numStepsMax;	// Correct segment length to fill the last blank
	
	// Find the number of segments
	while(stepsCount < numStepsMax-1)
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
			
	int atomId = sphereId / op[0].segmentCount;				
	int segmentId = (sphereId % op[0].segmentCount);	

	// Find begin segment pos	
	int beingSegmentId = segmentId;	
	float beingSegmentLerp =  op[0].rootPoints[beingSegmentId / 4][beingSegmentId % 4];
	float3 beginSegmentPos = CubicInterpolate(op[0].pos0, op[0].pos1, op[0].pos2, op[0].pos3, beingSegmentLerp); 		
	
	// Find end segment pos
	int endSegmentId = beingSegmentId + 1;	
	float endSegmentLerp =  (endSegmentId < op[0].segmentCount) ? op[0].rootPoints[endSegmentId / 4][endSegmentId % 4] : 1; // if this is the last step use 1 for the lerp value
	float3 endSegmentPos = CubicInterpolate(op[0].pos0, op[0].pos1, op[0].pos2, op[0].pos3, endSegmentLerp);
	
	// Find mid segment
	float3 diff = endSegmentPos - beginSegmentPos;	
	float3 tangent = normalize(diff);
	float3 sphereOffset = beginSegmentPos + diff * 0.5;	
	float midsegmentLerp = endSegmentLerp + (endSegmentLerp - endSegmentLerp) * 0.5;

	// Find normal at control points
	float3 n1 = normalize(cross(op[0].pos0 - op[0].pos1, op[0].pos2 - op[0].pos1));	
	float3 n2 = normalize(cross(op[0].pos1 - op[0].pos2,  op[0].pos3 - op[0].pos2));	

	// Find binormal
	float3 crossDirection = float3(0,1,0);	
	float3 binormal = normalize(lerp(n1, n2, beingSegmentLerp));		
	//float3 binormal = normalize(cross(tangent, crossDirection));		
		
	// Do helix rotation of the binormal arround the tangent
	float rotationAngle = 3.14 * segmentId * _TwistFactor / 180.0; 
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
	
	// Use this to draw the coordinate frame for debug
	//float3 sphereCenter;	
	//int halfSphereCount = op[0].localSphereCount * 0.5;
	//float hdd = atomId - halfSphereCount;
			
	//if(hdd < 0)
	//{
	//	float t = abs(hdd) / halfSphereCount;
	//	sphereCenter = float3(1,0,0) * t * 1;
	//}
	//else
	//{
	//	float t = abs(hdd) / halfSphereCount;
	//	sphereCenter = float3(0,1,0) * t * 1;
	//}

	// Fetch nucleotid atoms
	float4 sphereCenter = _DnaAtoms[atomId];	
		
	sphereCenter.xyz = QuaternionTransform(quat, sphereCenter.xyz);
	sphereCenter.xyz = QuaternionTransform(quat2, sphereCenter.xyz);	

	output.position = sphereOffset + sphereCenter * _Scale; 
	output.radius = (y >= input.tessFactor[0] || sphereId >= op[0].globalSphereCount) ? 0 : sphereCenter.w * 1.2; // Discard unwanted spheres	
	//output.color = float3(1, endSegmentLerp, ((float)atomId / 41.0f));	// Debug colors		
	output.color = float3(1,0.8,0.8);
}

//--------------------------------------------------------------------------------------

struct gs2fs
{				
	float2 uv: TEXCOORD0;	
	nointerpolation float radius : FLOAT0;	
	nointerpolation float3 color : FLOAT30;		
	centroid float4 position : SV_Position;	
};
							
[maxvertexcount(3)]
void GS(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
{	
	if(input[0].radius <= 0) return;	
		
	float radius = input[0].radius * _Scale;

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
	
void FS (gs2fs input, out float4 color : COLOR0, out float depth : sv_depthgreaterequal) 
{					
	float lensqr = dot(input.uv, input.uv);   
	if(lensqr > 1) discard;

	// Find normal
	float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));		
		
	// Find color
	//float ndotl = pow(max( 0.0, dot(float3(0,0,1), normal)),0.1); //, 0.15 * input.lambertFalloff);						
	color = float4(input.color, 1);				

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