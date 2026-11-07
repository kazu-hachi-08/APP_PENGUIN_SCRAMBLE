using System;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 魔球・必殺打の調整値（Docs/MINIGAME_BASEBALL_PLAN.md「魔球と必殺打」）。★（1〜3）で強さが変わる所は ★1・★2・★3 の順の表。
    /// 釣り合いは CPU の試合の確認（Tools/PenmonScramble/Baseball/CPUの試合の確認）で決めた
    /// </summary>
    public static partial class BaseballBalance
    {
        /// <summary>カットインの長さ（本編の必殺技と同じ 0.6 秒）。この間は盤が止まる</summary>
        public const float SpecialCutInSeconds = 0.6f;

        // ---- 魔球: 氷 フリーズボール（途中で一瞬凍って止まり、また動き出す）----

        /// <summary>
        /// 道のりのこの割合で凍る。曲げられる区間の後（まっすぐ来る区間）で止めて、打つ人の取ったタイミングを崩す
        /// （曲げられる区間の中で止めると、動き出した後に読み直す間があって崩れないため）
        /// </summary>
        public const float FreezeAt = 0.8f;

        /// <summary>凍って止まる秒の幅（★が高いほど幅が広く、いつ動き出すかを読みにくい）</summary>
        private static readonly float[] FreezeMinSecondsByStars = { 0.3f, 0.25f, 0.2f };
        private static readonly float[] FreezeMaxSecondsByStars = { 0.45f, 0.6f, 0.75f };

        // ---- 魔球: 水 ゆらゆらボール（波のように左右に揺れて進む）----

        /// <summary>揺れる幅（片側。★で大きく）</summary>
        private static readonly float[] WobbleAmplitudeByStars = { 0.22f, 0.3f, 0.38f };

        /// <summary>マウンドからホームまでに揺れる回数</summary>
        public const float WobbleCycles = 2.5f;

        // ---- 魔球: 風 旋風ボール（曲げる操作が大きく効き、最後に逆へ曲がる）----

        /// <summary>曲げられる区間の、曲げる入力の効き（加速と横の速さの上限）の倍率</summary>
        public const float WindBendScale = 1.7f;

        /// <summary>曲げられる区間が終わったら、それまでと逆へ曲がる横の速さ（★で大きく）</summary>
        private static readonly float[] WindReverseSpeedByStars = { 1.0f, 1.4f, 1.8f };

        // ---- 魔球: 雷 消える魔球（途中で消えて、ホームの手前で現れる）----

        /// <summary>道のりのこの割合で消える</summary>
        public const float VanishAt = 0.3f;

        /// <summary>道のりのこの割合で現れる（★で遠くまで消えている）</summary>
        private static readonly float[] AppearAtByStars = { 0.72f, 0.8f, 0.86f };

        // ---- 必殺打: 氷 アイスロード（打球の通り道が凍り、アウトの穴の上を滑って渡る）----

        /// <summary>凍る道の長さ（転がった道のり。★で長く）</summary>
        private static readonly float[] IceRoadLengthByStars = { 5f, 7f, 9f };

        /// <summary>凍った道の上の摩擦の倍率（よく滑る）</summary>
        public const float IceRoadFriction = 0.45f;

        // ---- 必殺打: 水 水切りヒット（水切りのように跳ねて穴を飛び越える）----

        /// <summary>跳ねる上向きの速さ（はじめの山。跳ねるたびに WaterHopDecay 倍）</summary>
        public const float WaterHopSpeed = 3.0f;
        public const float WaterHopDecay = 0.8f;

        /// <summary>跳ねるたびに残る横の速さ</summary>
        public const float WaterHopKeep = 0.92f;

        // ---- 必殺打: 風 追い風ヒット（打った後にスティックで打球を左右に曲げられる）----

        /// <summary>曲げる入力いっぱいのときの横の加速（毎秒。速さは変えず向きだけ変える。★で大きく）</summary>
        private static readonly float[] WindSteerAccelByStars = { 9f, 13f, 17f };

        /// <summary>打ってからこの秒の間だけ曲げられる</summary>
        public const float WindSteerSeconds = 2f;

        // ---- 必殺打: 雷 イナズマ打ち（とても速い打球。穴の上を通っても落ちず、フェンスを越えやすい）----

        /// <summary>打球の横の速さの倍率（★で速い）</summary>
        private static readonly float[] ThunderSpeedScaleByStars = { 1.15f, 1.25f, 1.35f };

        /// <summary>この速さより遅くなったら、イナズマ打ちも穴に落ちる（転がり切ってから穴の上で止まったのまで渡らせない）</summary>
        public const float ThunderHoleSpeed = 3f;

        // ---- CPU ----

        /// <summary>魔球が残っている投げる子が、1球ごとに魔球を出す割合（ふだん・ピンチ）</summary>
        public const float AiMagicChance = 0.08f;
        public const float AiMagicPinchChance = 0.5f;

        /// <summary>必殺打が残っている打つ子が、1球ごとに必殺打を出す割合（ふだん・チャンス）</summary>
        public const float AiSpecialChance = 0.12f;
        public const float AiSpecialChanceChance = 0.5f;

        /// <summary>最後のイニングは出す割合をこれだけ増やす（残したまま試合を終えないように）</summary>
        public const float AiLastInningScale = 2.5f;

        /// <summary>
        /// 魔球ごとの打ちにくさ（打つ CPU の振る時刻のぶれに掛ける）。ゆらゆらボールは揺れで、消える魔球は現れてから読むので、タイミングが合いにくい
        /// （フリーズボールは凍った球に取ったタイミングのまま振ってしまうこと、旋風ボールは逆へ曲がるのを読めないことで崩れるので 1）
        /// </summary>
        public static float AiMagicDifficulty(BaseballSpecial magic)
        {
            if (!magic.IsActive)
            {
                return 1f;
            }

            return magic.Element switch
            {
                Element.Water => 2.6f,
                Element.Thunder => 2.8f,
                _ => 1f,
            };
        }

        /// <summary>旋風ボールは、ゾーンの外へこれだけ逃がしてから逆へ曲げて戻す</summary>
        public const float AiWindOvershoot = 0.35f;

        /// <summary>追い風ヒットで、ねらうヒットの穴との向きの差（度）をこれだけで曲げる入力いっぱいにする</summary>
        public const float AiSteerFullDegrees = 8f;

        public static float FreezeMinSeconds(int stars) => ByStars(FreezeMinSecondsByStars, stars);
        public static float FreezeMaxSeconds(int stars) => ByStars(FreezeMaxSecondsByStars, stars);
        public static float WobbleAmplitude(int stars) => ByStars(WobbleAmplitudeByStars, stars);
        public static float WindReverseSpeed(int stars) => ByStars(WindReverseSpeedByStars, stars);
        public static float AppearAt(int stars) => ByStars(AppearAtByStars, stars);
        public static float IceRoadLength(int stars) => ByStars(IceRoadLengthByStars, stars);
        public static float WindSteerAccel(int stars) => ByStars(WindSteerAccelByStars, stars);
        public static float ThunderSpeedScale(int stars) => ByStars(ThunderSpeedScaleByStars, stars);

        /// <summary>水切りヒットの跳ねる回数（★の数だけ）</summary>
        public static int WaterHops(int stars) => Math.Clamp(stars, BaseballSpecial.MinStars, BaseballSpecial.MaxStars);

        private static float ByStars(float[] table, int stars) => table[Math.Clamp(stars, BaseballSpecial.MinStars, BaseballSpecial.MaxStars) - BaseballSpecial.MinStars];
    }
}
