Shader "Custom/LightDebugShader" 
{
	SubShader
	{
        Pass 
		{
			Cull Off
			ZWrite On

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 frag(v2f_img i) : SV_Target 
			{
                return fixed4(1,1,1,1);
            }
            ENDCG
        }
    }
}
