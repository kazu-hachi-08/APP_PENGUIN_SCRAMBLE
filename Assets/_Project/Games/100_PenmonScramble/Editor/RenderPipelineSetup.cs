using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 描画を URP の Universal Renderer（Forward）にする。3D の物体に光を当てるため。
    /// 画質の設定（Mobile / PC）ごとに URP の設定と Renderer を分け、縁の線・MSAA などを変える。
    /// 設定ファイルが無ければ作る。あれば作り直さず、ここで決めている値（影・縁の線・MSAA など）だけを毎回そろえる
    /// </summary>
    public static class RenderPipelineSetup
    {
        private const string Folder = "Assets/_Project/Games/100_PenmonScramble/Settings";
        private const string PcRendererPath = Folder + "/PenmonScramble_Renderer.asset";
        private const string MobileRendererPath = Folder + "/PenmonScramble_Renderer_Mobile.asset";
        private const string MobilePath = Folder + "/PenmonScramble_URP_Mobile.asset";
        private const string PcPath = Folder + "/PenmonScramble_URP_PC.asset";

        /// <summary>トゥーンの縁の線のパス（Toon.shader の LightMode）。本体と交互に描くと SRP Batcher のまとまりが切れるので、不透明の後にまとめて描く</summary>
        public const string OutlinePassName = "PenmonOutline";
        private const string OutlineFeatureName = "ToonOutline";

        private readonly struct Tier
        {
            public readonly int Msaa;
            public readonly bool Outline;

            public Tier(int msaa, bool outline)
            {
                Msaa = msaa;
                Outline = outline;
            }
        }

        // 影はどの環境も足元の丸い影なので、URP の本物の影は両方とも切る
        private static readonly Tier Mobile = new Tier(msaa: 2, outline: true);
        private static readonly Tier Pc = new Tier(msaa: 4, outline: true);

        // Rebuild・Release・試作の場を開くときに必ず呼ぶので、手で押すメニューは置かない
        public static void Ensure()
        {
            Directory.CreateDirectory(Folder);
            var pcRenderer = LoadOrCreateRenderer(PcRendererPath, Pc);
            var mobileRenderer = LoadOrCreateRenderer(MobileRendererPath, Mobile);
            var mobile = LoadOrCreatePipeline(MobilePath, mobileRenderer, Mobile);
            var pc = LoadOrCreatePipeline(PcPath, pcRenderer, Pc);

            GraphicsSettings.defaultRenderPipeline = pc;
            AssignQualityLevels(mobile, pc);
            AssetDatabase.SaveAssets();
        }

        private static UniversalRendererData LoadOrCreateRenderer(string path, Tier tier)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.renderingMode = RenderingMode.Forward;
                AssetDatabase.CreateAsset(renderer, path);
            }

            EnsureOutlineFeature(renderer).SetActive(tier.Outline);
            EditorUtility.SetDirty(renderer);
            return renderer;
        }

        /// <summary>縁の線のパスだけを、不透明の物を描き終えた後にまとめて描く（RenderObjects）</summary>
        private static RenderObjects EnsureOutlineFeature(UniversalRendererData renderer)
        {
            foreach (var existing in renderer.rendererFeatures)
            {
                if (existing is RenderObjects found && existing.name == OutlineFeatureName)
                {
                    return found;
                }
            }

            var feature = ScriptableObject.CreateInstance<RenderObjects>();
            feature.name = OutlineFeatureName;
            feature.hideFlags = HideFlags.HideInHierarchy;
            feature.settings.passTag = OutlineFeatureName;
            feature.settings.Event = RenderPassEvent.AfterRenderingOpaques;
            feature.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
            feature.settings.filterSettings.LayerMask = ~0;
            feature.settings.filterSettings.PassNames = new[] { OutlinePassName };
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            // 機能の一覧と、そのファイル内の番号の表は、Inspector で足したときと同じく両方そろえる
            var so = new SerializedObject(renderer);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            features.InsertArrayElementAtIndex(features.arraySize);
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.InsertArrayElementAtIndex(map.arraySize);
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return feature;
        }

        private static UniversalRenderPipelineAsset LoadOrCreatePipeline(string path, UniversalRendererData renderer, Tier tier)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                // 明るさの上限を超える演出は無いので HDR は使わない（スマホ・ブラウザで描く色の量を減らす）
                pipeline.supportsHDR = false;
                AssetDatabase.CreateAsset(pipeline, path);
            }

            ApplyTier(pipeline, renderer, tier);
            return pipeline;
        }

        /// <summary>
        /// 影・追加の光は使わない（足元の丸い影と太陽1つだけ）。切っておくと、使わないシェーダーの組み合わせもビルドに入らない。
        /// setter が公開されていない項目もあるので、保存される項目を直接書く
        /// </summary>
        private static void ApplyTier(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer, Tier tier)
        {
            var so = new SerializedObject(pipeline);
            var renderers = so.FindProperty("m_RendererDataList");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.FindProperty("m_MSAA").intValue = tier.Msaa;
            so.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            so.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.Disabled;
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            so.FindProperty("m_SoftShadowsSupported").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 画質の設定の名前で URP の設定を選ぶ（Mobile → スマホ用、それ以外 → PC 用）。
        /// PC は画面の書き換えに合わせて描く（上限なしで回すと、余計に熱くなるだけで見た目は変わらない）。
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
                level.FindPropertyRelative("shadows").intValue = (int)UnityEngine.ShadowQuality.Disable;
                level.FindPropertyRelative("vSyncCount").intValue = isMobile ? 0 : 1;
            }

            settings.ApplyModifiedProperties();
        }
    }
}
