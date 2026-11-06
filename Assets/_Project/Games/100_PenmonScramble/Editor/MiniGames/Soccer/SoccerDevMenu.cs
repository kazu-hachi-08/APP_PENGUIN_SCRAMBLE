using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーを、タイトルを飛ばして開く（開発用）。記録には何も書かない。
    /// 試しのピッチ: スタジアム1と同じ 3分の試合。PK戦の確認: 試合を飛ばして、いきなり PK戦から
    /// </summary>
    public static class SoccerDevMenu
    {
        [MenuItem(EditorMenu.Soccer + "試しのピッチ", priority = EditorMenu.SoccerPriority)]
        public static void TestPitch() => Open(SoccerDevRequest.TestPitchKey);

        [MenuItem(EditorMenu.Soccer + "PK戦の確認", priority = EditorMenu.SoccerPriority + 1)]
        public static void Shootout() => Open(SoccerDevRequest.ShootoutKey);

        private static void Open(string requestKey)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(SoccerSceneBuilder.ScenePath);
            SessionState.SetBool(requestKey, true);
            EditorApplication.EnterPlaymode();
        }
    }
}
