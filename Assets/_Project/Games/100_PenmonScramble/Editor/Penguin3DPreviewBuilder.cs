using PenmonScramble.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 3D の試作の場のシーンを作る。ゲームのビルドには入れない。
    /// 床とペンギンはシーンに保存せず、PenguinPreviewStage が開くたびに組み立てる
    /// </summary>
    public static partial class Penguin3DPreviewBuilder
    {
        private const string GameFolder = "Assets/_Project/Games/100_PenmonScramble/";
        private const string ScenePath = GameFolder + "Scenes/Preview3D.unity";
        private const string ToonShaderPath = GameFolder + "Shaders/Toon.shader";
        private const string BlobShadowShaderPath = GameFolder + "Shaders/BlobShadow.shader";
        private const string FloorTexturePath = GameFolder + "Art/Stage/World1.png";

        // 左上の奥から照らす。影が右手前に落ちて、ペンギンの顔（手前）が明るく見える
        private static readonly Vector3 SunAngles = new Vector3(50f, -35f, 0f);
        private const float FarClip = 300f;

        [MenuItem(EditorMenu.Capture + "Open 3D Preview", priority = EditorMenu.CaptureOpenPriority)]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildScene();
        }

        public static void BuildScene()
        {
            RenderPipelineSetup.Ensure();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = PenmonScrambleSceneBuilder.CreateMainCamera(null, FarClip);
            var light = PenmonScrambleSceneBuilder.CreateSunLight("Sun", null, Quaternion.Euler(SunAngles), LightShadows.Soft);
            CreateStage(camera, light);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Penguin3DPreviewBuilder] {ScenePath} を作りました。Play で動かせます");
        }

        /// <summary>撮影の入口。場が無ければ開いてから探し直す（開けなかったら null）</summary>
        private static PenguinPreviewStage FindOrOpenPreviewStage()
        {
            var stage = Object.FindAnyObjectByType<PenguinPreviewStage>();
            if (stage == null)
            {
                Open();
                stage = Object.FindAnyObjectByType<PenguinPreviewStage>();
            }

            return stage;
        }

        /// <summary>撮る前の共通設定。影は撮影用に切って、輪郭線は必ず描く</summary>
        private static void BeginPreviewCapture(PenguinPreviewStage stage)
        {
            stage.SetRealShadows(false);
            stage.SetOutline(true);
        }

        private static void CreateStage(Camera camera, Light light)
        {
            var obj = new GameObject("PreviewStage");
            var stage = obj.AddComponent<PenguinPreviewStage>();
            obj.AddComponent<PenguinPreviewInput>();
            var so = new SerializedObject(stage);
            so.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ToonShaderPath);
            so.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(BlobShadowShaderPath);
            so.FindProperty("_floorTexture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture>(FloorTexturePath);
            so.FindProperty("_light").objectReferenceValue = light;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 参照を入れる前の OnEnable では組み立てられないので、入れ直して組み立てさせる
            stage.enabled = false;
            stage.enabled = true;
        }
    }
}
