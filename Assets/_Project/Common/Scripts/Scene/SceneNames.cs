
namespace PenguinScramble.Common.Scene
{
    /// <summary>
    /// シーン名を型安全に管理する定数クラス
    /// </summary>
    public static class SceneNames
    {
        /// <summary>ゲーム本体（タイトルもこのシーンの中にある）</summary>
        public const string PenguinScramble = "PenguinScrambleScene";

        /// <summary>「タイトルへ戻る」の行き先。タイトル専用のシーンは無いので、ゲームのシーンを読み直す</summary>
        public const string Title = PenguinScramble;
    }
}
