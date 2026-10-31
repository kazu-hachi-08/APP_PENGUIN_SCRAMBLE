using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ペンモン画面: 左上に編成の帯、左下に持っている子の一覧、右に選んだ子の図鑑カード（技の実演の小窓つき）。
    /// 実演は、離れた所に置いた練習場を専用のカメラで撮って小窓に映す（SkillDemoView）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string PenmonHeading = "ペンモン";
        private static readonly Color PanelColor = new Color(0.15f, 0.21f, 0.35f);
        private static readonly Color BandColor = new Color(0.20f, 0.27f, 0.45f);
        private static readonly Color LeadColor = new Color(1f, 0.82f, 0.30f);
        private static readonly Color DarkTextColor = new Color(0.12f, 0.12f, 0.20f);
        private static readonly Color SubTextColor = new Color(0.78f, 0.84f, 0.95f);
        private static readonly Color BarBackColor = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color BarColor = new Color(0.45f, 0.85f, 1f);
        private static readonly Color DemoFrameColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color CardStripBack = new Color(1f, 1f, 1f, 0.15f);

        // 上の段
        private static readonly Vector2 PenmonBackPosition = new Vector2(-810f, 472f);
        private static readonly Vector2 PenmonBackSize = new Vector2(240f, 90f);
        private static readonly Vector2 PenmonHeadingPosition = new Vector2(-540f, 472f);
        private static readonly Vector2 PenmonHeadingSize = new Vector2(260f, 90f);
        private const int PenmonHeadingFontSize = 56;
        private static readonly Vector2 CountPosition = new Vector2(-290f, 472f);
        private static readonly Vector2 CountSize = new Vector2(280f, 70f);
        private const int CountFontSize = 40;
        private static readonly Vector2 SortPosition = new Vector2(-20f, 472f);
        private static readonly Vector2 SortSize = new Vector2(220f, 84f);

        // 左: 編成の帯・案内・一覧（左の列の中心は x = -410、幅 1060）
        private static readonly Vector2 BandPosition = new Vector2(-410f, 300f);
        private static readonly Vector2 BandSize = new Vector2(1060f, 210f);
        private static readonly Vector2 BandLabelPosition = new Vector2(-885f, 300f);
        private static readonly Vector2 BandLabelSize = new Vector2(80f, 170f);
        private const int BandLabelFontSize = 44;
        private static readonly float[] PartySlotX = { -685f, -370f, -55f };
        private static readonly Vector2 PartySlotSize = new Vector2(290f, 170f);
        private static readonly Vector2 LeadTagOffset = new Vector2(90f, 85f);
        private static readonly Vector2 LeadTagSize = new Vector2(100f, 40f);
        private const int LeadTagFontSize = 26;
        private static readonly Vector2 HintPosition = new Vector2(-410f, 168f);
        private static readonly Vector2 HintSize = new Vector2(1060f, 44f);
        private const int HintFontSize = 28;
        private static readonly Vector2 GridPosition = new Vector2(-410f, -183f);
        private static readonly Vector2 GridSize = new Vector2(1060f, 680f);
        private static readonly Vector2 PenguinCardSize = new Vector2(255f, 130f);
        private const float CardSpacing = 10f;
        private const int CardColumns = 4;
        private const float ScrollSensitivity = 40f;

        // カードの中身（一覧と編成の枠で大きさだけ変える）
        private const float CardFaceRatio = 0.78f;
        private const float CardPadding = 8f;
        private const float CardStripHeight = 6f;
        private const float BadgeSize = 44f;
        private const int BadgeFontSize = 30;
        private const int ListCardFontSize = 26;
        private const int SlotCardFontSize = 30;

        // 右: 図鑑カード。中身の位置はパネルの中心から
        private static readonly Vector2 DetailPosition = new Vector2(545f, -52f);
        private static readonly Vector2 DetailSize = new Vector2(790f, 946f);
        private static readonly Vector2 FacePosition = new Vector2(-300f, 365f);
        private const float FaceSize = 150f;
        private static readonly Vector2 TitlePosition = new Vector2(60f, 404f);
        private static readonly Vector2 TitleSize = new Vector2(560f, 64f);
        private const int TitleFontSize = 48;
        private static readonly Vector2 ElementChipPosition = new Vector2(-170f, 340f);
        private static readonly Vector2 ElementChipSize = new Vector2(90f, 46f);
        private const int ElementFontSize = 30;
        private static readonly Vector2 PersonalityPosition = new Vector2(110f, 340f);
        private static readonly Vector2 PersonalitySize = new Vector2(460f, 46f);
        private const int PersonalityFontSize = 28;
        private static readonly Vector2 StatsPosition = new Vector2(60f, 284f);
        private static readonly Vector2 StatsSize = new Vector2(560f, 50f);
        private const int StatsFontSize = 34;
        private static readonly float[] StatBarX = { -255f, 0f, 255f };
        private const float StatLabelY = 215f;
        private const float StatBarY = 185f;
        private static readonly Vector2 StatLabelSize = new Vector2(230f, 36f);
        private static readonly Vector2 StatBarSize = new Vector2(230f, 20f);
        private const int StatLabelFontSize = 26;
        private static readonly Vector2 DemoPosition = new Vector2(0f, 40f);
        private static readonly Vector2 DemoSize = new Vector2(760f, 250f);
        private const float DemoFrameWidth = 6f;
        private static readonly float[] SkillCardY = { -160f, -290f };
        private static readonly Vector2 SkillCardSize = new Vector2(760f, 120f);
        private static readonly float[] DetailButtonX = { -190f, 190f };
        private const float DetailButtonY = -415f;
        private static readonly Vector2 DetailButtonSize = new Vector2(360f, 96f);
        private static readonly Vector2 EmptyTextSize = new Vector2(700f, 100f);

        // 技のカードの中身（カードの中心から）
        private static readonly Vector2 SkillIconPosition = new Vector2(-320f, 0f);
        private const float SkillIconSize = 96f;
        private static readonly Vector2 SkillNamePosition = new Vector2(-37f, 32f);
        private static readonly Vector2 SkillNameSize = new Vector2(465f, 44f);
        private const int SkillNameFontSize = 32;
        private static readonly Vector2 SkillTextPosition = new Vector2(-37f, -22f);
        private static readonly Vector2 SkillTextSize = new Vector2(465f, 60f);
        private const int SkillTextFontSize = 24;
        private static readonly Vector2 RatingLabelPosition = new Vector2(262f, 0f);
        private static readonly Vector2 RatingLabelSize = new Vector2(110f, 108f);
        private static readonly Vector2 RatingPipsPosition = new Vector2(352f, 0f);
        private static readonly Vector2 RatingPipsSize = new Vector2(70f, 108f);
        private const int RatingFontSize = 22;

        private static PenguinListScreen CreatePenguinListScreen(Transform canvas)
        {
            var root = CreateScreen(canvas, "Penguins", string.Empty);
            var back = CreateTitleButton(root.transform, "Back", "もどる", PenmonBackPosition, PenmonBackSize, CancelColor, MenuFontSize);
            CreateLabel(root.transform, "Title", PenmonHeading, PenmonHeadingFontSize, PenmonHeadingPosition, PenmonHeadingSize).fontStyle = FontStyle.Bold;
            var count = CreateLabel(root.transform, "Count", string.Empty, CountFontSize, CountPosition, CountSize);
            var sort = CreateTitleButton(root.transform, "Sort", string.Empty, SortPosition, SortSize, ArrowColor, SmallFontSize);
            var coin = CreateLabel(root.transform, "Coins", string.Empty, MenuFontSize, CoinPosition, CoinSize);
            coin.alignment = TextAnchor.MiddleRight;

            var slots = CreatePartyBand(root.transform);
            var hint = CreateLabel(root.transform, "Hint", string.Empty, HintFontSize, HintPosition, HintSize);
            var content = CreateCardGrid(root.transform);
            var template = CreatePenguinCard(content, "CardTemplate", PenguinCardSize, ListCardFontSize);
            template.gameObject.SetActive(false);
            var (detail, setParty, sell) = CreateDetailPanel(root.transform);

            var screen = root.AddComponent<PenguinListScreen>();
            var so = new SerializedObject(screen);
            SetObjectArray(so.FindProperty("_partySlots"), slots);

            so.FindProperty("_cardTemplate").objectReferenceValue = template;
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_countText").objectReferenceValue = count;
            so.FindProperty("_hintText").objectReferenceValue = hint;
            so.FindProperty("_detail").objectReferenceValue = detail;
            so.FindProperty("_setPartyButton").objectReferenceValue = setParty;
            so.FindProperty("_setPartyLabel").objectReferenceValue = setParty.GetComponentInChildren<Text>();
            so.FindProperty("_sellButton").objectReferenceValue = sell;
            so.FindProperty("_sellLabel").objectReferenceValue = sell.GetComponentInChildren<Text>();
            so.FindProperty("_sortButton").objectReferenceValue = sort;
            so.FindProperty("_sortLabel").objectReferenceValue = sort.GetComponentInChildren<Text>();
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return screen;
        }

        /// <summary>色の付いた帯に「編成」と3枠。1番目の枠には「先頭」の札（最初に場に出る子）</summary>
        private static PenguinCard[] CreatePartyBand(Transform parent)
        {
            CreatePanel(parent, "PartyBand", BandPosition, BandSize, BandColor);
            CreateLabel(parent, "PartyLabel", "編\n成", BandLabelFontSize, BandLabelPosition, BandLabelSize).fontStyle = FontStyle.Bold;
            var slots = new PenguinCard[PartySlotX.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = CreatePenguinCard(parent, $"Slot{i + 1}", PartySlotSize, SlotCardFontSize);
                PlaceCenter(slots[i].GetComponent<RectTransform>(), new Vector2(PartySlotX[i], BandPosition.y), PartySlotSize);
            }

            var tagPosition = new Vector2(PartySlotX[0], BandPosition.y) + LeadTagOffset;
            CreatePanel(parent, "LeadTag", tagPosition, LeadTagSize, LeadColor).raycastTarget = false;
            CreatePlainText(parent, "LeadTagText", "先頭", LeadTagFontSize, tagPosition, LeadTagSize, TextAnchor.MiddleCenter, DarkTextColor).fontStyle = FontStyle.Bold;
            return slots;
        }

        /// <summary>縦にスクロールする4列のカード置き場。カードは PenguinListScreen が実行時に並べる</summary>
        private static RectTransform CreateCardGrid(Transform parent)
        {
            var viewport = UIDialogBuilder.CreateUIObject("Grid", parent);
            var viewportRect = viewport.GetComponent<RectTransform>();
            PlaceCenter(viewportRect, GridPosition, GridSize);
            // スクロールの当たり判定のため、透明な Image を敷く
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<RectMask2D>();

            var contentObj = UIDialogBuilder.CreateUIObject("Content", viewport.transform);
            var content = contentObj.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var grid = contentObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = PenguinCardSize;
            grid.spacing = Vector2.one * CardSpacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CardColumns;
            contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = ScrollSensitivity;
            return content;
        }

        /// <summary>左に顔、右に★・名前・パワー、下に属性の色の帯、左上に編成の番号のバッジ</summary>
        private static PenguinCard CreatePenguinCard(Transform parent, string name, Vector2 size, int fontSize)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            obj.GetComponent<RectTransform>().sizeDelta = size;
            var frame = obj.AddComponent<Image>();
            frame.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(RoundedSpritePath);
            frame.type = Image.Type.Sliced;
            frame.color = CardFrameColor;
            var button = obj.AddComponent<Button>();
            button.targetGraphic = frame;

            var faceSize = size.y * CardFaceRatio;
            var faceX = -size.x * 0.5f + faceSize * 0.5f + CardPadding;
            var body = CreateCircle(obj.transform, "Body", new Vector2(faceX, CardStripHeight * 0.5f), faceSize, Color.white);

            var labelX = faceSize + CardPadding * 2f;
            var label = CreatePlainText(obj.transform, "Label", string.Empty, fontSize,
                new Vector2(labelX * 0.5f, CardStripHeight * 0.5f), new Vector2(size.x - labelX, size.y - CardStripHeight), TextAnchor.MiddleLeft, Color.white);

            var strip = CreatePanel(obj.transform, "ElementStrip", new Vector2(0f, (CardStripHeight - size.y) * 0.5f + CardPadding * 0.5f),
                new Vector2(size.x - CardPadding * 2f, CardStripHeight), CardStripBack);
            strip.raycastTarget = false;
            // 細い帯は角の丸い絵だと潰れるので、四角で描く
            strip.sprite = null;
            strip.type = Image.Type.Simple;

            var badgePosition = new Vector2(-size.x * 0.5f + BadgeSize * 0.5f + CardPadding * 0.5f, size.y * 0.5f - BadgeSize * 0.5f - CardPadding * 0.5f);
            var badge = CreateCircle(obj.transform, "Badge", badgePosition, BadgeSize, LeadColor);
            var badgeText = CreatePlainText(badge.transform, "Number", string.Empty, BadgeFontSize, Vector2.zero, Vector2.one * BadgeSize, TextAnchor.MiddleCenter, DarkTextColor);
            badgeText.fontStyle = FontStyle.Bold;
            badge.gameObject.SetActive(false);

            var card = obj.AddComponent<PenguinCard>();
            var so = new SerializedObject(card);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_elementStrip").objectReferenceValue = strip;
            so.FindProperty("_badge").objectReferenceValue = badge.gameObject;
            so.FindProperty("_badgeText").objectReferenceValue = badgeText;
            so.ApplyModifiedProperties();
            return card;
        }

        /// <summary>右側の図鑑カード。下の2つのボタン（編成に入れる・売る）も返す</summary>
        private static (PenmonDetailPanel panel, Button setParty, Button sell) CreateDetailPanel(Transform parent)
        {
            var panelImage = CreatePanel(parent, "Detail", DetailPosition, DetailSize, PanelColor);
            var panelObj = panelImage.gameObject;
            var emptyText = CreatePlainText(panelObj.transform, "Empty", "ペンモンを選んでください", MenuFontSize, Vector2.zero, EmptyTextSize, TextAnchor.MiddleCenter, SubTextColor);

            var contentObj = CreateStretchGroup(panelObj.transform, "Content");
            var content = contentObj.transform;
            var face = CreateCircle(content, "Face", FacePosition, FaceSize, Color.white);
            var title = CreatePlainText(content, "Title", string.Empty, TitleFontSize, TitlePosition, TitleSize, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            var chip = CreatePanel(content, "Element", ElementChipPosition, ElementChipSize, Color.white);
            chip.raycastTarget = false;
            var elementText = CreatePlainText(chip.transform, "Text", string.Empty, ElementFontSize, Vector2.zero, ElementChipSize, TextAnchor.MiddleCenter, DarkTextColor);
            elementText.fontStyle = FontStyle.Bold;
            var personality = CreatePlainText(content, "Personality", string.Empty, PersonalityFontSize, PersonalityPosition, PersonalitySize, TextAnchor.MiddleLeft, SubTextColor);
            var stats = CreatePlainText(content, "Stats", string.Empty, StatsFontSize, StatsPosition, StatsSize, TextAnchor.MiddleLeft, Color.white);
            var hpBar = CreateStatBar(content, "体力", StatBarX[0]);
            var attackBar = CreateStatBar(content, "攻撃", StatBarX[1]);
            var speedBar = CreateStatBar(content, "速さ", StatBarX[2]);
            var demo = CreateSkillDemo(content);
            var skillCards = new SkillCardView[SkillCardY.Length];
            for (var i = 0; i < skillCards.Length; i++)
            {
                skillCards[i] = CreateSkillCard(content, $"Skill{i + 1}", new Vector2(0f, SkillCardY[i]));
            }

            // ボタンは何も選んでいないときも出しておく（押せない状態で）ので、中身の外に置く
            var setParty = CreateTitleButton(panelObj.transform, "SetParty", string.Empty, new Vector2(DetailButtonX[0], DetailButtonY), DetailButtonSize, StartColor, SmallFontSize);
            var sell = CreateTitleButton(panelObj.transform, "Sell", string.Empty, new Vector2(DetailButtonX[1], DetailButtonY), DetailButtonSize, SoloColor, SmallFontSize);

            var panel = panelObj.AddComponent<PenmonDetailPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_content").objectReferenceValue = contentObj;
            so.FindProperty("_emptyText").objectReferenceValue = emptyText;
            so.FindProperty("_face").objectReferenceValue = face;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_elementChip").objectReferenceValue = chip;
            so.FindProperty("_elementText").objectReferenceValue = elementText;
            so.FindProperty("_personalityText").objectReferenceValue = personality;
            so.FindProperty("_statsText").objectReferenceValue = stats;
            so.FindProperty("_hpBar").objectReferenceValue = hpBar;
            so.FindProperty("_attackBar").objectReferenceValue = attackBar;
            so.FindProperty("_speedBar").objectReferenceValue = speedBar;
            SetObjectArray(so.FindProperty("_skillCards"), skillCards);

            so.FindProperty("_demo").objectReferenceValue = demo;
            so.ApplyModifiedProperties();
            return (panel, setParty, sell);
        }

        /// <summary>見出しと、横に伸びる棒（Image の fillAmount で長さを出す）</summary>
        private static Image CreateStatBar(Transform parent, string label, float x)
        {
            CreatePlainText(parent, $"{label}Label", label, StatLabelFontSize, new Vector2(x, StatLabelY), StatLabelSize, TextAnchor.MiddleLeft, SubTextColor);
            CreatePanel(parent, $"{label}Back", new Vector2(x, StatBarY), StatBarSize, BarBackColor).raycastTarget = false;
            var fill = CreatePanel(parent, $"{label}Bar", new Vector2(x, StatBarY), StatBarSize, BarColor);
            fill.raycastTarget = false;
            // 角の丸い絵は塗りつぶしで切ると端が欠けるので、棒は四角で描く
            fill.sprite = null;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            return fill;
        }

        /// <summary>技1つ分のカード: 左にアイコン、まん中に名前と説明、右に威力・出しやすさ・範囲の●</summary>
        private static SkillCardView CreateSkillCard(Transform parent, string name, Vector2 position)
        {
            var frame = CreatePanel(parent, name, position, SkillCardSize, CardFrameColor);
            var obj = frame.gameObject;
            frame.raycastTarget = true;
            var button = obj.AddComponent<Button>();
            button.targetGraphic = frame;

            var iconObj = UIDialogBuilder.CreateUIObject("Icon", obj.transform);
            PlaceCenter(iconObj.GetComponent<RectTransform>(), SkillIconPosition, Vector2.one * SkillIconSize);
            var icon = iconObj.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            var nameText = CreatePlainText(obj.transform, "Name", string.Empty, SkillNameFontSize, SkillNamePosition, SkillNameSize, TextAnchor.MiddleLeft, Color.white);
            var description = CreatePlainText(obj.transform, "Text", string.Empty, SkillTextFontSize, SkillTextPosition, SkillTextSize, TextAnchor.UpperLeft, SubTextColor);
            CreatePlainText(obj.transform, "RatingLabels", "威力\n出しやすさ\n範囲", RatingFontSize, RatingLabelPosition, RatingLabelSize, TextAnchor.MiddleLeft, SubTextColor);
            var ratings = CreatePlainText(obj.transform, "Ratings", string.Empty, RatingFontSize, RatingPipsPosition, RatingPipsSize, TextAnchor.MiddleLeft, Color.white);

            var card = obj.AddComponent<SkillCardView>();
            var so = new SerializedObject(card);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_descriptionText").objectReferenceValue = description;
            so.FindProperty("_ratingsText").objectReferenceValue = ratings;
            so.ApplyModifiedProperties();
            return card;
        }

        /// <summary>
        /// 実演の小窓（RawImage）と、それを撮るカメラ・描く StageView（実演用）。
        /// カメラと StageView はシーンの直下に置き、ペンモン画面が開いている間だけ SkillDemoView が点ける
        /// </summary>
        private static SkillDemoView CreateSkillDemo(Transform parent)
        {
            CreatePanel(parent, "DemoFrame", DemoPosition, DemoSize + Vector2.one * DemoFrameWidth * 2f, DemoFrameColor).raycastTarget = false;
            var screenObj = UIDialogBuilder.CreateUIObject("Demo", parent);
            PlaceCenter(screenObj.GetComponent<RectTransform>(), DemoPosition, DemoSize);
            var screen = screenObj.AddComponent<RawImage>();
            screen.raycastTarget = false;

            // 実演のカメラはタイトルのジオラマと同じく、近くだけを描く
            var camera = CreateStandbyCamera("SkillDemoCamera", TitleCameraFarClip);

            var stageObj = new GameObject("SkillDemoStage");
            var view = stageObj.AddComponent<StageView>();
            var viewSo = new SerializedObject(view);
            BindStageShaders(viewSo);
            viewSo.FindProperty("_preview").boolValue = true;
            viewSo.ApplyModifiedProperties();
            stageObj.SetActive(false);

            var demo = screenObj.AddComponent<SkillDemoView>();
            var so = new SerializedObject(demo);
            so.FindProperty("_screen").objectReferenceValue = screen;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_view").objectReferenceValue = view;
            so.ApplyModifiedProperties();
            return demo;
        }

        /// <summary>角の丸い色の板（パネル・札・棒）</summary>
        private static Image CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var image = CreateRoundedBox(parent, name, position, size, color, AssetDatabase.GetBuiltinExtraResource<Sprite>(RoundedSpritePath));
            image.raycastTarget = true;
            return image;
        }

        /// <summary>縁取りの無い文字（色の板の上に置くので、CreateLabel の縁取りは要らない）</summary>
        private static Text CreatePlainText(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size, TextAnchor alignment, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, size);
            var text = AddText(obj, value, fontSize);
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }
    }
}
