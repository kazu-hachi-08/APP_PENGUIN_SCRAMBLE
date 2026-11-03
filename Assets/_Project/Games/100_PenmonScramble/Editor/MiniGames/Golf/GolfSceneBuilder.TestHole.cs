using PenmonScramble.Common.Editor;
using PenmonScramble.Golf.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 開発用の試しのホール（フェーズ 2）: 仮の見た目・打つ操作・仮の HUD。Tools/PenmonScramble/Golf から開いたときだけ動く。
    /// 本番の見た目と HUD はフェーズ 3 で作り直す
    /// </summary>
    public static partial class GolfSceneBuilder
    {
        private const string ShaderFolder = "Assets/_Project/Games/100_PenmonScramble/Shaders/";
        private const string HelpText = "←→ / スティック: 向き　　J / ×: ゲージ開始 → もう一度で打つ　　K / ○: やり直す　　Esc: もどる";

        private static readonly Vector2 StatusPosition = new Vector2(-560f, 330f);
        private static readonly Vector2 StatusSize = new Vector2(760f, 380f);
        private const int StatusFontSize = 34;
        private static readonly Vector2 MessagePosition = new Vector2(0f, 300f);
        private static readonly Vector2 MessageSize = new Vector2(1400f, 110f);
        private const int MessageFontSize = 64;
        private static readonly Vector2 HelpPosition = new Vector2(0f, -500f);
        private static readonly Vector2 HelpSize = new Vector2(1800f, 60f);
        private const int HelpFontSize = 30;

        private static readonly Vector2 GaugePosition = new Vector2(840f, -60f);
        private static readonly Vector2 GaugeSize = new Vector2(56f, 480f);
        private const float GaugeInset = 6f;
        private static readonly Color GaugeBackColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color GaugeFillColor = new Color(1f, 0.62f, 0.2f);

        private static GolfTestHolePlay CreateTestHole(Transform canvas, Camera camera)
        {
            var root = new GameObject("TestHole");
            var overview = root.AddComponent<GolfOverviewCamera>();
            var cameraSo = new SerializedObject(overview);
            cameraSo.FindProperty("_camera").objectReferenceValue = camera;
            cameraSo.ApplyModifiedProperties();

            var view = root.AddComponent<GolfHoleView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader");
            viewSo.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader");
            viewSo.FindProperty("_camera").objectReferenceValue = overview;
            viewSo.ApplyModifiedProperties();

            var control = root.AddComponent<GolfShotControl>();
            var hud = CreateDevHud(canvas);
            var play = root.AddComponent<GolfTestHolePlay>();
            var playSo = new SerializedObject(play);
            playSo.FindProperty("_view").objectReferenceValue = view;
            playSo.FindProperty("_control").objectReferenceValue = control;
            playSo.FindProperty("_hud").objectReferenceValue = hud;
            playSo.ApplyModifiedProperties();
            return play;
        }

        /// <summary>打順・打数の文字、起きたことの大きな文字、操作の説明、右のゲージ。試しのホールを開くまで隠す</summary>
        private static GolfDevHud CreateDevHud(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("DevHud", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            var parent = root.transform;

            var status = PenmonScrambleSceneBuilder.CreateLabel(parent, "Status", string.Empty, StatusFontSize, StatusPosition, StatusSize);
            status.alignment = TextAnchor.UpperLeft;
            var message = PenmonScrambleSceneBuilder.CreateLabel(parent, "Message", string.Empty, MessageFontSize, MessagePosition, MessageSize);
            message.fontStyle = FontStyle.Bold;
            message.enabled = false;
            PenmonScrambleSceneBuilder.CreateLabel(parent, "Help", HelpText, HelpFontSize, HelpPosition, HelpSize);
            var gauge = CreateGauge(parent, out var fill);

            var hud = root.AddComponent<GolfDevHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("_status").objectReferenceValue = status;
            so.FindProperty("_message").objectReferenceValue = message;
            so.FindProperty("_gauge").objectReferenceValue = gauge;
            so.FindProperty("_gaugeFill").objectReferenceValue = fill;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return hud;
        }

        /// <summary>縦のゲージ。中身は下を軸に縦に伸び縮みさせる（GolfDevHud が強さに合わせて scale を変える）</summary>
        private static GameObject CreateGauge(Transform parent, out RectTransform fill)
        {
            var gauge = UIDialogBuilder.CreateUIObject("Gauge", parent);
            var rect = gauge.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = GaugePosition;
            rect.sizeDelta = GaugeSize;
            var back = gauge.AddComponent<Image>();
            back.color = GaugeBackColor;
            back.raycastTarget = false;

            var fillObj = UIDialogBuilder.CreateUIObject("Fill", gauge.transform);
            fill = fillObj.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.pivot = new Vector2(0.5f, 0f);
            fill.offsetMin = new Vector2(GaugeInset, GaugeInset);
            fill.offsetMax = new Vector2(-GaugeInset, -GaugeInset);
            fill.localScale = new Vector3(1f, 0f, 1f);
            var image = fillObj.AddComponent<Image>();
            image.color = GaugeFillColor;
            image.raycastTarget = false;
            return gauge;
        }
    }
}
