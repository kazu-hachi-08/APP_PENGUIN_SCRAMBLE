using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// 必殺シュートの調整値（案。フェーズ 7 の試遊で決め直す）。★（1〜3）で強さが変わる所は ★1・★2・★3 の順の表
    /// </summary>
    public static partial class SoccerBalance
    {
        // ---- チームのゲージ（0〜1。満タンで出せる）----

        /// <summary>ボールを持ったとき・スライディングで奪ったとき・味方のパスを受けたとき（持った分に足す）にたまる量</summary>
        public const float GaugePossess = 0.012f;
        public const float GaugeTackle = 0.05f;
        public const float GaugePassReceived = 0.02f;

        // ---- 出すとき ----

        /// <summary>カットインの長さ（本編の必殺技と同じ 0.6 秒）。この間は全体が止まる（試合の時間も減らない）</summary>
        public const float SpecialCutInSeconds = 0.6f;

        /// <summary>的はゴールの口の端からこれだけ内側（柱に当てない）</summary>
        public const float SpecialPostInset = 0.8f;

        /// <summary>必殺シュートとして飛ぶ、いちばん長い秒（その後はふつうのボールに戻る）</summary>
        public const float SpecialMaxSeconds = 2f;

        /// <summary>
        /// キーパーが必殺シュートを止める割合（★ごと。止めにくいが、必ず入るわけではない）。
        /// 球の速さ・キーパーからの距離では決めない（曲がる・跳ねる・消える球ごとに読みやすさが大きく違い、属性で決まる割合が偏ったため）
        /// </summary>
        private static readonly float[] KeeperSpecialSaveByStars = { 0.42f, 0.37f, 0.32f };

        public static float KeeperSpecialSave(int stars) => ByStars(KeeperSpecialSaveByStars, stars);

        // ---- 氷: フリーズショット（まっすぐ飛び、当たった相手を凍らせて貫く）----

        public const float IceShotSpeed = 24f;

        /// <summary>凍らせる秒（★で長くなる）</summary>
        private static readonly float[] IceFreezeSecondsByStars = { 1.2f, 1.7f, 2.2f };

        /// <summary>ボールの通った線からこの距離にいる相手を凍らせる</summary>
        public const float IceFreezeReach = PlayerRadius + BallRadius + 0.15f;

        // ---- 水: アクアバウンド（水切りのように地面で跳ねて進み、相手を飛び越える）----

        public const float WaterShotSpeed = 15f;

        /// <summary>重さの倍率（ふつうの重さでは、短い距離で何度も高く跳ねられないため）</summary>
        public const float WaterGravityScale = 3f;

        /// <summary>跳ねるたびに、次の山の長さ（宙にいる秒）を何倍にするか</summary>
        public const float WaterHopRatio = 0.65f;

        /// <summary>はじめの山の高さ（ボールの下の端）の範囲。上はバーの下を通る高さ</summary>
        public const float WaterMinPeak = 0.6f;
        public const float WaterMaxPeak = 1.05f;

        // ---- 風: トルネードカーブ（大きく曲がって回り込む）----

        public const float WindShotSpeed = 18f;

        /// <summary>蹴り出す向きを、的へのまっすぐな向きからずらす角度（度。★で大きく曲がる）。曲がり終わると的に着く</summary>
        private static readonly float[] WindCurveDegreesByStars = { 22f, 30f, 38f };

        // ---- 雷: イナズマショット（一瞬消えて、少し先から超高速で現れる）----

        /// <summary>消えている秒と、その間に進む距離（★で遠くまで）</summary>
        public const float ThunderHiddenSeconds = 0.2f;
        private static readonly float[] ThunderBlinkDistanceByStars = { 3.5f, 4.5f, 5.5f };

        /// <summary>現れた後の速さ（★で速い）</summary>
        private static readonly float[] ThunderSpeedByStars = { 25f, 28f, 31f };

        /// <summary>現れる所は、キーパーの線からこれより手前（消えたままゴールに入らないように）</summary>
        public const float ThunderAppearGap = 2.5f;

        public static float IceFreezeSeconds(int stars) => ByStars(IceFreezeSecondsByStars, stars);

        public static float WindCurveDegrees(int stars) => ByStars(WindCurveDegreesByStars, stars);

        public static float ThunderBlinkDistance(int stars) => ByStars(ThunderBlinkDistanceByStars, stars);

        public static float ThunderSpeed(int stars) => ByStars(ThunderSpeedByStars, stars);

        /// <summary>水切りの跳ねる回数（★1〜★3 で 1〜3 回）</summary>
        public static int WaterHops(int stars) => Math.Clamp(stars, 1, IceFreezeSecondsByStars.Length);

        private static float ByStars(float[] table, int stars) => table[Math.Clamp(stars, 1, table.Length) - 1];
    }

    /// <summary>必殺シュートの名前（カットイン・遊び方に出す）</summary>
    public static class SoccerSpecialNames
    {
        public static string Of(Element element)
        {
            switch (element)
            {
                case Element.Water: return "アクアバウンド";
                case Element.Wind: return "トルネードカーブ";
                case Element.Thunder: return "イナズマショット";
                default: return "フリーズショット";
            }
        }
    }
}
