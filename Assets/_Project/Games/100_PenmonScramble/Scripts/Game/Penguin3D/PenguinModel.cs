using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 組み立てた3Dのペンギンの部品への参照。動かすのは PenguinMotion、組み立てるのは PenguinModelBuilder。
    /// 前は +Z、足元が原点
    /// </summary>
    public sealed class PenguinModel : MonoBehaviour
    {
        private readonly List<Part> _parts = new List<Part>();
        private ToonMaterials _materials;

        /// <summary>体全体（胴と足）。向き・転がる・倒れる・跳ぶときに丸ごと動かす。足元の丸い影は入れない（床に残すため）</summary>
        public Transform Figure { get; private set; }

        /// <summary>胴・頭・ヒレをまとめた土台。足元を軸に揺らす・かがませる</summary>
        public Transform Body { get; private set; }
        public Transform Head { get; private set; }
        public Transform FlipperLeft { get; private set; }
        public Transform FlipperRight { get; private set; }
        public Transform FootLeft { get; private set; }
        public Transform FootRight { get; private set; }
        public Renderer BlobShadow { get; private set; }

        /// <summary>ひとみと目の光。まばたきで縦につぶす</summary>
        public IReadOnlyList<Transform> Eyes { get; private set; }

        /// <summary>胴の中心の高さ。転がる・倒れるときはここを軸に回す</summary>
        public float CenterHeight { get; private set; }

        /// <summary>お腹を下にして倒れたときの、胴の中心の高さ（お腹の厚み）</summary>
        public float LieHeight { get; private set; }

        /// <summary>頭のてっぺんの高さ（飾りは含めない）。転がって逆さになったとき、床にめり込まない高さを決める</summary>
        public float TopHeight { get; private set; }

        /// <summary>今の色の変え方（SetTint）</summary>
        public PenguinTint Tint { get; private set; }

        internal void Assign(Transform figure, Transform body, Transform head, Transform flipperLeft, Transform flipperRight, Transform footLeft, Transform footRight, Renderer blobShadow)
        {
            Figure = figure;
            Body = body;
            Head = head;
            FlipperLeft = flipperLeft;
            FlipperRight = flipperRight;
            FootLeft = footLeft;
            FootRight = footRight;
            BlobShadow = blobShadow;
        }

        internal void AssignShape(IReadOnlyList<Transform> eyes, float centerHeight, float lieHeight, float topHeight)
        {
            Eyes = eyes;
            CenterHeight = centerHeight;
            LieHeight = lieHeight;
            TopHeight = topHeight;
        }

        internal void SetMaterials(ToonMaterials materials) => _materials = materials;

        internal void AddPart(Renderer renderer, Color color, float gloss) => _parts.Add(new Part(renderer, color, gloss));

        /// <summary>部品の色をまとめて変える（敵の暗さ・凍る・ダウン・被弾の光）。同じ変え方なら何もしないので、毎フレーム呼んでよい</summary>
        public void SetTint(PenguinTint tint)
        {
            if (tint.Equals(Tint))
            {
                return;
            }

            Tint = tint;
            foreach (var part in _parts)
            {
                part.Renderer.sharedMaterial = _materials.Get(tint.Apply(part.Color), part.Gloss);
            }
        }

        /// <summary>本物の影を落とすか、足元の丸い影にするか。両方出すと影が二重になるのでどちらか一方</summary>
        public void SetRealShadows(bool real)
        {
            foreach (var part in _parts)
            {
                part.Renderer.shadowCastingMode = real ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }

            BlobShadow.enabled = !real;
        }

        /// <summary>組み立てたときの色。色を変えるときは、いつもこの色から計算し直す</summary>
        private readonly struct Part
        {
            public readonly Renderer Renderer;
            public readonly Color Color;
            public readonly float Gloss;

            public Part(Renderer renderer, Color color, float gloss)
            {
                Renderer = renderer;
                Color = color;
                Gloss = gloss;
            }
        }
    }
}
