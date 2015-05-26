//void VS_SHADOW_MAP(uint id : SV_VertexID, out vs2hs output)
//{
//	uint subInstanceId = id + _SubInstanceStart;

//	int subInstanceCullFlag = _SubInstanceCullFlags[subInstanceId];
//	float4 subInstanceInfo = _SubInstanceInformations[subInstanceId];
		
//	uint instanceId = subInstanceInfo.x;
//	uint instanceType = _InstanceTypes[instanceId];
//	uint instanceAtomCount = _IngredientAtomCount[instanceType];				
//	uint instanceAtomStart = _IngredientAtomStart[instanceType];		
	
//	uint subInstanceAtomCount = subInstanceInfo.y;		
//	uint subInstanceAtomStart = subInstanceInfo.z;
//	uint subInstanceBoundingSphere = subInstanceInfo.w;	
				
//	output.id = instanceId;
//	output.type = instanceType;		
//	output.state = _InstanceStates[instanceId];
//	output.rot = _InstanceRotations[instanceId];
//	output.pos = _InstancePositions[instanceId].xyz * _Scale;
									
//	output.atomCount = instanceAtomCount;
//	output.atomStart = instanceAtomStart;

//	// Set default lod values
//	output.lodInfo.level = 0;
//	output.lodInfo.atomRadiusMin = 8;
//	output.lodInfo.atomRadiusMax = 8; 			
//	output.lodInfo.atomRadiusLerp = 0; 					
//	output.lodInfo.decimationFactor = 25;
		
//	output.subIntanceAtomStart = subInstanceAtomStart;
//	output.subIntanceAtomCount = ceil((float)subInstanceAtomCount / (float)output.lodInfo.decimationFactor);	
					
//	// Do cross section
//	if(_EnableCrossSection && PlaneTest(_CrossSectionPlane, output.pos, 0)) output.subIntanceAtomCount = 0;	
			
//	// Do Early frustrum culling 
//	if(!SphereInFrustum(output.pos, subInstanceBoundingSphere * _Scale)) output.subIntanceAtomCount = 0;
							
//	return;
//}		

////--------------------------------------------------------------------------------------

//[domain("isoline")]
//[partitioning("integer")]
//[outputtopology("point")]
//[outputcontrolpoints(1)]				
//[patchconstantfunc("HSConst")]
//hs2ds HS_SHADOW_MAP (InputPatch<vs2hs, 1> input, uint ID : SV_OutputControlPointID)
//{
//	return input[0];
//} 
			
////--------------------------------------------------------------------------------------

//[domain("isoline")]
//void DS_SHADOW_MAP(hsConst input, const OutputPatch<hs2ds, 1> op, float2 uv : SV_DomainLocation, out ds2gs output)
//{
//	output.id = op[0].id;
//	output.type = op[0].type;
//	output.state = op[0].state;
//	output.lodInfo  = op[0].lodInfo;
//	output.subInstanceId = op[0].subInstanceId;

//	int x = round(uv.y * input.tessFactor[0]);
//	int y = round(uv.x * input.tessFactor[0]);		
//	int pointId = x + y * input.tessFactor[0];						
				
//	int atomId = op[0].subIntanceAtomStart + pointId * op[0].lodInfo.decimationFactor;
//	float4 atomDataPDB = _AtomPositions[op[0].atomStart + atomId];				
				
//	// Discard additional atoms
//	output.atomType = (y >= input.tessFactor[0] || pointId >= op[0].subIntanceAtomCount || atomId >= op[0].atomCount) ? -1 : atomDataPDB.w;
//	output.pos = op[0].pos + qtransform(op[0].rot, atomDataPDB.xyz) * _Scale;
																										
//	return;			
//}	
////--------------------------------------------------------------------------------------		
	
//struct GS2FS_SHADOW_MAP
//{				
//	float2 uv: TEXCOORD0;	
//	centroid float4 pos : SV_Position;			
		
//	nointerpolation float radius : FLOAT0;
//	nointerpolation float eyeDistance : FLOAT1;	
//	nointerpolation float3 color : FLOAT30;	
//};		

//[maxvertexcount(4)]
//void GS_SHADOW_MAP(point ds2gs input[1], inout TriangleStream<GS2FS_SHADOW_MAP> triangleStream)
//{
//	// Discard unwanted atoms
//	if( input[0].atomType < 0 ) return;			

//	input[0].lodInfo.atomRadiusMin = max(_AtomRadii[input[0].atomType], input[0].lodInfo.atomRadiusMin);
//	input[0].lodInfo.atomRadiusMax = max(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax);

//	GS2FS_SHADOW_MAP output;					
//	output.radius = lerp(input[0].lodInfo.atomRadiusMin, input[0].lodInfo.atomRadiusMax, input[0].lodInfo.atomRadiusLerp) * _Scale;	
//	output.color = SetHSL(_IngredientColors[input[0].type].rgb, float3(-1, 0.35 , 0.5));	

//	// Eye distance to write in shadow map
//	output.eyeDistance = mul(_ShadowCameraViewMatrix, float4(input[0].pos,1)).z;

//	float4 pos = mul(_ShadowCameraViewProjMatrix, float4(input[0].pos, 1));
//	float4 offset = mul(UNITY_MATRIX_P, float4(output.radius, output.radius, 0, 0));

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

////--------------------------------------------------------------------------------------

//void FS_SHADOW_MAP (GS2FS_SHADOW_MAP input, out float4 eyeDepth : COLOR0, out float4 colorDebug : COLOR1) 
//{					
//	float lensqr = dot(input.uv, input.uv);   
//	if(lensqr > 1) discard;

//	// Find color		
//	eyeDepth = float4(input.eyeDistance, 0,0,0);
		
//	// Find color		
//	colorDebug = float4(input.color, 1);			
//}