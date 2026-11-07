using PenmonScramble.Golf.Game;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ゴルフの試しの1ホールを、タイトルを飛ばして開く。
    /// 2人の方は、1つの手元で交代に打って、球の弾き合いと打順を確かめる用。記録には何も書かない
    /// </summary>
    public static class GolfDevMenu
    {
        [MenuItem(EditorMenu.Golf + "試しのホール", priority = EditorMenu.GolfPriority)]
        public static void TestHoleSolo() => Play(1);

        [MenuItem(EditorMenu.Golf + "試しのホール（2人で交代）", priority = EditorMenu.GolfPriority + 1)]
        public static void TestHoleTwo() => Play(2);

        private static void Play(int players)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(GolfSceneBuilder.ScenePath);
            SessionState.SetInt(GolfDevRequest.PlayersKey, players);
            EditorApplication.EnterPlaymode();
        }
    }
}
