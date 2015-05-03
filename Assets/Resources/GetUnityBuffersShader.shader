Shader "Custom/GetUnityBuffersShader" 
{
	Properties {
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		
		Pass 
		{
			ZWrite On
			ZTest Always

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D_float _CameraDepthTexture;
			sampler2D_float _CameraDepthNormalsTexture;
			
			//sampler2D _CameraNormalsTexture;

            void frag(v2f_img i,  out float4 color : COLOR0, out float4 normal : COLOR1, out float depth : DEPTH) 
			{             
				color = tex2D(_MainTex, i.uv);       
				depth = tex2D(_CameraDepthTexture, i.uv);
				normal = tex2D(_CameraDepthNormalsTexture, i.uv); 
            }
            ENDCG
        }
	}	

	FallBack "Diffuse"
}