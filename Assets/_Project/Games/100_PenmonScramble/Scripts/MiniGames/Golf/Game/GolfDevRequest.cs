namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// エディタの Tools/PenmonScramble/Golf から「試しのホールを開く」と頼まれたかどうか。
    /// Play に入るとドメインが読み直されて static が消えるので、エディタの SessionState に預けて受け渡す。ビルドした版では何もしない
    /// </summary>
    public static class GolfDevRequest
    {
        public const string PlayersKey = "PenmonScramble.Golf.TestHole.Players";

        /// <summary>頼まれていれば人数を取り出して消す（Play し直したときに、また試しのホールから始まらないように）</summary>
        public static bool TryTakeTestHole(out int players)
        {
#if UNITY_EDITOR
            players = UnityEditor.SessionState.GetInt(PlayersKey, 0);
            UnityEditor.SessionState.EraseInt(PlayersKey);
            return players > 0;
#else
            players = 0;
            return false;
#endif
        }
    }
}
