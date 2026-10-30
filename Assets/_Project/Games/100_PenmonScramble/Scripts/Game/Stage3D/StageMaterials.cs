using UnityEngine;
using UnityEngine.Rendering;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージの3Dで使う素材をまとめて持つ。ペンギン・床はトゥーン（ToonMaterials）、演出は Fx シェーダーの3つの素材を使い回す。
    /// 色は SpriteRenderer・TextMesh の頂点の色で付けるので、演出が何百出ても素材は増えない
    /// </summary>
    public sealed class StageMaterials
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZTestId = Shader.PropertyToID("_ZTest");
        private static readonly int AlphaOnlyId = Shader.PropertyToID("_AlphaOnly");
        private static readonly int SpriteColorId = Shader.PropertyToID("_SpriteColor");

        private readonly Material[] _fx;

        public StageMaterials(Shader toon, Shader blobShadow, Shader fx)
        {
            Toon = new ToonMaterials(toon, blobShadow);
            Decal = CreateFx(fx, "FxDecal", BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, CompareFunction.LessEqual);
            Glow = CreateFx(fx, "FxGlow", BlendMode.SrcAlpha, BlendMode.One, CompareFunction.LessEqual);
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            OverlayText = CreateFx(fx, "FxOverlayText", BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, CompareFunction.Always);
            // 文字は TextMesh（頂点の色）で描くので、SpriteRenderer の色は使わない
            OverlayText.SetFloat(AlphaOnlyId, 1f);
            OverlayText.SetFloat(SpriteColorId, 0f);
            OverlayText.mainTexture = Font.material.mainTexture;
            _fx = new[] { Decal, Glow, OverlayText };
            UnityEngine.Font.textureRebuilt += OnFontTextureRebuilt;
        }

        public ToonMaterials Toon { get; }

        /// <summary>床に寝かせる円・輪（ほかの物に隠れる）</summary>
        public Material Decal { get; }

        /// <summary>足して光らせる（弾の光・合体技の光）</summary>
        public Material Glow { get; }

        /// <summary>頭の上の「P1」・ダメージ数字。ほかのペンギンの後ろに隠れて読めなくならないよう、いつも手前に描く</summary>
        public Material OverlayText { get; }

        /// <summary>英数字だけなので、日本語フォントの要らない標準フォントで描く</summary>
        public Font Font { get; }

        public void DestroyAll()
        {
            UnityEngine.Font.textureRebuilt -= OnFontTextureRebuilt;
            Toon.DestroyAll();
            foreach (var material in _fx)
            {
                Object.DestroyImmediate(material);
            }
        }

        // 動くフォントは、文字が増えるとテクスチャを作り直すので、素材のテクスチャも差し替える
        private void OnFontTextureRebuilt(Font font)
        {
            if (font == Font)
            {
                OverlayText.mainTexture = Font.material.mainTexture;
            }
        }

        private static Material CreateFx(Shader shader, string name, BlendMode src, BlendMode dst, CompareFunction zTest)
        {
            var material = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
            material.SetColor(ColorId, Color.white);
            material.SetFloat(SrcBlendId, (float)src);
            material.SetFloat(DstBlendId, (float)dst);
            material.SetFloat(ZTestId, (float)zTest);
            return material;
        }
    }
}
