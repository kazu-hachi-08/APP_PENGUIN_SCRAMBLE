using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 強化の窓（ペンモン画面の上に重ねる）: 左に強化する子（顔・◆・パワーの見込み・ゲージ・上限）、
    /// 右に素材を選ぶ一覧（ペンモン画面と同じカード）、下に必要なコインと「強化する」。演出用の光と、飛ぶ顔の型も作る
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Vector2 EnhanceWindowSize = new Vector2(1860f, 1030f);
        private static readonly Color EnhanceGlowColor = new Color(1f, 0.92f, 0.55f, 0f);
        private static readonly Color GaugeAfterColor = new Color(0.49f, 1f, 0.54f);
        private static readonly Color GaugeNowColor = new Color(0.45f, 0.85f, 1f);
        private static readonly Color EnhanceGoColor = new Color(1f, 0.55f, 0.20f);

        // 上の段
        private static readonly Vector2 EnhanceBackPosition = new Vector2(-790f, 448f);
        private static readonly Vector2 EnhanceBackSize = new Vector2(220f, 84f);
        private static readonly Vector2 EnhanceHeadingPosition = new Vector2(-560f, 448f);
        private static readonly Vector2 EnhanceHeadingSize = new Vector2(220f, 90f);
        private static readonly Vector2 EnhanceCoinPosition = new Vector2(640f, 448f);

        // 左: 強化する子（板の中心からの位置）
        private static readonly Vector2 TargetPanelPosition = new Vector2(-520f, -10f);
        private static readonly Vector2 TargetPanelSize = new Vector2(720f, 740f);
        private static readonly Vector2 TargetFacePosition = new Vector2(0f, 205f);
        private const float TargetFaceSize = 220f;
        private const float TargetGlowSize = 300f;
        private const float TargetPipY = 70f;
        private const float TargetPipSpacing = 56f;
        private static readonly Vector2 TargetPipSize = new Vector2(56f, 56f);
        private const int TargetPipFontSize = 46;
        private static readonly Vector2 TargetTitlePosition = new Vector2(0f, 5f);
        private const int TargetTitleFontSize = 46;
        private static readonly Vector2 TargetPowerPosition = new Vector2(0f, -85f);
        private const int TargetPowerFontSize = 56;
        private static readonly Vector2 TargetGaugePosition = new Vector2(0f, -165f);
        private static readonly Vector2 TargetGaugeSize = new Vector2(600f, 30f);
        private static readonly Vector2 TargetCapPosition = new Vector2(0f, -225f);
        private const int TargetCapFontSize = 34;
        private static readonly Vector2 TargetRoomPosition = new Vector2(0f, -285f);
        private const int TargetRoomFontSize = 30;
        private const float TargetLineWidth = 680f;
        private const float TargetLineHeight = 60f;

        // 右: 素材の一覧
        private static readonly Vector2 MaterialHeaderPosition = new Vector2(270f, 380f);
        private static readonly Vector2 MaterialHeaderSize = new Vector2(900f, 56f);
        private const int MaterialHeaderFontSize = 36;
        private static readonly Vector2 MaterialClearPosition = new Vector2(770f, 380f);
        private static readonly Vector2 MaterialClearSize = new Vector2(220f, 70f);
        private static readonly Vector2 MaterialGridPosition = new Vector2(330f, -10f);
        private static readonly Vector2 MaterialGridSize = new Vector2(1060f, 700f);

        // 下の段
        private const float EnhanceBottomY = -450f;
        private static readonly Vector2 EnhanceHintPosition = new Vector2(-460f, EnhanceBottomY);
        private static readonly Vector2 EnhanceHintSize = new Vector2(840f, 60f);
        private const int EnhanceHintFontSize = 28;
        private static readonly Vector2 EnhanceCostPosition = new Vector2(220f, EnhanceBottomY);
        private static readonly Vector2 EnhanceCostSize = new Vector2(440f, 70f);
        private static readonly Vector2 EnhanceGoPosition = new Vector2(680f, EnhanceBottomY);
        private static readonly Vector2 EnhanceGoSize = new Vector2(440f, 100f);
        private const int EnhanceGoFontSize = 44;

        private const float FlyFaceSize = 100f;

        private static EnhanceScreen CreateEnhanceScreen(Transform parent)
        {
            var root = CreateFullScreenBacking(parent, "Enhance", ModalDimColor);
            CreatePanel(root.transform, "Window", Vector2.zero, EnhanceWindowSize);
            var back = CreateTitleButton(root.transform, "Back", "もどる", EnhanceBackPosition, EnhanceBackSize, CancelColor, MenuFontSize);
            CreateLabel(root.transform, "Heading", "強化", PenmonHeadingFontSize, EnhanceHeadingPosition, EnhanceHeadingSize).fontStyle = FontStyle.Bold;
            var coin = CreateLabel(root.transform, "Coins", string.Empty, MenuFontSize, EnhanceCoinPosition, CoinSize);
            coin.alignment = TextAnchor.MiddleRight;

            var screen = root.AddComponent<EnhanceScreen>();
            var so = new SerializedObject(screen);
            CreateEnhanceTarget(root.transform, so);
            CreateEnhanceMaterials(root.transform, so);
            CreateEnhanceBottom(root.transform, so);
            CreateEnhanceEffects(root.transform, so);
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return screen;
        }

        private static void CreateEnhanceTarget(Transform root, SerializedObject so)
        {
            var panel = CreatePanel(root, "Target", TargetPanelPosition, TargetPanelSize, PanelColor).transform;
            // 光は顔の後ろ（先に作った方が奥）
            var glow = CreateCircle(panel, "Glow", TargetFacePosition, TargetGlowSize, EnhanceGlowColor);
            var face = CreateCircle(panel, "Face", TargetFacePosition, TargetFaceSize, Color.white);
            var pips = new Text[Core.Balance.MaxLimitBreak];
            for (var i = 0; i < pips.Length; i++)
            {
                var x = (i - (pips.Length - 1) * 0.5f) * TargetPipSpacing;
                pips[i] = CreatePlainText(panel, $"Pip{i + 1}", "◇", TargetPipFontSize, new Vector2(x, TargetPipY), TargetPipSize, TextAnchor.MiddleCenter, Color.white);
                pips[i].gameObject.AddComponent<Outline>().effectColor = LabelOutlineColor;
            }

            var title = CreateTargetLine(panel, "Title", TargetTitlePosition, TargetTitleFontSize);
            title.fontStyle = FontStyle.Bold;
            var power = CreateTargetLine(panel, "Power", TargetPowerPosition, TargetPowerFontSize);
            power.fontStyle = FontStyle.Bold;
            CreatePanel(panel, "GaugeBack", TargetGaugePosition, TargetGaugeSize, BarBackColor).raycastTarget = false;
            var after = CreateGaugeFill(panel, "GaugeAfter", GaugeAfterColor);
            var now = CreateGaugeFill(panel, "GaugeNow", GaugeNowColor);

            so.FindProperty("_glow").objectReferenceValue = glow;
            so.FindProperty("_face").objectReferenceValue = face;
            SetObjectArray(so.FindProperty("_pips"), pips);
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_powerText").objectReferenceValue = power;
            so.FindProperty("_gaugeNow").objectReferenceValue = now;
            so.FindProperty("_gaugeAfter").objectReferenceValue = after;
            so.FindProperty("_capText").objectReferenceValue = CreateTargetLine(panel, "Cap", TargetCapPosition, TargetCapFontSize);
            so.FindProperty("_roomText").objectReferenceValue = CreateTargetLine(panel, "Room", TargetRoomPosition, TargetRoomFontSize);
        }

        private static Text CreateTargetLine(Transform panel, string name, Vector2 position, int fontSize)
        {
            return CreatePlainText(panel, name, string.Empty, fontSize, position, new Vector2(TargetLineWidth, TargetLineHeight), TextAnchor.MiddleCenter, Color.white);
        }

        /// <summary>ゲージの塗り（今のパワーを手前、強化後を奥に重ね、伸びる分が明るい緑で見える）</summary>
        private static Image CreateGaugeFill(Transform panel, string name, Color color)
        {
            var fill = CreatePanel(panel, name, TargetGaugePosition, TargetGaugeSize, color);
            fill.raycastTarget = false;
            // 角の丸い絵は塗りつぶしで切ると端が欠けるので、四角で描く
            fill.sprite = null;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            return fill;
        }

        private static void CreateEnhanceMaterials(Transform root, SerializedObject so)
        {
            var header = CreateLabel(root, "MaterialHeader", string.Empty, MaterialHeaderFontSize, MaterialHeaderPosition, MaterialHeaderSize);
            header.alignment = TextAnchor.MiddleLeft;
            var clear = CreateTitleButton(root, "Clear", "選び直す", MaterialClearPosition, MaterialClearSize, ArrowColor, SmallFontSize);
            var content = CreateCardGrid(root, MaterialGridPosition, MaterialGridSize);
            var template = CreatePenguinCard(content, "CardTemplate", PenguinCardSize, ListCardFontSize);
            template.gameObject.SetActive(false);
            var empty = CreatePlainText(root, "Empty", "素材にできる子がいません\n（編成の3体は素材にできません）", MenuFontSize, MaterialGridPosition, MaterialGridSize, TextAnchor.MiddleCenter, SubTextColor);

            so.FindProperty("_materialCountText").objectReferenceValue = header;
            so.FindProperty("_clearButton").objectReferenceValue = clear;
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_cardTemplate").objectReferenceValue = template;
            so.FindProperty("_emptyText").objectReferenceValue = empty;
        }

        private static void CreateEnhanceBottom(Transform root, SerializedObject so)
        {
            var hint = CreateLabel(root, "Hint", string.Empty, EnhanceHintFontSize, EnhanceHintPosition, EnhanceHintSize);
            hint.alignment = TextAnchor.MiddleLeft;
            var cost = CreateLabel(root, "Cost", string.Empty, MenuFontSize, EnhanceCostPosition, EnhanceCostSize);
            cost.alignment = TextAnchor.MiddleRight;
            var go = CreateTitleButton(root, "Go", "強化する", EnhanceGoPosition, EnhanceGoSize, EnhanceGoColor, EnhanceGoFontSize);

            so.FindProperty("_hintText").objectReferenceValue = hint;
            so.FindProperty("_costText").objectReferenceValue = cost;
            so.FindProperty("_enhanceButton").objectReferenceValue = go;
            so.FindProperty("_enhanceLabel").objectReferenceValue = go.GetComponentInChildren<Text>();
        }

        /// <summary>飛ぶ顔の型と、演出の間だけ出す「どこを押してもとばす」透明なボタン（一番手前）</summary>
        private static void CreateEnhanceEffects(Transform root, SerializedObject so)
        {
            var fly = CreateCircle(root, "FlyTemplate", Vector2.zero, FlyFaceSize, Color.white);
            fly.gameObject.SetActive(false);
            var skipObj = CreateFullScreenBacking(root, "Skip", Color.clear);
            var skip = skipObj.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skipObj.SetActive(false);

            so.FindProperty("_flyTemplate").objectReferenceValue = fly;
            so.FindProperty("_skipButton").objectReferenceValue = skip;
        }
    }
}
