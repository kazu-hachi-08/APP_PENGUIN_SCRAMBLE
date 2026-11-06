using PenmonScramble.Game;
using PenmonScramble.Golf.Game;
using PenmonScramble.MiniGames.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// コースを回る部品: ホールの3D・カメラ・打つ操作・進行役（GolfRoundPlay）。HUD は GolfSceneBuilder.Hud.cs、
    /// スコアカード・結果・メニューは GolfSceneBuilder.Scorecard.cs
    /// </summary>
    public static partial class GolfSceneBuilder
    {
        private const string ShaderFolder = "Assets/_Project/Games/100_PenmonScramble/Shaders/";

        // 寄って見たときに、ペンモンと球が大きく見えるよう画角は狭めにする
        private const float CameraFieldOfView = 40f;

        private static GolfRoundPlay CreatePlay(Camera camera, GolfHudParts hud, GolfScorecard scorecard, MiniGameResultPanel result, StageMenu menu)
        {
            camera.fieldOfView = CameraFieldOfView;
            var root = new GameObject("Golf3D");

            var view = root.AddComponent<GolfHoleView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader");
            viewSo.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader");
            viewSo.ApplyModifiedProperties();

            var golfCamera = root.AddComponent<GolfCamera>();
            var cameraSo = new SerializedObject(golfCamera);
            cameraSo.FindProperty("_camera").objectReferenceValue = camera;
            cameraSo.ApplyModifiedProperties();

            var control = root.AddComponent<GolfShotControl>();
            var controlSo = new SerializedObject(control);
            controlSo.FindProperty("_pad").objectReferenceValue = hud.Pad;
            controlSo.ApplyModifiedProperties();

            var play = root.AddComponent<GolfRoundPlay>();
            var playSo = new SerializedObject(play);
            playSo.FindProperty("_view").objectReferenceValue = view;
            playSo.FindProperty("_camera").objectReferenceValue = golfCamera;
            playSo.FindProperty("_control").objectReferenceValue = control;
            playSo.FindProperty("_hud").objectReferenceValue = hud.Hud;
            playSo.FindProperty("_scorecard").objectReferenceValue = scorecard;
            playSo.FindProperty("_result").objectReferenceValue = result;
            playSo.FindProperty("_menu").objectReferenceValue = menu;
            playSo.FindProperty("_musicClips").objectReferenceValue = PenmonScrambleSceneBuilder.CreateMusicClips(root);
            playSo.ApplyModifiedProperties();
            return play;
        }
    }
}
