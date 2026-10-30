// ステージの演出（床に寝かせた円・輪、光、頭の上の文字・ダメージ数字）用の、光の当たらない半透明のシェーダー（URP）。
// 色は テクスチャ × 描く物の色 × _Color。描く物の色は、TextMesh は頂点の色、SpriteRenderer は unity_SpriteColor で届く（URP）。
// 混ぜ方（ふつう / 足して光らせる）と、ほかの物に隠れるか（_ZTest）を素材ごとに変えて、1つのシェーダーで使い回す
Shader "PenmonScramble/Fx"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        // 文字（フォントのテクスチャ）は透明度だけを持つので、色は頂点の色だけで決める
        [MaterialToggle] _AlphaOnly ("Alpha Only", Float) = 0
        // SpriteRenderer の色（unity_SpriteColor）を掛けるか。TextMesh では値が入らないので切る
        [MaterialToggle] _SpriteColor ("Sprite Color", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _AlphaOnly;
                float _SpriteColor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * lerp(float4(1, 1, 1, 1), unity_SpriteColor, _SpriteColor);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                tex = lerp(tex, half4(1, 1, 1, tex.a), _AlphaOnly);
                return tex * input.color;
            }
            ENDHLSL
        }
    }
}
