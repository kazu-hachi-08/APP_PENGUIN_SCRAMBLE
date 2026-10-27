using System.Linq;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.Online;
using PenmonScramble.Common.Scene;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 乱戦！ペンモンスクランブルのシーンをコードから生成する。シーンは生成物なので手で編集せず、ここを直して Rebuild する。
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Games/100_PenmonScramble/Scenes/" + SceneNames.PenmonScramble + ".unity";

        // 横画面の基準解像度。共通基盤のダイアログと同じ値にそろえる
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Color BackgroundColor = new Color(0.55f, 0.70f, 0.80f);
        private static readonly Color TextColor = new Color(0.15f, 0.20f, 0.30f);
        private const int VersionFontSize = 32;
        private static readonly Vector2 VersionMargin = new Vector2(-24f, 16f);
        private static readonly Vector2 VersionSize = new Vector2(400f, 60f);

        // エリアの上下に HUD 分の余白を足して、エリア全体が画面に収まるようにする
        private const float CameraVerticalMargin = 0.8f;

        [MenuItem("Tools/PenmonScramble/Rebuild")]
        public static void Rebuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildScene();
        }

        /// <summary>バッチモード（-executeMethod）からも呼べるよう、保存の確認をしない入口を分けている</summary>
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var (shake, stageCamera) = CreateCamera();
            CreateEventSystem();
            var canvas = CreateCanvas();
            var touch = CreateTouchControls(canvas.transform);
            var hud = CreateHud(canvas.transform, touch.SpecialImage);
            var cutIn = CreateCutIn(canvas.transform);
            var session = new GameObject("OnlineSession").AddComponent<OnlineSession>();
            // タイトルはステージの HUD より手前に出す（後に作った方が手前）
            var title = CreateTitlePanel(canvas.transform, session);
            // ガチャ・一覧はタイトルの上に開く
            var gacha = CreateGachaScreen(canvas.transform);
            var penguins = CreatePenguinListScreen(canvas.transform);
            var titleMenu = CreateTitleMenu(title, gacha, penguins);
            CreateVersionLabel(canvas.transform);
            CreateInputManager(touch);
            CreateStage(hud, session, title, titleMenu, shake, stageCamera, cutIn);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[PenmonScrambleSceneBuilder] {ScenePath} を生成しました");
        }

        /// <summary>エリアへ動く土台（StageCamera）の子にカメラを置き、揺れはカメラ側で出す（2つの動きがぶつからないように）</summary>
        private static (CameraShake shake, StageCamera stageCamera) CreateCamera()
        {
            var rig = new GameObject("CameraRig").AddComponent<StageCamera>();
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.transform.SetParent(rig.transform, false);
            cameraObj.transform.localPosition = new Vector3(0f, 0f, -10f);
            var camera = cameraObj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Balance.AreaHeight * 0.5f + CameraVerticalMargin;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            cameraObj.AddComponent<AudioListener>();
            return (cameraObj.AddComponent<CameraShake>(), rig);
        }

        private static void CreateEventSystem()
        {
            // Input System のみの設定なので、旧 StandaloneInputModule ではなくこちらを使う
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static Canvas CreateCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            // 比率が違う画面でも基準の 1920x1080 が必ず全部見えるよう、足りない側を広げる
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateVersionLabel(Transform canvas)
        {
            var obj = UIDialogBuilder.CreateUIObject("Version", canvas);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = VersionMargin;
            rect.sizeDelta = VersionSize;
            var text = AddText(obj, string.Empty, VersionFontSize);
            text.alignment = TextAnchor.LowerRight;
            obj.AddComponent<VersionLabel>();
        }

        private static void CreateInputManager(TouchControls touch)
        {
            var manager = new GameObject("InputManager").AddComponent<InputManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("_virtualJoystick").objectReferenceValue = touch.Joystick;
            so.FindProperty("_virtualAction1Button").objectReferenceValue = touch.Skill1;
            so.FindProperty("_virtualAction2Button").objectReferenceValue = touch.Skill2;
            so.FindProperty("_virtualAction3Button").objectReferenceValue = touch.Special;
            so.FindProperty("_virtualAction4Button").objectReferenceValue = touch.Switch;
            // 仮想ボタンはこのシーンの物なので、シーンをまたいで残すと参照が切れる
            so.FindProperty("_dontDestroyOnLoad").boolValue = false;
            so.ApplyModifiedProperties();
        }

        private static void CreateStage(StageHud hud, OnlineSession session, ModeSelectPanel title, TitleMenu titleMenu, CameraShake shake, StageCamera stageCamera, SpecialCutIn cutIn)
        {
            var stageObj = new GameObject("Stage");
            var view = stageObj.AddComponent<StageView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_cameraShake").objectReferenceValue = shake;
            viewSo.FindProperty("_stageCamera").objectReferenceValue = stageCamera;
            viewSo.FindProperty("_cutIn").objectReferenceValue = cutIn;
            viewSo.ApplyModifiedProperties();
            var runner = stageObj.AddComponent<StageRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("_view").objectReferenceValue = view;
            so.FindProperty("_hud").objectReferenceValue = hud;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_modeSelect").objectReferenceValue = title;
            so.FindProperty("_titleMenu").objectReferenceValue = titleMenu;
            so.ApplyModifiedProperties();
        }

        private static Text AddText(GameObject obj, string value, int fontSize)
        {
            var text = obj.AddComponent<Text>();
            // 実行時に GameFontApplier が同梱の日本語フォントへ差し替える
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.color = TextColor;
            return text;
        }

        private static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
