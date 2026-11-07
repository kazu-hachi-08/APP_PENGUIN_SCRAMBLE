using System.Linq;
using PenmonScramble.Baseball.Game;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.MiniGames.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球のシーン（BaseballScene）をコードから生成する。シーンは生成物なので手で編集せず、ここを直して Rebuild する
    /// （Rebuild は本編・ゴルフ・サッカーのシーンの後にこれを呼ぶ。ボタンの絵などは本編の SceneBuilder が描いた物を使う）。
    /// 今は球場が無いので、「準備中」の板と、開発用メニューから遊ぶ試しの打席（仮の見た目）だけを置く
    /// （Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 1）
    /// </summary>
    public static class BaseballSceneBuilder
    {
        public static readonly string ScenePath = "Assets/_Project/Games/100_PenmonScramble/Scenes/" + MiniGameTable.Get(MiniGameTable.BaseballId).SceneName + ".unity";

        private const string ShaderFolder = "Assets/_Project/Games/100_PenmonScramble/Shaders/";
        private const float CameraFarClip = 200f;

        // キャッチャーの後ろの高めから、盤（ホームからフェンスまで 14 マスの扇形）のほぼ全体を見せる仮のカメラ（打球へ寄るカメラはフェーズ 2）
        private static readonly Vector3 CameraPosition = new Vector3(0f, 12f, -7f);
        private static readonly Vector3 CameraAngles = new Vector3(45f, 0f, 0f);
        private const float CameraFieldOfView = 45f;

        private const string Heading = "ペンモンベースボール";
        private const string ComingSoon = "球場は準備中です";
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

        private const string TestHelp = "振る: J / ×（タイミングよく）    バント: K / ○ を押している間    もどる: Esc";
        private static readonly Vector2 TallyPosition = new Vector2(0f, 480f);
        private static readonly Vector2 TallySize = new Vector2(1400f, 80f);
        private const int TallyFontSize = 48;
        private static readonly Vector2 MessagePosition = new Vector2(0f, 220f);
        private static readonly Vector2 MessageSize = new Vector2(1400f, 160f);
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
            var testAtBat = CreateTestAtBat(canvas);
            CreateInputManager();

            var runner = new GameObject("Baseball").AddComponent<BaseballSceneRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("_placeholder").objectReferenceValue = placeholder;
            so.FindProperty("_courseText").objectReferenceValue = course;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_testAtBat").objectReferenceValue = testAtBat;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[BaseballSceneBuilder] {ScenePath} を生成しました");
        }

        private static void CreateCamera()
        {
            var camera = PenmonScrambleSceneBuilder.CreateMainCamera(null, CameraFarClip);
            camera.fieldOfView = CameraFieldOfView;
            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraAngles));
        }

        /// <summary>まだ作っていない球場のときの板（球場の名前・準備中・もどる）。試しの打席では隠す</summary>
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

        /// <summary>試しの打席: 仮の見た目（BaseballTestAtBatView）と、集計・結果の大きな文字・操作の説明の字</summary>
        private static BaseballTestAtBat CreateTestAtBat(Transform canvas)
        {
            var root = new GameObject("TestAtBat");
            var view = root.AddComponent<BaseballTestAtBatView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader");
            viewSo.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader");
            viewSo.ApplyModifiedProperties();

            var hud = UIDialogBuilder.CreateUIObject("TestAtBatHud", canvas);
            UIDialogBuilder.SetStretchAll(hud.GetComponent<RectTransform>());
            var tally = PenmonScrambleSceneBuilder.CreateLabel(hud.transform, "Tally", string.Empty, TallyFontSize, TallyPosition, TallySize);
            tally.fontStyle = FontStyle.Bold;
            var message = PenmonScrambleSceneBuilder.CreateLabel(hud.transform, "Message", string.Empty, MessageFontSize, MessagePosition, MessageSize);
            message.fontStyle = FontStyle.Bold;
            PenmonScrambleSceneBuilder.CreateLabel(hud.transform, "Help", TestHelp, HelpFontSize, HelpPosition, HelpSize);

            var atBat = root.AddComponent<BaseballTestAtBat>();
            var so = new SerializedObject(atBat);
            so.FindProperty("_view").objectReferenceValue = view;
            so.FindProperty("_hud").objectReferenceValue = hud;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_tallyText").objectReferenceValue = tally;
            so.ApplyModifiedProperties();
            return atBat;
        }

        /// <summary>操作を読む。シーンごとに作り直す。タッチの仮想ボタンはフェーズ 2 で足す（試しの打席はキーボード・コントローラーで遊ぶ）</summary>
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
