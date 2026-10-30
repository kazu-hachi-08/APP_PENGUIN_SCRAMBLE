using PenmonScramble.Game;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// Editor の Play で重さの計測（StageBenchmark）を走らせる。Editor の fps は端末の目安にならないので、
    /// 描画の回数・三角形の数を見る用。端末の fps は Tools/PenmonScramble/Build/Benchmark のビルドで測る
    /// </summary>
    public static class BenchmarkMenu
    {
        [MenuItem("Tools/PenmonScramble/Benchmark")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            PenmonScrambleSceneBuilder.BuildScene();
            SessionState.SetBool(StageBenchmark.EditorRequestKey, true);
            EditorApplication.EnterPlaymode();
        }
    }
}
