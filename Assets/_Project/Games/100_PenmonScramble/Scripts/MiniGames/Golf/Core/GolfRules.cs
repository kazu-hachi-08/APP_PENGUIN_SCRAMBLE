namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// ゴルフのルールの数値（打順・時間・上限）。球の動きの数値は GolfBalance（Docs/MINIGAME_GOLF_PLAN.md）
    /// </summary>
    public static class GolfRules
    {
        /// <summary>1コースのホールの数（1回の遊び＝1コース）</summary>
        public const int HolesPerCourse = 3;

        /// <summary>打つ子の数（本編の編成の3体）</summary>
        public const int TeamSize = 3;

        /// <summary>一緒に回れる最大の人数（オンライン）</summary>
        public const int MaxPlayers = 4;

        /// <summary>1打の持ち時間（秒）。過ぎたら自動で打つ（待たせすぎない・通信が切れかけでも進む）</summary>
        public const float ShotSeconds = 20f;

        /// <summary>持ち時間が過ぎたときの強さ（いま向いている向きで打つ）</summary>
        public const float AutoShotPower = 0.5f;

        /// <summary>打数の上限 ＝ パー ＋ これ。上限まで打っても入らなければ「上限 ＋ 1」打で終わる</summary>
        public const int StrokeLimitOverPar = 4;

        /// <summary>ティーに並べる球どうしの間（m）。重ならないよう、カップの方向と直角にずらす</summary>
        public const float TeeSpacing = 0.55f;

        /// <summary>
        /// パワーゲージが 0 → 1 → 0 と1往復する秒数。止める間合いはオンラインで全員同じにしたいので Core に置く
        /// </summary>
        public const float GaugeCycleSeconds = 1.6f;

        /// <summary>編成が空のとき（記録が壊れた等）に打つ子（アデリン・ジェンツ・オヤブン。タイトルのジオラマと同じ）</summary>
        public static readonly int[] DefaultSpecies = { 1, 3, 5 };
    }
}
