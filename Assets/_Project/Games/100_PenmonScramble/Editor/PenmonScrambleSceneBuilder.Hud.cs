using PenmonScramble.Common.Editor;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ステージ中の HUDの土台・画面の真ん中の知らせ・結果・右上のメニューボタンと、必殺技のカットイン。
    /// 自分のプレート・エリアの進み具合・ボスの HP は PenmonScrambleSceneBuilder.HudPlates.cs
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        // HUD の板や文字の縁取りの濃い紺
        private static readonly Color HudBackColor = new Color(0.08f, 0.11f, 0.22f, 0.85f);

        // 画面の端の印: 大ボス1・仲間3・予告がいくつか出ても足りる数。大きさはスマホでも指の先くらい
        private const int EdgeMarkerCount = 8;
        private const float EdgeMarkerSize = 120f;
        private const float EdgeMarkerRimRatio = 1.14f;
        // 顔の丸は、とがった丸の丸い所（絵の半分の 0.6）に収まる大きさ（AddPortraitFace はこの 0.76 倍で描く）
        private const float EdgeMarkerFaceRatio = 0.72f;
        private const int EdgeMarkerSignFontSize = 52;
        private const int EdgeMarkerLabelFontSize = 26;
        private static readonly Vector2 EdgeMarkerLabelPosition = new Vector2(0f, -64f);
        private static readonly Vector2 EdgeMarkerLabelSize = new Vector2(120f, 36f);
        private const float BannerY = 200f;
        private static readonly Vector2 BannerSize = new Vector2(1400f, 160f);
        private const int BannerFontSize = 104;
        private static readonly Vector2 BannerOutlineDistance = new Vector2(4f, -4f);
        private static readonly Vector2 BannerShadowDistance = new Vector2(0f, -8f);

        private const float CutInHeight = 320f;
        private const float CutInFaceSize = 240f;
        private const float CutInFaceX = 360f;
        private const int CutInFontSize = 64;

        private const int ResultFontSize = 120;
        private static readonly Color ResultOverlayColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Vector2 RetryButtonSize = new Vector2(420f, 120f);
        private static readonly Vector2 RetryButtonPosition = new Vector2(-240f, -200f);
        private static readonly Vector2 TitleButtonPosition = new Vector2(240f, -200f);
        private static readonly Color TitleButtonColor = new Color(0.45f, 0.5f, 0.6f);
        private const int RetryFontSize = 48;
        private static readonly Color RetryButtonColor = new Color(0.18f, 0.55f, 0.9f);

        // 右上のメニューボタン。指で押しやすい大きさにして、進み具合はその左へずらす
        private const float HudMenuButtonSize = 120f;
        private const float MenuButtonGap = 24f;
        private const float MenuBarWidthRatio = 0.14f;
        private const float MenuBarHeightRatio = 0.48f;
        private const float MenuBarSpacingRatio = 0.24f;
        private const string MenuHeading = "メニュー";

        private static StageHud CreateHud(Transform canvas)
        {
            var hudObj = CreateStretchGroup(canvas, "Hud");
            var hud = hudObj.AddComponent<StageHud>();
            var so = new SerializedObject(hud);

            // プレートはノッチや角の丸みで欠けないよう、セーフエリアの内側に置く
            var safe = CreateStretchGroup(hudObj.transform, "SafeArea");
            safe.AddComponent<SafeAreaFitter>();
            CreatePlayerPlate(safe.transform, so);
            CreateProgressPlate(safe.transform, so);
            CreateBossPlate(safe.transform, so);
            so.FindProperty("_edgeMarkers").objectReferenceValue = CreateEdgeMarkers(safe.transform);
            // タイトルの間は隠しておき、ステージが始まったら StageHud が出す
            safe.SetActive(false);
            so.FindProperty("_plates").objectReferenceValue = safe;
            so.FindProperty("_bannerText").objectReferenceValue = CreateBanner(hudObj.transform);

            var (panel, resultText, retry, toTitle) = CreateResultPanel(hudObj.transform);
            so.FindProperty("_resultPanel").objectReferenceValue = panel;
            so.FindProperty("_resultText").objectReferenceValue = resultText;
            so.FindProperty("_retryButton").objectReferenceValue = retry;
            so.FindProperty("_titleButton").objectReferenceValue = toTitle;
            so.ApplyModifiedProperties();
            return hud;
        }

        /// <summary>
        /// 画面の外の大ボス・仲間・予告を指す端の印（EdgeMarkers）。セーフエリアいっぱいに広げた入れ物に、決まった数だけ作っておく。
        /// 印は、外を指すとがった丸（暗い縁の上に色）・丸の中の顔か「！」・丸の下の名前
        /// </summary>
        private static EdgeMarkers CreateEdgeMarkers(Transform safe)
        {
            var area = CreateHudRect(safe, "EdgeMarkers", Middle, Vector2.zero, Vector2.zero);
            UIDialogBuilder.SetStretchAll(area);
            var markers = area.gameObject.AddComponent<EdgeMarkers>();
            var so = new SerializedObject(markers);
            so.FindProperty("_area").objectReferenceValue = area;
            var list = so.FindProperty("_markers");
            list.arraySize = EdgeMarkerCount;
            for (var i = 0; i < EdgeMarkerCount; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = CreateEdgeMarker(area, i);
            }

            so.ApplyModifiedProperties();
            return markers;
        }

        private static EdgeMarkerView CreateEdgeMarker(RectTransform area, int index)
        {
            var size = Vector2.one * EdgeMarkerSize;
            var root = CreateHudRect(area, $"Marker{index}", Middle, Vector2.zero, size);
            var pivot = CreateHudRect(root, "PointerPivot", Middle, Vector2.zero, size);
            CreateHudImage(pivot, "Rim", HudMarkerPinPath, HudRimColor, Middle, Vector2.zero, size * EdgeMarkerRimRatio);
            var pointer = CreateHudImage(pivot, "Pointer", HudMarkerPinPath, Color.white, Middle, Vector2.zero, size);
            var faceGroup = CreateHudRect(root, "FaceGroup", Middle, Vector2.zero, size);
            var (_, face) = AddPortraitFace(faceGroup, EdgeMarkerSize * EdgeMarkerFaceRatio);
            var sign = CreateHudLabel(root, "Sign", EdgeMarkerSignFontSize, TextAnchor.MiddleCenter, 0f, 0f);
            var labelRect = CreateHudRect(root, "LabelArea", Middle, EdgeMarkerLabelPosition, EdgeMarkerLabelSize);
            var label = CreateHudLabel(labelRect, "Label", EdgeMarkerLabelFontSize, TextAnchor.MiddleCenter, 0f, 0f);

            var view = root.gameObject.AddComponent<EdgeMarkerView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_pointerPivot").objectReferenceValue = pivot;
            so.FindProperty("_pointer").objectReferenceValue = pointer;
            so.FindProperty("_faceGroup").objectReferenceValue = faceGroup.gameObject;
            so.FindProperty("_face").objectReferenceValue = face;
            so.FindProperty("_sign").objectReferenceValue = sign;
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedProperties();
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>画面の真ん中を横切る帯。出す間だけ有効にする（SpecialCutIn）</summary>
        private static SpecialCutIn CreateCutIn(Transform canvas)
        {
            var root = CreateStretchGroup(canvas, "SpecialCutIn");

            var band = UIDialogBuilder.CreateUIObject("Band", root.transform);
            var bandRect = band.GetComponent<RectTransform>();
            bandRect.anchorMin = new Vector2(0f, 0.5f);
            bandRect.anchorMax = new Vector2(1f, 0.5f);
            bandRect.sizeDelta = new Vector2(0f, CutInHeight);
            var bandImage = band.AddComponent<Image>();
            // 帯の下のボタンを押せなくしない
            bandImage.raycastTarget = false;

            var face = UIDialogBuilder.CreateUIObject("Face", band.transform);
            var faceRect = face.GetComponent<RectTransform>();
            faceRect.anchorMin = faceRect.anchorMax = new Vector2(0f, 0.5f);
            faceRect.anchoredPosition = new Vector2(CutInFaceX, 0f);
            faceRect.sizeDelta = Vector2.one * CutInFaceSize;
            var faceImage = face.AddComponent<Image>();
            faceImage.raycastTarget = false;

            var textObj = CreateStretchGroup(band.transform, "Text");
            var text = AddText(textObj, string.Empty, CutInFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            var cutIn = root.AddComponent<SpecialCutIn>();
            var so = new SerializedObject(cutIn);
            so.FindProperty("_band").objectReferenceValue = bandRect;
            so.FindProperty("_bandImage").objectReferenceValue = bandImage;
            so.FindProperty("_face").objectReferenceValue = faceImage;
            so.FindProperty("_text").objectReferenceValue = text;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return cutIn;
        }

        /// <summary>「エリア 2」「すすめ →」などを真ん中の少し上に大きく出す。下のボタンを押せなくしない</summary>
        private static Text CreateBanner(Transform parent)
        {
            var obj = UIDialogBuilder.CreateUIObject("Banner", parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, BannerY);
            rect.sizeDelta = BannerSize;
            var text = AddText(obj, string.Empty, BannerFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 1f, 1f, 0f);
            text.raycastTarget = false;
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = HudBackColor;
            outline.effectDistance = BannerOutlineDistance;
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = ButtonShadowColor;
            shadow.effectDistance = BannerShadowDistance;
            return text;
        }

        /// <summary>
        /// 右上の「‖」。角に寄せすぎると機種によって押せないので、セーフエリアの内側に置く。
        /// 遊んでいる間だけ StageMenu が出す
        /// </summary>
        private static Button CreateMenuButton(Transform hud)
        {
            var area = CreateStretchGroup(hud, "MenuButtonArea");
            area.AddComponent<SafeAreaFitter>();
            var obj = UIDialogBuilder.CreateUIObject("MenuButton", area.transform);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-HudMargin.x, HudMargin.y);
            rect.sizeDelta = Vector2.one * HudMenuButtonSize;
            var image = obj.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(RoundedSpritePath);
            image.type = Image.Type.Sliced;
            image.color = HudBackColor;
            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            // キーボード・コントローラーの「決定」で押されないように（Space は交代なので）
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            CreateMenuIcon(obj.transform, "Icon", HudMenuButtonSize);
            return button;
        }

        /// <summary>縦の棒2本の「‖」（一時停止の印）。文字にするとフォントで形が変わるので絵で描く</summary>
        private static GameObject CreateMenuIcon(Transform parent, string name, float size)
        {
            var icon = UIDialogBuilder.CreateUIObject(name, parent);
            icon.GetComponent<RectTransform>().sizeDelta = Vector2.one * size;
            var barSize = new Vector2(size * MenuBarWidthRatio, size * MenuBarHeightRatio);
            foreach (var side in new[] { -1f, 1f })
            {
                CreateRoundedBox(icon.transform, "Bar", new Vector2(side * size * MenuBarSpacingRatio * 0.5f, 0f), barSize, Color.white, null);
            }

            return icon;
        }

        /// <summary>ステージ中のメニュー。ポーズと確認は共通基盤のダイアログ、遊び方はタイトルと同じ画面を使う</summary>
        private static StageMenu CreateStageMenu(Button openButton, PauseDialog dialog, CommonDialog confirm, HowToPlayScreen howToPlay)
        {
            var menu = new GameObject("StageMenu").AddComponent<StageMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("_openButton").objectReferenceValue = openButton;
            so.FindProperty("_dialog").objectReferenceValue = dialog;
            so.FindProperty("_confirm").objectReferenceValue = confirm;
            so.FindProperty("_howToPlay").objectReferenceValue = howToPlay;
            so.ApplyModifiedProperties();
            return menu;
        }

        private static (GameObject panel, Text text, Button retry, Button toTitle) CreateResultPanel(Transform parent)
        {
            var panel = CreateStretchGroup(parent, "Result");
            panel.AddComponent<Image>().color = ResultOverlayColor;

            var textObj = CreateStretchGroup(panel.transform, "Text");
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
