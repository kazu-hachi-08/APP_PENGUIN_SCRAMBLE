using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 闘技場の柵の形と色。ストーリーの闘技場は、おもちゃの丸い柵（細い柱・玉・丸い横木）。
    /// ボスラッシュの闘技場（コロシアム）は、太い石の柱と低い石の壁、奥の柱に旗の飾り。
    /// 手前の柵はペンギンの足元を隠さないよう、どちらも奥より低くする
    /// </summary>
    public sealed class ArenaFenceStyle
    {
        // コロシアムの石の色。ワールドの柵の色を少しだけ混ぜて、闘技場ごとの色味を残す
        private static readonly Color StoneColor = new Color(0.86f, 0.82f, 0.74f);
        private static readonly Color WallStoneColor = new Color(0.62f, 0.58f, 0.53f);
        private const float StoneThemeMix = 0.25f;

        /// <summary>ストーリーの闘技場（おもちゃの丸い柵）</summary>
        public static readonly ArenaFenceStyle Toy = new ArenaFenceStyle(false, spacing: 1.1f,
            postDiameter: 0.32f, postHeight: 0.5f, frontPostHeight: 0.5f, capDiameter: 0.34f, capHeight: 0f,
            railThickness: 0.16f, railHeight: 0.3f, bannerEvery: 0);

        /// <summary>ボスラッシュのコロシアム（太い石の柱・低い石の壁・旗）</summary>
        public static readonly ArenaFenceStyle Colosseum = new ArenaFenceStyle(true, spacing: 1.6f,
            postDiameter: 0.5f, postHeight: 0.75f, frontPostHeight: 0.55f, capDiameter: 0.66f, capHeight: 0.12f,
            railThickness: 0.3f, railHeight: 0.2f, bannerEvery: 4);

        private ArenaFenceStyle(bool stone, float spacing, float postDiameter, float postHeight, float frontPostHeight, float capDiameter, float capHeight,
            float railThickness, float railHeight, int bannerEvery)
        {
            IsStone = stone;
            Spacing = spacing;
            PostDiameter = postDiameter;
            PostHeight = postHeight;
            FrontPostHeight = frontPostHeight;
            CapDiameter = capDiameter;
            CapHeight = capHeight;
            RailThickness = railThickness;
            RailHeight = railHeight;
            BannerEvery = bannerEvery;
        }

        public bool IsStone { get; }

        /// <summary>柱の間（マス）</summary>
        public float Spacing { get; }
        public float PostDiameter { get; }

        /// <summary>奥（闘技場のまん中より上）の柱の高さ</summary>
        public float PostHeight { get; }

        /// <summary>手前の柱の高さ。手前のペンギンの足元を隠さないよう低くする</summary>
        public float FrontPostHeight { get; }
        public float CapDiameter { get; }

        /// <summary>柱の上の飾りの厚み。0 なら丸い玉、それより大きければ平たい石の笠</summary>
        public float CapHeight { get; }
        public float RailThickness { get; }

        /// <summary>横木（石の壁）の芯の高さ</summary>
        public float RailHeight { get; }

        /// <summary>奥の柱の、この本数ごとに旗を立てる。0 なら立てない</summary>
        public int BannerEvery { get; }

        // 旗: 細い竿の先に、縦長の旗を横向き（カメラから見える向き）に下げる
        public const float BannerPoleDiameter = 0.08f;
        public const float BannerPoleHeight = 1.5f;
        public static readonly Vector3 BannerSize = new Vector3(0.5f, 0.6f, 0.06f);

        public Color PostColor(StageTheme theme) => IsStone ? Color.Lerp(StoneColor, theme.FencePost, StoneThemeMix) : theme.FencePost;

        public Color RailColor(StageTheme theme) => IsStone ? Color.Lerp(WallStoneColor, theme.FenceRail, StoneThemeMix) : theme.FenceRail;

        /// <summary>柱の上の飾り。石の笠は柱と同じ石の色を明るくし、おもちゃの玉は1本おきに2色</summary>
        public Color CapColor(StageTheme theme, bool alt)
        {
            if (IsStone)
            {
                return Color.Lerp(PostColor(theme), Color.white, 0.3f);
            }

            return alt ? theme.FenceAltCap : theme.FenceCap;
        }

        /// <summary>旗はワールドの玉の2色を交互に使う（ワールドの色を一番強く見せる所）</summary>
        public static Color BannerColor(StageTheme theme, bool alt) => alt ? theme.FenceAltCap : theme.FenceCap;
    }
}
