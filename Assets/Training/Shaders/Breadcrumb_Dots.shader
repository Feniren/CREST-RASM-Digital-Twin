// Dotted guidance trail for the training breadcrumb. Unlit and drawn with ZTest
// Always so the mill enclosure never hides the trail — a wayfinding aid the
// trainee cannot see is worthless, which is the whole reason the line exists.
//
// The dots are procedural (frac along the line's U axis), so there is no texture
// asset to commit and spacing is a single float in metres. Breadcrumb_Trail sets
// the LineRenderer to LineTextureMode.Tile, which makes U the distance along the
// line in world units — that is what holds the dots at a fixed spacing however far
// the trainee is standing from the mill. Walking away adds dots rather than
// stretching the ones already there.
Shader "Training/Breadcrumb_Dots"{
    Properties{
        // Amber to match Part_Highlighter's TargetGlow: the trail ends inside that
        // glow shell, so the two should read as one cue. Cyan already means
        // "hovered" in this project.
        [MainColor] _BaseColor("Color", Color) = (1, 0.85, 0.2, 0.5)
        _DotSpacing("Dot Spacing (metres)", Range(0.02, 1)) = 0.2
        _DotLength("Dot Length (fraction of spacing)", Range(0.02, 1)) = 0.15
        _ScrollSpeed("Scroll Speed (m/s, + = toward target)", Float) = 0.5
    }

    SubShader{
        // Queued BEFORE the other transparents on purpose. ZTest Always punches this
        // line through whatever is already in the frame buffer, so drawing it late
        // would let a trail passing in front of the world-space prompt panel erase
        // the text the trainee is meant to be reading.
        Tags{
            "RenderType" = "Transparent"
            "Queue" = "Transparent-100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass{
            Name "Breadcrumb"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            // The point of the whole effect: the machine must never hide the trail
            // that explains the machine.
            ZTest Always
            // The LineRenderer's winding depends on which side of the camera the trail
            // runs, so a trainee who walks past the target can put the ribbon's back
            // face toward themselves; culling would blink the whole trail out.
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // This project renders OpenXR in Single Pass Instanced (m_renderMode: 1
            // on both build targets in OpenXRPackageSettings.asset), where the eye
            // index arrives in the instance id. Without this pragma and the UNITY_
            // macros below the trail draws in the left eye only — which is invisible
            // on a desktop screen and only shows up in the headset.
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes{
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings{
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Named UnityPerMaterial so the SRP Batcher accepts the material.
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _DotSpacing;
                float _DotLength;
                float _ScrollSpeed;
            CBUFFER_END

            Varyings vert(Attributes input){
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target{
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // frac the scroll before it reaches the cell maths: _Time.y grows
                // without bound and would eat the mantissa after a few minutes of
                // play, making the dots stutter.
                float scroll = frac(_ScrollSpeed * _Time.y / _DotSpacing);
                float cell = frac(input.uv.x / _DotSpacing - scroll);

                // Offset from this dot's centre, normalised so 1 is its edge on both
                // axes — v spans the ribbon width, and along the line the dot fills
                // _DotLength of its cell. Fading rather than clipping antialiases the
                // edge for free, which matters at 2-4 m in a headset where a hard
                // edge crawls with shimmer.
                float2 offset = float2((cell - 0.5) / (0.5 * _DotLength),
                                       input.uv.y * 2.0 - 1.0);
                half mask = 1.0h - smoothstep(0.65, 1.0, length(offset));

                return half4(_BaseColor.rgb, _BaseColor.a * mask);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
