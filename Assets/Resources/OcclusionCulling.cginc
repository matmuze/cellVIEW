//void VS_CULLING(uint id : SV_VertexID, out vs2hs output)
//{
//	uint subInstanceId = id + _SubInstanceStart;
//	int subInstanceCullFlag = _SubInstanceCullFlags[subInstanceId];
//	float4 subInstanceInfo = _SubInstanceInformations[subInstanceId];
		
//	uint instanceId = subInstanceInfo.x;
//	uint subInstanceAtomCount = subInstanceInfo.y;		
//	uint subInstanceAtomStart = subInstanceInfo.z;
//	uint subInstanceBoundingSphere = subInstanceInfo.w;

//	uint instanceType = _InstanceTypes[instanceId];
//	uint instanceAtomCount = _IngredientAtomCount[instanceType];				
//	uint instanceAtomStart = _IngredientAtomStart[instanceType];		
				
//	output.id = instanceId;
//	output.subInstanceId = subInstanceId;
//	output.type = instanceType;		
//	output.state = _InstanceStates[instanceId];
//	output.rot = _InstanceRotations[instanceId];
//	output.pos = _InstancePositions[instanceId].xyz * _Scale;
									
//	output.atomCount = instanceAtomCount;
//	output.atomStart = instanceAtomStart;

//	// Set default lod values
//	output.lodInfo.level = 0;
//	output.lodInfo.atomRadiusMin = 10;
//	output.lodInfo.atomRadiusMax = 10; 			
//	output.lodInfo.atomRadiusLerp = 0; 					
//	output.lodInfo.decimationFactor = 40;			

//	output.subIntanceAtomStart = subInstanceAtomStart;
//	output.subIntanceAtomCount = ceil((float)subInstanceAtomCount / (float)output.lodInfo.decimationFactor);		
					
//	// Toggle ingredients visibily 
//	output.subIntanceAtomCount = (_IngredientToggle[instanceType] == 0) ? 0 : output.subIntanceAtomCount;			
										
//	// Do cross section
//	if(_EnableCrossSection && PlaneTest(_CrossSectionPlane, output.pos, 0)) output.subIntanceAtomCount = 0;	
			
//	// Do Early frustrum culling 
//	if(!SphereInFrustum(output.pos, subInstanceBoundingSphere * _Scale)) output.subIntanceAtomCount = 0;

//	// Check if instance has been previously culled
//	output.subIntanceAtomCount = (subInstanceCullFlag == 1) ? 0 : output.subIntanceAtomCount;

//	return;
//}			

////--------------------------------------------------------------------------------------

//[domain("isoline")]
//[partitioning("integer")]
//[outputtopology("point")]
//[outputcontrolpoints(1)]				
//[patchconstantfunc("HSConst")]
//hs2ds HS_CULLING (InputPatch<vs2hs, 1> input, uint ID : SV_OutputControlPointID)
//{
//	return input[0];
//} 

////--------------------------------------------------------------------------------------
			
//[domain("isoline")]
//void DS_CULLING(hsConst input, const OutputPatch<hs2ds, 1> op, float2 uv : SV_DomainLocation, out ds2gs output)
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

//struct GS2FS_CULLING
//{		
//	int id : INT0;		
//	float2 uv: TEXCOORD0;	
//	centroid float4 pos : SV_Position;				
//	nointerpolation float radius : FLOAT0;	
//};
							
//[maxvertexcount(4)]
//void GS_CULLING(point ds2gs input[1], inout TriangleStream<GS2FS_CULLING> triangleStream)
//{
//	// Discard unwanted atoms
//	if( input[0].atomType < 0 ) return;	
								
//	GS2FS_CULLING output;	
//	output.id = input[0].subInstanceId;				
//	output.radius = input[0].lodInfo.atomRadiusMin * _Scale;
	
//	//*****//
		
//	float4 pos = mul(UNITY_MATRIX_MVP, float4(input[0].pos, 1));
//	float4 offset = mul(UNITY_MATRIX_P, float4(output.radius, output.radius, 0, 0));

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

//uniform	RWStructuredBuffer<int> _RWSubInstanceCullFlags : register(u1);

//[earlydepthstencil]
//void FS_CULLING (GS2FS_CULLING input, out float4 color : COLOR0) 
//{	
//	if(_RWSubInstanceCullFlags[input.id] == 0) _RWSubInstanceCullFlags[input.id] = 1;
	
//	discard;

//	// Find color		
//	//color = float4(Linear01Depth(input.pos.z), 0, 0, 1);			
//}