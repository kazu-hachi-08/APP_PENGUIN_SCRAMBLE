using PenmonScramble.Baseball.Game;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球の試しの打席を、タイトルを飛ばして開く（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 2）。記録には何も書かない。
    /// 打つ: CPU が球種と曲げ方を選んで投げる球を、振る・バントで打つ。投げる: 球種を選んで投げて曲げ、CPU が打つ
    /// </summary>
    public static class BaseballDevMenu
    {
        [MenuItem(EditorMenu.Baseball + "試しの打席（打つ）", priority = EditorMenu.BaseballPriority)]
        public static void TestBat() => Open(BaseballTestSide.Bat);

        [MenuItem(EditorMenu.Baseball + "試しの打席（投げる）", priority = EditorMenu.BaseballPriority + 1)]
        public static void TestPitch() => Open(BaseballTestSide.Pitch);

        private static void Open(BaseballTestSide side)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(BaseballSceneBuilder.ScenePath);
            SessionState.SetInt(BaseballDevRequest.TestSideKey, (int)side);
            EditorApplication.EnterPlaymode();
        }
    }
}
