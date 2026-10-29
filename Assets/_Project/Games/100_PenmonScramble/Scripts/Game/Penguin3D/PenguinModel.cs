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

        /// <summary>胴・頭・ヒレをまとめた土台。足元を軸に揺らす・かがませる</summary>
        public Transform Body { get; private set; }
        public Transform Head { get; private set; }
        public Transform FlipperLeft { get; private set; }
        public Transform FlipperRight { get; private set; }
        public Transform FootLeft { get; private set; }
        public Transform FootRight { get; private set; }
        public Renderer BlobShadow { get; private set; }

        internal void Assign(Transform body, Transform head, Transform flipperLeft, Transform flipperRight, Transform footLeft, Transform footRight, Renderer blobShadow)
        {
            Body = body;
            Head = head;
            FlipperLeft = flipperLeft;
            FlipperRight = flipperRight;
            FootLeft = footLeft;
            FootRight = footRight;
            BlobShadow = blobShadow;
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
