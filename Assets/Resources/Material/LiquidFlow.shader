Shader "Custom/LiquidFlow"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.3, 0.2, 0.1, 0.5) // verdastro/marrone semitrasparente
        _FlowSpeed ("Flow Speed", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _TintColor;
            float _FlowSpeed;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);

                // Applichiamo tiling/offset di Unity
                float2 uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                // Scroll in Y basato sul tempo
                uv.y += _Time.y * _FlowSpeed;

                OUT.uv = uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texCol = tex2D(_BaseMap, IN.uv);
                return texCol * _TintColor;
            }
            ENDHLSL
        }
    }
}
