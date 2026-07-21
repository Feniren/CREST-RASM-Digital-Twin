Shader "Hidden/RevealBrush"
{
    Properties
    {
        _BrushPos    ("Brush UV Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius (UV space)", Float) = 0.05
        _Hardness    ("Hardness", Range(0,1)) = 0.8
    }

    SubShader
    {
        // Additive: never un-reveals already-painted areas
        Blend One One
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            float4 _BrushPos;
            float  _BrushRadius;
            float  _Hardness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.pos = TransformObjectToHClip(IN.pos.xyz);
                OUT.uv  = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 delta = IN.uv - _BrushPos.xy;
                float  dist  = length(delta) / max(_BrushRadius, 0.0001);
                float  alpha = 1.0 - smoothstep(_Hardness, 1.0, dist);
                return half4(alpha, alpha, alpha, alpha);
            }
            ENDHLSL
        }
    }
}