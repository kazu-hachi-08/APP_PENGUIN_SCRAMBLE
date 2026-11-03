namespace PenmonScramble.Golf.Core
{
    /// <summary>1ホールの打数の呼び名（ホールインワン・バーディー・パー・ボギー…）。カップインの知らせとスコアカードで使う</summary>
    public static class GolfScoreNames
    {
        public static string ForStrokes(int strokes, int par)
        {
            if (strokes == 1)
            {
                return "ホールインワン";
            }

            switch (strokes - par)
            {
                case -3: return "アルバトロス";
                case -2: return "イーグル";
                case -1: return "バーディー";
                case 0: return "パー";
                case 1: return "ボギー";
                case 2: return "ダブルボギー";
                case 3: return "トリプルボギー";
                default: return strokes < par ? $"{par - strokes} アンダー" : $"{strokes - par} オーバー";
            }
        }

        /// <summary>パーとの差（−1・±0・+2）。表示用</summary>
        public static string Difference(int strokes, int par)
        {
            var diff = strokes - par;
            return diff == 0 ? "±0" : diff > 0 ? $"+{diff}" : $"−{-diff}";
        }
    }
}
