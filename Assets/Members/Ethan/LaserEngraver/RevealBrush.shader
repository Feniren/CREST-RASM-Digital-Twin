Shader "Hidden/RevealBrush"
{
    Properties
    {
        _BrushPos    ("Brush UV Start Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushPosEnd ("Brush UV End Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius (UV space)", Float) = 0.05
        _Hardness    ("Hardness", Range(0,1)) = 0.8
    }

    SubShader
    {
        Blend One One
        BlendOp Max
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
            float4 _BrushPosEnd;
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
                // Distance to the swept segment (a capsule), not to a single point.
                float2 segment = _BrushPosEnd.xy - _BrushPos.xy;
                float2 toPixel = IN.uv - _BrushPos.xy;

                float  along   = saturate(dot(toPixel, segment) /
                                          max(dot(segment, segment), 1e-12));

                float  dist    = length(toPixel - segment * along) /
                                 max(_BrushRadius, 0.0001);

                float  alpha   = 1.0 - smoothstep(_Hardness, 1.0, dist);
                return half4(alpha, alpha, alpha, alpha);
            }
            ENDHLSL
        }
    }
}
