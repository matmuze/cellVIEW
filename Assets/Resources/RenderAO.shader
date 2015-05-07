Shader "Hidden/HBAO+/RenderAo"
{
    SubShader
    {   
        Pass
        {
            ZTest Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            Texture2D _AoResult;
            SamplerState sampler_AoResult;

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

            float4 frag(v2f i) : SV_Target
            {   
                const float ao = _AoResult.Sample(sampler_AoResult, i.uv);

                return ao.xxxx * _MainTex.Sample(sampler_MainTex, i.uv) ;
            }

            ENDCG
        }
    }
}