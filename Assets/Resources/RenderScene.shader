Shader "Custom/RenderScene" 
{	
	CGINCLUDE
		
	#include "UnityCG.cginc"	
	#include "Common.cginc"	
	#include "ShaderHelper.cginc"
	#include "ObjectCulling.cginc"
			
	//--------------------------------------------------------------------------------------		
														
	void VS(uint id : SV_VertexID, out vs2hs output)
	{
		int subInstanceCullFlag = _SubInstanceCullFlags[id + _SubInstanceStart];
		float4 subInstanceInfo = _SubInstanceInformations[id + _SubInstanceStart];
		
		uint instanceId = subInstanceInfo.x;
		uint subInstanceAtomCount = subInstanceInfo.y;		
		uint subInstanceAtomStart = subInstanceInfo.z;
		uint subInstanceBoundingSphere = subInstanceInfo.w;

		uint instanceType = _InstanceTypes[instanceId];
		uint instanceAtomCount = _IngredientAtomCount[instanceType];				
		uint instanceAtomStart = _IngredientAtomStart[instanceType];		
				
		output.id = instanceId;
		output.type = instanceType;		
		output.state = _InstanceStates[instanceId];
		output.rot = _InstanceRotations[instanceId];
		output.pos = _InstancePositions[instanceId].xyz * _Scale;
									
		output.atomCount = instanceAtomCount;
		output.atomStart = instanceAtomStart;

		// Set default lod values
		output.lodInfo.level = 0;
		output.lodInfo.atomRadiusMin = 0;
		output.lodInfo.atomRadiusMax = 0; 			
		output.lodInfo.atomRadiusLerp = 0; 					
		output.lodInfo.decimationFactor = 1;

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
				output.lodInfo.decimationFactor = max(_DecimationFactorLod0, 1);				
			}				
			else
			{
				output.lodInfo.level = 1;
				output.lodInfo.atomRadiusLerp = 0;
				output.lodInfo.atomRadiusMin = _MinAtomRadiusLod1;
				output.lodInfo.atomRadiusMax = 0; 							
				output.lodInfo.decimationFactor = max(_DecimationFactorLod1, 1);
			}
		}					

		output.subIntanceAtomStart = subInstanceAtomStart;
		output.subIntanceAtomCount = ceil((float)subInstanceAtomCount / (float)output.lodInfo.decimationFactor);		
					
		// Toggle ingredients visibily 
		output.subIntanceAtomCount = (_IngredientToggle[instanceType] == 0) ? 0 : output.subIntanceAtomCount;			
										
		// Do cross section
		if(_EnableCrossSection && PlaneTest(_CrossSectionPlane, output.pos, 0)) output.subIntanceAtomCount = 0;	
			
		// Do Early frustrum culling 
		if(!SphereInFrustum(output.pos, subInstanceBoundingSphere * _Scale)) output.subIntanceAtomCount = 0;
							
		// Check if instance has been previously culled
		if(_EnableObjectCulling == 1 && subInstanceCullFlag == 0) output.subIntanceAtomCount = 0;

		return;
	}			
	
	//--------------------------------------------------------------------------------------

	[domain("isoline")]
	[partitioning("integer")]
	[outputtopology("point")]
	[outputcontrolpoints(1)]				
	[patchconstantfunc("HSConst")]
	hs2ds HS (InputPatch<vs2hs, 1> input, uint ID : SV_OutputControlPointID)
	{
		return input[0];
	} 

	//--------------------------------------------------------------------------------------
			
	[domain("isoline")]
	void DS(hsConst input, const OutputPatch<hs2ds, 1> op, float2 uv : SV_DomainLocation, out ds2gs output)
	{
		output.id = op[0].id;
		output.type = op[0].type;
		output.state = op[0].state;
		output.lodInfo  = op[0].lodInfo;

		int x = round(uv.y * input.tessFactor[0]);
		int y = round(uv.x * input.tessFactor[0]);		
		int pointId = x + y * input.tessFactor[0];						
				
		int atomId = op[0].subIntanceAtomStart + pointId * op[0].lodInfo.decimationFactor;
		float4 atomDataPDB = _AtomPositions[op[0].atomStart + atomId];				
				
		// Discard additional atoms
		output.atomType = (y >= input.tessFactor[0] || pointId >= op[0].subIntanceAtomCount || atomId >= op[0].atomCount) ? -1 : atomDataPDB.w;
		output.pos = op[0].pos + qtransform(op[0].rot, atomDataPDB.xyz) * _Scale;
																										
		return;			
	}

	//--------------------------------------------------------------------------------------

	struct gs2fs
	{
		uint id : UINT0;		
		uint type : UINT1;
		uint state : UINT2;	
				
		float2 uv: TEXCOORD0;	
		centroid float4 pos : SV_Position;			
		
		nointerpolation float radius : FLOAT0;	
		nointerpolation float lambertFalloff : FLOAT1;
		nointerpolation float3 color : FLOAT30;

		LodInfo lodInfo;	
	};
							
	[maxvertexcount(4)]
	void GS(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		// Discard unwanted atoms
		if( input[0].atomType < 0 ) return;

		input[0].lodInfo.atomRadiusMin = max(_AtomRadii[input[0].atomType], input[0].lodInfo.atomRadiusMin);
		input[0].lodInfo.atomRadiusMax = max(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax);

		float minl = 15;
		float maxl = 50;
		float d = min(distance(_WorldSpaceCameraPos, input[0].pos), maxl);
			
		float shadowFactor = 1;

		//if(_EnableShadows)
		//{
		//	float4 shadowProj = mul(_ShadowCameraViewProjMatrix, float4(input[0].pos,1));

		//	shadowProj.xyz /= shadowProj.w;
		//	shadowProj.xy = shadowProj.xy * 0.5 + 0.5;
		//	shadowProj.y = 1 - shadowProj.y;

		//	float shadowCameraEyePos = mul(_ShadowCameraViewMatrix, float4(input[0].pos,1)).z + 1.5;
		//	float shadowMapCameraEyePos = tex2Dlod(_ShadowMap, float4(shadowProj.xy, 0, 0));				
		//	shadowFactor = (shadowMapCameraEyePos > shadowCameraEyePos ) ? 0.75 : 1.1;
		//}		

		gs2fs output;	
		output.id = input[0].id;	
		output.type = input[0].type;
		output.state = input[0].state; 			
		output.lodInfo  = input[0].lodInfo;				
		output.lambertFalloff = 1-( max(d - minl, 0) / (maxl -minl));
				
		output.radius = lerp(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax, input[0].lodInfo.atomRadiusLerp) * _Scale;				
		output.color = SetHSL(_IngredientColors[output.type].rgb, float3(-1, (output.state == 0) ? 0.35 : 0.5 + (sin(_Time.z * 3) + 1) / 4 , -1)) * shadowFactor;	
						
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

	//--------------------------------------------------------------------------------------
	
	void FS (gs2fs input, out float4 color : COLOR0, out float4 id : COLOR1, out float depth : sv_depthgreaterequal) 
	{					
		float lensqr = dot(input.uv, input.uv);   
		if(lensqr > 1) discard;

		// Find normal
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));		
				
		// Find depth
		float eyeDepth = LinearEyeDepth(input.pos.z) + input.radius * (1-normal.z);
		depth = 1 / (eyeDepth * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;	
				
		// Find id color
		uint t1 = input.id / 256;
		uint t2 = t1 / 256;
	    float3 colorId = float3(t2 % 256, t1 % 256, input.id % 256) * 1/255;	
		id = float4(colorId, 1);		

		// Find color
		float ndotl = pow(max( 0.0, dot(float3(0,0,1), normal)), 0.15 * input.lambertFalloff);						
		color = float4(input.color * ndotl, 1);		
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
			ZWrite Off

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex VS_CULLING	
			#pragma hull HS_CULLING
			#pragma domain DS_CULLING				
			#pragma geometry GS_CULLING		
			#pragma fragment FS_CULLING
						
			ENDCG
		}			
	}
	Fallback Off
}	