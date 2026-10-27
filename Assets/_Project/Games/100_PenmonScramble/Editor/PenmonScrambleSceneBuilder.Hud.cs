using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>ステージ中の HUD（HP・残りの敵・結果）</summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Vector2 HudMargin = new Vector2(40f, -32f);
        private static readonly Vector2 HpBarSize = new Vector2(560f, 56f);
        private static readonly Vector2 EnemiesTextSize = new Vector2(480f, 64f);
        private const int HudFontSize = 40;
        private static readonly Color HpBarBackColor = new Color(0.15f, 0.18f, 0.25f, 0.8f);
        private static readonly Color HpBarFillColor = new Color(0.35f, 0.85f, 0.45f);

        private const int ResultFontSize = 120;
        private static readonly Color ResultOverlayColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Vector2 RetryButtonSize = new Vector2(420f, 120f);
        private static readonly Vector2 RetryButtonPosition = new Vector2(-240f, -200f);
        private static readonly Vector2 TitleButtonPosition = new Vector2(240f, -200f);
        private static readonly Color TitleButtonColor = new Color(0.45f, 0.5f, 0.6f);
        private const int RetryFontSize = 48;
        private static readonly Color RetryButtonColor = new Color(0.18f, 0.55f, 0.9f);

        private static StageHud CreateHud(Transform canvas)
        {
            var hudObj = UIDialogBuilder.CreateUIObject("Hud", canvas);
            UIDialogBuilder.SetStretchAll(hudObj.GetComponent<RectTransform>());
            var hud = hudObj.AddComponent<StageHud>();

            var (fill, hpText) = CreateHpBar(hudObj.transform);
            var enemiesText = CreateEnemiesText(hudObj.transform);
            var (panel, resultText, retry, toTitle) = CreateResultPanel(hudObj.transform);

            var so = new SerializedObject(hud);
            so.FindProperty("_hpFill").objectReferenceValue = fill;
            so.FindProperty("_hpFillImage").objectReferenceValue = fill.GetComponent<Image>();
            so.FindProperty("_hpText").objectReferenceValue = hpText;
            so.FindProperty("_enemiesText").objectReferenceValue = enemiesText;
            so.FindProperty("_resultPanel").objectReferenceValue = panel;
            so.FindProperty("_resultText").objectReferenceValue = resultText;
            so.FindProperty("_retryButton").objectReferenceValue = retry;
            so.FindProperty("_titleButton").objectReferenceValue = toTitle;
            so.ApplyModifiedProperties();
            return hud;
        }

        private static (RectTransform fill, Text text) CreateHpBar(Transform parent)
        {
            var barObj = UIDialogBuilder.CreateUIObject("HpBar", parent);
            var rect = barObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = HudMargin;
            rect.sizeDelta = HpBarSize;
            barObj.AddComponent<Image>().color = HpBarBackColor;

            // 右端（anchorMax.x）を HP の割合に合わせて縮める
            var fillObj = UIDialogBuilder.CreateUIObject("Fill", barObj.transform);
            var fill = fillObj.GetComponent<RectTransform>();
            UIDialogBuilder.SetStretchAll(fill);
            fillObj.AddComponent<Image>().color = HpBarFillColor;

            var textObj = UIDialogBuilder.CreateUIObject("Text", barObj.transform);
            UIDialogBuilder.SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = AddText(textObj, "HP", HudFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return (fill, text);
        }

        private static Text CreateEnemiesText(Transform parent)
        {
            var obj = UIDialogBuilder.CreateUIObject("EnemiesLeft", parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-HudMargin.x, HudMargin.y);
            rect.sizeDelta = EnemiesTextSize;
            var text = AddText(obj, string.Empty, HudFontSize);
            text.alignment = TextAnchor.MiddleRight;
            text.color = Color.white;
            return text;
        }

        private static (GameObject panel, Text text, Button retry, Button toTitle) CreateResultPanel(Transform parent)
        {
            var panel = UIDialogBuilder.CreateUIObject("Result", parent);
            UIDialogBuilder.SetStretchAll(panel.GetComponent<RectTransform>());
            panel.AddComponent<Image>().color = ResultOverlayColor;

            var textObj = UIDialogBuilder.CreateUIObject("Text", panel.transform);
            UIDialogBuilder.SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = AddText(textObj, string.Empty, ResultFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            var retry = CreateResultButton(panel.transform, "Retry", "もう一度", RetryButtonPosition, RetryButtonColor);
            var toTitle = CreateResultButton(panel.transform, "Title", "タイトルへ", TitleButtonPosition, TitleButtonColor);

            panel.SetActive(false);
            return (panel, text, retry, toTitle);
        }

        private static Button CreateResultButton(Transform parent, string name, string label, Vector2 position, Color color)
        {
            var buttonObj = UIDialogBuilder.CreateButton(parent, name, label, RetryButtonSize.x, RetryButtonSize.y, color);
            buttonObj.GetComponent<RectTransform>().anchoredPosition = position;
            buttonObj.GetComponentInChildren<Text>().fontSize = RetryFontSize;
            return buttonObj.GetComponent<Button>();
        }
    }
}
