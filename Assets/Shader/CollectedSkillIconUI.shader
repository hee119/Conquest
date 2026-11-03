Shader "UI/Collected Skill Icon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _UseDarkMask ("Use Dark Pixels As Mask", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
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
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float4 localPosition:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _UseDarkMask;
            fixed IconMask(float2 uv)
            {
                fixed4 pixel = tex2D(_MainTex, uv);
                return pixel.a * lerp(1.0, 1.0 - dot(pixel.rgb, fixed3(0.299, 0.587, 0.114)), _UseDarkMask);
            }
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;
                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 source = tex2D(_MainTex, i.uv);
                // Sample one display pixel around the silhouette, independent of
                // source texture resolution or atlas packing scale.
                float2 dx = ddx(i.uv) * 1.25;
                float2 dy = ddy(i.uv) * 1.25;
                fixed surrounding = IconMask(i.uv);
                surrounding = max(surrounding, IconMask(i.uv + dx));
                surrounding = max(surrounding, IconMask(i.uv - dx));
                surrounding = max(surrounding, IconMask(i.uv + dy));
                surrounding = max(surrounding, IconMask(i.uv - dy));
                surrounding = max(surrounding, IconMask(i.uv + dx + dy));
                surrounding = max(surrounding, IconMask(i.uv + dx - dy));
                surrounding = max(surrounding, IconMask(i.uv - dx + dy));
                surrounding = max(surrounding, IconMask(i.uv - dx - dy));
                fixed fill = smoothstep(0.1, 0.5, IconMask(i.uv));
                fixed alpha = smoothstep(0.1, 0.5, surrounding);
                // White interior, black outline; retain UI opacity and clipping.
                fixed4 result = fixed4(fill, fill, fill, alpha * i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
