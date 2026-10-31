using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ガチャ画面:画面いっぱいに3Dのガチャの部屋（GachaStage を専用のカメラで撮ったもの）を映し、その上に
    /// 左上のもどる・見出し、右上の持っている数・コイン、右に結果のカード、右下の「確率」と「引く」を置く
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string GachaHeading = "ガチャ";
        private const float GachaFarClip = 60f;
        private const float GachaTopFadeHeight = 260f;
        private const float GachaBottomFadeHeight = 320f;

        // 上の段（ペンモン画面と同じ並び）
        private static readonly Vector2 GachaCountPosition = new Vector2(180f, 466f);
        private static readonly Vector2 GachaCountSize = new Vector2(360f, 92f);
        private const int GachaCountFontSize = 40;

        // 出てきた子の上の★（台の子は画面の横 0.36 の所に立つ。GachaStage.CloseViewportX）
        private static readonly Vector2 GachaStarsPosition = new Vector2(-269f, 330f);
        private static readonly Vector2 GachaStarsSize = new Vector2(600f, 140f);
        private const int GachaStarsFontSize = 110;

        // 結果のカード。中身の位置はカードの中心から
        private static readonly Vector2 ResultCardPosition = new Vector2(520f, 50f);
        private static readonly Vector2 ResultCardSize = new Vector2(820f, 600f);
        private static readonly Vector2 ResultSlideFrom = new Vector2(700f, 0f);
        private static readonly Vector2 NewTagPosition = new Vector2(-350f, 300f);
        private static readonly Vector2 NewTagSize = new Vector2(180f, 72f);
        private const float NewTagTilt = 12f;
        private const int NewTagFontSize = 44;
        private static readonly Color NewTagColor = new Color(1f, 0.82f, 0.25f);
        private static readonly Vector2 ResultTitlePosition = new Vector2(20f, 228f);
        private static readonly Vector2 ResultTitleSize = new Vector2(740f, 70f);
        private const int ResultTitleFontSize = 54;
        private static readonly Vector2 ResultChipPosition = new Vector2(-325f, 158f);
        private static readonly Vector2 ResultPersonalityPosition = new Vector2(65f, 158f);
        private static readonly Vector2 ResultPersonalitySize = new Vector2(620f, 46f);
        private static readonly Vector2 ResultStatsPosition = new Vector2(20f, 98f);
        private static readonly Vector2 ResultStatsSize = new Vector2(740f, 50f);
        private static readonly float[] ResultSkillY = { -32f, -166f };
        private static readonly Vector2 ResultHintPosition = new Vector2(0f, -262f);
        private static readonly Vector2 ResultHintSize = new Vector2(760f, 40f);
        private const string ResultHint = "編成はペンモン画面で変えられます";

        // 下の段
        private static readonly Vector2 GachaPullPosition = new Vector2(690f, -415f);
        private static readonly Vector2 GachaPullSize = new Vector2(420f, 170f);
        private static readonly Vector2 GachaRatesPosition = new Vector2(330f, -430f);
        private static readonly Vector2 GachaRatesSize = new Vector2(240f, 110f);
        private static readonly Vector2 GachaInfoPosition = new Vector2(-300f, -450f);
        private static readonly Vector2 GachaInfoSize = new Vector2(1100f, 70f);
        private const string SkipHint = "タップでスキップ";

        // 確率の窓
        private static readonly Vector2 RatesPanelSize = new Vector2(1100f, 800f);
        private static readonly Vector2 RatesTextPosition = new Vector2(0f, 10f);
        private static readonly Vector2 RatesTextSize = new Vector2(980f, 560f);
        private const int RatesFontSize = 34;
        private static readonly Vector2 RatesClosePosition = new Vector2(0f, -320f);
        private static readonly Vector2 RatesCloseSize = new Vector2(300f, 100f);

        private static GachaScreen CreateGachaScreen(Transform canvas)
        {
            var root = CreateStretchGroup(canvas, "Gacha");
            // 3Dを映す板が後ろのタイトルをふさぐ（押せないように raycastTarget は残す）
            var screen = root.AddComponent<RawImage>();
            screen.color = Color.white;
            var stage = CreateGachaStage(root, screen);

            CreateEdgeFade(root.transform, "TopFade", top: true, GachaTopFadeHeight, TitleFadeColor);
            CreateEdgeFade(root.transform, "BottomFade", top: false, GachaBottomFadeHeight, TitleFadeColor);

            var back = CreateTitleButton(root.transform, "Back", "もどる", PenmonBackPosition, PenmonBackSize, CancelColor, MenuFontSize);
            CreateLabel(root.transform, "Title", GachaHeading, PenmonHeadingFontSize, PenmonHeadingPosition, PenmonHeadingSize).fontStyle = FontStyle.Bold;
            var count = CreatePanel(root.transform, "Count", GachaCountPosition, GachaCountSize).transform;
            var countText = CreateLabel(count, "Value", string.Empty, GachaCountFontSize, Vector2.zero, GachaCountSize);
            var coin = CreateCoinPill(root.transform);

            var stars = CreateLabel(root.transform, "Stars", string.Empty, GachaStarsFontSize, GachaStarsPosition, GachaStarsSize);
            var card = CreateResultCard(root.transform);

            var rates = CreateTitleButton(root.transform, "Rates", "確率", GachaRatesPosition, GachaRatesSize, ArrowColor, MenuFontSize);
            var pull = CreateTitleButton(root.transform, "Pull", string.Empty, GachaPullPosition, GachaPullSize, GachaColor, CardFontSize);
            var info = CreateLabel(root.transform, "Info", string.Empty, SmallFontSize, GachaInfoPosition, GachaInfoSize);
            var skipHint = CreateLabel(root.transform, "SkipHint", SkipHint, SmallFontSize, GachaInfoPosition, GachaInfoSize).gameObject;
            skipHint.AddComponent<CanvasGroup>();
            skipHint.AddComponent<UiPulse>();
            var skip = CreateSkipButton(root.transform);
            var ratesDialog = CreateRatesDialog(root.transform, out var ratesText, out var ratesClose);

            var gacha = root.AddComponent<GachaScreen>();
            var so = new SerializedObject(gacha);
            so.FindProperty("_stage").objectReferenceValue = stage;
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_countText").objectReferenceValue = countText;
            so.FindProperty("_infoText").objectReferenceValue = info;
            so.FindProperty("_pullButton").objectReferenceValue = pull;
            so.FindProperty("_pullLabel").objectReferenceValue = pull.GetComponentInChildren<Text>();
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_skipButton").objectReferenceValue = skip;
            so.FindProperty("_skipHint").objectReferenceValue = skipHint;
            so.FindProperty("_starsText").objectReferenceValue = stars;
            so.FindProperty("_card").objectReferenceValue = card;
            so.FindProperty("_ratesButton").objectReferenceValue = rates;
            so.FindProperty("_ratesDialog").objectReferenceValue = ratesDialog;
            so.FindProperty("_ratesText").objectReferenceValue = ratesText;
            so.FindProperty("_ratesCloseButton").objectReferenceValue = ratesClose;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return gacha;
        }

        /// <summary>3Dを撮るカメラ（シーンの直下。ガチャ画面が開いている間だけ GachaStage が点ける）と、部屋を組む GachaStage</summary>
        private static GachaStage CreateGachaStage(GameObject root, RawImage screen)
        {
            var camera = CreateStandbyCamera("GachaCamera", GachaFarClip);

            var stage = root.AddComponent<GachaStage>();
            var so = new SerializedObject(stage);
            so.FindProperty("_screen").objectReferenceValue = screen;
            so.FindProperty("_camera").objectReferenceValue = camera;
            BindStageShaders(so);
            so.ApplyModifiedProperties();
            return stage;
        }

        /// <summary>画面全体の透明なボタン。演出の間だけ出すので、ほかのボタンより手前に置いて、どこを押してもとばせるようにする</summary>
        private static Button CreateSkipButton(Transform parent)
        {
            var obj = CreateStretchGroup(parent, "Skip");
            var image = obj.AddComponent<Image>();
            image.color = Color.clear;
            var button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static GachaResultCard CreateResultCard(Transform parent)
        {
            var panel = CreatePanel(parent, "Result", ResultCardPosition, ResultCardSize, PanelColor);
            panel.raycastTarget = false;
            var obj = panel.gameObject;
            SetSlideIn(obj, ResultSlideFrom, 0f);
            var content = obj.transform;
            var title = CreatePlainText(content, "Title", string.Empty, ResultTitleFontSize, ResultTitlePosition, ResultTitleSize, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            var chip = CreatePanel(content, "Element", ResultChipPosition, ElementChipSize, Color.white);
            chip.raycastTarget = false;
            var elementText = CreatePlainText(chip.transform, "Text", string.Empty, ElementFontSize, Vector2.zero, ElementChipSize, TextAnchor.MiddleCenter, DarkTextColor);
            elementText.fontStyle = FontStyle.Bold;
            var personality = CreatePlainText(content, "Personality", string.Empty, PersonalityFontSize, ResultPersonalityPosition, ResultPersonalitySize, TextAnchor.MiddleLeft, SubTextColor);
            var stats = CreatePlainText(content, "Stats", string.Empty, StatsFontSize, ResultStatsPosition, ResultStatsSize, TextAnchor.MiddleLeft, Color.white);
            var skillCards = new SkillCardView[ResultSkillY.Length];
            for (var i = 0; i < skillCards.Length; i++)
            {
                skillCards[i] = CreateSkillCard(content, $"Skill{i + 1}", new Vector2(0f, ResultSkillY[i]));
            }

            CreatePlainText(content, "Hint", ResultHint, HintFontSize, ResultHintPosition, ResultHintSize, TextAnchor.MiddleCenter, SubTextColor);
            var newTag = CreateNewTag(content);

            var card = obj.AddComponent<GachaResultCard>();
            var so = new SerializedObject(card);
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_elementChip").objectReferenceValue = chip;
            so.FindProperty("_elementText").objectReferenceValue = elementText;
            so.FindProperty("_personalityText").objectReferenceValue = personality;
            so.FindProperty("_statsText").objectReferenceValue = stats;
            SetObjectArray(so.FindProperty("_skillCards"), skillCards);

            so.FindProperty("_newTag").objectReferenceValue = newTag;
            so.ApplyModifiedProperties();
            obj.SetActive(false);
            return card;
        }

        /// <summary>カードの左上に斜めに貼る金の「NEW!」。ゆっくり明滅して目を引く</summary>
        private static GameObject CreateNewTag(Transform parent)
        {
            var tag = CreatePanel(parent, "New", NewTagPosition, NewTagSize, NewTagColor);
            tag.raycastTarget = false;
            tag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, NewTagTilt);
            var text = CreatePlainText(tag.transform, "Text", "NEW!", NewTagFontSize, Vector2.zero, NewTagSize, TextAnchor.MiddleCenter, DarkTextColor);
            text.fontStyle = FontStyle.Bold;
            tag.gameObject.AddComponent<CanvasGroup>();
            tag.gameObject.AddComponent<UiPulse>();
            return tag.gameObject;
        }

        private static GameObject CreateRatesDialog(Transform parent, out Text text, out Button close)
        {
            var root = CreateModal(parent, "Rates", RatesPanelSize, "ガチャの確率", out var panel, out _);
            text = CreatePlainText(panel, "Text", string.Empty, RatesFontSize, RatesTextPosition, RatesTextSize, TextAnchor.MiddleLeft, Color.white);
            close = CreateTitleButton(panel, "Close", "とじる", RatesClosePosition, RatesCloseSize, CancelColor, MenuFontSize);
            return root;
        }
    }
}
