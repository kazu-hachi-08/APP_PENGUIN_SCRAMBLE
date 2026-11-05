namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// エディタの Tools/PenmonScramble/Soccer から「試しのピッチを開く」と頼まれたかどうか。
    /// Play に入るとドメインが読み直されて static が消えるので、エディタの SessionState に預けて受け渡す。ビルドした版では何もしない
    /// </summary>
    public static class SoccerDevRequest
    {
        public const string TestPitchKey = "PenmonScramble.Soccer.TestPitch";

        /// <summary>頼まれていれば消して true（Play し直したときに、また試しのピッチから始まらないように）</summary>
        public static bool TryTakeTestPitch()
        {
#if UNITY_EDITOR
            var requested = UnityEditor.SessionState.GetBool(TestPitchKey, false);
            UnityEditor.SessionState.EraseBool(TestPitchKey);
            return requested;
#else
            return false;
#endif
        }
    }
}
