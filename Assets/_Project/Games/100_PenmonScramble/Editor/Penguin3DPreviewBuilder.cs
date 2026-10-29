using PenmonScramble.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 3D の試作の場のシーンを作る（PLAN_3D フェーズ0）。ゲームのビルドには入れない。
    /// 床とペンギンはシーンに保存せず、PenguinPreviewStage が開くたびに組み立てる
    /// </summary>
    public static partial class Penguin3DPreviewBuilder
    {
        private const string GameFolder = "Assets/_Project/Games/100_PenmonScramble/";
        private const string ScenePath = GameFolder + "Scenes/Preview3D.unity";
        private const string ToonShaderPath = GameFolder + "Shaders/Toon.shader";
        private const string BlobShadowShaderPath = GameFolder + "Shaders/BlobShadow.shader";
        private const string FloorTexturePath = GameFolder + "Art/Stage/World1.png";

        private static readonly Color BackgroundColor = new Color(0.55f, 0.70f, 0.80f);
        // 左上の奥から照らす。影が右手前に落ちて、ペンギンの顔（手前）が明るく見える
        private static readonly Vector3 SunAngles = new Vector3(50f, -35f, 0f);
        private static readonly Color SunColor = new Color(1f, 0.97f, 0.92f);
        private const float SunIntensity = 1.1f;
        private const float FarClip = 300f;

        [MenuItem("Tools/PenmonScramble/3D Preview")]
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
            var camera = CreateCamera();
            var light = CreateSun();
            CreateStage(camera, light);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Penguin3DPreviewBuilder] {ScenePath} を作りました。Play で動かせます");
        }

        private static Camera CreateCamera()
        {
            var obj = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = obj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.farClipPlane = FarClip;
            obj.AddComponent<AudioListener>();
            return camera;
        }

        private static Light CreateSun()
        {
            var obj = new GameObject("Sun");
            obj.transform.rotation = Quaternion.Euler(SunAngles);
            var light = obj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = SunIntensity;
            light.shadows = LightShadows.Soft;
            return light;
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
