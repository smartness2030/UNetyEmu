Shader "Custom/LinearDepthShader"
{
    Properties {}
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                // SAMPLE_DEPTH_TEXTURE pega o valor do Z-Buffer
                float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                // LinearEyeDepth converte para metros reais
                float linearDistance = LinearEyeDepth(d);
                return float4(linearDistance, 0, 0, 1);
            }
            ENDCG
        }
    }
}