Shader "Custom/RenderScene" 
{	
	CGINCLUDE
		
	#include "UnityCG.cginc"
	#include "Helper.cginc"		
	#include "Common.cginc"	
	
	#include "RenderLipids.cginc"
	#include "RenderProteins.cginc"

	//#include "ShadowMap.cginc"
	//#include "OcclusionCulling.cginc"
	
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
			
			#pragma vertex vs_protein
			#pragma hull hs
			#pragma domain ds_protein				
			#pragma geometry gs_protein			
			#pragma fragment fs_protein
						
			ENDCG
		}	

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
		
		//Pass 
	 //   {
		//	ZWrite Off

	 //   	CGPROGRAM			
	    		
		//	#include "UnityCG.cginc"
			
		//	#pragma only_renderers d3d11
		//	#pragma target 5.0				
			
		//	#pragma vertex VS_CULLING	
		//	#pragma hull HS_CULLING
		//	#pragma domain DS_CULLING				
		//	#pragma geometry GS_CULLING		
		//	#pragma fragment FS_CULLING
						
		//	ENDCG
		//}		
		
		//Pass 
	 //   {
		//	ZWrite On

	 //   	CGPROGRAM			
	    		
		//	#include "UnityCG.cginc"
			
		//	#pragma only_renderers d3d11
		//	#pragma target 5.0				
			
		//	#pragma vertex VS_SHADOW_MAP	
		//	#pragma hull HS_SHADOW_MAP
		//	#pragma domain DS_SHADOW_MAP				
		//	#pragma geometry GS_SHADOW_MAP				
		//	#pragma fragment FS_SHADOW_MAP
						
		//	ENDCG
		//}	
	}
	Fallback Off
}	