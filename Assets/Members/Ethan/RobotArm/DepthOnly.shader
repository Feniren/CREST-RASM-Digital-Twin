Shader "Custom/DepthOnly"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent-1" }
        Pass
        {
            ZWrite On
            ColorMask 0
        }
    }
}