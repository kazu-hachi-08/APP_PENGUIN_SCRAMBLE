namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// エディタの Tools/PenmonScramble/Baseball から「試しの打席・試しの試合を開く」と頼まれたかどうか（どの遊び方か）。
    /// Play に入るとドメインが読み直されて static が消えるので、エディタの SessionState に預けて受け渡す。ビルドした版では何もしない
    /// </summary>
    public static class BaseballDevRequest
    {
        public const string ModeKey = "PenmonScramble.Baseball.TestMode";

        private const int None = -1;

        /// <summary>頼まれていれば消して true（Play し直したときに、また試しの打席から始まらないように）</summary>
        public static bool TryTake(out BaseballPlayMode mode)
        {
#if UNITY_EDITOR
            var requested = UnityEditor.SessionState.GetInt(ModeKey, None);
            UnityEditor.SessionState.EraseInt(ModeKey);
            mode = System.Enum.IsDefined(typeof(BaseballPlayMode), requested) ? (BaseballPlayMode)requested : BaseballPlayMode.PracticeBat;
            return requested != None;
#else
            mode = BaseballPlayMode.PracticeBat;
            return false;
#endif
        }
    }
}
