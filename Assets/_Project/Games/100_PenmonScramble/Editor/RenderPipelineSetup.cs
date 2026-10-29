using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 描画を URP の Universal Renderer（Forward）にする。3D の物体に光と影を当てるため。
    /// 画質の設定（Mobile / PC）ごとに URP の設定を分け、影の細かさなどを変えられるようにする。
    /// 設定ファイルが無ければ作り、あれば作り直さずに割り当てだけ直す（手で調整した値を消さないように）
    /// </summary>
    public static class RenderPipelineSetup
    {
        private const string Folder = "Assets/_Project/Games/100_PenmonScramble/Settings";
        private const string RendererPath = Folder + "/PenmonScramble_Renderer.asset";
        private const string MobilePath = Folder + "/PenmonScramble_URP_Mobile.asset";
        private const string PcPath = Folder + "/PenmonScramble_URP_PC.asset";

        // 本物の影を出す距離（マス）。エリア（16x9）をカメラが斜めに見たときに全部入る長さ
        private const float ShadowDistance = 40f;
        private const int MobileShadowResolution = 1024;
        private const int PcShadowResolution = 2048;
        private const int MobileMsaa = 2;
        private const int PcMsaa = 4;

        [MenuItem("Tools/PenmonScramble/Setup URP")]
        public static void SetupMenu()
        {
            Ensure();
            Debug.Log("[RenderPipelineSetup] URP（Universal Renderer）に切り替えました");
        }

        public static void Ensure()
        {
            Directory.CreateDirectory(Folder);
            var renderer = LoadOrCreateRenderer();
            var mobile = LoadOrCreatePipeline(MobilePath, renderer, MobileShadowResolution, MobileMsaa, softShadows: false);
            var pc = LoadOrCreatePipeline(PcPath, renderer, PcShadowResolution, PcMsaa, softShadows: true);

            GraphicsSettings.defaultRenderPipeline = pc;
            AssignQualityLevels(mobile, pc);
            AssetDatabase.SaveAssets();
        }

        private static UniversalRendererData LoadOrCreateRenderer()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer != null)
            {
                return renderer;
            }

            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.renderingMode = RenderingMode.Forward;
            AssetDatabase.CreateAsset(renderer, RendererPath);
            return renderer;
        }

        private static UniversalRenderPipelineAsset LoadOrCreatePipeline(string path, UniversalRendererData renderer, int shadowResolution, int msaa, bool softShadows)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline != null)
            {
                return pipeline;
            }

            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            // 今の2Dの見た目（スプライトの色）を変えないよう、HDR は使わない
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = msaa;
            pipeline.mainLightShadowmapResolution = shadowResolution;
            pipeline.shadowDistance = ShadowDistance;
            pipeline.shadowCascadeCount = 1;
            AssetDatabase.CreateAsset(pipeline, path);

            // ふちのやわらかい影は setter が公開されていないので、保存される項目を直接書く
            var so = new SerializedObject(pipeline);
            so.FindProperty("m_SoftShadowsSupported").boolValue = softShadows;
            so.ApplyModifiedPropertiesWithoutUndo();
            return pipeline;
        }

        /// <summary>
        /// 画質の設定の名前で URP の設定を選ぶ（Mobile → スマホ用、それ以外 → PC 用）。
        /// QualitySettings には画質ごとに設定する API が無いので、設定ファイルを直接書き換える
        /// </summary>
        private static void AssignQualityLevels(RenderPipelineAsset mobile, RenderPipelineAsset pc)
        {
            var settings = new SerializedObject(QualitySettings.GetQualitySettings());
            var levels = settings.FindProperty("m_QualitySettings");
            for (var i = 0; i < levels.arraySize; i++)
            {
                var level = levels.GetArrayElementAtIndex(i);
                var isMobile = level.FindPropertyRelative("name").stringValue == "Mobile";
                level.FindPropertyRelative("customRenderPipeline").objectReferenceValue = isMobile ? mobile : pc;
            }

            settings.ApplyModifiedProperties();
        }
    }
}
