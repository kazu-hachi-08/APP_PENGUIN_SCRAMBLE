using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        // つやの大きさ（光の向きとの近さの下限）。1 に近いほど小さく鋭い点になる。大きいと灰色の円に見えておもちゃらしくない
        private const float GlossSize = 0.99f;

        // 氷は縁を強く光らせて、透けていても塊の形が分かるようにする
        private static readonly Color IceColor = new Color(0.72f, 0.93f, 1f, 0.42f);
        private const float IceRim = 0.9f;

        private readonly Shader _toon;
        private readonly Dictionary<(Color color, float gloss), Material> _cache = new Dictionary<(Color, float), Material>();
        private readonly Dictionary<(Texture texture, Color color), Material> _floors = new Dictionary<(Texture, Color), Material>();
        private readonly List<Material> _all = new List<Material>();

        private Material _ice;
        private Material _ghost;

        public Material BlobShadow { get; }

        /// <summary>凍ったペンギンを包む氷の塊（半透明）</summary>
        public Material Ice => _ice != null ? _ice : _ice = CreateIce();

        /// <summary>
        /// 突進の残像（半透明）。色と透明度は部品ごとに MaterialPropertyBlock の _BaseColor で変える。
        /// 残像は一瞬で消えるので、まとめて描けなくなっても素材を1つで済ませる方を選ぶ
        /// </summary>
        public Material Ghost => _ghost != null ? _ghost : _ghost = CreateTransparent("ToonGhost", Color.white, 0f);

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

        /// <summary>床の板。縁の線とつやは出さない。ステージを作り直すたびに増えないよう、絵と色ごとに1つ</summary>
        public Material Floor(Texture texture, Color color)
        {
            if (_floors.TryGetValue((texture, color), out var material))
            {
                return material;
            }

            material = Track(new Material(_toon) { name = "ToonFloor" });
            material.SetTexture(BaseMapId, texture);
            material.SetColor(BaseColorId, color);
            material.SetFloat(GlossId, 0f);
            material.SetFloat(OutlineWidthId, 0f);
            _floors.Add((texture, color), material);
            return material;
        }

        private Material CreateIce()
        {
            var material = CreateTransparent("ToonIce", IceColor, 1f);
            material.SetFloat(RimStrengthId, IceRim);
            return material;
        }

        /// <summary>
        /// 半透明のトゥーン。縁の線は切る（裏返した殻が透けて、中が黒く見えるため）。
        /// 奥行きは書かない（ほかの半透明の物と重なっても、後ろが消えないように）
        /// </summary>
        private Material CreateTransparent(string materialName, Color color, float gloss)
        {
            var material = Track(new Material(_toon) { name = materialName });
            material.SetColor(BaseColorId, color);
            material.SetFloat(GlossId, gloss);
            material.SetFloat(GlossSizeId, GlossSize);
            material.SetFloat(OutlineWidthId, 0f);
            material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat(ZWriteId, 0f);
            material.renderQueue = (int)RenderQueue.Transparent;
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
            _floors.Clear();
            _ice = null;
            _ghost = null;
        }

        private Material Track(Material material)
        {
            material.hideFlags = HideFlags.DontSave;
            _all.Add(material);
            return material;
        }
    }
}
