// Render Lipids

CGINCLUDE

#include "UnityCG.cginc"
#include "Helper.cginc"		
	
#define NUM_STEPS_PER_SEGMENT_MAX 16
#define NUM_ROOT_POINTS_FLOAT (NUM_STEPS_PER_SEGMENT_MAX / 4)

uniform int _NumSteps;
uniform int _NumSegments;		
uniform int _EnableTwist;	

uniform float _Scale;
uniform float _TwistFactor;
uniform float _SegmentLength;

uniform	StructuredBuffer<float4> _DnaAtoms;
uniform	StructuredBuffer<float4> _DnaControlPoints;
uniform	StructuredBuffer<float4> _DnaControlPointsNormals;

//--------------------------------------------------------------------------------------

struct vs2ds
{			
	int segmentId : INT0;			
	int numSteps : INT1;		
	int localSphereCount : INT2;		
	int globalSphereCount : INT3;		
	int pid : INT4;
	
	float radiusScale : FLOAT0;		
		
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

	float4 pos0 = _DnaControlPoints[id]; // We skip the first segment
	float4 pos1 = _DnaControlPoints[id + 1];
	float4 pos2 = _DnaControlPoints[id + 2];
	float4 pos3 = _DnaControlPoints[id + 3];

	output.n1 = _DnaControlPointsNormals[id].xyz;
	output.n2 = _DnaControlPointsNormals[id + 1].xyz;

	bool skipSegment = pos0.w != pos1.w || pos1.w != pos2.w || pos2.w != pos3.w;
    
    output.pid = pos0.w;
    
	output.pos0 = pos0.xyz;
	output.pos1 = pos1.xyz;
	output.pos2 = pos2.xyz;
	output.pos3 = pos3.xyz;
	
	output.segmentId = id.x;
	output.numSteps = numSteps;
	
	output.localSphereCount = 1;
	output.radiusScale = 2;

	output.localSphereCount = skipSegment ? 0 : 41;
	output.radiusScale = 1.0f;
	
	output.globalSphereCount = numSteps * output.localSphereCount;
	
	/*****/

	// First pass to find out the size of the last interval 

	float3 current;	
	int stepsCount = 0;
	float3 previous = pos1;	
	float interpolationValue = linearStepSize;

	float rootPoints[NUM_STEPS_PER_SEGMENT_MAX];		
	for(int i = 0; i < NUM_STEPS_PER_SEGMENT_MAX; i++)
	{
		rootPoints[i] = 0;
	}

