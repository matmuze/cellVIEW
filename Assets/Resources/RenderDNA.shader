﻿// Render Lipids

CGINCLUDE

#include "UnityCG.cginc"
#include "Helper.cginc"		
	
#define NUM_STEPS_PER_SEGMENT_MAX 32
#define NUM_ROOT_POINTS_FLOAT (NUM_STEPS_PER_SEGMENT_MAX / 4)

uniform int _NumSteps;
uniform int _NumSegments;		
uniform int _EnableTwist;	

uniform float _Scale;
uniform float _TwistFactor;
uniform float _SegmentLength;

uniform	StructuredBuffer<float4> _DnaControlPoints;

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

struct vs2ds
{					
	int segmentCount : INT0;		
	int outputSphereCount : INT1;		
		
	float3 pos0 : FLOAT30;
	float3 pos1 : FLOAT31;
	float3 pos2 : FLOAT32;
	float3 pos3 : FLOAT33;						
	float4 rootPoints[NUM_ROOT_POINTS_FLOAT] : FLOAT4;
};

void VS(uint id : SV_VertexID, out vs2ds output)
{			 
	float3 pos0 = (id.x > 0) ? _DnaControlPoints[id.x -1].xyz: _DnaControlPoints[id.x].xyz;
	float3 pos1 = _DnaControlPoints[id.x].xyz;
	float3 pos2 = _DnaControlPoints[id.x + 1].xyz;
	float3 pos3 = (id.x < _NumSegments -1) ?_DnaControlPoints[id.x + 2].xyz : _DnaControlPoints[id.x + 1].xyz;

	int numLinearSeachStep = 16;
	int numBinarySearchStep = 6;

	int stepsCount = 0;
	int numStepsMax = min(_NumSteps, NUM_STEPS_PER_SEGMENT_MAX);	
	float stepLength = _SegmentLength / (float)numStepsMax;
	float linearStepSize = 0.5f / numStepsMax;	
	float interpolationValue = linearStepSize;

	float3 current;	
	float3 previous = pos1;	

	output.pos0 = pos0;
	output.pos1 = pos1;
	output.pos2 = pos2;
	output.pos3 = pos3;
	
	output.rootPoints[0][0] = 0;

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
			
	previous = pos1;	
	stepsCount = 0;
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

	output.segmentCount = numStepsMax;
	output.outputSphereCount = numStepsMax * 8;
}	

//--------------------------------------------------------------------------------------

struct hsConst
{
	float tessFactor[2] : SV_TessFactor;
};

void HSConst(InputPatch<vs2ds, 1> input, uint patchID : SV_PrimitiveID, out hsConst output)
{
	output.tessFactor[0] = output.tessFactor[1] = ( input[0].outputSphereCount <= 0 ) ? 0 : ceil(sqrt(input[0].outputSphereCount));									
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
	int cull : INT0;
	float3 pos : FLOAT30;
	float3 color : FLOAT31;		
};

float4 quaternionFromAxisAngle(float3 axis, float angle)
{
	float4 q;
	q.x = axis.x * sin(angle/2);
	q.y = axis.y * sin(angle/2);
	q.z = axis.z * sin(angle/2);
	q.w = cos(angle/2);
	return q;
}

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
	float endSegmentLerp =  (endSegmentId < op[0].segmentCount) ? op[0].rootPoints[endSegmentId / 4][endSegmentId % 4] : 1;
	float3 endSegmentPos = CubicInterpolate(op[0].pos0, op[0].pos1, op[0].pos2, op[0].pos3, endSegmentLerp);
		
	// Find mid segment
	float3 diff = endSegmentPos - beginSegmentPos;
	float3 tangent = normalize(diff);
	float3 sphereCenter = beginSegmentPos + diff * 0.5;	
	float middleSegmentLerp = beingSegmentLerp + (endSegmentLerp - beingSegmentLerp) * 0.5;
	
	// Find normal at control points
	float3 n1 = normalize(cross(op[0].pos0 - op[0].pos1, op[0].pos2 - op[0].pos1));	
	float3 n2 = normalize(cross(op[0].pos1 - op[0].pos2,  op[0].pos3 - op[0].pos2));	

	// Find othogonal vector
	//float3 normal2 = normalize(lerp(-n1, n2, middleSegmentLerp));  // Strangly enough all segments that are twister in normal1 are fine in normal2... 		
	float3 normal = normalize(lerp(n1, n2, middleSegmentLerp)); // interpolate between control points normals			
	normal = normalize(cross(tangent, normal));		

	//// Find previous end segment pos
	//int pSegmentId = beingSegmentId - 1;	
	//float plerp = op[0].rootPoints[pSegmentId / 4][pSegmentId % 4];
	//float3 ppos = CubicInterpolate(op[0].pos0, op[0].pos1, op[0].pos2, op[0].pos3, plerp);
	//float3 ptangent = beginSegmentPos - ppos;
	//float3 pnormal = normalize(lerp(n1, n2, plerp)); // interpolate between control points normals			
	//pnormal = normalize(cross(ptangent, pnormal));					

	// Find othogonal vector
	//float3 refVector = normalize(float3(0,1,0));
	//float normal = normalize(cross(tangent, refVector));
	//normal = normalize(cross(tangent, normal));	

	// Do rotation
	float rotationAngle = 3.14 * segmentId * _TwistFactor / 180.0; // 3.14 * atomId * 10 / 180.0; 
	float4 q = quaternionFromAxisAngle(tangent, (_EnableTwist == 1) ? rotationAngle : 0 );		
	normal = qtransform(q, normal);	
			
	float centerOffset = (atomId - 4) * 0.25;
	output.pos = (sphereCenter + normal * centerOffset) * _Scale; 
	output.color = float3(1, beingSegmentLerp, 0);
	output.cull = (y >= input.tessFactor[0] || sphereId >= op[0].outputSphereCount) ? 1 : 0; // Discard unwanted spheres		
}

//--------------------------------------------------------------------------------------

struct gs2fs
{				
	float2 uv: TEXCOORD0;	
	centroid float4 pos : SV_Position;	
	nointerpolation float radius : FLOAT0;	
	nointerpolation float3 color : FLOAT30;		
};
							
[maxvertexcount(3)]
void GS(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
{	
	if(input[0].cull == 1) return;	
		
	float radius = 0.25 * _Scale;

	float4 pos = mul(UNITY_MATRIX_MVP, float4(input[0].pos, 1));
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
	output.pos = pos + float4(output.uv * offset.xy, 0, 0);
	triangleStream.Append(output);

	output.uv = float2(triBaseHalf, triHeigth) - triOffset;
	output.pos = pos + float4(output.uv * offset.xy, 0, 0);
	triangleStream.Append(output);	
								
	output.uv = float2(triBase,0) - triOffset;
	output.pos = pos + float4(output.uv * offset.xy, 0, 0);
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
	//float ndotl = pow(max( 0.0, dot(float3(0,0,1), normal)),1); //, 0.15 * input.lambertFalloff);						
	color = float4(input.color, 1);				

	// Find depth
	float eyeDepth = LinearEyeDepth(input.pos.z) + input.radius * (1-normal.z);
	depth = 1 / (eyeDepth * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;			
}
ENDCG

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