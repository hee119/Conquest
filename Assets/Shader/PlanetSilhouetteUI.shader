Shader "UI/Planet Silhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineTexels ("Outline Width In Texture Pixels", Float) = 8
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _OutlineTexels;
            float4 _ClipRect;

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

            fixed4 frag(v2f input) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * _OutlineTexels;
                float2 diagonal = d * 0.70710678;
                float2 halfStep = d * 0.5;
                float neighbors = 0;
                neighbors = max(neighbors, tex2D(_MainTex, input.uv + float2(d.x, 0)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv - float2(d.x, 0)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv + float2(0, d.y)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv - float2(0, d.y)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv + diagonal).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv - diagonal).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv + float2(diagonal.x, -diagonal.y)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv + float2(-diagonal.x, diagonal.y)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv + float2(halfStep.x, 0)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv - float2(halfStep.x, 0)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv + float2(0, halfStep.y)).a);
                neighbors = max(neighbors, tex2D(_MainTex, input.uv - float2(0, halfStep.y)).a);
                clip(neighbors - 0.5);
                clip(0.5 - tex2D(_MainTex, input.uv).a);
                clip(input.color.a - 0.5);
                #ifdef UNITY_UI_CLIP_RECT
                clip(UnityGet2DClipping(input.localPosition.xy, _ClipRect) - 0.5);
                #endif
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
