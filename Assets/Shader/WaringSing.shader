Shader "Custom/URP/RadialFillSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        _Progress ("Progress", Range(0,1)) = 0
        _StartAngle ("Start Angle", Range(0,360)) = 90
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;
                float4 _Color;

                float _Progress;
                float _StartAngle;

            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = TRANSFORM_TEX(
                    IN.uv,
                    _MainTex
                );

                OUT.color = IN.color * _Color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sprite Texture
                half4 texColor = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    IN.uv
                );

                // 중심을 (0, 0)으로 이동
                float2 centeredUV = IN.uv - 0.5;

                // 중심에서 현재 픽셀까지의 각도
                float angle = atan2(
                    centeredUV.y,
                    centeredUV.x
                );

                // -PI ~ PI
                // ↓
                // 0 ~ 1
                float normalizedAngle =
                    (angle + 3.14159265) /
                    (2.0 * 3.14159265);

                // 시작 각도
                float start =
                    _StartAngle / 360.0;

                // 시작점을 기준으로 각도 재계산
                normalizedAngle =
                    frac(normalizedAngle - start);

                // Progress보다 작은 영역만 표시
                float mask =
                    step(
                        normalizedAngle,
                        _Progress
                    );

                // 원형 마스크 적용
                texColor.a *= mask;

                // SpriteRenderer Color 적용
                texColor *= IN.color;

                return texColor;
            }

            ENDHLSL
        }
    }
}