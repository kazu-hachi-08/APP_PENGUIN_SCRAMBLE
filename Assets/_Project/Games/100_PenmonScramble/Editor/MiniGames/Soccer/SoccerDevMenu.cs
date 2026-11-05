using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーの試しのピッチを、タイトルを飛ばして開く（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 1）。
    /// 仮の見た目の1体でボールを運んでゴールへ蹴り込む。記録には何も書かない
    /// </summary>
    public static class SoccerDevMenu
    {
        [MenuItem(EditorMenu.Soccer + "試しのピッチ", priority = EditorMenu.SoccerPriority)]
        public static void TestPitch()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(SoccerSceneBuilder.ScenePath);
            SessionState.SetBool(SoccerDevRequest.TestPitchKey, true);
            EditorApplication.EnterPlaymode();
        }
    }
}
