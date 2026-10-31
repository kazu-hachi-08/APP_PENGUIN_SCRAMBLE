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

        /// <summary>ひとりで遊ぶ。席0 だけが自分の名前になる（直前の協力の名前を残さないように丸ごと入れ替える）</summary>
        public static void UseSolo(string name)
        {
            _names = new[] { name };
        }

        /// <summary>席の表示名。名前が無ければ「P{席+1}」</summary>
        public static string Get(int seat)
        {
            var name = seat >= 0 && seat < _names.Length ? _names[seat] : null;
            return string.IsNullOrEmpty(name) ? $"P{seat + 1}" : name;
        }
    }
}
