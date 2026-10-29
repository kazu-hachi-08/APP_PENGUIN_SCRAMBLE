using System.Collections.Generic;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// トゥーンの素材を色ごとに1つだけ作って使い回す。同じ素材の部品はまとめて描かれるので、色の数だけ素材があればよい。
    /// シェーダーはシーンの参照から受け取る（Shader.Find だとビルドに入らないことがあるため）
    /// </summary>
    public sealed class ToonMaterials
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int GlossId = Shader.PropertyToID("_Gloss");
        private static readonly int GlossSizeId = Shader.PropertyToID("_GlossSize");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        // つやの大きさ（光の向きとの近さの下限）。1 に近いほど小さく鋭い点になる。大きいと灰色の円に見えておもちゃらしくない
        private const float GlossSize = 0.99f;

        private readonly Shader _toon;
        private readonly Dictionary<(Color color, float gloss), Material> _cache = new Dictionary<(Color, float), Material>();
        private readonly List<Material> _all = new List<Material>();

        public Material BlobShadow { get; }

        public ToonMaterials(Shader toon, Shader blobShadow)
        {
            _toon = toon;
            BlobShadow = Track(new Material(blobShadow) { name = "BlobShadow" });
        }

        public Material Get(Color color, float gloss)
        {
            if (_cache.TryGetValue((color, gloss), out var material))
            {
                return material;
            }

            material = Track(new Material(_toon) { name = $"Toon_{ColorUtility.ToHtmlStringRGB(color)}" });
            material.SetColor(BaseColorId, color);
            material.SetFloat(GlossId, gloss);
            material.SetFloat(GlossSizeId, GlossSize);
            _cache.Add((color, gloss), material);
            return material;
        }

        /// <summary>床の板。縁の線とつやは出さない</summary>
        public Material Floor(Texture texture, Color color)
        {
            var material = Track(new Material(_toon) { name = "ToonFloor" });
            material.SetTexture(BaseMapId, texture);
            material.SetColor(BaseColorId, color);
            material.SetFloat(GlossId, 0f);
            material.SetFloat(OutlineWidthId, 0f);
            return material;
        }

        /// <summary>縁の線の太さをまとめて変える（0 で消える）</summary>
        public void SetOutlineWidth(float width)
        {
            foreach (var pair in _cache)
            {
                pair.Value.SetFloat(OutlineWidthId, width);
            }
        }

        /// <summary>シーンに保存しない作り物なので、片付けるときにまとめて消す</summary>
        public void DestroyAll()
        {
            foreach (var material in _all)
            {
                Object.DestroyImmediate(material);
            }

            _all.Clear();
            _cache.Clear();
        }

        private Material Track(Material material)
        {
            material.hideFlags = HideFlags.DontSave;
            _all.Add(material);
            return material;
        }
    }
}
