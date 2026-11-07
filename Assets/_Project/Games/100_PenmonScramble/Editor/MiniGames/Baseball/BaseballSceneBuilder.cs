using System.Linq;
using PenmonScramble.Baseball.Game;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球のシーン（BaseballScene）をコードから生成する。シーンは生成物なので手で編集せず、ここを直して Rebuild する
    /// （Rebuild は本編・ゴルフ・サッカーのシーンの後にこれを呼ぶ。ボタンの絵などは本編の SceneBuilder が描いた物を使う）。
    /// 試合（BaseballMatch）・魔球と必殺打のカットイン（上下に2本）・結果の欄・試合中のメニューと、まだ作っていない球場の「準備中」の板、
    /// みんなで遊ぶための部品（本編から部屋を引き継ぐ OnlineSession・通信の段取りの BaseballOnlinePlay・始まるまでの「待っています」）を置く
    /// （Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 4・5）。HUD は BaseballSceneBuilder.Hud.cs、スマホのボタンは PenmonScrambleSceneBuilder.BaseballTouch.cs
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
        // 魔球（上）と必殺打（下）のカットインの帯を、画面のまん中からずらす量
        private const float CutInOffset = 165f;
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
            var pitchCutIn = PenmonScrambleSceneBuilder.CreateBaseballCutIn(canvas, "PitchCutIn", CutInOffset);
            var hitCutIn = PenmonScrambleSceneBuilder.CreateBaseballCutIn(canvas, "HitCutIn", -CutInOffset);
            var result = PenmonScrambleSceneBuilder.CreateMiniGameResultPanel(canvas, "球場を選ぶ", "ホストが球場を選ぶと、みんなで待ちの窓へ戻ります");
            // メニューは HUD・カットイン・結果より手前に出すので、最後に作る
            var menu = CreateMenu(canvas, hud.Menu);
            var match = CreateMatch(camera, hud.Hud, pitchCutIn, hitCutIn, menu, result);
            var input = new GameObject("InputManager").AddComponent<InputManager>();
            PenmonScrambleSceneBuilder.BindBaseballInput(input, hud.Touch);
            // 本編のシーンが預けた部屋を、起きたときに引き継ぐ（ひとりで遊ぶときは何もしない）
            var session = new GameObject("OnlineSession").AddComponent<OnlineSession>();
            var online = CreateOnline(canvas, session, match);

            var runner = new GameObject("Baseball").AddComponent<BaseballSceneRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("_placeholder").objectReferenceValue = placeholder;
            so.FindProperty("_courseText").objectReferenceValue = course;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_match").objectReferenceValue = match;
            so.FindProperty("_online").objectReferenceValue = online;
            so.FindProperty("_session").objectReferenceValue = session;
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

        /// <summary>まだ作っていない球場のときの板（球場の名前・準備中・もどる）。試しの打席・試合では隠す</summary>
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

        /// <summary>試合: 盤・球・バット・子の見た目（BaseballView）と、試合を1球ずつ進める BaseballMatch</summary>
        private static BaseballMatch CreateMatch(BaseballCamera camera, BaseballHud hud, SpecialCutIn pitchCutIn, SpecialCutIn hitCutIn, StageMenu menu, MiniGameResultPanel result)
        {
            var root = new GameObject("Match");
            var view = root.AddComponent<BaseballView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader");
            viewSo.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader");
            viewSo.ApplyModifiedProperties();

            var play = root.AddComponent<BaseballMatch>();
            var so = new SerializedObject(play);
            so.FindProperty("_view").objectReferenceValue = view;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_hud").objectReferenceValue = hud;
            so.FindProperty("_pitchCutIn").objectReferenceValue = pitchCutIn;
            so.FindProperty("_hitCutIn").objectReferenceValue = hitCutIn;
            so.FindProperty("_menu").objectReferenceValue = menu;
            so.FindProperty("_result").objectReferenceValue = result;
            so.ApplyModifiedProperties();
            return play;
        }

        /// <summary>みんなで遊ぶときの通信の段取りと、始まるまで（全員の準備・ホストを待つ間）画面を覆う「待っています」</summary>
        private static BaseballOnlinePlay CreateOnline(Transform canvas, OnlineSession session, BaseballMatch match)
        {
            var waiting = PenmonScrambleSceneBuilder.CreateFullScreenBacking(canvas, "Waiting", WaitingBackColor);
            var text = PenmonScrambleSceneBuilder.CreateLabel(waiting.transform, "Text", string.Empty, WaitingFontSize, Vector2.zero, WaitingTextSize);
            text.fontStyle = FontStyle.Bold;
            waiting.SetActive(false);

            var online = match.gameObject.AddComponent<BaseballOnlinePlay>();
            var so = new SerializedObject(online);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_play").objectReferenceValue = match;
            so.FindProperty("_waiting").objectReferenceValue = waiting;
            so.FindProperty("_waitingText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
            return online;
        }

        /// <summary>
        /// 試合中のメニュー（本編の StageMenu の見た目）。札は つづける・試合をやり直す・設定（音量）・ミニゲームへ
        /// （遊び方はフェーズ 7 で足す。それまでは遊び方の画面を渡さないので札は出ない）。右の編成の欄は作らない
        /// </summary>
        private static StageMenu CreateMenu(Transform canvas, Button openButton)
        {
            var labels = new[] { "つづける", "試合をやり直す", "遊び方", "設定", "ミニゲームへ" };
            var menu = PenmonScrambleSceneBuilder.CreateStageMenu(canvas, openButton, null, labels, withParty: false);
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
