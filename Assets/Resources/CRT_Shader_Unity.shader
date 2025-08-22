
Shader "Custom/CRTShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _Curvature ("Screen Curvature", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _ScanlineIntensity;
            float _Curvature;

            v2f vert (appdata v)
            {
                v2f o;
                float2 offset = (v.vertex.xy - 0.5) * _Curvature;
                o.vertex = UnityObjectToClipPos(v.vertex + float4(offset, 0.0, 0.0));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 col = tex2D(_MainTex, uv);

                // Scanline effect
                float scanline = sin(uv.y * _ScreenParams.y * 2.0) * _ScanlineIntensity;
                col.rgb -= scanline;

                return col;
            }
            ENDCG
        }
    }
}
