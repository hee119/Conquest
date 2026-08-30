Shader "Custom/LightningGlow"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.5, 1, 1)
        _Intensity ("Intensity", Range(0, 10)) = 3
        _FlickerSpeed ("Flicker Speed", Range(0, 20)) = 8
        _Glow ("Glow", Range(0, 2)) = 0.5

        _Jaggedness ("Jaggedness", Range(0, 1)) = 0.35
        _Segments ("Segments", Range(2, 20)) = 8
        _RandomSeed ("Random Seed", Float) = 0
        _CoreWidth ("Core Width", Range(0.001, 0.3)) = 0.03
        _GlowWidth ("Glow Width", Range(0.01, 0.5)) = 0.12
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

            float _Jaggedness;
            float _Segments;
            float _RandomSeed;
            float _CoreWidth;
            float _GlowWidth;

            CBUFFER_END


            // 0~1 값을 랜덤한 값으로 변환
            float random(float2 p)
            {
                return frac(
                    sin(
                        dot(
                            p,
                            float2(12.9898, 78.233)
                        )
                    ) * 43758.5453
                );
            }

            // LineRenderer 기본 UV 기준: uv.x = 선 길이 방향(0~1), uv.y = 선 폭 방향(0~1, 0.5=중심)
            // 길이 방향(u)을 _Segments 구간으로 나누고 각 구간 경계마다 랜덤값을 만든 뒤
            // 구간 사이를 선형보간해서 꺾인 지그재그 경로를 얻는다 (1D Value Noise)
            float JaggedPath(float u)
            {
                float segF = u * _Segments;
                float segIndex = floor(segF);
                float segFrac = frac(segF);

                float r0 = random(float2(segIndex, _RandomSeed));
                float r1 = random(float2(segIndex + 1.0, _RandomSeed));

                // 0~1 → -1~1
                r0 = r0 * 2.0 - 1.0;
                r1 = r1 * 2.0 - 1.0;

                return lerp(r0, r1, segFrac);
            }


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );

                OUT.uv = IN.uv;

                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                float u = IN.uv.x; // 선 길이 방향
                float v = IN.uv.y; // 선 폭 방향

                // 시작점/끝점은 흔들리지 않도록 (라인이 지정한 두 점에 정확히 붙음)
                float edgeMask = sin(u * 3.14159265);

                // 폭 방향 좌표계 기준 지그재그 오프셋 (중심 0.5 기준 ±)
                float path =
                    JaggedPath(u) *
                    _Jaggedness *
                    0.5 *
                    edgeMask;

                // 현재 픽셀이 지그재그 경로에서 얼마나 떨어져 있는지
                float dist = abs((v - 0.5) - path);

                float time =
                    _Time.y * _FlickerSpeed;

                // 깜빡임
                float flicker =
                    0.75 +
                    0.25 * sin(time);

                // 화면상 픽셀 크기에 맞춰 경계를 안티에일리어싱
                // (라인 폭이 얇아져도 코어가 뭉개지지 않고 선명하게 유지됨)
                float aa = max(fwidth(dist), 0.0005);

                // 경로에 딱 붙은 얇고 밝은 코어 (선명한 경계)
                float core =
                    1.0 - smoothstep(
                        _CoreWidth - aa,
                        _CoreWidth + aa,
                        dist
                    );

                // 코어 바깥으로 퍼지는 글로우 (폭을 별도로 조절 가능)
                float glow =
                    (1.0 - smoothstep(
                        _GlowWidth - aa,
                        _GlowWidth + aa,
                        dist
                    )) * _Glow;

                float brightness =
                    (core + glow) *
                    flicker *
                    _Intensity;

                float3 color =
                    _Color.rgb *
                    brightness;

                return half4(
                    color,
                    saturate(brightness)
                );
            }

            ENDHLSL
        }
    }
}
