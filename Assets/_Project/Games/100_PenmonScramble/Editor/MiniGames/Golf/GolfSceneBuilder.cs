using System.Linq;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.Online;
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
    /// まだ作っていないコースのときの「準備中」の板と、コースを回る部品（3D・カメラ・操作・HUD・スコアカード・結果・メニュー・遊び方）、
    /// みんなで回るための部品（本編から部屋を引き継ぐ OnlineSession・通信の段取りの GolfOnlinePlay・始まるまでの「待っています」）を置く
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
        private static readonly Color WaitingBackColor = new Color(0.02f, 0.03f, 0.10f, 0.7f);
        private static readonly Vector2 WaitingTextSize = new Vector2(1400f, 120f);
        private const int WaitingFontSize = 60;

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
            var scorecard = CreateScorecard(canvas);
            var result = CreateResultPanel(canvas);
            // メニューは HUD・スコアカードより手前に、遊び方はメニューよりさらに手前に出す（CreateStageMenu が並べ替える）
            var howToPlay = CreateHowToPlay(canvas);
            var menu = CreateMenu(canvas, hud.Menu, howToPlay);
            var play = CreatePlay(camera, hud, scorecard, result, menu);
            CreateInputManager(hud.Shoot, hud.Special);
            // 本編のシーンが預けた部屋を、起きたときに引き継ぐ（ひとりで遊ぶときは何もしない）
            var session = new GameObject("OnlineSession").AddComponent<OnlineSession>();
            var online = CreateOnline(canvas, session, play);

            var runner = new GameObject("Golf").AddComponent<GolfSceneRunner>();
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
            Debug.Log($"[GolfSceneBuilder] {ScenePath} を生成しました");
        }

        /// <summary>まだ作っていないコースのときの板（コースの名前・準備中・もどる）。コースを回るときは隠す</summary>
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

        /// <summary>みんなで回るときの通信の段取りと、始まるまで（全員の準備・ホストを待つ間）画面を覆う「待っています」</summary>
        private static GolfOnlinePlay CreateOnline(Transform canvas, OnlineSession session, GolfRoundPlay play)
        {
            var waiting = PenmonScrambleSceneBuilder.CreateFullScreenBacking(canvas, "Waiting", WaitingBackColor);
            var text = PenmonScrambleSceneBuilder.CreateLabel(waiting.transform, "Text", string.Empty, WaitingFontSize, Vector2.zero, WaitingTextSize);
            text.fontStyle = FontStyle.Bold;
            waiting.SetActive(false);

            var online = play.gameObject.AddComponent<GolfOnlinePlay>();
            var so = new SerializedObject(online);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_play").objectReferenceValue = play;
            so.FindProperty("_waiting").objectReferenceValue = waiting;
            so.FindProperty("_waitingText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
            return online;
        }

        /// <summary>
        /// 操作を読む。シーンごとに作り直す（本編の物は仮想ボタンがあるのでシーンをまたがない）。
        /// スマホの打つ大ボタンは J / ×（Action1）、特殊のボタンは L / △（Action3）と同じ扱いにする
        /// </summary>
        private static void CreateInputManager(VirtualButton shoot, VirtualButton special)
        {
            var manager = new GameObject("InputManager").AddComponent<InputManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("_virtualAction1Button").objectReferenceValue = shoot;
            so.FindProperty("_virtualAction3Button").objectReferenceValue = special;
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
