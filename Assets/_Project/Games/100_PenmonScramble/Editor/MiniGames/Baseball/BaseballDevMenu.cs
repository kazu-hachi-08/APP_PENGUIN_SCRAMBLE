using PenmonScramble.Baseball.Game;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球の試しの打席を、タイトルを飛ばして開く（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 1）。
    /// 仮の見た目の盤で、CPU がくり返し投げる球を振って打つ。記録には何も書かない
    /// </summary>
    public static class BaseballDevMenu
    {
        [MenuItem(EditorMenu.Baseball + "試しの打席", priority = EditorMenu.BaseballPriority)]
        public static void TestAtBat()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(BaseballSceneBuilder.ScenePath);
            SessionState.SetBool(BaseballDevRequest.TestAtBatKey, true);
            EditorApplication.EnterPlaymode();
        }
    }
}
