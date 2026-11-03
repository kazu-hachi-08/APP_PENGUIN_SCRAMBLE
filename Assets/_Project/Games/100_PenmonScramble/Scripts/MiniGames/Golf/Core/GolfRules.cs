namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// ゴルフのルールの数値。球の計算・打順・特殊ショットはフェーズ 2 以降でこの Core に足す
    /// （Docs/MINIGAME_GOLF_PLAN.md）
    /// </summary>
    public static class GolfRules
    {
        /// <summary>1コースのホールの数（1回の遊び＝1コース）</summary>
        public const int HolesPerCourse = 3;
    }
}
