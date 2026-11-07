using System;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>特殊ショットの種類。属性ごとに1つ</summary>
    public enum GolfSpecialKind
    {
        None,
        /// <summary>氷: 球の通り道が凍り、止まりにくくなる。池・砂地の上も凍らせて渡れる</summary>
        IceRoad,
        /// <summary>水: 水切りのように跳ねて進む。低い壁・池を越える</summary>
        SkipBounce,
        /// <summary>風: 打つ前に選んだ右か左へ、大きく曲がって進む</summary>
        WindCurve,
        /// <summary>雷: 打った後にもう一度押すと、その場で一瞬加速して向きを少し変える</summary>
        ThunderBoost,
    }

    /// <summary>
    /// 1打の特殊ショット（種類・★・風の曲げる向き）。★は打つ子の★（1〜3）。
    /// 強さの数は GolfSpecialBalance。ふつうのショットは None
    /// </summary>
    public readonly struct GolfSpecial
    {
        public readonly GolfSpecialKind Kind;
        public readonly int Stars;

        /// <summary>風の曲げる向き。+1 で右（上から見て時計回り）、−1 で左。ほかの種類は 0</summary>
        public readonly int Curve;

        public GolfSpecial(GolfSpecialKind kind, int stars, int curve = 0)
        {
            Kind = kind;
            Stars = Math.Clamp(stars, 1, GolfSpecialBalance.MaxStars);
            Curve = kind == GolfSpecialKind.WindCurve ? (curve < 0 ? -1 : 1) : 0;
        }

        public static GolfSpecial None => default;

        public bool IsNone => Kind == GolfSpecialKind.None;

        /// <summary>打つ子の特殊ショット。curve は風のときだけ使う（右 +1 / 左 −1）</summary>
        public static GolfSpecial For(GolfMember member, int curve = 1)
        {
            return new GolfSpecial(KindOf(member.Element), member.Stars, curve);
        }

        public static GolfSpecialKind KindOf(Element element)
        {
            switch (element)
            {
                case Element.Ice: return GolfSpecialKind.IceRoad;
                case Element.Water: return GolfSpecialKind.SkipBounce;
                case Element.Wind: return GolfSpecialKind.WindCurve;
                default: return GolfSpecialKind.ThunderBoost;
            }
        }

        public static string NameOf(GolfSpecialKind kind)
        {
            switch (kind)
            {
                case GolfSpecialKind.IceRoad: return "アイスロード";
                case GolfSpecialKind.SkipBounce: return "水切りバウンド";
                case GolfSpecialKind.WindCurve: return "追い風カーブ";
                case GolfSpecialKind.ThunderBoost: return "イナズマブースト";
                default: return string.Empty;
            }
        }
    }

    /// <summary>
    /// 特殊ショットの強さの数（★1 / ★2 / ★3）。パワー（育成）は効かせない（オンラインで育成の差で勝負が決まらないように）
    /// </summary>
    public static class GolfSpecialBalance
    {
        public const int MaxStars = 3;

        // ---- 氷: アイスロード ----
        private static readonly float[] IceLengths = { 7f, 10f, 13f };

        /// <summary>凍った道の上の摩擦は何倍か（止まりにくい）</summary>
        public const float IceDecelScale = 0.3f;

        // ---- 水: 水切りバウンド ----
        private static readonly int[] SkipBouncesByStars = { 1, 2, 3 };

        /// <summary>
        /// 打ち出したときに上へ跳ぶ速さ（m/s）。頂点は 速さ² ÷（2 × 重力）＝ 約 0.46m で、低い壁（0.2m）は越え、高い壁（0.6m）は越えない
        /// </summary>
        public const float SkipLaunch = 3f;

        /// <summary>跳ねるたびに、上へ跳ぶ速さと前へ進む速さを何倍にするか</summary>
        public const float SkipVerticalKeep = 0.85f;
        public const float SkipForwardKeep = 0.85f;

        // ---- 風: 追い風カーブ ----
        // 曲がり方は「進んだ距離あたりの角度」で決める（打つ強さで道筋の形が変わらないので、ガイド線で曲がり方を見せられる）
        private static readonly float[] CurveRadiansPerMeter = { 0.10f, 0.14f, 0.18f };

        /// <summary>曲がりながら進む距離（m）。これより先はまっすぐ</summary>
        public const float CurveLength = 8f;

        // ---- 雷: イナズマブースト ----
        private static readonly float[] BoostSpeeds = { 3f, 4.5f, 6f };
        private static readonly float[] BoostTurnDegrees = { 20f, 35f, 50f };

        /// <summary>これより遅い球は加速できない（止まりかけで向きが決められないため）</summary>
        public const float BoostMinSpeed = 0.3f;

        public static float IceLength(int stars) => IceLengths[Index(stars)];
        public static int SkipBounces(int stars) => SkipBouncesByStars[Index(stars)];
        public static float Curvature(int stars) => CurveRadiansPerMeter[Index(stars)];
        public static float BoostSpeed(int stars) => BoostSpeeds[Index(stars)];
        public static float BoostMaxTurn(int stars) => BoostTurnDegrees[Index(stars)] * (MathF.PI / 180f);

        private static int Index(int stars) => Math.Clamp(stars, 1, MaxStars) - 1;
    }
}
