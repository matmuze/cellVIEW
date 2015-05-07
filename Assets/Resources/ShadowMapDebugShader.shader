Shader "Custom/ShadowMapDebugShader" {
	Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            uniform sampler2D_float _MainTex;

            fixed4 frag(v2f_img i) : SV_Target 
			{
				float d = pow(tex2D(_MainTex, i.uv) / 120, 2);
                return float4(d.xxx, 1);
            }
            ENDCG
        }
    }
}
