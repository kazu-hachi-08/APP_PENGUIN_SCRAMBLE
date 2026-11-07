using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーを、タイトルを飛ばして開く（開発用）。記録には何も書かない。
    /// 試しのピッチ: スタジアム1と同じ 3分の試合。スタジアムを試す: スタジアム2〜5の仕掛けと CPU の強さで（ストーリーを進めずに）。
    /// PK戦の確認: 試合を飛ばして、いきなり PK戦から
    /// </summary>
    public static class SoccerDevMenu
    {
        private const string StadiumMenu = EditorMenu.Soccer + "スタジアムを試す/";

        [MenuItem(EditorMenu.Soccer + "試しのピッチ", priority = EditorMenu.SoccerPriority)]
        public static void TestPitch() => Open(SoccerDevRequest.TestPitchKey, SoccerDevRequest.DefaultStadium);

        [MenuItem(EditorMenu.Soccer + "PK戦の確認", priority = EditorMenu.SoccerPriority + 1)]
        public static void Shootout() => Open(SoccerDevRequest.ShootoutKey, SoccerDevRequest.DefaultStadium);

        [MenuItem(StadiumMenu + "2 氷山", priority = EditorMenu.SoccerPriority + 2)]
        public static void Iceberg() => Open(SoccerDevRequest.TestPitchKey, 2);

        [MenuItem(StadiumMenu + "3 深海", priority = EditorMenu.SoccerPriority + 3)]
        public static void DeepSea() => Open(SoccerDevRequest.TestPitchKey, 3);

        [MenuItem(StadiumMenu + "4 嵐の崖", priority = EditorMenu.SoccerPriority + 4)]
        public static void StormCliff() => Open(SoccerDevRequest.TestPitchKey, 4);

        [MenuItem(StadiumMenu + "5 火山の入り江", priority = EditorMenu.SoccerPriority + 5)]
        public static void VolcanoCove() => Open(SoccerDevRequest.TestPitchKey, 5);

        private static void Open(string requestKey, int stadium)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(SoccerSceneBuilder.ScenePath);
            SessionState.SetBool(requestKey, true);
            SessionState.SetInt(SoccerDevRequest.StadiumKey, stadium);
            EditorApplication.EnterPlaymode();
        }
    }
}
