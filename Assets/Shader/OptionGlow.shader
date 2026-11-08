Shader "Custom/OptionGlow"
{
    Properties
    {
        [MainTexture]
        _MainTex ("Texture", 2D) = "white" {}

        [HDR]
        _Color ("Color", Color) = (1, 1, 1, 1)

        _Brightness ("Brightness", Range(0, 10)) = 1

        _Glow ("Glow", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Glow"

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

            // 텍스처
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                float4 _Color;
                float _Brightness;
                float _Glow;

            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv =
                    TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        IN.uv
                    );

                float brightness =
                    _Brightness * _Glow;

                float3 finalColor =
                    texColor.rgb *
                    _Color.rgb *
                    brightness;

                float alpha =
                    texColor.a *
                    _Color.a;

                return half4(
                    finalColor,
                    alpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}