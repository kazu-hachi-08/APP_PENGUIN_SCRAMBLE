namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// エディタの Tools/PenmonScramble/Baseball から「試しの打席を開く」（打つ側か投げる側か）と頼まれたかどうか。
    /// Play に入るとドメインが読み直されて static が消えるので、エディタの SessionState に預けて受け渡す。ビルドした版では何もしない
    /// </summary>
    public static class BaseballDevRequest
    {
        public const string TestSideKey = "PenmonScramble.Baseball.TestSide";

        private const int None = -1;

        /// <summary>頼まれていれば消して true（Play し直したときに、また試しの打席から始まらないように）</summary>
        public static bool TryTakeTestAtBat(out BaseballTestSide side)
        {
#if UNITY_EDITOR
            var requested = UnityEditor.SessionState.GetInt(TestSideKey, None);
            UnityEditor.SessionState.EraseInt(TestSideKey);
            side = requested == (int)BaseballTestSide.Pitch ? BaseballTestSide.Pitch : BaseballTestSide.Bat;
            return requested != None;
#else
            side = BaseballTestSide.Bat;
            return false;
#endif
        }
    }
}
