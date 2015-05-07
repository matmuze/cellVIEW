Shader "Custom/ContourShader" {
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

            #include "UnityCG.cginc"
            
			uniform int _ContourOptions;	
			uniform float _ContourStrength;

			uniform sampler2D _MainTex;			
			uniform sampler2D _IdTexture;

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
				float stepX = 1.0 / (_ScreenParams.x * factor);
				float stepY = 1.0 / (_ScreenParams.y * factor);

				float2 offset[9];

				offset[0] = float2(-stepX, -stepY); offset[1] = float2(0.0, -stepY); offset[2] = float2(stepX, -stepY);
				offset[3] = float2(-stepX,    0.0); offset[4] = float2(0.0,    0.0); offset[5] = float2(stepX,    0.0);
				offset[6] = float2(-stepX,  stepY); offset[7] = float2(0.0,  stepY); offset[8] = float2(stepX,  stepY);
				
				float3 s3 = tex2D(_IdTexture, i.uv + offset[3].xy).rgb;				
				float3 s5 = tex2D(_IdTexture, i.uv + offset[5].xy).rgb;				
				if(GetIdFromColor(s3) != GetIdFromColor(s5)) { s3 = float3(0,0,0); s5 = float3(1,1,1) * _ContourStrength; }	
				float3 cd0 = s3 * (-0.5) + s5 * (+0.5);
				
				float3 s1 = tex2D(_IdTexture, i.uv + offset[1].xy).rgb;
				float3 s7 = tex2D(_IdTexture, i.uv + offset[7].xy).rgb;				
				if(GetIdFromColor(s1) != GetIdFromColor(s7)) { s1 = float3(0,0,0); s7 = float3(1,1,1) * _ContourStrength; }			
				float3 cd1 = s1 * (+0.5) + s7 * (-0.5);

				float3 contour = pow(pow(cd0.rgb, 2) + pow(cd1.rgb, 2), 0.5);				
				float3 color = tex2D(_MainTex, i.uv).xyz;	

				float3 finalColor;

				if(_ContourOptions == 0) finalColor = color - contour;
				else if(_ContourOptions == 1) finalColor = color;
				else if(_ContourOptions == 2) finalColor = float3(1,1,1) - contour;

				return float4(finalColor, 1);			

            }
            ENDCG
        }
	}	

	FallBack "Diffuse"
}