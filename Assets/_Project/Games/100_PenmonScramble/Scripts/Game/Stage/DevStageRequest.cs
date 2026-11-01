using PenmonScramble.Core;

namespace PenmonScramble.Game
{
    /// <summary>
    /// エディタの Tools/PenmonScramble/Dev から「このステージを遊ぶ」と頼まれたかどうか。
    /// Play に入るとドメインが読み直されて static が消えるので、エディタの SessionState に預けて受け渡す。ビルドした版では何もしない
    /// </summary>
    public static class DevStageRequest
    {
        public const string StageKey = "PenmonScramble.DevStage.No";
        public const string DifficultyKey = "PenmonScramble.DevStage.Difficulty";

        /// <summary>頼まれていれば取り出して消す（Play し直したときに、また開発用のステージから始まらないように）</summary>
        public static bool TryTake(out int stageNo, out Difficulty difficulty)
        {
#if UNITY_EDITOR
            stageNo = UnityEditor.SessionState.GetInt(StageKey, 0);
            difficulty = (Difficulty)UnityEditor.SessionState.GetInt(DifficultyKey, 0);
            UnityEditor.SessionState.EraseInt(StageKey);
            UnityEditor.SessionState.EraseInt(DifficultyKey);
            return stageNo > 0;
#else
            stageNo = 0;
            difficulty = Difficulty.Normal;
            return false;
#endif
        }
    }
}
