using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 3D のペンギン1種類の見た目（色と体型）。見た目だけの値なので Core の SpeciesTable とは分けて持つ。
    /// フェーズ1で20種類の定義表（PenguinLooks）にする
    /// </summary>
    public sealed class PenguinLook
    {
        public Color Back = new Color(0.17f, 0.19f, 0.27f);
        public Color Belly = new Color(0.97f, 0.97f, 0.95f);
        public Color Beak = new Color(1.00f, 0.56f, 0.14f);
        public Color Feet = new Color(1.00f, 0.50f, 0.12f);
        public Color EyeRing = new Color(0.97f, 0.97f, 0.95f);

        /// <summary>胴の太さ・背の高さ・頭の大きさ（標準の体型に掛ける倍率）</summary>
        public float Width = 1f;
        public float Height = 1f;
        public float HeadSize = 1f;

        public static readonly PenguinLook Default = new PenguinLook();
    }
}
