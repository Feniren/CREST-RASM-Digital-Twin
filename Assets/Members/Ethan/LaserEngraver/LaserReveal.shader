Shader "Custom/LaserReveal"
{
    Properties
    {
        _EngraveTex ("Engraving Atlas", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}
        _Color      ("Tint", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"  = "Transparent"
            "Queue"       = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "LaserRevealPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_EngraveTex);  SAMPLER(sampler_EngraveTex);
            TEXTURE2D(_RevealMask);  SAMPLER(sampler_RevealMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _EngraveTex_ST;
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _EngraveTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half engraveAlpha = SAMPLE_TEXTURE2D(_EngraveTex,  sampler_EngraveTex, IN.uv).a;
                half revealAmount = SAMPLE_TEXTURE2D(_RevealMask,  sampler_RevealMask, IN.uv).r;
                half alpha = engraveAlpha * revealAmount;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}