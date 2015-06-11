Shader "Custom/RenderProteins" 
{	
	CGINCLUDE
		
	#include "UnityCG.cginc"
	#include "Helper.cginc"		
	#include "Common.cginc"			
	
	uniform int _EnableLod;
	uniform	StructuredBuffer<float4> _ProteinInstanceInfo;
	uniform	StructuredBuffer<float4> _ProteinInstancePositions;
	uniform	StructuredBuffer<float4> _ProteinInstanceRotations;
						
	uniform StructuredBuffer<float4> _IngredientColors;	
	uniform StructuredBuffer<float4> _ProteinAtomPositions;	
	uniform StructuredBuffer<float4> _ProteinClusterPositions;	
	uniform StructuredBuffer<int4> _ProteinSphereBatchInfos;	

	uniform StructuredBuffer<float4> _ProteinUnitInstancePosition;	
	
	void vs_protein(uint id : SV_VertexID, out vs2ds output)
	{		
		int4 sphereBatchInfo = _ProteinSphereBatchInfos[id];	
		
		float4 infos = _ProteinInstanceInfo[sphereBatchInfo.x];		
		float3 color = ColorCorrection(_IngredientColors[infos.x]);
		float3 highlight = HighlightColor(color);

		output.id = sphereBatchInfo.x;
		output.type = infos.x;		
		output.state = infos.y;
		output.color = (output.state == 0) ? color : highlight; // Read color here and pass it to the next levels to avoid unnecessary buffer reads
		output.rot = _ProteinInstanceRotations[output.id];	
		output.pos = _ProteinInstancePositions[output.id].xyz * _Scale;	
	
		// Set LOD values	
		float beginRange = (sphereBatchInfo.y == 0) ? _FirstLevelBeingRange : _LodLevelsInfos[sphereBatchInfo.y -1][0];
		float endRange = max(_LodLevelsInfos[sphereBatchInfo.y][0], beginRange);
		float cameraDistance = min(max(dot(output.pos - _WorldSpaceCameraPos, _CameraForward), beginRange), endRange);			
		float radiusLerp = saturate((cameraDistance - beginRange) / (endRange - beginRange)); 	
		float radiusMin =  max(_LodLevelsInfos[sphereBatchInfo.y][1], 1);
		float radiusMax = max(_LodLevelsInfos[sphereBatchInfo.y][2], radiusMin);
		
		output.lodLevel = (_EnableLod) ? sphereBatchInfo.y : 0;		
		output.radiusScale = ((_EnableLod) ? lerp(radiusMin, radiusMax, radiusLerp) : 1) * _Scale;	
		output.sphereCount = sphereBatchInfo.z;	
		output.sphereStart = sphereBatchInfo.w;

		//output.sphereCount = 0;	
	}	

	//--------------------------------------------------------------------------------------
			
	[domain("isoline")]
	void ds_protein(hsConst input, const OutputPatch<vs2ds, 1> op, float2 uv : SV_DomainLocation, out ds2gs output)
	{
		int x = round(uv.y * input.tessFactor[0]);
		int y = round(uv.x * input.tessFactor[0]);		
		int sphereIndex = x + y * input.tessFactor[0];							
		float4 spherePosition = (op[0].lodLevel == 0) ? _ProteinAtomPositions[op[0].sphereStart + sphereIndex] : _ProteinClusterPositions[op[0].sphereStart + sphereIndex];				
	
		output.id = op[0].id;
		output.type = op[0].type;
		output.state = op[0].state;
		output.color = op[0].color;
		output.pos = op[0].pos + QuaternionTransform(op[0].rot, spherePosition.xyz) * _Scale;	
		output.radius = (y >= input.tessFactor[0] || sphereIndex >= op[0].sphereCount) ? 0 : max(spherePosition.w * _Scale, 1 * op[0].radiusScale); // Discard unwanted spheres	
	}

	//--------------------------------------------------------------------------------------
							
	[maxvertexcount(3)]
	void gs_protein(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		// Discard unwanted atoms
		if( input[0].radius <= 0 ) return;

		float minl = 15;
		float maxl = 30;
		float d = min(distance(_WorldSpaceCameraPos, input[0].pos), maxl);
		//output.lambertFalloff = 1-( max(d - minl, 0) / (maxl -minl));
	
		//float shadowFactor = 1;
		//if(_EnableShadows)
		//{
		//	float4 shadowProj = mul(_ShadowCameraViewProjMatrix, float4(input[0].pos,1));

		//	shadowProj.xyz /= shadowProj.w;
		//	shadowProj.xy = shadowProj.xy * 0.5 + 0.5;
		//	shadowProj.y = 1 - shadowProj.y;

		//	float shadowCameraEyePos = mul(_ShadowCameraViewMatrix, float4(input[0].pos,1)).z + 0.75;
		//	float shadowMapCameraEyePos = tex2Dlod(_ShadowMap, float4(shadowProj.xy, 0, 0));				
		//	shadowFactor = (shadowMapCameraEyePos > shadowCameraEyePos ) ? 0.75 : 1.0;
		//}			
		//output.color = SetHSL(_IngredientColors[output.type].rgb, float3(-1, (output.state == 0) ? 0.35 : 0.5 + (sin(_Time.z * 3) + 1) / 4 , -1)) * shadowFactor;		
	
		float4 viewPos = mul(UNITY_MATRIX_MV, float4(input[0].pos, 1));
		viewPos -= normalize( viewPos ) * input[0].radius;
		float4 projPos = mul(UNITY_MATRIX_P, float4(viewPos.xyz, 1));
		float4 offset = mul(UNITY_MATRIX_P, float4(input[0].radius, input[0].radius, 0, 0));

		gs2fs output;	
		output.id = input[0].id;		
		output.color = input[0].color;			
		output.radius = input[0].radius;
		output.lambertFalloff = 1-( max(d - minl, 0) / (maxl -minl));

		//*****//
		
		float triBase = 3.464;
		float triHeigth = 3;
		float triBaseHalf = triBase * 0.5;
		float2 triOffset = float2(triBaseHalf, 1.0);

		output.uv = float2(0, 0) - triOffset;
		output.pos = projPos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);

		output.uv = float2(triBaseHalf, triHeigth) - triOffset;
		output.pos = projPos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);	
								
		output.uv = float2(triBase,0) - triOffset;
		output.pos = projPos + float4(output.uv * offset.xy, 0, 0);
		triangleStream.Append(output);
	}

	//--------------------------------------------------------------------------------------
	
	void fs_protein(gs2fs input, out float4 color : COLOR0, out int id : COLOR1, out float depth : sv_depthgreaterequal) 
	{					
		float lensqr = dot(input.uv, input.uv);   
		if(lensqr > 1) discard;

		// Find normal
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));		
		
		// Find color
		float ndotl = pow(max( 0.0, dot(float3(0,0,1), normal)), 0.15 * input.lambertFalloff); //, );						
		//float edgeStart = 0.6;
		//float edgeFactor = (1 - (max((lensqr - edgeStart) * (1 / (1 - edgeStart)), 0) * 5) * (input.lambertFalloff));
	
		//max(((lensqr > 0.8) ? lensqr * 1-input.lambertFalloff : 1), 0.5);
		color = float4(input.color, 1);		

		// Set id to idbuffer
		id = input.id;

		// Find depth
		float eyeDepth = LinearEyeDepth(input.pos.z) + input.radius * (1-normal.z);
		depth = 1 / (eyeDepth * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;			
	}
	
	//--------------------------------------------------------------------------------------
	
	uniform	StructuredBuffer<int> _ProteinVisibilityFlag;

	struct gs_input
	{	
		float radius : FLOAT0;			
		float3 color : FLOAT30;	
		float3 position : FLOAT31;		
	};

	struct fs_input
	{	
		float2 uv: TEXCOORD0;	
		float3 color : FLOAT30;			
		float4 position : SV_Position;	
	};

	void vs_fluo(uint id : SV_VertexID, out gs_input output)
	{		
		float4 infos = _ProteinInstanceInfo[id];		
		float4 position = _ProteinInstancePositions[id];
		
		//output.id = id;
		//output.type = infos.x;	
		//output.state = infos.y;	
		//output.color = _IngredientColors[infos.x]; 
		//output.pos = position.xyz * _Scale;
		//output.radius = 25 * _Scale;//position.w * _Scale;	
		
		bool visible = _ProteinVisibilityFlag[infos.x] == 0;

		output.color = _IngredientColors[infos.x]; 
		output.radius = (visible) ? 0 : position.w * _Scale;
		output.position = position.xyz * _Scale;
	}	
		
	[maxvertexcount(3)]
	void gs_fluo(point gs_input input[1], inout TriangleStream<fs_input> triangleStream)
	{	
		if( input[0].radius <= 0 ) return;
		
		float4 viewPos = mul(UNITY_MATRIX_MV, float4(input[0].position, 1));
		viewPos -= normalize( viewPos ) * input[0].radius;
		float4 projPos = mul(UNITY_MATRIX_P, float4(viewPos.xyz, 1));
		float4 cornerPos = mul(UNITY_MATRIX_P, float4(input[0].radius, input[0].radius, 0, 0));

		fs_input output;		
		output.color = input[0].color;	

		//*****//
		
		float triBase = 3.464;
		float triHeigth = 3;
		float triBaseHalf = triBase * 0.5;
		float2 triOffset = float2(triBaseHalf, 1.0);

		output.uv = float2(0, 0) - triOffset;
		output.position = projPos + float4(output.uv * cornerPos.xy, 0, 0);
		triangleStream.Append(output);

		output.uv = float2(triBaseHalf, triHeigth) - triOffset;
		output.position = projPos + float4(output.uv * cornerPos.xy, 0, 0);
		triangleStream.Append(output);	
								
		output.uv = float2(triBase,0) - triOffset;
		output.position = projPos + float4(output.uv * cornerPos.xy, 0, 0);
		triangleStream.Append(output);
	}

	void fs_fluo(fs_input input, out float4 color : COLOR0) 
	{					
		float lensqr = dot(input.uv, input.uv);   
		if(lensqr > 1) discard;

		color = float4(input.color,  pow(1-lensqr, 4));				
	}

	ENDCG

	SubShader 
	{	
		Pass 
	    {
			ZTest Lequal
			ZWrite On

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex vs_protein
			#pragma hull hs
			#pragma domain ds_protein				
			#pragma geometry gs_protein			
			#pragma fragment fs_protein
						
			ENDCG
		}	

		Pass 
	    {
			ZWrite Off // don't write to depth buffer 
			Blend SrcAlpha One // Soft Additive

	    	CGPROGRAM			
	    		
			#include "UnityCG.cginc"
			
			#pragma only_renderers d3d11
			#pragma target 5.0				
			
			#pragma vertex vs_fluo		
			#pragma geometry gs_fluo				
			#pragma fragment fs_fluo	
						
			ENDCG
		}	
	}
	Fallback Off
}	