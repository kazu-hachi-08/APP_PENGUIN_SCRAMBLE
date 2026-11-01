using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 開発用のステージ（DevStageTable）を、タイトルを飛ばしてひとりで遊ぶ。ボスラッシュの土台（闘技場の連戦・巨大ペンモン・本気・難易度）と、新しい大ボスを確かめる用。
    /// 記録には何も書かない（StageRunner.RecordResult）
    /// </summary>
    public static class DevStageMenu
    {
        [MenuItem(EditorMenu.Dev + "闘技場3連戦（ふつう）", priority = EditorMenu.DevPriority)]
        public static void ArenaGauntletNormal() => Play(DevStageTable.ArenaGauntletNo, Difficulty.Normal);

        [MenuItem(EditorMenu.Dev + "闘技場3連戦（むずかしい）", priority = EditorMenu.DevPriority + 1)]
        public static void ArenaGauntletHard() => Play(DevStageTable.ArenaGauntletNo, Difficulty.Hard);

        [MenuItem(EditorMenu.Dev + "闘技場3連戦（げきむず）", priority = EditorMenu.DevPriority + 2)]
        public static void ArenaGauntletExtreme() => Play(DevStageTable.ArenaGauntletNo, Difficulty.Extreme);

        [MenuItem(EditorMenu.Dev + "新しい大ボス（ふつう）", priority = EditorMenu.DevPriority + 20)]
        public static void NewBossesNormal() => Play(DevStageTable.NewBossesNo, Difficulty.Normal);

        [MenuItem(EditorMenu.Dev + "新しい大ボス（げきむず）", priority = EditorMenu.DevPriority + 21)]
        public static void NewBossesExtreme() => Play(DevStageTable.NewBossesNo, Difficulty.Extreme);

        private static void Play(int stageNo, Difficulty difficulty)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(PenmonScrambleSceneBuilder.ScenePath);
            SessionState.SetInt(DevStageRequest.StageKey, stageNo);
            SessionState.SetInt(DevStageRequest.DifficultyKey, (int)difficulty);
            EditorApplication.EnterPlaymode();
        }
    }
}
