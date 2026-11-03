using System.Linq;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Golf.Game;
using PenmonScramble.MiniGames.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ゴルフのシーン（GolfScene）をコードから生成する。シーンは生成物なので手で編集せず、ここを直して Rebuild する
    /// （Rebuild は本編のシーンの後にこれを呼ぶ。ボタンの絵などは本編の SceneBuilder が描いた物を使う）。
    /// ミニゲーム画面から来たときの「準備中」の板と、1ホールを遊ぶ部品（3D・カメラ・操作・HUD。いまは開発用の試しのホールだけで動く）を置く
    /// （Docs/MINIGAME_GOLF_PLAN.md）
    /// </summary>
    public static partial class GolfSceneBuilder
    {
        public static readonly string ScenePath = "Assets/_Project/Games/100_PenmonScramble/Scenes/" + MiniGameTable.Get(MiniGameTable.GolfId).SceneName + ".unity";

        private const float CameraFarClip = 200f;
        private const string Heading = "ペンモンゴルフ";
        private const string ComingSoon = "コースは準備中です";
        private static readonly Vector2 HeadingPosition = new Vector2(0f, 160f);
        private static readonly Vector2 HeadingSize = new Vector2(1200f, 140f);
        private const int HeadingFontSize = 96;
        private static readonly Vector2 CoursePosition = new Vector2(0f, 30f);
        private static readonly Vector2 CourseSize = new Vector2(1200f, 80f);
        private const int CourseFontSize = 52;
        private static readonly Vector2 ComingSoonPosition = new Vector2(0f, -60f);
        private static readonly Vector2 ComingSoonSize = new Vector2(1200f, 70f);
        private const int ComingSoonFontSize = 40;
        private static readonly Vector2 BackPosition = new Vector2(0f, -260f);
        private static readonly Vector2 BackSize = new Vector2(360f, 130f);
        private const int BackFontSize = 52;

        /// <summary>バッチモードからも呼べるよう、保存の確認はしない（呼ぶ側の Rebuild が済ませている）</summary>
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = PenmonScrambleSceneBuilder.CreateMainCamera(null, CameraFarClip);
            PenmonScrambleSceneBuilder.CreateSunLight("Sun", null, Quaternion.Euler(PenmonScrambleSceneBuilder.SunAngles), LightShadows.None);
            PenmonScrambleSceneBuilder.CreateEventSystem();
            var canvas = PenmonScrambleSceneBuilder.CreateCanvas().transform;
            // 顔の絵と地面の絵は本編と同じもの（本編の SceneBuilder が先に取り込んでいる）
            PenmonScrambleSceneBuilder.CreateCreatureArt();

            var placeholder = CreatePlaceholder(canvas, out var course, out var back);
            var hud = CreateHud(canvas);
            var play = CreatePlay(camera, hud);
            CreateInputManager(hud.Shoot);

            var runner = new GameObject("Golf").AddComponent<GolfSceneRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("_placeholder").objectReferenceValue = placeholder;
            so.FindProperty("_courseText").objectReferenceValue = course;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_playBackButton").objectReferenceValue = hud.Back;
            so.FindProperty("_testHole").objectReferenceValue = play;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[GolfSceneBuilder] {ScenePath} を生成しました");
        }

        /// <summary>ミニゲーム画面から来たときの板（コースの名前・準備中・もどる）。試しのホールでは隠す</summary>
        private static GameObject CreatePlaceholder(Transform canvas, out UnityEngine.UI.Text course, out UnityEngine.UI.Button back)
        {
            var group = UIDialogBuilder.CreateUIObject("Placeholder", canvas);
            UIDialogBuilder.SetStretchAll(group.GetComponent<RectTransform>());
            var parent = group.transform;
            PenmonScrambleSceneBuilder.CreateLabel(parent, "Heading", Heading, HeadingFontSize, HeadingPosition, HeadingSize).fontStyle = FontStyle.Bold;
            course = PenmonScrambleSceneBuilder.CreateLabel(parent, "Course", string.Empty, CourseFontSize, CoursePosition, CourseSize);
            PenmonScrambleSceneBuilder.CreateLabel(parent, "ComingSoon", ComingSoon, ComingSoonFontSize, ComingSoonPosition, ComingSoonSize);
            back = PenmonScrambleSceneBuilder.CreateTitleButton(parent, "Back", "もどる", BackPosition, BackSize, PenmonScrambleSceneBuilder.CancelColor, BackFontSize);
            return group;
        }

        /// <summary>
        /// 操作を読む。シーンごとに作り直す（本編の物は仮想ボタンがあるのでシーンをまたがない）。
        /// スマホの打つ大ボタンは J / ×（Action1）と同じ扱いにする
        /// </summary>
        private static void CreateInputManager(VirtualButton shoot)
        {
            var manager = new GameObject("InputManager").AddComponent<InputManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("_virtualAction1Button").objectReferenceValue = shoot;
            so.FindProperty("_dontDestroyOnLoad").boolValue = false;
            so.ApplyModifiedProperties();
        }

        /// <summary>本編のシーン（0番目＝起動したときのシーン）の後ろに足す</summary>
        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
