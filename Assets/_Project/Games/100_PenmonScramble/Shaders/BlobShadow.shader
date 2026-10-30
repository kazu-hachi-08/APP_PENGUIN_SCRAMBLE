// 足元の丸い影（URP）。本物の影より軽いので、スマホ・ブラウザではこちらを使う想定。
// テクスチャを使わず、板の UV から中心ほど濃い円を作る
Shader "PenmonScramble/BlobShadow"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.15, 0.3, 0.45)
        _Softness ("Softness", Range(0.01, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-10" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float distance = length(input.uv - 0.5) * 2.0;
                float alpha = 1.0 - smoothstep(1.0 - _Softness, 1.0, distance);
                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }
}
