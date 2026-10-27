namespace PenmonScramble.Common.Profile
{
    /// <summary>
    /// 「席N の表示名」表。各ゲームは席番号から名前を引くだけにして、ローカル/オンラインの違いはここで吸収する。
    /// 名前が入っていない席は「P1」「P2」… を返す。
    /// </summary>
    public static class SeatNames
    {
        private static string[] _names = System.Array.Empty<string>();

        /// <summary>オンライン。null や届いていない席は「P{n}」になる</summary>
        public static void UseOnline(string[] names)
        {
            _names = names ?? System.Array.Empty<string>();
        }
    }
}
