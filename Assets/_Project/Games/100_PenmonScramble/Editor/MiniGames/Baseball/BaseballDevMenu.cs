using PenmonScramble.Baseball.Game;
using UnityEditor;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球の試しの打席・試しの試合を、タイトルを飛ばして開く。記録には何も書かない。
    /// 打つ / 投げる: 同じ側をくり返す（3アウトでアウトと走者を戻すだけ）。試合: 自分の3体 対 CPU の3体で3イニング（先攻か後攻か）
    /// </summary>
    public static class BaseballDevMenu
    {
        [MenuItem(EditorMenu.Baseball + "試しの打席（打つ）", priority = EditorMenu.BaseballPriority)]
        public static void TestBat() => Open(BaseballPlayMode.PracticeBat);

        [MenuItem(EditorMenu.Baseball + "試しの打席（投げる）", priority = EditorMenu.BaseballPriority + 1)]
        public static void TestPitch() => Open(BaseballPlayMode.PracticePitch);

        [MenuItem(EditorMenu.Baseball + "試しの試合（先攻）", priority = EditorMenu.BaseballPriority + 2)]
        public static void MatchAway() => Open(BaseballPlayMode.MatchAway);

        [MenuItem(EditorMenu.Baseball + "試しの試合（後攻）", priority = EditorMenu.BaseballPriority + 3)]
        public static void MatchHome() => Open(BaseballPlayMode.MatchHome);

        private static void Open(BaseballPlayMode mode) =>
            DevStageMenu.OpenTestScene(BaseballSceneBuilder.ScenePath, () => SessionState.SetInt(BaseballDevRequest.ModeKey, (int)mode));
    }
}
