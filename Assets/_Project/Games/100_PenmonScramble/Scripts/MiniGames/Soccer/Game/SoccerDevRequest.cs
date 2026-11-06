namespace PenmonScramble.Soccer.Game
{
    /// <summary>開発用メニューから開くときの遊び方</summary>
    public enum SoccerDevMode
    {
        None,
        /// <summary>試しのピッチ: スタジアム1と同じ試合を、記録に書かずに遊ぶ</summary>
        TestPitch,
        /// <summary>PK戦の確認: 試合を飛ばして、いきなり PK戦から（記録に書かない）</summary>
        Shootout,
    }

    /// <summary>
    /// エディタの Tools/PenmonScramble/Soccer から「試しのピッチ」「PK戦の確認」を頼まれたかどうか。
    /// Play に入るとドメインが読み直されて static が消えるので、エディタの SessionState に預けて受け渡す。ビルドした版では何もしない
    /// </summary>
    public static class SoccerDevRequest
    {
        public const string TestPitchKey = "PenmonScramble.Soccer.TestPitch";
        public const string ShootoutKey = "PenmonScramble.Soccer.Shootout";

        /// <summary>頼まれていれば消して返す（Play し直したときに、また開発用の遊び方から始まらないように）</summary>
        public static SoccerDevMode Take()
        {
#if UNITY_EDITOR
            var shootout = UnityEditor.SessionState.GetBool(ShootoutKey, false);
            var testPitch = UnityEditor.SessionState.GetBool(TestPitchKey, false);
            UnityEditor.SessionState.EraseBool(ShootoutKey);
            UnityEditor.SessionState.EraseBool(TestPitchKey);
            return shootout ? SoccerDevMode.Shootout : testPitch ? SoccerDevMode.TestPitch : SoccerDevMode.None;
#else
            return SoccerDevMode.None;
#endif
        }
    }
}
