using System.Linq;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーのシーン（SoccerScene）をコードから生成する。シーンは生成物なので手で編集せず、ここを直して Rebuild する
    /// （Rebuild は本編とゴルフのシーンの後にこれを呼ぶ。ボタンの絵などは本編の SceneBuilder が描いた物を使う）。
    /// 今はスタジアムが無いので、「準備中」の板と、開発用メニューから遊ぶ試しのピッチ（仮の見た目）だけを置く
    /// （Docs/MINIGAME_SOCCER_PLAN.md フェーズ 1）
    /// </summary>
    public static class SoccerSceneBuilder
    {
        public static readonly string ScenePath = "Assets/_Project/Games/100_PenmonScramble/Scenes/" + MiniGameTable.Get(MiniGameTable.SoccerId).SceneName + ".unity";

        private const string ShaderFolder = "Assets/_Project/Games/100_PenmonScramble/Shaders/";
        private const float CameraFarClip = 200f;

        // 横長のピッチ（28 × 16 マス）を斜め上からほぼ全部見せる仮のカメラ（ボールへ寄るカメラはフェーズ 2）
        private static readonly Vector3 CameraPosition = new Vector3(0f, 21f, -16f);
        private static readonly Vector3 CameraAngles = new Vector3(56f, 0f, 0f);
        private const float CameraFieldOfView = 45f;

        private const string Heading = "ペンモンサッカー";
        private const string ComingSoon = "スタジアムは準備中です";
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

        private const string TestHelp = "移動: WASD / 左スティック    シュート: J / × を押してためて離す（満タンで浮く）    もどる: Esc";
        private static readonly Vector2 ScorePosition = new Vector2(0f, 480f);
        private static readonly Vector2 ScoreSize = new Vector2(800f, 90f);
        private const int ScoreFontSize = 60;
        private static readonly Vector2 MessagePosition = new Vector2(0f, 200f);
        private static readonly Vector2 MessageSize = new Vector2(1200f, 160f);
        private const int MessageFontSize = 120;
        private static readonly Vector2 HelpPosition = new Vector2(0f, -500f);
        private static readonly Vector2 HelpSize = new Vector2(1800f, 60f);
        private const int HelpFontSize = 30;

        /// <summary>バッチモードからも呼べるよう、保存の確認はしない（呼ぶ側の Rebuild が済ませている）</summary>
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            PenmonScrambleSceneBuilder.CreateSunLight("Sun", null, Quaternion.Euler(PenmonScrambleSceneBuilder.SunAngles), LightShadows.None);
            PenmonScrambleSceneBuilder.CreateEventSystem();
            var canvas = PenmonScrambleSceneBuilder.CreateCanvas().transform;

            var placeholder = CreatePlaceholder(canvas, out var course, out var back);
            var testPitch = CreateTestPitch(canvas);
            CreateInputManager();

            var runner = new GameObject("Soccer").AddComponent<SoccerSceneRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("_placeholder").objectReferenceValue = placeholder;
            so.FindProperty("_courseText").objectReferenceValue = course;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_testPitch").objectReferenceValue = testPitch;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[SoccerSceneBuilder] {ScenePath} を生成しました");
        }

        private static void CreateCamera()
        {
            var camera = PenmonScrambleSceneBuilder.CreateMainCamera(null, CameraFarClip);
            camera.fieldOfView = CameraFieldOfView;
            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraAngles));
        }

        /// <summary>まだ作っていないスタジアムのときの板（スタジアムの名前・準備中・もどる）。試しのピッチでは隠す</summary>
        private static GameObject CreatePlaceholder(Transform canvas, out Text course, out Button back)
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

        /// <summary>試しのピッチ: 仮の見た目（SoccerTestPitchView）と、点・「ゴール！」・操作の説明の字</summary>
        private static SoccerTestPitch CreateTestPitch(Transform canvas)
        {
            var root = new GameObject("TestPitch");
            var view = root.AddComponent<SoccerTestPitchView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader");
            viewSo.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader");
            viewSo.ApplyModifiedProperties();

            var hud = UIDialogBuilder.CreateUIObject("TestPitchHud", canvas);
            UIDialogBuilder.SetStretchAll(hud.GetComponent<RectTransform>());
            var score = PenmonScrambleSceneBuilder.CreateLabel(hud.transform, "Score", string.Empty, ScoreFontSize, ScorePosition, ScoreSize);
            score.fontStyle = FontStyle.Bold;
            var message = PenmonScrambleSceneBuilder.CreateLabel(hud.transform, "Message", string.Empty, MessageFontSize, MessagePosition, MessageSize);
            message.fontStyle = FontStyle.Bold;
            PenmonScrambleSceneBuilder.CreateLabel(hud.transform, "Help", TestHelp, HelpFontSize, HelpPosition, HelpSize);

            var pitch = root.AddComponent<SoccerTestPitch>();
            var so = new SerializedObject(pitch);
            so.FindProperty("_view").objectReferenceValue = view;
            so.FindProperty("_hud").objectReferenceValue = hud;
            so.FindProperty("_scoreText").objectReferenceValue = score;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.ApplyModifiedProperties();
            return pitch;
        }

        /// <summary>操作を読む。シーンごとに作り直す。タッチの仮想ボタンはフェーズ 2 で足す（試しのピッチはキーボード・コントローラーで遊ぶ）</summary>
        private static void CreateInputManager()
        {
            var manager = new GameObject("InputManager").AddComponent<InputManager>();
            var so = new SerializedObject(manager);
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