	// Find the number of segments
	for(int i = 1; i < numSteps; i++)
	{	
		// Linear search
		[unroll(4)]
		while(true)
		{					
			current = CubicInterpolate(pos0, pos1, pos2, pos3, interpolationValue);						
			interpolationValue += (distance(current, previous) < stepLength) ? linearStepSize : 0;
		}
	
		float binaryStepSize = linearStepSize * 0.5;
		interpolationValue -= binaryStepSize;

		// Binary search
		[unroll(8)]
		while(true)
		{
			binaryStepSize *= 0.5;	
			current = CubicInterpolate(pos0, pos1, pos2, pos3, interpolationValue);				
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
	for(int i = 1; i < numSteps; i++)
	{	
		// Linear search
		[unroll(4)]
		while(true)
		{					
			current = CubicInterpolate(pos0, pos1, pos2, pos3, interpolationValue);						
			interpolationValue += (distance(current, previous) < stepLength) ? linearStepSize : 0;
		}
	
		float binaryStepSize = linearStepSize * 0.5;
		interpolationValue -= binaryStepSize;

		// Binary search
		[unroll(8)]
		while(true)
		{
			binaryStepSize *= 0.5;	
			current = CubicInterpolate(pos0, pos1, pos2, pos3, interpolationValue);				
			interpolationValue += (stepLength - distance(current, previous) < 0) ? -binaryStepSize : binaryStepSize;									
		}	
		
		rootPoints[i] = interpolationValue;	
				
		stepsCount ++;				
		previous = current;
		interpolationValue += linearStepSize;					
	}	

	for(int i = 0; i < numSteps; i++)
	{
		output.rootPoints[i/4][i%4] = rootPoints[i];
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
    
    int pid = op[0].pid;
	
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
	float3 baseCenter = beginStepPos + diff * 0.5;	
	float midStepLerp = beingStepLerp + (endStepLerp - beingStepLerp) * 0.5;
	
	// Find binormal
	float3 crossDirection = float3(0,1,0);	
	float3 normal = lerp(op[0].n1, op[0].n2, midStepLerp);	
	
	//float3 binormal = normalize(cross(op[0].pos2 - op[0].pos1, normal));
	float3 binormal = normalize(cross(tangent, normal));
	normal = normalize(cross(tangent, binormal));

	//float3 cb1 = normalize(cross(op[0].pos0 - op[0].pos1, op[0].pos2 - op[0].pos1));	
	//float3 cb2 = normalize(cross(op[0].pos1 - op[0].pos2,  op[0].pos3 - op[0].pos2));	
	
	//float3 ct1 = normalize(op[0].pos2 - op[0].pos0);
	//float3 cb1 = normalize(cross(ct1, crossDirection));			
	//float3 cn1 = normalize(cross(ct1, cb1));

	//float3 ct2 = normalize(op[0].pos3 - op[0].pos1);
	//float3 cb2 = normalize(cross(ct2, crossDirection));			
	//float3 cn2 = normalize(cross(ct2, cb2));

	//float3 normal = lerp(cn1, cn2, midStepLerp);
	//float3 binormal = normalize(cross(tangent, normal));
	//normal = normalize(cross(tangent, binormal));

	// Do helix rotation of the binormal arround the tangent
	float angleStep = -_TwistFactor * (3.14 / 180);
	float angleStart = op[0].segmentId * op[0].numSteps * angleStep;
	float rotationAngle = stepId * angleStep; 
	float4 q = QuaternionFromAxisAngle(tangent, (_EnableTwist == 1) ? rotationAngle : 0 );		
	
	normal = QuaternionTransform(q, normal);	
	binormal = QuaternionTransform(q, binormal);	

	// Get rotation to align with the normal
	float3 from = float3(0,1,0);	// Assuming that the nucleotide is pointing in the up direction
	float3 to = normal;	
    float3 axis = normalize(cross(from, to));
	float cos_theta = dot(normalize(from), normalize(to));
    float angle = acos(cos_theta);
    float4 quat = QuaternionFromAxisAngle(axis, angle);
	
	// Get rotation to align with the binormal
	float3 from2 = QuaternionTransform(quat, float3(1,0,0));	
	float3 to2 = binormal;	
    float3 axis2 = normalize(cross(from2, to2));
	float cos_theta2 = dot(normalize(from2), normalize(to2));
    float angle2 = acos(cos_theta2);
    float4 quat2 = QuaternionFromAxisAngle(axis2, angle2);
	
	// Fetch nucleotid atoms
	float4 sphere = _DnaAtoms[pid+atomId];
	float3 sphereCenter = sphere.xyz;		
	
	//sphereCenter.xyz *= 0;
	//sphereCenter = from * atomId /  op[0].localSphereCount * 6;
	
	sphereCenter = QuaternionTransform(quat, sphereCenter.xyz);
	sphereCenter = QuaternionTransform(quat2, sphereCenter.xyz);
	
	//sphereCenter.xyz = normal * atomId /  op[0].localSphereCount * 8;

	// Use this to draw the coordinate frame for debug
	//int halfSphereCount = op[0].localSphereCount * 0.5;
	//float hdd = atomId - halfSphereCount;
			
	//if(hdd < 0) sphereCenter.xyz = normal * abs(hdd) / halfSphereCount * 5;
	//else sphereCenter.xyz = binormal * abs(hdd) / halfSphereCount * 5;
	
	//baseCenter = lerp(op[0].pos1, op[0].pos2, beingStepLerp);
	//sphereCenter = float3(0,0,0);
	
	//if(beingStepLerp == 0) 
	//{
	//	sphereOffset.xyz = op[0].n1 * atomId / op[0].localSphereCount * 10;
	//}
	
	//if(endStepLerp == 1)
	//{
	//	sphereOffset.xyz = op[0].n2 * atomId / op[0].localSphereCount * 10;
	//}

	output.position = baseCenter * _Scale + sphereCenter * _Scale; 
	//output.radius =; // Discard unwanted spheres	

	output.radius = (y >= input.tessFactor[0] || sphereId >= op[0].globalSphereCount) ? 0 : sphere.w * op[0].radiusScale * _Scale; // Discard unwanted spheres	

	output.color = float3(1, midStepLerp, ((float)atomId / 41.0f));	// Debug colors		
	//output.color = float3(1,0, 0);	// Debug colors		
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

	float4 viewPos = mul(UNITY_MATRIX_MV, float4(input[0].position, 1));
	viewPos -= normalize( viewPos ) * input[0].radius;
	float4 projPos = mul(UNITY_MATRIX_P, float4(viewPos.xyz, 1));
	float4 offset = mul(UNITY_MATRIX_P, float4(input[0].radius, input[0].radius, 0, 0));

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
	output.position = projPos + float4(output.uv * offset.xy, 0, 0);
	triangleStream.Append(output);

	output.uv = float2(triBaseHalf, triHeigth) - triOffset;
	output.position = projPos + float4(output.uv * offset.xy, 0, 0);
	triangleStream.Append(output);	
								
	output.uv = float2(triBase,0) - triOffset;
	output.position = projPos + float4(output.uv * offset.xy, 0, 0);
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