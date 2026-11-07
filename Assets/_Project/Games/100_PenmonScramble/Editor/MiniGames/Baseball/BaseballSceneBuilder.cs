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
    /// 今は球場が無いので、「準備中」の板と、開発用メニューから遊ぶ試しの打席（本番の見た目と操作。打つ側・投げる側）を置く
    /// （Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 2）。HUD は BaseballSceneBuilder.Hud.cs、スマホのボタンは PenmonScrambleSceneBuilder.BaseballTouch.cs
    /// </summary>
    public static partial class BaseballSceneBuilder
    {
        public static readonly string ScenePath = "Assets/_Project/Games/100_PenmonScramble/Scenes/" + MiniGameTable.Get(MiniGameTable.BaseballId).SceneName + ".unity";

        private const string ShaderFolder = "Assets/_Project/Games/100_PenmonScramble/Shaders/";
        private const float CameraFarClip = 200f;
        private const float CameraFieldOfView = 45f;
        // 置く位置は BaseballCamera が起きたときに決める（ここは始まる前の1フレームの位置。BaseballCamera のいつもの位置と同じ）
        private static readonly Vector3 CameraPosition = new Vector3(0f, 13f, -8.5f);
        private static readonly Vector3 CameraAngles = new Vector3(48f, 0f, 0f);

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

        /// <summary>バッチモードからも呼べるよう、保存の確認はしない（呼ぶ側の Rebuild が済ませている）</summary>
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera();
            PenmonScrambleSceneBuilder.CreateSunLight("Sun", null, Quaternion.Euler(PenmonScrambleSceneBuilder.SunAngles), LightShadows.None);
            PenmonScrambleSceneBuilder.CreateEventSystem();
            var canvas = PenmonScrambleSceneBuilder.CreateCanvas().transform;
            // 顔の絵と地面の絵は本編と同じもの（本編の SceneBuilder が先に取り込んでいる）
            PenmonScrambleSceneBuilder.CreateCreatureArt();

            var placeholder = CreatePlaceholder(canvas, out var course, out var back);
            var hud = CreateHud(canvas);
            var testPlay = CreateTestPlay(camera, hud.Hud);
            var input = new GameObject("InputManager").AddComponent<InputManager>();
            PenmonScrambleSceneBuilder.BindBaseballInput(input, hud.Touch);

            var runner = new GameObject("Baseball").AddComponent<BaseballSceneRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("_placeholder").objectReferenceValue = placeholder;
            so.FindProperty("_courseText").objectReferenceValue = course;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_testPlay").objectReferenceValue = testPlay;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[BaseballSceneBuilder] {ScenePath} を生成しました");
        }

        /// <summary>キャッチャーの後ろの高めから盤を見るカメラと、打球へ寄せる BaseballCamera</summary>
        private static BaseballCamera CreateCamera()
        {
            var camera = PenmonScrambleSceneBuilder.CreateMainCamera(null, CameraFarClip);
            camera.fieldOfView = CameraFieldOfView;
            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraAngles));
            var rig = camera.gameObject.AddComponent<BaseballCamera>();
            var so = new SerializedObject(rig);
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_homePosition").vector3Value = CameraPosition;
            so.FindProperty("_homeAngles").vector3Value = CameraAngles;
            so.ApplyModifiedProperties();
            return rig;
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

        /// <summary>試しの打席: 盤・球・バット・子の見た目（BaseballView）と、1球ずつ進める BaseballTestPlay</summary>
        private static BaseballTestPlay CreateTestPlay(BaseballCamera camera, BaseballHud hud)
        {
            var root = new GameObject("TestPlay");
            var view = root.AddComponent<BaseballView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader");
            viewSo.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader");
            viewSo.ApplyModifiedProperties();

            var play = root.AddComponent<BaseballTestPlay>();
            var so = new SerializedObject(play);
            so.FindProperty("_view").objectReferenceValue = view;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_hud").objectReferenceValue = hud;
            so.ApplyModifiedProperties();
            return play;
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
