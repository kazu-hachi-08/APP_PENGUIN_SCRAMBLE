using PenmonScramble.Common.Editor;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>ステージ中の HUD（HP・3体・必殺技のゲージ・エリアの進み具合・ボスの HP・結果・右上のメニューボタン）と必殺技のカットイン</summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Vector2 HudMargin = new Vector2(40f, -32f);
        private static readonly Vector2 HpBarSize = new Vector2(560f, 56f);
        private static readonly Vector2 ProgressTextSize = new Vector2(720f, 64f);
        private const int HudFontSize = 40;
        private static readonly Color HpBarBackColor = new Color(0.15f, 0.18f, 0.25f, 0.8f);
        private static readonly Color HpBarFillColor = new Color(0.35f, 0.85f, 0.45f);
        private const float PartyTextY = -96f;
        private static readonly Vector2 PartyTextSize = new Vector2(1100f, 48f);
        private const int PartyFontSize = 32;
        private const float GaugeBarY = -152f;
        private static readonly Vector2 GaugeBarSize = new Vector2(560f, 36f);
        private const int GaugeFontSize = 28;
        private static readonly Color GaugeFillColor = new Color(1f, 0.6f, 0.2f);

        // HP のバーと右上の文字の間（上の真ん中）。下の段は3体の文字とぶつかるため
        private static readonly Vector2 BossBarSize = new Vector2(520f, 56f);
        private static readonly Color BossBarFillColor = new Color(0.9f, 0.25f, 0.25f);
        private const int BossFontSize = 30;
        private const float BannerY = 200f;
        private static readonly Vector2 BannerSize = new Vector2(1400f, 160f);
        private const int BannerFontSize = 104;

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

        // 右上のメニューボタン（§3.2）。指で押しやすい大きさにして、進み具合の文字はその左へずらす
        private const float HudMenuButtonSize = 120f;
        private const float MenuButtonGap = 24f;
        private const float MenuBarWidthRatio = 0.14f;
        private const float MenuBarHeightRatio = 0.48f;
        private const float MenuBarSpacingRatio = 0.24f;
        private const string MenuHeading = "メニュー";

        private static StageHud CreateHud(Transform canvas)
        {
            var hudObj = UIDialogBuilder.CreateUIObject("Hud", canvas);
            UIDialogBuilder.SetStretchAll(hudObj.GetComponent<RectTransform>());
            var hud = hudObj.AddComponent<StageHud>();

            var (fill, hpText) = CreateBar(hudObj.transform, "HpBar", HudMargin, HpBarSize, HpBarFillColor, HudFontSize);
            var partyText = CreatePartyText(hudObj.transform);
            var (gaugeFill, gaugeText) = CreateBar(hudObj.transform, "GaugeBar", new Vector2(HudMargin.x, GaugeBarY), GaugeBarSize, GaugeFillColor, GaugeFontSize);
            var progressText = CreateProgressText(hudObj.transform);
            var (bossBar, bossFill, bossText) = CreateBossBar(hudObj.transform);
            var bannerText = CreateBanner(hudObj.transform);
            var (panel, resultText, retry, toTitle) = CreateResultPanel(hudObj.transform);

            var so = new SerializedObject(hud);
            so.FindProperty("_hpFill").objectReferenceValue = fill;
            so.FindProperty("_hpFillImage").objectReferenceValue = fill.GetComponent<Image>();
            so.FindProperty("_hpText").objectReferenceValue = hpText;
            so.FindProperty("_partyText").objectReferenceValue = partyText;
            so.FindProperty("_gaugeFill").objectReferenceValue = gaugeFill;
            so.FindProperty("_gaugeFillImage").objectReferenceValue = gaugeFill.GetComponent<Image>();
            so.FindProperty("_gaugeText").objectReferenceValue = gaugeText;
            so.FindProperty("_progressText").objectReferenceValue = progressText;
            so.FindProperty("_bossBar").objectReferenceValue = bossBar;
            so.FindProperty("_bossFill").objectReferenceValue = bossFill;
            so.FindProperty("_bossText").objectReferenceValue = bossText;
            so.FindProperty("_bannerText").objectReferenceValue = bannerText;
            so.FindProperty("_resultPanel").objectReferenceValue = panel;
            so.FindProperty("_resultText").objectReferenceValue = resultText;
            so.FindProperty("_retryButton").objectReferenceValue = retry;
            so.FindProperty("_titleButton").objectReferenceValue = toTitle;
            so.ApplyModifiedProperties();
            return hud;
        }

        /// <summary>左上に置く横長のバー（HP・必殺技のゲージ）</summary>
        private static (RectTransform fill, Text text) CreateBar(Transform parent, string name, Vector2 position, Vector2 size, Color fillColor, int fontSize)
        {
            var barObj = UIDialogBuilder.CreateUIObject(name, parent);
            var rect = barObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            barObj.AddComponent<Image>().color = HpBarBackColor;

            // 右端（anchorMax.x）を割合に合わせて縮める
            var fillObj = UIDialogBuilder.CreateUIObject("Fill", barObj.transform);
            var fill = fillObj.GetComponent<RectTransform>();
            UIDialogBuilder.SetStretchAll(fill);
            fillObj.AddComponent<Image>().color = fillColor;

            var textObj = UIDialogBuilder.CreateUIObject("Text", barObj.transform);
            UIDialogBuilder.SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = AddText(textObj, string.Empty, fontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return (fill, text);
        }

        private static Text CreatePartyText(Transform parent)
        {
            var obj = UIDialogBuilder.CreateUIObject("Party", parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(HudMargin.x, PartyTextY);
            rect.sizeDelta = PartyTextSize;
            var text = AddText(obj, string.Empty, PartyFontSize);
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            // 背景が明るいエリアでも読めるよう、縁取りをつける
            obj.AddComponent<Outline>().effectColor = HpBarBackColor;
            return text;
        }

        /// <summary>画面の真ん中を横切る帯。出す間だけ有効にする（SpecialCutIn）</summary>
        private static SpecialCutIn CreateCutIn(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("SpecialCutIn", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());

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

            var textObj = UIDialogBuilder.CreateUIObject("Text", band.transform);
            UIDialogBuilder.SetStretchAll(textObj.GetComponent<RectTransform>());
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

        /// <summary>上の真ん中に出すボスの HP。ボスがいる間だけ出す</summary>
        private static (GameObject bar, RectTransform fill, Text text) CreateBossBar(Transform parent)
        {
            var (fill, text) = CreateBar(parent, "BossBar", Vector2.zero, BossBarSize, BossBarFillColor, BossFontSize);
            var bar = fill.parent.gameObject;
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, HudMargin.y);
            bar.SetActive(false);
            return (bar, fill, text);
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
            text.color = new Color(1f, 1f, 1f, 0f);
            text.raycastTarget = false;
            obj.AddComponent<Outline>().effectColor = HpBarBackColor;
            return text;
        }

        private static Text CreateProgressText(Transform parent)
        {
            var obj = UIDialogBuilder.CreateUIObject("Progress", parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-HudMargin.x - HudMenuButtonSize - MenuButtonGap, HudMargin.y);
            rect.sizeDelta = ProgressTextSize;
            var text = AddText(obj, string.Empty, HudFontSize);
            text.alignment = TextAnchor.MiddleRight;
            text.color = Color.white;
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
            image.color = HpBarBackColor;
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
