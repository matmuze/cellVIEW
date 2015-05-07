Shader "Hidden/HBAO+/FetchDepth"
{
    SubShader
    {   
        Pass
        {			
            ZWrite On
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"

            sampler2D_float _CameraDepthTexture;

            struct v2f
            {
                float4 posPS : SV_Position;
                float2 uv : TEXCOORD;
            };

            v2f vert(appdata_base i) 
            {
                v2f o;
                o.posPS = mul(UNITY_MATRIX_MVP, float4(i.vertex.xyz, 1));
                o.uv = i.texcoord;
                return o;
            }

            void frag(v2f i, out float4 color : COLOR0, out float depth : SV_DEPTH)
            {   
                color = float4(1,1,1,1);
				depth = tex2D(_CameraDepthTexture, i.uv);
				return;
            }

            ENDCG
        }
    }
}