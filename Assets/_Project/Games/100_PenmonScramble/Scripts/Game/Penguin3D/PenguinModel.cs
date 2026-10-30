using System.Collections.Generic;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 組み立てた3Dのペンギンの部品への参照。動かすのは PenguinMotion、組み立てるのは PenguinModelBuilder。
    /// 前は +Z、足元が原点。体全体（Figure）・丸い影・色の変え方は ToyModel
    /// </summary>
    public sealed class PenguinModel : ToyModel
    {
        /// <summary>胴・頭・ヒレをまとめた土台。足元を軸に揺らす・かがませる</summary>
        public Transform Body { get; private set; }
        public Transform Head { get; private set; }
        public Transform FlipperLeft { get; private set; }
        public Transform FlipperRight { get; private set; }
        public Transform FootLeft { get; private set; }
        public Transform FootRight { get; private set; }

        /// <summary>ひとみと目の光。まばたきで縦につぶす</summary>
        public IReadOnlyList<Transform> Eyes { get; private set; }

        /// <summary>胴の中心の高さ。転がる・倒れるときはここを軸に回す</summary>
        public float CenterHeight { get; private set; }

        /// <summary>お腹を下にして倒れたときの、胴の中心の高さ（お腹の厚み）</summary>
        public float LieHeight { get; private set; }

        internal void Assign(Transform body, Transform head, Transform flipperLeft, Transform flipperRight, Transform footLeft, Transform footRight)
        {
            Body = body;
            Head = head;
            FlipperLeft = flipperLeft;
            FlipperRight = flipperRight;
            FootLeft = footLeft;
            FootRight = footRight;
        }

        internal void AssignShape(IReadOnlyList<Transform> eyes, float centerHeight, float lieHeight)
        {
            Eyes = eyes;
            CenterHeight = centerHeight;
            LieHeight = lieHeight;
        }
    }
}
