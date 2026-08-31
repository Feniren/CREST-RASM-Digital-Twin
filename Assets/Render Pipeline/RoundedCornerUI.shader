Shader "UI/RoundedCornerUI"{    
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint / Alpha", Color) = (1,1,1,1)
        _CornerRadius ("Corner Radius (pixels)", Float) = 24
        _Size ("Rect Size (pixels, set by script)", Vector) = (256, 256, 0, 0)
        _BorderColor ("Border Color", Color) = (1,1,1,1)
        _BorderWidth ("Border Width (pixels)", Float) = 0

        // Required so this plays nicely with Canvas masking / RectMask2D if ever combined
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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
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
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                half4 color     : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localPos : TEXCOORD1; // pixel-space position relative to rect center
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            float _CornerRadius;
            float4 _Size; // x = width, y = height, in pixels
            half4 _BorderColor;
            float _BorderWidth;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;

                // Convert UV (0-1) into pixel-space offset from rect center
                o.localPos = (v.texcoord - 0.5) * _Size.xy;
                return o;
            }

            // Signed distance function for a rounded box
            float roundedBoxSDF(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, i.texcoord);
                half4 contentCol = texColor * i.color;

                float2 halfSize = _Size.xy * 0.5;
                float radius = min(_CornerRadius, min(halfSize.x, halfSize.y));

                float dist = roundedBoxSDF(i.localPos, halfSize, radius);
                float aa = fwidth(dist);

                // Outer mask: 1 inside the whole rounded shape (content + border), 0 outside
                float shapeMask = 1.0 - smoothstep(-aa, aa, dist);

                // Content mask: 1 in the interior (past the border band), 0 out at the border/edge.
                // Reuses the same dist value, just measured against an inset edge at -_BorderWidth.
                float contentMask = 1.0 - smoothstep(-_BorderWidth - aa, -_BorderWidth + aa, dist);

                half4 borderCol = _BorderColor * i.color.a; // border respects the overall alpha too
                half4 col = lerp(borderCol, contentCol, contentMask);
                col.a *= shapeMask;

                return col;
            }
            ENDHLSL
        }
    }
}
