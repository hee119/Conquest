Shader "UI/Planet Selected Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "CanUseSpriteAtlas" = "True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float4 _ClipRect;
            float _GlowIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 position : SV_POSITION;
                float4 localPosition : TEXCOORD1;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.localPosition = input.vertex;
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            float RingCoverage(float2 uv, float texels)
            {
                float2 d = _MainTex_TexelSize.xy * texels;
                float2 diagonal = d * 0.70710678;
                float coverage = 0;
                coverage = max(coverage, tex2D(_MainTex, uv + float2(d.x, 0)).a);
                coverage = max(coverage, tex2D(_MainTex, uv - float2(d.x, 0)).a);
                coverage = max(coverage, tex2D(_MainTex, uv + float2(0, d.y)).a);
                coverage = max(coverage, tex2D(_MainTex, uv - float2(0, d.y)).a);
                coverage = max(coverage, tex2D(_MainTex, uv + diagonal).a);
                coverage = max(coverage, tex2D(_MainTex, uv - diagonal).a);
                coverage = max(coverage, tex2D(_MainTex, uv + float2(diagonal.x, -diagonal.y)).a);
                coverage = max(coverage, tex2D(_MainTex, uv + float2(-diagonal.x, diagonal.y)).a);
                return coverage;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                clip(input.color.a - 0.5);
                clip(0.5 - tex2D(_MainTex, input.uv).a);
                #ifdef UNITY_UI_CLIP_RECT
                clip(UnityGet2DClipping(input.localPosition.xy, _ClipRect) - 0.5);
                #endif

                float core = step(0.5, max(RingCoverage(input.uv, 4), RingCoverage(input.uv, 8)));
                float nearEdge = step(0.5, max(RingCoverage(input.uv, 11), RingCoverage(input.uv, 14)));
                float outerEdge = step(0.5, max(RingCoverage(input.uv, 17), RingCoverage(input.uv, 20)));
                float light = core + (1 - core) * nearEdge * 0.7
                                   + (1 - core) * (1 - nearEdge) * outerEdge * 0.25;
                light *= _GlowIntensity;
                clip(light - 0.001);
                return fixed4(light, light, light, 1);
            }
            ENDCG
        }
    }
}
