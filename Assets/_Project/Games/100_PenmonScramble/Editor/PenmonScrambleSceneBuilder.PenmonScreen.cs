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
        private static readonly Vector2 CountPosition = new Vector2(-270f, 472f);
        private static readonly Vector2 CountSize = new Vector2(260f, 70f);
        private const int CountFontSize = 40;
        private static readonly Vector2 SortPosition = new Vector2(-20f, 472f);
        private static readonly Vector2 SortSize = new Vector2(220f, 84f);

        // 左: 編成の帯・案内・一覧（左の列の中心は x = -410、幅 1060）。部品どうしは 10px 以上あける
        private static readonly Vector2 BandPosition = new Vector2(-410f, 305f);
        private static readonly Vector2 BandSize = new Vector2(1060f, 220f);
        private static readonly Vector2 BandLabelPosition = new Vector2(-885f, 305f);
        private static readonly Vector2 BandLabelSize = new Vector2(80f, 170f);
        private const int BandLabelFontSize = 44;
        private static readonly float[] PartySlotX = { -685f, -370f, -55f };
        // 枠は帯の下寄せにして、上に「先頭」の札を置く隙間を作る（札が枠の中の★や名前にかぶらないように）
        private const float PartySlotY = 291f;
        private static readonly Vector2 PartySlotSize = new Vector2(290f, 150f);
        private const float LeadTagGap = 4f;
        private static readonly Vector2 LeadTagSize = new Vector2(100f, 36f);
        private const int LeadTagFontSize = 26;
        private static readonly Vector2 HintPosition = new Vector2(-410f, 167f);
        private static readonly Vector2 HintSize = new Vector2(1060f, 42f);
        private const int HintFontSize = 28;
        private static readonly Vector2 GridPosition = new Vector2(-410f, -193f);
        private static readonly Vector2 GridSize = new Vector2(1060f, 658f);
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
        // ★とパワーは名前より小さく添える（主役は名前）
        private const float CardStarsFontRatio = 0.8f;
        private const float CardPowerFontRatio = 0.85f;
        // 長い名前・桁の多いパワーは、その行だけこの割合まで縮める
        private const float CardMinFontRatio = 0.7f;
        private const int CardLimitFontSize = 18;
        private const float CardLimitHeight = 22f;
        private const float CardTagHeight = 26f;
        private const int CardTagFontSize = 19;
        private static readonly Color LimitUpTagColor = new Color(1f, 0.78f, 0.25f);
        // 右: 図鑑カード。中身の位置はパネルの中心から（パネルの高さ 946 = 上下 ±473）
        private static readonly Vector2 DetailPosition = new Vector2(545f, -52f);
        private static readonly Vector2 DetailSize = new Vector2(790f, 946f);
        private static readonly Vector2 FacePosition = new Vector2(-300f, 365f);
        private const float FaceSize = 150f;
        // 顔の右の文字の列。顔の右の端（-225）から 25px あけて始める
        private static readonly Vector2 TitlePosition = new Vector2(70f, 404f);
        private static readonly Vector2 TitleSize = new Vector2(540f, 64f);
        private const int TitleFontSize = 48;
        private static readonly Vector2 ElementChipPosition = new Vector2(-155f, 340f);
        private static readonly Vector2 ElementChipSize = new Vector2(90f, 46f);
        private const int ElementFontSize = 30;
        private static readonly Vector2 PersonalityPosition = new Vector2(125f, 340f);
        private static readonly Vector2 PersonalitySize = new Vector2(450f, 46f);
        private const int PersonalityFontSize = 28;
        private static readonly Vector2 StatsPosition = new Vector2(70f, 284f);
        private static readonly Vector2 StatsSize = new Vector2(540f, 50f);
        private const int StatsFontSize = 34;
        // 体力・攻撃・速さは、見出しと棒を横に並べて1段にする（2段だと下の技のカードの場所が足りない）
        private static readonly float[] StatColumnX = { -255f, 0f, 255f };
        private const float StatColumnWidth = 230f;
        private const float StatRowY = 220f;
        private static readonly Vector2 StatLabelSize = new Vector2(56f, 38f);
        private const float StatLabelGap = 6f;
        private const float StatBarHeight = 20f;
        private const int StatLabelFontSize = 26;
        // 小窓・技のカード3枚・ボタンを、上から 12 / 8 / 12px の隙間で積む（ボタンの下はパネルの端まで 18px）
        private static readonly Vector2 DemoPosition = new Vector2(0f, 83f);
        private static readonly Vector2 DemoSize = new Vector2(760f, 200f);
        private const float DemoFrameWidth = 6f;
        private static readonly string[] SkillCardNames = { "Skill1", "Skill2", "Special" };
        private static readonly float[] SkillCardY = { -87f, -199f, -311f };
        private static readonly Vector2 SkillCardSize = new Vector2(760f, 104f);
        // 編成に入れる・強化・売る
        private static readonly float[] DetailButtonX = { -255f, 0f, 255f };
        private const float DetailButtonY = -415f;
        private static readonly Vector2 DetailButtonSize = new Vector2(240f, 80f);
        private const int DetailButtonFontSize = 32;
        private static readonly Color EnhanceColor = new Color(0.62f, 0.40f, 0.90f);
        // 顔の下の上限解放の◆
        private static readonly Vector2 DetailLimitOffset = new Vector2(0f, -88f);
        private static readonly Vector2 DetailLimitSize = new Vector2(170f, 34f);
        private const int DetailLimitFontSize = 28;
        private static readonly Vector2 EmptyTextSize = new Vector2(700f, 100f);

        // 技のカードの中身（カードの中心から。カードは 760 x 104 = 左右 ±380・上下 ±52）
        private static readonly Vector2 SkillIconPosition = new Vector2(-325f, 0f);
        private const float SkillIconSize = 78f;
        // 名前と説明の列は、アイコンの右（-274）から●の列の手前（200）まで
        private static readonly Vector2 SkillNamePosition = new Vector2(-37f, 28f);
        private static readonly Vector2 SkillNameSize = new Vector2(474f, 38f);
        private const int SkillNameFontSize = 26;
        private const int SkillNameMinFontSize = 20;
        // 説明は［形・特徴］と合わせてどの技も2行になるので、2行ぶんの高さを取り、それでも長ければ縮める
        private static readonly Vector2 SkillTextPosition = new Vector2(-37f, -21f);
        private static readonly Vector2 SkillTextSize = new Vector2(474f, 58f);
        private const int SkillTextFontSize = 20;
        private const int SkillTextMinFontSize = 16;
        // ●の列はカードの右の端から 10px 内側に収める
        private static readonly Vector2 RatingLabelPosition = new Vector2(258f, 0f);
        private static readonly Vector2 RatingLabelSize = new Vector2(96f, 84f);
        private static readonly Vector2 RatingPipsPosition = new Vector2(340f, 0f);
        private static readonly Vector2 RatingPipsSize = new Vector2(60f, 84f);
        private const int RatingFontSize = 18;

        // 実演の小窓の中の、必殺技のカットインの帯（小窓の高さ 200 に合わせて縮めたもの）
        private const float DemoCutInHeight = 112f;
        private const float DemoCutInFaceSize = 92f;
        private const float DemoCutInFaceX = 90f;
        private const int DemoCutInFontSize = 24;
        private const int DemoCutInNameFontSize = 42;

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
            var content = CreateCardGrid(root.transform, GridPosition, GridSize);
            var template = CreatePenguinCard(content, "CardTemplate", PenguinCardSize, ListCardFontSize);
            template.gameObject.SetActive(false);
            var (detail, setParty, enhanceButton, sell) = CreateDetailPanel(root.transform);
            // 強化の窓はペンモン画面の上に重ねるので、最後に作る（一番手前）
            var enhance = CreateEnhanceScreen(root.transform);

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
            so.FindProperty("_enhanceButton").objectReferenceValue = enhanceButton;
            so.FindProperty("_enhance").objectReferenceValue = enhance;
            so.FindProperty("_sortButton").objectReferenceValue = sort;
            so.FindProperty("_sortLabel").objectReferenceValue = sort.GetComponentInChildren<Text>();
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return screen;
        }

        /// <summary>色の付いた帯に「編成」と3枠。1番目の枠の上には「先頭」の札（最初に場に出る子）</summary>
        private static PenguinCard[] CreatePartyBand(Transform parent)
        {
            CreatePanel(parent, "PartyBand", BandPosition, BandSize, BandColor);
            CreateLabel(parent, "PartyLabel", "編\n成", BandLabelFontSize, BandLabelPosition, BandLabelSize).fontStyle = FontStyle.Bold;
            var slots = new PenguinCard[PartySlotX.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = CreatePenguinCard(parent, $"Slot{i + 1}", PartySlotSize, SlotCardFontSize);
                PlaceCenter(slots[i].GetComponent<RectTransform>(), new Vector2(PartySlotX[i], PartySlotY), PartySlotSize);
            }

            // 札は枠の上の辺の外に置く（枠の中に載せると、右側の★・名前の行にかぶる）
            var tagY = PartySlotY + (PartySlotSize.y + LeadTagSize.y) * 0.5f + LeadTagGap;
            var tagPosition = new Vector2(PartySlotX[0], tagY);
            CreatePanel(parent, "LeadTag", tagPosition, LeadTagSize, LeadColor).raycastTarget = false;
            CreatePlainText(parent, "LeadTagText", "先頭", LeadTagFontSize, tagPosition, LeadTagSize, TextAnchor.MiddleCenter, DarkTextColor).fontStyle = FontStyle.Bold;
            return slots;
        }

        /// <summary>縦にスクロールする4列のカード置き場。カードは PenguinListScreen・EnhanceScreen が実行時に並べる</summary>
        private static RectTransform CreateCardGrid(Transform parent, Vector2 position, Vector2 size)
        {
            var viewport = UIDialogBuilder.CreateUIObject("Grid", parent);
            var viewportRect = viewport.GetComponent<RectTransform>();
            PlaceCenter(viewportRect, position, size);
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

            var (stars, nameText, power) = CreateCardLines(obj.transform, size, faceSize, fontSize);

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

            var limit = CreatePlainText(obj.transform, "Limit", string.Empty, CardLimitFontSize,
                new Vector2(faceX, CardStripHeight * 0.5f - faceSize * 0.5f + CardLimitHeight * 0.5f), new Vector2(faceSize, CardLimitHeight), TextAnchor.MiddleCenter, Color.white);
            limit.gameObject.AddComponent<Outline>().effectColor = LabelOutlineColor;
            var limitUp = CreateCardTag(obj.transform, new Vector2(faceX, size.y * 0.5f - CardTagHeight * 0.5f - CardPadding * 0.5f), faceSize);
            var group = obj.AddComponent<CanvasGroup>();

            var card = obj.AddComponent<PenguinCard>();
            var so = new SerializedObject(card);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_starsText").objectReferenceValue = stars;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_powerText").objectReferenceValue = power;
            so.FindProperty("_elementStrip").objectReferenceValue = strip;
            so.FindProperty("_badge").objectReferenceValue = badge.gameObject;
            so.FindProperty("_badgeText").objectReferenceValue = badgeText;
            so.FindProperty("_limitText").objectReferenceValue = limit;
            so.FindProperty("_limitUpTag").objectReferenceValue = limitUp;
            so.FindProperty("_group").objectReferenceValue = group;
            so.ApplyModifiedProperties();
            return card;
        }

        /// <summary>
        /// 顔の右の★・名前・パワーの3行。行ごとに1行ぶんの高さの枠にし、長い文字はその行だけ縮める
        /// （Best Fit は枠の高さで縮めるかを決めるので、1行の高さの枠なら折り返さずに縮む）
        /// </summary>
        private static (Text stars, Text name, Text power) CreateCardLines(Transform card, Vector2 size, float faceSize, int fontSize)
        {
            var left = -size.x * 0.5f + faceSize + CardPadding * 2f;
            var width = size.x * 0.5f - CardPadding - left;
            var starsSize = Mathf.RoundToInt(fontSize * CardStarsFontRatio);
            var powerSize = Mathf.RoundToInt(fontSize * CardPowerFontRatio);
            var heights = new[] { LineHeight(starsSize), LineHeight(fontSize), LineHeight(powerSize) };
            // 3行の束を、属性の帯を除いたカードの縦のまん中に置く
            var top = CardStripHeight * 0.5f + (heights[0] + heights[1] + heights[2]) * 0.5f;
            var x = left + width * 0.5f;

            var stars = CreateCardLine(card, "Stars", starsSize, new Vector2(x, top - heights[0] * 0.5f), new Vector2(width, heights[0]));
            var name = CreateCardLine(card, "Name", fontSize, new Vector2(x, top - heights[0] - heights[1] * 0.5f), new Vector2(width, heights[1]));
            name.fontStyle = FontStyle.Bold;
            var power = CreateCardLine(card, "Power", powerSize, new Vector2(x, top - heights[0] - heights[1] - heights[2] * 0.5f), new Vector2(width, heights[2]));
            return (stars, name, power);
        }

        private static Text CreateCardLine(Transform card, string name, int fontSize, Vector2 position, Vector2 size)
        {
            var text = CreatePlainText(card, name, string.Empty, fontSize, position, size, TextAnchor.MiddleLeft, Color.white);
            ShrinkToFit(text, Mathf.RoundToInt(fontSize * CardMinFontRatio));
            return text;
        }

        private static float LineHeight(int fontSize) => Mathf.Ceil(fontSize * FontLineHeightRatio);

        /// <summary>枠に入りきらないときだけ、minSize まで字を小さくする（Unity の Best Fit は折り返しを許さないと縮めない）</summary>
        private static void ShrinkToFit(Text text, int minSize)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = text.fontSize;
        }

        /// <summary>強化の素材の一覧で、同じ種類の子の顔の上に出す金の「上限UP」の札（ふだんは隠す）</summary>
        private static GameObject CreateCardTag(Transform parent, Vector2 position, float width)
        {
            var size = new Vector2(width, CardTagHeight);
            var tag = CreatePanel(parent, "LimitUpTag", position, size, LimitUpTagColor);
            tag.raycastTarget = false;
            CreatePlainText(tag.transform, "Text", "上限UP", CardTagFontSize, Vector2.zero, size, TextAnchor.MiddleCenter, DarkTextColor).fontStyle = FontStyle.Bold;
            tag.gameObject.SetActive(false);
            return tag.gameObject;
        }

        /// <summary>右側の図鑑カード。下の3つのボタン（編成に入れる・強化・売る）も返す</summary>
        private static (PenmonDetailPanel panel, Button setParty, Button enhance, Button sell) CreateDetailPanel(Transform parent)
        {
            var panelImage = CreatePanel(parent, "Detail", DetailPosition, DetailSize, PanelColor);
            var panelObj = panelImage.gameObject;
            var emptyText = CreatePlainText(panelObj.transform, "Empty", "ペンモンを選んでください", MenuFontSize, Vector2.zero, EmptyTextSize, TextAnchor.MiddleCenter, SubTextColor);

            var contentObj = CreateStretchGroup(panelObj.transform, "Content");
            var content = contentObj.transform;
            var face = CreateCircle(content, "Face", FacePosition, FaceSize, Color.white);
            var limit = CreatePlainText(content, "Limit", string.Empty, DetailLimitFontSize, FacePosition + DetailLimitOffset, DetailLimitSize, TextAnchor.MiddleCenter, Color.white);
            limit.gameObject.AddComponent<Outline>().effectColor = LabelOutlineColor;
            var title = CreatePlainText(content, "Title", string.Empty, TitleFontSize, TitlePosition, TitleSize, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            var chip = CreatePanel(content, "Element", ElementChipPosition, ElementChipSize, Color.white);
            chip.raycastTarget = false;
            var elementText = CreatePlainText(chip.transform, "Text", string.Empty, ElementFontSize, Vector2.zero, ElementChipSize, TextAnchor.MiddleCenter, DarkTextColor);
            elementText.fontStyle = FontStyle.Bold;
            var personality = CreatePlainText(content, "Personality", string.Empty, PersonalityFontSize, PersonalityPosition, PersonalitySize, TextAnchor.MiddleLeft, SubTextColor);
            var stats = CreatePlainText(content, "Stats", string.Empty, StatsFontSize, StatsPosition, StatsSize, TextAnchor.MiddleLeft, Color.white);
            var hpBar = CreateStatBar(content, "体力", StatColumnX[0]);
            var attackBar = CreateStatBar(content, "攻撃", StatColumnX[1]);
            var speedBar = CreateStatBar(content, "速さ", StatColumnX[2]);
            var demo = CreateSkillDemo(content);
            // 並びの番号が SkillPractice の slot になる（最後が必殺技 = SpecialSlot）
            var skillCards = new SkillCardView[SkillCardY.Length];
            for (var i = 0; i < skillCards.Length; i++)
            {
                skillCards[i] = CreateSkillCard(content, SkillCardNames[i], new Vector2(0f, SkillCardY[i]));
            }

            // ボタンは何も選んでいないときも出しておく（押せない状態で）ので、中身の外に置く
            var setParty = CreateTitleButton(panelObj.transform, "SetParty", string.Empty, new Vector2(DetailButtonX[0], DetailButtonY), DetailButtonSize, StartColor, DetailButtonFontSize);
            var enhance = CreateTitleButton(panelObj.transform, "Enhance", "強化", new Vector2(DetailButtonX[1], DetailButtonY), DetailButtonSize, EnhanceColor, DetailButtonFontSize);
            var sell = CreateTitleButton(panelObj.transform, "Sell", string.Empty, new Vector2(DetailButtonX[2], DetailButtonY), DetailButtonSize, SoloColor, DetailButtonFontSize);

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
            so.FindProperty("_limitText").objectReferenceValue = limit;
            so.FindProperty("_hpBar").objectReferenceValue = hpBar;
            so.FindProperty("_attackBar").objectReferenceValue = attackBar;
            so.FindProperty("_speedBar").objectReferenceValue = speedBar;
            SetObjectArray(so.FindProperty("_skillCards"), skillCards);

            so.FindProperty("_demo").objectReferenceValue = demo;
            so.ApplyModifiedProperties();
            return (panel, setParty, enhance, sell);
        }

        /// <summary>左に見出し、その右に横に伸びる棒（Image の fillAmount で長さを出す）。x は列の中心</summary>
        private static Image CreateStatBar(Transform parent, string label, float x)
        {
            var columnLeft = x - StatColumnWidth * 0.5f;
            var labelPosition = new Vector2(columnLeft + StatLabelSize.x * 0.5f, StatRowY);
            CreatePlainText(parent, $"{label}Label", label, StatLabelFontSize, labelPosition, StatLabelSize, TextAnchor.MiddleLeft, SubTextColor);
            var barWidth = StatColumnWidth - StatLabelSize.x - StatLabelGap;
            var barSize = new Vector2(barWidth, StatBarHeight);
            var barPosition = new Vector2(x + StatColumnWidth * 0.5f - barWidth * 0.5f, StatRowY);
            CreatePanel(parent, $"{label}Back", barPosition, barSize, BarBackColor).raycastTarget = false;
            var fill = CreatePanel(parent, $"{label}Bar", barPosition, barSize, BarColor);
            fill.raycastTarget = false;
            // 角の丸い絵は塗りつぶしで切ると端が欠けるので、棒は四角で描く
            fill.sprite = null;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            return fill;
        }

        /// <summary>技1つ分のカード: 左にアイコン、まん中に名前と説明、右に威力・出しやすさ・範囲の●（見出しの文は SkillCardView が入れる）</summary>
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
            ShrinkToFit(nameText, SkillNameMinFontSize);
            var description = CreatePlainText(obj.transform, "Text", string.Empty, SkillTextFontSize, SkillTextPosition, SkillTextSize, TextAnchor.UpperLeft, SubTextColor);
            ShrinkToFit(description, SkillTextMinFontSize);
            var ratingLabels = CreatePlainText(obj.transform, "RatingLabels", string.Empty, RatingFontSize, RatingLabelPosition, RatingLabelSize, TextAnchor.MiddleLeft, SubTextColor);
            var ratings = CreatePlainText(obj.transform, "Ratings", string.Empty, RatingFontSize, RatingPipsPosition, RatingPipsSize, TextAnchor.MiddleLeft, Color.white);

            var card = obj.AddComponent<SkillCardView>();
            var so = new SerializedObject(card);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_descriptionText").objectReferenceValue = description;
            so.FindProperty("_ratingLabelsText").objectReferenceValue = ratingLabels;
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
            // 帯は横から滑り込むので、小窓からはみ出さないように切る
            screenObj.AddComponent<RectMask2D>();
            var cutIn = CreateCutIn(screenObj.transform, new CutInLayout(DemoCutInHeight, DemoCutInFaceSize, DemoCutInFaceX, DemoCutInFontSize, DemoCutInNameFontSize));

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
            so.FindProperty("_cutIn").objectReferenceValue = cutIn;
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
