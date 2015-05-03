Shader "Custom/MoleculeShader" 
{	
	CGINCLUDE

	#include "UnityCG.cginc"
	#include "ColorHelper.cginc"
	
	//*****//			

	float3 qtransform( float4 q, float3 v )
	{ 
		return v + 2.0 * cross(cross(v, q.xyz ) + q.w * v, q.xyz);
	}

	//*****//
	
	
	float hash( float n )
	{
		return frac(sin(n)*43758.5453);
	}

	float noise_3D( float3 x )
	{
		// The noise function returns a value in the range -1.0f -> 1.0f

		float3 p = floor(x);
		float3 f = frac(x);

		f       = f*f*(3.0-2.0*f);
		float n = p.x + p.y*57.0 + 113.0*p.z;

		return lerp(lerp(lerp( hash(n+0.0), hash(n+1.0),f.x),
					   lerp( hash(n+57.0), hash(n+58.0),f.x),f.y),
				   lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
					   lerp( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
	}	

	//*****//

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

	uniform float4 _FrustrumPlane_0;
	uniform float4 _FrustrumPlane_1;
	uniform float4 _FrustrumPlane_2;
	uniform float4 _FrustrumPlane_3;
	uniform float4 _FrustrumPlane_4;
	uniform float4 _FrustrumPlane_5;
						
	uniform StructuredBuffer<int> molAtomCountBuffer;										
	uniform StructuredBuffer<int> molAtomStartBuffer;		
	uniform StructuredBuffer<float4> atomDataPDBBuffer;					
	uniform StructuredBuffer<int> _ToggleIngredientsBuffer;
	uniform StructuredBuffer<float>_IngredientsBoundingSphereRadius;
		
	uniform	StructuredBuffer<float4> molPositions;
	uniform	StructuredBuffer<float4> molRotations;				
	uniform StructuredBuffer<float4> _SubInstancesInfo;	
	uniform StructuredBuffer<float4> _SubInstancesPositions;	

	uniform	StructuredBuffer<int> molStates;
	uniform	StructuredBuffer<int> molTypes;
	uniform	StructuredBuffer<float> atomRadii;
	uniform StructuredBuffer<float4> molColors;		
	uniform	StructuredBuffer<float4> atomColors;	
		
	uniform	float4x4 SHADOW_CAMERA_MATRIX_MVP;	
	uniform float3 _ShadowCameraWorldPos;
	uniform sampler2D_float _ShadowMap;

	struct LodInfo
	{
		int level : INT10;					
		int atomDecimationFactor : INT11;		

		float atomRadiusMin : FLOAT10;	
		float atomRadiusMax : FLOAT11;	 		
		float atomRadiusLerp : FLOAT12; 
	};

	struct vs2hs
	{
		uint id : UINT0;
		uint molType : UINT1;
		uint molState : UINT2;	
		uint atomCount : UINT3;
		uint atomStart : UINT4;	
		uint subIntanceAtomCount : UINT5;
		uint subIntanceAtomStart : UINT6;

	    float3 pos : FLOAT30;
	    float4 rot : FLOAT40;	
		
		LodInfo lodInfo;	
    };
        	
	struct hs2ds
	{
		uint id : UINT0;		
		uint molType : UINT1;
		uint molState : UINT2;	
		uint atomCount : UINT3;
		uint atomStart : UINT4;	
		uint subIntanceAtomCount : UINT5;
		uint subIntanceAtomStart : UINT6;

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
		uint molType : UINT1;
		uint molState : UINT2;	
				
		int atomType : INT0;		
		float3 pos : FLOAT30;			
		
		LodInfo lodInfo;	
	};
				
	struct gs2fs
	{
		uint id : UINT0;		
		uint molType : UINT1;
		uint molState : UINT2;	
		uint atomType : UINT3;	
				
		float2 uv: TEXCOORD0;	
		centroid float4 pos : SV_Position;			
		
		nointerpolation float radius : FLOAT0;
		nointerpolation float lambertFalloff : FLOAT1;	
		nointerpolation float eyeDistance : FLOAT2;	
		nointerpolation float3 color : FLOAT30;

		LodInfo lodInfo;	
	};

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
												
	void VS(uint id : SV_VertexID, out vs2hs output)
	{
		float4 subInstanceInfo = _SubInstancesInfo[id];

		uint instanceId = subInstanceInfo.y;
		uint instanceType = molTypes[instanceId];
		uint instanceAtomCount = molAtomCountBuffer[instanceType];				
		uint instanceAtomStart = molAtomStartBuffer[instanceType];

		uint subInstanceId = subInstanceInfo.x;
		uint subInstanceEffectiveAtomCount = subInstanceInfo.z;
				
		output.id = instanceId;
		output.molType = instanceType;		
		output.molState = molStates[instanceId];
		output.rot = molRotations[instanceId];
		output.pos = (molPositions[instanceId].xyz + _SubInstancesPositions[id].xyz) * _Scale;
									
		output.atomCount = instanceAtomCount;
		output.atomStart = instanceAtomStart;

		// Set default lod values
		output.lodInfo.level = 0;
		output.lodInfo.atomRadiusMin = 0;
		output.lodInfo.atomRadiusMax = 0; 			
		output.lodInfo.atomRadiusLerp = 0; 					
		output.lodInfo.atomDecimationFactor = 1;

		// Do LOD...		
		if(_EnableLod == 1)
		{
			float d = distance(_WorldSpaceCameraPos, output.pos);

			if(d < _DistanceLod1) 
			{
				output.lodInfo.level = 0;
				output.lodInfo.atomRadiusLerp = max((min(d,_DistanceLod1) - _DistanceLod0) / (_DistanceLod1 - _DistanceLod0), 0); 
				output.lodInfo.atomRadiusMin = 0;
				output.lodInfo.atomRadiusMax = _MaxAtomRadiusLod0; 						
				output.lodInfo.atomDecimationFactor = max(_DecimationFactorLod0, 1);				
			}				
			else
			{
				output.lodInfo.level = 1;
				output.lodInfo.atomRadiusLerp = 0;
				output.lodInfo.atomRadiusMin = _MinAtomRadiusLod1;
				output.lodInfo.atomRadiusMax = 0; 							
				output.lodInfo.atomDecimationFactor = max(_DecimationFactorLod1, 1);
			}
		}					

		output.subIntanceAtomStart = subInstanceInfo.w;
		output.subIntanceAtomCount = ceil((float)subInstanceEffectiveAtomCount / (float)output.lodInfo.atomDecimationFactor);		
					
		// Toggle ingredients visibily 
		output.subIntanceAtomCount = (_ToggleIngredientsBuffer[instanceType] == 0) ? 0 : output.subIntanceAtomCount;			
										
		// Do cross section
		//output.subIntanceAtomCount = (_EnableCrossSection == 1 && output.pos.x >= 0) ? 0 : output.subIntanceAtomCount;
		if(_EnableCrossSection && PlaneTest(_CrossSectionPlane, output.pos, 0)) output.subIntanceAtomCount = 0;	
			
		// Do Early frustrum culling 
		if(!SphereInFrustum(output.pos, _IngredientsBoundingSphereRadius[instanceType] * _Scale)) output.subIntanceAtomCount = 0;	

		// Debug subinstancing
		//if(subInstanceInfo.x == 1)	output.subIntanceAtomCount = 0;	
							
		return;
	}			

	void HSConst(InputPatch<vs2hs, 1> input, uint patchID : SV_PrimitiveID, out hsConst output)
	{
		// Discard unwanted instances			
		if( input[0].molType < 0 || input[0].subIntanceAtomCount == 0 ) 
		{
			output.tessFactor[0] = output.tessFactor[1] = 0;
			return;
		}
				
		output.tessFactor[0] = output.tessFactor[1] = ceil(sqrt(input[0].subIntanceAtomCount));			
								
		return;
	}	

	[domain("isoline")]
	[partitioning("integer")]
	[outputtopology("point")]
	[outputcontrolpoints(1)]				
	[patchconstantfunc("HSConst")]
	hs2ds HS (InputPatch<vs2hs, 1> input, uint ID : SV_OutputControlPointID)
	{
		hs2ds output;
		output.id = input[0].id;			
		output.molType = input[0].molType;
		output.molState = input[0].molState;
		output.atomCount = input[0].atomCount;
		output.atomStart = input[0].atomStart;
		output.subIntanceAtomCount = input[0].subIntanceAtomCount;
		output.subIntanceAtomStart = input[0].subIntanceAtomStart;
		output.lodInfo  = input[0].lodInfo;

		// Simulate BM
		if(_EnableBrownianMotion == 1)
		{
			float speedFactor = 1.5;
			float translationScaleFactor = 1.0;
			float rotationScaleFactor = 0.25;

			float randx = frac(sin(dot(float2(output.id, input[0].pos.x) ,float2(12.9898,78.233))) * 43758.5453);
			float randy = frac(sin(dot(float2(output.id, input[0].pos.y) ,float2(12.9898,78.233))) * 43758.5453);
			float randz = frac(sin(dot(float2(output.id, input[0].pos.z) ,float2(12.9898,78.233))) * 43758.5453);

			float3 pn;

			pn.x = noise_3D( randx + 100 + input[0].pos.xyz + _Time.xyz * speedFactor);
			pn.y = noise_3D( randy + 200 + input[0].pos.yzx + _Time.yzx * speedFactor);
			pn.z = noise_3D( randz + 300 + input[0].pos.zxy + _Time.zxy * speedFactor);				
			pn -= 0.5;

			output.pos = input[0].pos + pn * translationScaleFactor;		
			
			float4 rn;

			rn.x = noise_3D( randx + 400 + input[0].pos.xzy + _Time.xyz * speedFactor);
			rn.y = noise_3D( randy + 500 + input[0].pos.yxz + _Time.yzx * speedFactor);
			rn.z = noise_3D( randz + 600 + input[0].pos.zyx + _Time.zxy * speedFactor);			
			rn.w = noise_3D( randz + 700 + input[0].pos.xyx + _Time.xyz * speedFactor);				
			rn -= 0.5;
			
			output.rot = normalize(input[0].rot + rn * rotationScaleFactor);	
		}
		else
		{
			output.pos = input[0].pos;
			output.rot = input[0].rot;	
		}

		return output;
	} 
			
	[domain("isoline")]
	void DS(hsConst input, const OutputPatch<hs2ds, 1> op, float2 uv : SV_DomainLocation, out ds2gs output)
	{
		output.id = op[0].id;
		output.molType = op[0].molType;
		output.molState = op[0].molState;
		output.lodInfo  = op[0].lodInfo;

		int x = round(uv.y * input.tessFactor[0]);
		int y = round(uv.x * input.tessFactor[0]);		
		int pointId = x + y * input.tessFactor[0];						
				
		int atomId = op[0].subIntanceAtomStart + pointId * op[0].lodInfo.atomDecimationFactor;
		float4 atomDataPDB = atomDataPDBBuffer[op[0].atomStart + atomId];			

		// Debug tesselation
		//output.atomType = (y >= input.tessFactor[0] || pointId >= op[0].atomCount) ? -1 : 0; 				
		//output.pos = float3(x * _Scale * 2, 0, y * _Scale * 2);					
				
		// Discard additional atoms
		output.atomType = (y >= input.tessFactor[0] || pointId >= op[0].subIntanceAtomCount || atomId >= op[0].atomCount) ? -1 : atomDataPDB.w;
		output.pos = op[0].pos + qtransform(op[0].rot, atomDataPDB.xyz) * _Scale;		
		//output.pos = op[0].pos + atomDataPDB.xyz;
																										
		return;			
	}
							
	[maxvertexcount(4)]
	void GS(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		// Discard unwanted atoms
		if( input[0].atomType < 0 ) return;
		//if( _EnableCrossSection == 1 && input[0].pos.x <= 0 ) return;

		gs2fs output;	
		output.id = input[0].id;	
		output.molType = input[0].molType;
		output.molState = input[0].molState; 	
		output.atomType = input[0].atomType; 		
		output.lodInfo  = input[0].lodInfo;				
		output.color = SetHSL(molColors[output.molType].rgb, float3(-1, (output.molState == 0) ? 0.35 : 0.5 + (sin(_Time.z * 3) + 1) / 4 , 0.5));	

		output.eyeDistance = 0;

		float minl = 15;
		float maxl = 50;
		float d = min(distance(_WorldSpaceCameraPos, input[0].pos), maxl);
		output.lambertFalloff = 1-( max(d - minl, 0) / (maxl -minl));
				
		input[0].lodInfo.atomRadiusMin = max(atomRadii[input[0].atomType], input[0].lodInfo.atomRadiusMin);
		input[0].lodInfo.atomRadiusMax = max(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax);

		output.radius = lerp(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax, input[0].lodInfo.atomRadiusLerp) * _Scale;	
				
		float4 pos = mul(UNITY_MATRIX_MVP, float4(input[0].pos, 1));
		float4 offset = mul(UNITY_MATRIX_P, float4(output.radius, output.radius, 0, 0));

		//*****//

		output.uv = float2(1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
		triangleStream.Append(output);

		output.uv = float2(1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
		triangleStream.Append(output);	
								
		output.uv = float2(-1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
		triangleStream.Append(output);

		output.uv = float2(-1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
		triangleStream.Append(output);	
	}

	//[maxvertexcount(4)]
	//void GS_SHADOW(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	//{
	//	// Discard unwanted atoms
	//	if( input[0].atomType < 0 ) return;
	//	//if( _EnableCrossSection == 1 && input[0].pos.x <= 0 ) return;

	//	gs2fs output;	
	//	output.id = input[0].id;	
	//	output.molType = input[0].molType;
	//	output.molState = input[0].molState; 	
	//	output.atomType = input[0].atomType; 		
	//	output.lodInfo  = input[0].lodInfo;		
		
	//	output.eyeDistance = 0;
		
	//	float shadowCameraEyePos = distance(_ShadowCameraWorldPos, input[0].pos) - 5;

	//	float4 shadowProj = mul(SHADOW_CAMERA_MATRIX_MVP, float4(input[0].pos, 1));
	//	shadowProj /= shadowProj.w;
	//	shadowProj.xy = shadowProj.xy * 0.5 + 0.5;
		
	//	float shadowMapCameraEyePos = tex2Dlod(_ShadowMap, float4(shadowProj.xy, 0, 0));
				

	//	float shadow = 1;
	//	if(shadowMapCameraEyePos < shadowCameraEyePos) 
	//	{
	//		shadow = 0.65;
	//	}

	//	output.color = SetHSL(molColors[output.molType].rgb, float3(-1, (output.molState == 0) ? 0.35 : 0.5 + (sin(_Time.z * 3) + 1) / 4 , 0.5)) * shadow;	

	//	float minl = 15;
	//	float maxl = 50;
	//	float d = min(distance(_WorldSpaceCameraPos, input[0].pos), maxl);
	//	output.lambertFalloff = 1-( max(d - minl, 0) / (maxl -minl));
				
	//	input[0].lodInfo.atomRadiusMin = max(atomRadii[input[0].atomType], input[0].lodInfo.atomRadiusMin);
	//	input[0].lodInfo.atomRadiusMax = max(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax);

	//	output.radius = lerp(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax, input[0].lodInfo.atomRadiusLerp) * _Scale;	
				
	//	float4 pos = mul(CAMERA_MATRIX_MVP, float4(input[0].pos, 1));
	//	float4 offset = mul(CAMERA_MATRIX_P, float4(output.radius, output.radius, 0, 0));
						
		

	//	//*****//

	//	output.uv = float2(1.0f, 1.0f);
	//	output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
	//	triangleStream.Append(output);

	//	output.uv = float2(1.0f, -1.0f);
	//	output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
	//	triangleStream.Append(output);	
								
	//	output.uv = float2(-1.0f, 1.0f);
	//	output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
	//	triangleStream.Append(output);

	//	output.uv = float2(-1.0f, -1.0f);
	//	output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
	//	triangleStream.Append(output);	
	//}
	
	void FS (gs2fs input, out float4 color : COLOR0, out float4 depthNormal : COLOR1, out float4 id : COLOR2, out float depth : sv_depthgreaterequal) 
	{					
		float lensqr = dot(input.uv, input.uv);   
		if(lensqr > 1) discard;

		// Find normal
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));		
				
		// Find depth
		//float eyeDepth =  input.eyeDepth + input.radius * (1-normal.z);
		float eyeDepth = LinearEyeDepth(input.pos.z) + input.radius * (1-normal.z);
		depth = 1 / (eyeDepth * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;		
		
		//depthNormal = EncodeDepthNormal(depth, normal);
		depthNormal = EncodeDepthNormal(depth, float3(0,0,1));
				
		// Find id color
		uint t1 = input.id / 256;
		uint t2 = t1 / 256;
	    float3 colorId = float3(t2 % 256, t1 % 256, input.id % 256) * 1/255;	
		id = float4(colorId, 1);		

		float ndotl = pow(max( 0.0, dot(float3(0,0,1), normal)), 0.15 * input.lambertFalloff);

		// Find color				
		color = float4(input.color * ndotl, 1);		
	}

	/****/

	void VS_SHADOW_MAP(uint id : SV_VertexID, out vs2hs output)
	{
		float4 subInstanceInfo = _SubInstancesInfo[id];

		uint instanceId = subInstanceInfo.y;
		uint instanceType = molTypes[instanceId];
		uint instanceAtomCount = molAtomCountBuffer[instanceType];				
		uint instanceAtomStart = molAtomStartBuffer[instanceType];

		uint subInstanceId = subInstanceInfo.x;
		uint subInstanceEffectiveAtomCount = subInstanceInfo.z;
				
		output.id = instanceId;
		output.molType = instanceType;		
		output.molState = molStates[instanceId];
		output.rot = molRotations[instanceId];
		output.pos = (molPositions[instanceId].xyz + _SubInstancesPositions[id].xyz) * _Scale;
									
		output.atomCount = instanceAtomCount;
		output.atomStart = instanceAtomStart;

		// Set default lod values
		output.lodInfo.level = 0;
		output.lodInfo.atomRadiusMin = 0;
		output.lodInfo.atomRadiusMax = 0; 			
		output.lodInfo.atomRadiusLerp = 0; 					
		output.lodInfo.atomDecimationFactor = 1;
		
		output.subIntanceAtomStart = subInstanceInfo.w;
		output.subIntanceAtomCount = ceil((float)subInstanceEffectiveAtomCount / (float)output.lodInfo.atomDecimationFactor);		
					
		// Toggle ingredients visibily 
		output.subIntanceAtomCount = (_ToggleIngredientsBuffer[instanceType] == 0) ? 0 : output.subIntanceAtomCount;			
										
		// Do cross section
		if(_EnableCrossSection && PlaneTest(_CrossSectionPlane, output.pos, 0)) output.subIntanceAtomCount = 0;	
			
		// Do Early frustrum culling 
		//if(!SphereInFrustum(output.pos, _IngredientsBoundingSphereRadius[instanceType] * _Scale)) output.subIntanceAtomCount = 0;	
							
		return;
	}			

	[maxvertexcount(4)]
	void GS_SHADOW_MAP(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		// Discard unwanted atoms
		if( input[0].atomType < 0 ) return;			

		input[0].lodInfo.atomRadiusMin = max(atomRadii[input[0].atomType], input[0].lodInfo.atomRadiusMin);
		input[0].lodInfo.atomRadiusMax = max(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax);

		gs2fs output;	
		output.id = input[0].id;	
		output.molType = input[0].molType;
		output.molState = input[0].molState; 	
		output.atomType = input[0].atomType; 		
		output.lodInfo  = input[0].lodInfo;				
		output.radius = lerp(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax, input[0].lodInfo.atomRadiusLerp) * _Scale;	
		output.color = SetHSL(molColors[output.molType].rgb, float3(-1, (output.molState == 0) ? 0.35 : 0.5 + (sin(_Time.z * 3) + 1) / 4 , 0.5));	
		output.lambertFalloff = 0;		

		// Eye distance to write in shadow map
		output.eyeDistance = distance(_ShadowCameraWorldPos, input[0].pos);

		float4 pos = mul(UNITY_MATRIX_MVP, float4(input[0].pos, 1));
		float4 offset = mul(UNITY_MATRIX_MVP, float4(output.radius, output.radius, 0, 0));

		//*****//

		output.uv = float2(1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
		triangleStream.Append(output);

		output.uv = float2(1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
		triangleStream.Append(output);	
								
		output.uv = float2(-1.0f, 1.0f);
		output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
		triangleStream.Append(output);

		output.uv = float2(-1.0f, -1.0f);
		output.pos = pos + float4(output.uv * offset.xy, offset.z, 0);
		triangleStream.Append(output);	
	}

	void FS_SHADOW_MAP (gs2fs input, out float4 eyeDepth : COLOR0, out float4 colorDebug : COLOR1) 
	{					
		float lensqr = dot(input.uv, input.uv);   
		if(lensqr > 1) discard;

		// Find color		
		eyeDepth = float4(input.eyeDistance, 0,0,0);
		
		// Find color		
		colorDebug = float4(input.color, 1);			
	}

	/*****/

	//void FS (gs2fs input, out float4 color : COLOR0) 
	//{					
	//	float lensqr = dot(input.uv, input.uv);   
	//	if(lensqr > 1) discard;

	//	// Find color		
	//	color = float4(input.color,1);			
	//}

	//void FS_MSAA (gs2fs input, out uint coverage_mask : SV_Coverage, out float4 color : COLOR0, out float depth : sv_depthgreaterequal) //, out float4 id : COLOR1) //, out float4 normal_depth : COLOR1, out float depth : DEPTH) 
	//{	
	//	// Find coverage mask
	//	coverage_mask = 0;

	//	float2 sample_uv;
	//	for(int i = 0; i < 8; i++)
	//	{
	//		sample_uv = EvaluateAttributeAtSample(input.uv, i);
	//		if(dot(sample_uv, sample_uv) <= 1) coverage_mask += 1 << (i);
	//	}

	//	if(coverage_mask == 0) discard;

	//	// Find normal
	//	float lensqr = dot(input.uv, input.uv);   
				
	//	//if(lensqr > 1) discard;

	//	float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));		
				
	//	// Find depth
	//	float eyeDepth = LinearEyeDepth(input.pos.z);
	//	eyeDepth += (coverage_mask == 255) ?  input.radius * (1-normal.z) : input.radius;
	//	//depth = (input.lodInfo.level == 0) ? input.pos.z : 1 / (eyeDepth * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;

	//	// Find color		
	//	color = float4(input.color, 1);	
											
	//	//float ndotl = max( 0.0, dot(float3(0,0,1), normal));										
	//	//
	//	//float3 finalColor = atomColor * pow(ndotl, 0.00);				
	//	//color = float4(finalColor, 1);
		
	//	//// Set id
	//	//uint t1 = input.id / 256;
	//	//uint t2 = t1 / 256;
	//	//float3 colorId = float3(t2 % 256, t1 % 256, input.info.z % 256) * 1/255;	
	//	//id = float4(colorId, 1);					
	//}

	ENDCG

	SubShader 
	{	
		Pass 
	    {
			ZWrite On

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex VS	
			#pragma hull HS
			#pragma domain DS				
			#pragma geometry GS			
			#pragma fragment FS
						
			ENDCG
		}		
		
		Pass 
	    {
			ZWrite On

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex VS_SHADOW_MAP	
			#pragma hull HS
			#pragma domain DS				
			#pragma geometry GS_SHADOW_MAP				
			#pragma fragment FS_SHADOW_MAP
						
			ENDCG
		}			
	}
	Fallback Off
}	