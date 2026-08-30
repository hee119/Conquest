Shader "Custom/LightningGlow"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.5, 1, 1)
        _Intensity ("Intensity", Range(0, 10)) = 3
        _FlickerSpeed ("Flicker Speed", Range(0, 20)) = 8
        _Glow ("Glow", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

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

            CBUFFER_START(UnityPerMaterial)

            float4 _Color;
            float _Intensity;
            float _FlickerSpeed;
            float _Glow;

            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y * _FlickerSpeed;

                // 빠르게 밝기가 변함
                float flicker =
                    0.75 +
                    0.25 * sin(time);

                // UV 중앙이 가장 밝게
                float center =
                    1.0 - abs(IN.uv.y - 0.5) * 2.0;

                center = pow(saturate(center), 1.5);

                // 중심 + 약한 Glow
                float brightness =
                    (center + _Glow) *
                    flicker *
                    _Intensity;

                float3 color =
                    _Color.rgb * brightness;

                return half4(
                    color,
                    saturate(brightness)
                );
            }

            ENDHLSL
        }
    }
}