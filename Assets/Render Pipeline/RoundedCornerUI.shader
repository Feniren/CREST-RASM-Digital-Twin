Shader "UI/RoundedCornerUI"{    
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        BackgroundColor ("Tint / Alpha", Color) = (1,1,1,1)
        CornerRadius ("Corner Radius (pixels)", Float) = 24
        Size ("Rect Size (pixels, set by script)", Vector) = (256, 256, 0, 0)
        BorderColor ("Border Color", Color) = (1,1,1,1)
        BorderWidth ("Border Width (pixels)", Float) = 0

        StencilComp ("Stencil Comparison", Float) = 8
        StencilID ("Stencil ID", Float) = 0
        StencilOp ("Stencil Operation", Float) = 0
        StencilWriteMask ("Stencil Write Mask", Float) = 255
        StencilReadMask ("Stencil Read Mask", Float) = 255
        ColorMaskProperty ("Color Mask", Float) = 15
    }

    SubShader{
        Tags{
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Stencil{
            Ref [StencilID]
            Comp [StencilComp]
            Pass [StencilOp]
            ReadMask [StencilReadMask]
            WriteMask [StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [ColorMaskProperty]

        Pass{
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert	
            #pragma fragment frag
			#pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata_t{
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f{
                float4 vertex   : SV_POSITION;
                half4 color     : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localPos : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 BackgroundColor;
            float CornerRadius;
            float4 Size;
            half4 BorderColor;
            float BorderWidth;

            v2f vert(appdata_t v){
                v2f o = (v2f)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * BackgroundColor;

                o.localPos = (v.texcoord - 0.5) * Size.xy;

                return o;
            }

            float RoundedBoxSDF(float2 p, float2 halfSize, float radius){
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 frag(v2f i) : SV_Target{
                half4 texColor = tex2D(_MainTex, i.texcoord);
                half4 contentCol = texColor * i.color;

                float2 halfSize = Size.xy * 0.5;
                float radius = min(CornerRadius, min(halfSize.x, halfSize.y));

                float dist = RoundedBoxSDF(i.localPos, halfSize, radius);
                float aa = fwidth(dist);

                float shapeMask = 1.0 - smoothstep(-aa, aa, dist);

                float contentMask = 1.0 - smoothstep(-BorderWidth - aa, -BorderWidth + aa, dist);

                half4 borderCol = BorderColor * i.color.a;
                half4 col = lerp(borderCol, contentCol, contentMask);
                col.a *= shapeMask;

                return col;
            }
            ENDHLSL
        }
    }
}
