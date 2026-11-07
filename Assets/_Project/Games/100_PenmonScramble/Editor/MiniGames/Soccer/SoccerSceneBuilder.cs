using System.Linq;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
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
    /// まだ作っていないスタジアムのときの「準備中」の板と、ひとりで1試合（SoccerMatchPlay。3対3・キーパー・PK戦・必殺シュート・結果・メニュー）を置く
    /// （Docs/MINIGAME_SOCCER_PLAN.md フェーズ 4）。みんなで遊ぶための部品（本編から部屋を引き継ぐ OnlineSession・通信の段取りの SoccerOnlinePlay・
    /// 始まるまでの「待っています」）も置く（フェーズ 5）。HUD は SoccerSceneBuilder.Hud.cs、PK戦の HUD は SoccerSceneBuilder.HudShootout.cs、
    /// 遊び方は SoccerSceneBuilder.HowToPlay.cs
    /// </summary>
    public static partial class SoccerSceneBuilder
    {
        public static readonly string ScenePath = "Assets/_Project/Games/100_PenmonScramble/Scenes/" + MiniGameTable.Get(MiniGameTable.SoccerId).SceneName + ".unity";

        private const string ShaderFolder = "Assets/_Project/Games/100_PenmonScramble/Shaders/";
        private const float CameraFarClip = 200f;
        private const float CameraFieldOfView = 40f;
        // 置く位置は SoccerCamera が試しのピッチを始めたときに決める（ここは始まる前の1フレームの位置）
        private static readonly Vector3 CameraPosition = new Vector3(0f, 25f, -17f);
        private static readonly Vector3 CameraAngles = new Vector3(56f, 0f, 0f);

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
        private static readonly Color WaitingBackColor = new Color(0.02f, 0.03f, 0.10f, 0.7f);
        private static readonly Vector2 WaitingTextSize = new Vector2(1400f, 120f);
        private const int WaitingFontSize = 60;

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
            var cutIn = PenmonScrambleSceneBuilder.CreateSoccerCutIn(canvas);
            var result = PenmonScrambleSceneBuilder.CreateMiniGameResultPanel(canvas, "スタジアムを選ぶ", "ホストがスタジアムを選ぶと、みんなで待ちの窓へ戻ります");
            // メニューは HUD・結果より手前に、遊び方はメニューよりさらに手前に出す（CreateStageMenu が並べ替える）
            var howToPlay = CreateHowToPlay(canvas);
            var menu = CreateMenu(canvas, hud.Menu, howToPlay);
            var play = CreateMatchPlay(camera, hud, cutIn, menu, result);
            CreateInputManager(hud.Touch);
            // 本編のシーンが預けた部屋を、起きたときに引き継ぐ（ひとりで遊ぶときは何もしない）
            var session = new GameObject("OnlineSession").AddComponent<OnlineSession>();
            var online = CreateOnline(canvas, session, play);

            var runner = new GameObject("Soccer").AddComponent<SoccerSceneRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("_placeholder").objectReferenceValue = placeholder;
            so.FindProperty("_courseText").objectReferenceValue = course;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_play").objectReferenceValue = play;
            so.FindProperty("_online").objectReferenceValue = online;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_howToPlay").objectReferenceValue = howToPlay;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[SoccerSceneBuilder] {ScenePath} を生成しました");
        }

        /// <summary>ピッチを見下ろすカメラと、ボールへ少し寄せる SoccerCamera</summary>
        private static SoccerCamera CreateCamera()
        {
            var camera = PenmonScrambleSceneBuilder.CreateMainCamera(null, CameraFarClip);
            camera.fieldOfView = CameraFieldOfView;
            camera.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraAngles));
            var rig = camera.gameObject.AddComponent<SoccerCamera>();
            var so = new SerializedObject(rig);
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.ApplyModifiedProperties();
            return rig;
        }

        /// <summary>まだ作っていないスタジアムのときの板（スタジアムの名前・準備中・もどる）。試合を始めたら隠す</summary>
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

        /// <summary>1試合: ピッチ・球・子・キーパーの見た目（SoccerView）と、試合を進める SoccerMatchPlay</summary>
        private static SoccerMatchPlay CreateMatchPlay(SoccerCamera camera, SoccerHudParts hud, SpecialCutIn cutIn, StageMenu menu, MiniGameResultPanel result)
        {
            var root = new GameObject("Match");
            var view = root.AddComponent<SoccerView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader");
            viewSo.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader");
            viewSo.ApplyModifiedProperties();

            var play = root.AddComponent<SoccerMatchPlay>();
            var so = new SerializedObject(play);
            so.FindProperty("_view").objectReferenceValue = view;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_hud").objectReferenceValue = hud.Hud;
            so.FindProperty("_nameTags").objectReferenceValue = hud.NameTags;
            so.FindProperty("_cutIn").objectReferenceValue = cutIn;
            so.FindProperty("_menu").objectReferenceValue = menu;
            so.FindProperty("_result").objectReferenceValue = result;
            so.FindProperty("_musicClips").objectReferenceValue = PenmonScrambleSceneBuilder.CreateMusicClips(root);
            so.ApplyModifiedProperties();
            return play;
        }

        /// <summary>みんなで遊ぶときの通信の段取りと、始まるまで（全員の準備・ホストを待つ間）画面を覆う「待っています」</summary>
        private static SoccerOnlinePlay CreateOnline(Transform canvas, OnlineSession session, SoccerMatchPlay play)
        {
            var waiting = PenmonScrambleSceneBuilder.CreateFullScreenBacking(canvas, "Waiting", WaitingBackColor);
            var text = PenmonScrambleSceneBuilder.CreateLabel(waiting.transform, "Text", string.Empty, WaitingFontSize, Vector2.zero, WaitingTextSize);
            text.fontStyle = FontStyle.Bold;
            waiting.SetActive(false);

            var online = play.gameObject.AddComponent<SoccerOnlinePlay>();
            var so = new SerializedObject(online);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_play").objectReferenceValue = play;
            so.FindProperty("_waiting").objectReferenceValue = waiting;
            so.FindProperty("_waitingText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
            return online;
        }

        /// <summary>
        /// 試合中のメニュー（本編の StageMenu の見た目）。札は つづける・試合をやり直す・遊び方（サッカーのページ）・設定・ミニゲームへ。
        /// 右の編成の欄は作らない
        /// </summary>
        private static StageMenu CreateMenu(Transform canvas, Button openButton, HowToPlayScreen howToPlay)
        {
            var labels = new[] { "つづける", "試合をやり直す", "遊び方", "設定", "ミニゲームへ" };
            var menu = PenmonScrambleSceneBuilder.CreateStageMenu(canvas, openButton, howToPlay, labels, withParty: false);
            var so = new SerializedObject(menu);
            so.FindProperty("_restartLabel").stringValue = "試合をやり直す";
            so.FindProperty("_restartSoloMessage").stringValue = "試合をはじめからやり直します。\nいまの点は残りません";
            so.FindProperty("_exitLabel").stringValue = "ミニゲームへ";
            so.FindProperty("_exitSoloMessage").stringValue = "試合をやめてミニゲームの画面に戻ります。\nいまの点は残りません";
            so.FindProperty("_exitHostMessage").stringValue = "部屋を閉じてミニゲームの画面に戻ります。\nほかの人も全員ロビーに戻ります";
            so.FindProperty("_exitGuestMessage").stringValue = "部屋から抜けてミニゲームの画面に戻ります。\nいまの点は残りません";
            so.FindProperty("_coopNote").stringValue = "みんなで遊んでいる間は 止まりません";
            so.ApplyModifiedProperties();
            return menu;
        }

        /// <summary>
        /// 操作を読む。シーンごとに作り直す（仮想ボタンがこのシーンの物なので、シーンをまたいで残すと参照が切れる）。
        /// シュート / スライディングは J / ×（Action1）、パス / ダッシュは K / ○（Action2）、必殺シュートは L / △（Action3）、
        /// 交代は Space / L1（Action4）と同じ扱い
        /// </summary>
        private static void CreateInputManager(PenmonScrambleSceneBuilder.SoccerTouchParts touch)
        {
            var manager = new GameObject("InputManager").AddComponent<InputManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("_virtualJoystick").objectReferenceValue = touch.Joystick;
            so.FindProperty("_virtualAction1Button").objectReferenceValue = touch.Shoot;
            so.FindProperty("_virtualAction2Button").objectReferenceValue = touch.Pass;
            so.FindProperty("_virtualAction3Button").objectReferenceValue = touch.Special;
            so.FindProperty("_virtualAction4Button").objectReferenceValue = touch.Switch;
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
