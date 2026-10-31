using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// メニューの部品（§9.6）: 押せる物に見えるボタン（色の面・上のつや・白い縁・下の影・押すと縮む）、角の丸い板、
    /// 画面をふさぐ窓、丸いアイコンのボタン、画面の上下のぼかし。絵は PenmonScrambleSceneBuilder.UiArt.cs
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        // ボタンの角の丸み（画面の単位）。低いボタンは高さの半分（両端がまるい形）にする
        private const float ButtonCornerRadius = 36f;
        private const float PanelCornerRadius = 32f;
        private static readonly Color ButtonFrameColor = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Vector2 ButtonDropDistance = new Vector2(0f, -8f);
        // 押している間の色（縮むのは UiButtonMotion）。押せないときの薄さは UiButtonMotion が全体に掛ける
        private static readonly Color ButtonPressedTint = new Color(0.85f, 0.85f, 0.85f);
        private const float ButtonLabelDarken = 0.45f;
        private const float ButtonTextPadding = 12f;

        private static readonly Color MenuPanelColor = new Color(0.08f, 0.12f, 0.26f, 0.92f);
        private static readonly Color PanelFrameColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color ModalDimColor = new Color(0.02f, 0.03f, 0.10f, 0.6f);
        private static readonly Vector2 PanelDropDistance = new Vector2(0f, -10f);
        private const int ModalHeadingFontSize = 56;
        private const float ModalHeadingMargin = 70f;
        // 窓は下から少し持ち上がって開く
        private static readonly Vector2 ModalSlideFrom = new Vector2(0f, -120f);

        // 丸いアイコンのボタン: アイコンは丸の中のこの割合、名前は丸の下
        private const float IconButtonIconRatio = 0.66f;
        private const float IconButtonLabelGap = 34f;
        private const int IconButtonLabelFontSize = 34;

        /// <summary>
        /// 色の面に、上のつや・白い縁・下の影を重ねたボタン。色の面がボタンの targetGraphic（色を変える画面はここを塗る）。
        /// 文字はボタンの色を濃くした色で縁取る
        /// </summary>
        private static Button CreateTitleButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, int fontSize)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, size);
            var cornerScale = CornerScale(Mathf.Min(ButtonCornerRadius, Mathf.Min(size.x, size.y) * 0.5f));
            var body = AddSliced(obj, ButtonBodyPath, color, cornerScale);
            var drop = obj.AddComponent<Shadow>();
            drop.effectColor = ButtonShadowColor;
            drop.effectDistance = ButtonDropDistance;

            var button = obj.AddComponent<Button>();
            button.targetGraphic = body;
            var colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = ButtonPressedTint;
            colors.disabledColor = Color.white;
            button.colors = colors;

            AddSlicedLayer(obj.transform, "Gloss", ButtonGlossPath, Color.white, cornerScale);
            AddSlicedLayer(obj.transform, "Frame", ButtonFramePath, ButtonFrameColor, cornerScale);
            CreateButtonText(obj.transform, label, fontSize, color);
            obj.AddComponent<UiButtonMotion>();
            return button;
        }

        private static void CreateButtonText(Transform parent, string label, int fontSize, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject("Text", parent);
            var rect = obj.GetComponent<RectTransform>();
            UIDialogBuilder.SetStretchAll(rect);
            rect.offsetMin = new Vector2(ButtonTextPadding, ButtonTextPadding * 0.5f);
            rect.offsetMax = new Vector2(-ButtonTextPadding, 0f);
            var text = AddText(obj, label, fontSize);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = Color.Lerp(color, Color.black, 1f - ButtonLabelDarken);
            outline.effectDistance = LabelOutlineDistance;
        }

        /// <summary>角の丸み: 絵の半径に対して、画面でこの半径に見せる倍率（Image.pixelsPerUnitMultiplier）</summary>
        private static float CornerScale(float radius) => RoundSpriteRadius / Mathf.Max(1f, radius);

        private static Image AddSliced(GameObject obj, string spritePath, Color color, float cornerScale)
        {
            var image = obj.AddComponent<Image>();
            image.sprite = UiSprite(spritePath);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale;
            image.color = color;
            return image;
        }

        /// <summary>親いっぱいに重ねる飾りの層（押す邪魔をしない）</summary>
        private static Image AddSlicedLayer(Transform parent, string name, string spritePath, Color color, float cornerScale)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            var image = AddSliced(obj, spritePath, color, cornerScale);
            image.raycastTarget = false;
            return image;
        }

        /// <summary>濃い紺の半透明の板（うすい白の縁と下の影）。ステージのカード・コインの札・窓の中身に使う</summary>
        private static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, size);
            var cornerScale = CornerScale(Mathf.Min(PanelCornerRadius, Mathf.Min(size.x, size.y) * 0.5f));
            AddSliced(obj, RoundRectPath, MenuPanelColor, cornerScale).raycastTarget = false;
            var drop = obj.AddComponent<Shadow>();
            drop.effectColor = ButtonShadowColor;
            drop.effectDistance = PanelDropDistance;
            AddSlicedLayer(obj.transform, "Frame", ButtonFramePath, PanelFrameColor, cornerScale);
            return obj;
        }

        /// <summary>
        /// 画面全体を暗くしてふさぎ（後ろのボタンを押せないように）、まん中に板と見出しを出す窓。
        /// 中身は返した板（panel）に置く。閉じた状態で作る
        /// </summary>
        private static GameObject CreateModal(Transform parent, string name, Vector2 panelSize, string heading, out Transform panel, out Text headingText)
        {
            var root = UIDialogBuilder.CreateUIObject(name, parent);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            root.AddComponent<Image>().color = ModalDimColor;
            var panelObj = CreatePanel(root.transform, "Panel", Vector2.zero, panelSize);
            SetSlideIn(panelObj, ModalSlideFrom, 0f);
            panel = panelObj.transform;
            headingText = CreateLabel(panel, "Heading", heading, ModalHeadingFontSize, new Vector2(0f, panelSize.y * 0.5f - ModalHeadingMargin), new Vector2(panelSize.x, 90f));
            headingText.fontStyle = FontStyle.Bold;
            root.SetActive(false);
            return root;
        }

        /// <summary>丸いボタンの中にアイコン、下に名前。アイコンの絵が無ければ glyph の文字を大きく出す</summary>
        private static Button CreateIconButton(Transform parent, string name, string label, Vector2 position, float diameter, Color color, Sprite icon, string glyph = null)
        {
            var button = CreateTitleButton(parent, name, glyph ?? string.Empty, position, Vector2.one * diameter, color, Mathf.RoundToInt(diameter * 0.5f));
            if (icon != null)
            {
                var iconObj = UIDialogBuilder.CreateUIObject("Icon", button.transform);
                PlaceCenter(iconObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one * diameter * IconButtonIconRatio);
                var image = iconObj.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            if (!string.IsNullOrEmpty(label))
            {
                CreateLabel(button.transform, "Label", label, IconButtonLabelFontSize, new Vector2(0f, -diameter * 0.5f - IconButtonLabelGap), new Vector2(diameter * 1.6f, 50f)).fontStyle = FontStyle.Bold;
            }

            return button;
        }

        /// <summary>画面の上か下の端に、端が濃く内側へ消える暗い帯を敷く（ジオラマの上でも文字やボタンを読めるように）</summary>
        private static void CreateEdgeFade(Transform parent, string name, bool top, float height, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            var rect = obj.GetComponent<RectTransform>();
            var edge = top ? 1f : 0f;
            rect.anchorMin = new Vector2(0f, edge);
            rect.anchorMax = new Vector2(1f, edge);
            rect.pivot = new Vector2(0.5f, edge);
            rect.sizeDelta = new Vector2(0f, height);
            // 絵は下が濃いので、上の帯は上下を返す
            rect.localScale = new Vector3(1f, top ? -1f : 1f, 1f);
            var image = obj.AddComponent<Image>();
            image.sprite = UiSprite(FadePath);
            image.color = color;
            image.raycastTarget = false;
        }

        private static void SetSlideIn(GameObject obj, Vector2 from, float delay)
        {
            var slide = obj.AddComponent<UiSlideIn>();
            var so = new SerializedObject(slide);
            so.FindProperty("_from").vector2Value = from;
            so.FindProperty("_delay").floatValue = delay;
            so.ApplyModifiedProperties();
        }
    }
}
