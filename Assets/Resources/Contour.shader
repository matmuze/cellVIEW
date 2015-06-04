Shader "Custom/Contour" {
    Properties
	{
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader 
	{
		Pass 
		{
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
			  
			#include "Helper.cginc"	
            #include "UnityCG.cginc"  
			        
			uniform int _ContourOptions;	
			uniform float _ContourStrength;

			uniform sampler2D _MainTex;			
			uniform Texture2D<int> _IdTexture;

			int GetIdFromColor(float3 color)
			{
				int b = (int) (color.b*255.0f);
				int g = (int) (color.g*255.0f) << 8;
				int r = (int) (color.r*255.0f) << 16;
            
				return r + g + b;
			}

            fixed4 frag(v2f_img i) : SV_Target 
			{
				float factor = 1.0;
				float stepX = factor;
				float stepY = factor;

				float2 offset[9];

				offset[0] = float2(-stepX, -stepY); offset[1] = float2(0.0, -stepY); offset[2] = float2(stepX, -stepY);
				offset[3] = float2(-stepX,    0.0); offset[4] = float2(0.0,    0.0); offset[5] = float2(stepX,    0.0);
				offset[6] = float2(-stepX,  stepY); offset[7] = float2(0.0,  stepY); offset[8] = float2(stepX,  stepY);
				
				int2 uv = i.uv * _ScreenParams.xy;

				float3 c1, c3, c5, c7;
				c1 = c3 = c7 = c5 = float3(0,0,0); 

				int id3 = _IdTexture[uv + offset[3].xy];				
				int id5 = _IdTexture[uv + offset[5].xy];					
				int id1 = _IdTexture[uv + offset[1].xy];				
				int id7 = _IdTexture[uv + offset[7].xy];	

				if(id3 != id5) { c3 = float3(0,0,0); c5 = float3(1,1,1) * _ContourStrength; }	
				if(id1 != id7) { c1 = float3(0,0,0); c7 = float3(1,1,1) * _ContourStrength; }			
				
				float3 cd0 = c3 * (-0.5) + c5 * (+0.5);
				float3 cd1 = c1 * (+0.5) + c7 * (-0.5);

				float3 contour = pow(pow(cd0.rgb, 2) + pow(cd1.rgb, 2), 0.5);				
				float3 color = tex2D(_MainTex, i.uv).xyz;	

				float3 finalColor;

				if(_ContourOptions == 0) finalColor = OffsetHSV(color, float3(0,0,-contour.x * _ContourStrength)); //color - contour;
				else if(_ContourOptions == 1) finalColor = color;
				else if(_ContourOptions == 2) finalColor = float3(1,1,1) - contour;

				return float4(finalColor, 1);			

            }
            ENDCG
        }
	}	

	FallBack "Diffuse"
}