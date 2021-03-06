Shader "Custom/RenderLipids" 
{	
	CGINCLUDE
		
	#include "UnityCG.cginc"
	#include "Helper.cginc"		
	#include "Common.cginc"	
	
	uniform StructuredBuffer<float4> _LipidAtomPositions;		
	uniform StructuredBuffer<float4> _LipidSphereBatchInfos;	
	uniform StructuredBuffer<float4> _LipidInstancePositions;
	uniform StructuredBuffer<int4> _LipidInstanceCullFlags;	
		
	uniform int _EnableCrossSection;
	uniform float4 _CrossSectionPlane;

	void vs_lipid(uint id : SV_VertexID, out vs2ds output)
	{		
		float4 sphereBatchInfo = _LipidSphereBatchInfos[id];	
		float4 lipidPosition = _LipidInstancePositions[id.x];
				
		output.id = id.x; // Offset id to get unique id
		output.type = 0;		
		output.state = 0;
		output.rot = float4(0,0,0,1);	
		output.color = float3(1,1,0); // Read color here and pass it to the next levels to avoid unnecessary buffer reads
		output.pos = lipidPosition.xyz * _Scale;
			
		int4 cullFlags = _LipidInstanceCullFlags[id.x];
		//bool cullBatch = cullFlags.x == 1;
		bool cullBatch = cullFlags.x == 1 || cullFlags.y == 1 || cullFlags.z == 1;
		
		float beginRange = _FirstLevelBeingRange;
		float endRange = 50;		
		float cameraDistance = dot(output.pos - _WorldSpaceCameraPos, _CameraForward);
		float dd = min(max(cameraDistance, beginRange), endRange);			
		float radiusLerp = saturate((cameraDistance - beginRange) / (endRange - beginRange)); 	
		
		output.lodLevel = (cameraDistance < endRange ? 0 : 1);		
		
		float radiusMin = output.lodLevel == 0 ? 1 : 2;
		float radiusMax = output.lodLevel == 0 ? 1.5 : 2;
		
		output.radiusScale = lerp(radiusMin, radiusMax, radiusLerp) * _Scale;
		output.decimationFactor = (output.lodLevel == 0 ? 1 : 3);
		output.sphereStart = sphereBatchInfo.y;
		output.sphereCount = (cullBatch) ? 0 : floor(sphereBatchInfo.x / output.decimationFactor);		

		//output.radiusScale = lipidPosition.w * _Scale;
		//output.sphereCount = (cullBatch) ? 0 : 1;		
		//output.sphereCount = 0;			
	}	

	//--------------------------------------------------------------------------------------
			
	bool SpherePlaneTest( float4 plane, float3 center, float offset)
	{
		return dot(plane.xyz, center - plane.xyz * -plane.w) + offset > 0;
	}

	[domain("isoline")]
	void ds_lipid(hsConst input, const OutputPatch<vs2ds, 1> op, float2 uv : SV_DomainLocation, out ds2gs output)
	{
		int x = round(uv.y * input.tessFactor[0]);
		int y = round(uv.x * input.tessFactor[0]);		
		int sphereId = x + y * input.tessFactor[0];	
		int sphereIndex = sphereId * op[0].decimationFactor;						
		float4 sphere = _LipidAtomPositions[op[0].sphereStart + sphereIndex];				
		float3 sphereCenter = op[0].pos + _LipidAtomPositions[op[0].sphereStart + sphereIndex].xyz * _Scale;

		bool cullAtom = !SpherePlaneTest(_CrossSectionPlane, sphereCenter, 0) && _EnableCrossSection == 1;	

		output.id = op[0].id;
		output.type = op[0].type;
		output.state = op[0].state;
		output.color = op[0].color;
		output.pos = sphereCenter;		
		output.radius = (y >= input.tessFactor[0] || sphereId >= op[0].sphereCount || cullAtom) ? 0 : sphere.w * op[0].radiusScale; // Discard unwanted spheres		

		//output.pos = op[0].pos;
		//output.radius = 1 * op[0].radiusScale;
	}

	//--------------------------------------------------------------------------------------
							
	[maxvertexcount(3)]
	void gs_lipid(point ds2gs input[1], inout TriangleStream<gs2fs> triangleStream)
	{
		// Discard unwanted atoms
		if( input[0].radius <= 0 ) return;

		//float minl = 15;
		//float maxl = 50;
		//float d = min(distance(_WorldSpaceCameraPos, input[0].pos), maxl);
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
		output.color = ColorCorrection(input[0].color);			
		output.radius = input[0].radius;
		output.lambertFalloff = 0;		

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
	
	void fs_lipid(gs2fs input, out float4 color : COLOR0, out int id : COLOR1, out float depth : sv_depthgreaterequal) 
	{					
		float lensqr = dot(input.uv, input.uv);   
		if(lensqr > 1) discard;

		// Find normal
		float3 normal = normalize(float3(input.uv, sqrt(1.0 - lensqr)));		
		
		// Find color
		//float ndotl = pow(max( 0.0, dot(float3(0,0,1), normal)),1); //, 0.15 * input.lambertFalloff);						
		color = float4(input.color, 1);		
								
		// Set id to idbuffer
		id = 666666666;		

		// Find depth
		float eyeDepth = LinearEyeDepth(input.pos.z) + input.radius * (1-normal.z);
		depth = 1 / (eyeDepth * _ZBufferParams.z) - _ZBufferParams.w / _ZBufferParams.z;			
	}
	
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
			
			#pragma vertex vs_lipid
			#pragma hull hs
			#pragma domain ds_lipid				
			#pragma geometry gs_lipid			
			#pragma fragment fs_lipid
						
			ENDCG
		}
	}
	Fallback Off
}	