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
        private readonly List<Renderer> _parts = new List<Renderer>();

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

        internal void AddPart(Renderer part) => _parts.Add(part);

        /// <summary>本物の影を落とすか、足元の丸い影にするか。両方出すと影が二重になるのでどちらか一方</summary>
        public void SetRealShadows(bool real)
        {
            foreach (var part in _parts)
            {
                part.shadowCastingMode = real ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }

            BlobShadow.enabled = !real;
        }
    }
}
