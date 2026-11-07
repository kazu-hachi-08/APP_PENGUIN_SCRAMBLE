namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// エディタの Tools/PenmonScramble/Baseball から「試しの打席を開く」と頼まれたかどうか。
    /// Play に入るとドメインが読み直されて static が消えるので、エディタの SessionState に預けて受け渡す。ビルドした版では何もしない
    /// </summary>
    public static class BaseballDevRequest
    {
        public const string TestAtBatKey = "PenmonScramble.Baseball.TestAtBat";

        /// <summary>頼まれていれば消して true（Play し直したときに、また試しの打席から始まらないように）</summary>
        public static bool TryTakeTestAtBat()
        {
#if UNITY_EDITOR
            var requested = UnityEditor.SessionState.GetBool(TestAtBatKey, false);
            UnityEditor.SessionState.EraseBool(TestAtBatKey);
            return requested;
#else
            return false;
#endif
        }
    }
}
