using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトルに足すメニュー（コイン・ステージの選択・ガチャ・ペンギン一覧）と、ガチャ画面・ペンギン一覧の画面（§2・§7。仮の見た目）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Color ScreenBackColor = new Color(0.10f, 0.16f, 0.28f);
        private static readonly Color GachaColor = new Color(0.95f, 0.40f, 0.60f);
        private static readonly Color PenguinsColor = new Color(0.40f, 0.70f, 0.45f);
        private static readonly Color ArrowColor = new Color(0.30f, 0.38f, 0.55f);
        private static readonly Color CardFrameColor = new Color(0.20f, 0.26f, 0.40f);
        private static readonly Color PartyMarkColor = new Color(1f, 0.75f, 0.30f);
        private static readonly Color GlowColor = new Color(1f, 1f, 1f, 0.35f);

        private const int MenuFontSize = 44;
        private const int SmallFontSize = 36;

        // タイトル（ModeSelectPanel の Menu の中。接続待ちの間は一緒に隠れる）
        private static readonly Vector2 CoinPosition = new Vector2(680f, 480f);
        private static readonly Vector2 CoinSize = new Vector2(480f, 70f);
        // ステージの選択は下の帯の上の段、ガチャ・ペンギンは左上（ロゴと遊ぶボタンの邪魔をしない所）
        private const float StageRowY = -190f;
        private const float StageArrowX = 440f;
        private static readonly Vector2 StageArrowSize = new Vector2(110f, 90f);
        private static readonly Vector2 StageTextSize = new Vector2(760f, 90f);
        private static readonly Vector2 GachaButtonPosition = new Vector2(-790f, 470f);
        private static readonly Vector2 PenguinsButtonPosition = new Vector2(-510f, 470f);
        private static readonly Vector2 MenuButtonSize = new Vector2(260f, 100f);

        // ガチャ画面
        private static readonly Vector2 ScreenTitlePosition = new Vector2(0f, 460f);
        private static readonly Vector2 ScreenTitleSize = new Vector2(800f, 100f);
        private const int ScreenTitleFontSize = 64;
        private static readonly Vector2 CapsulePosition = new Vector2(-200f, 80f);
        private const float CapsuleSize = 320f;
        private const float GlowSize = 560f;
        private static readonly Vector2 StarsPosition = new Vector2(-200f, -170f);
        private static readonly Vector2 StarsSize = new Vector2(600f, 120f);
        private const int StarsFontSize = 100;
        private static readonly Vector2 NamePosition = new Vector2(-200f, -300f);
        private static readonly Vector2 NameSize = new Vector2(1100f, 140f);
        private static readonly Vector2 InfoPosition = new Vector2(0f, -440f);
        private static readonly Vector2 InfoSize = new Vector2(1400f, 70f);
        private static readonly Vector2 PullPosition = new Vector2(600f, 40f);
        private static readonly Vector2 PullSize = new Vector2(460f, 200f);
        private static readonly Vector2 BackPosition = new Vector2(-760f, 460f);
        private static readonly Vector2 BackSize = new Vector2(260f, 100f);

        // ペンギン一覧
        private static readonly Vector2 CountPosition = new Vector2(-330f, 460f);
        private static readonly Vector2 CountSize = new Vector2(420f, 70f);
        private static readonly Vector2 SortPosition = new Vector2(150f, 460f);
        private static readonly Vector2 SortSize = new Vector2(300f, 100f);
        private static readonly Vector2 PartyLabelPosition = new Vector2(-320f, 385f);
        private static readonly Vector2 PartyLabelSize = new Vector2(1200f, 50f);
        private const float PartyRowY = 290f;
        private static readonly float[] PartySlotX = { -780f, -470f, -160f };
        private static readonly Vector2 PenguinCardSize = new Vector2(300f, 140f);
        private const float CardBodySize = 100f;
        private const float CardSpacing = 10f;
        private const int CardColumns = 4;
        private const int PenguinCardFontSize = 28;
        private static readonly Vector2 GridPosition = new Vector2(-320f, -170f);
        private static readonly Vector2 GridSize = new Vector2(1250f, 680f);
        private static readonly Vector2 DetailPosition = new Vector2(640f, 80f);
        private static readonly Vector2 DetailSize = new Vector2(580f, 420f);
        private static readonly Vector2 SetPartyPosition = new Vector2(640f, -230f);
        private static readonly Vector2 SellPosition = new Vector2(640f, -380f);
        private static readonly Vector2 DetailButtonSize = new Vector2(560f, 110f);
        private const float ScrollSensitivity = 40f;

        /// <summary>タイトルの Menu の中にメニューを置き、TitleMenu をタイトルに付ける</summary>
        private static TitleMenu CreateTitleMenu(ModeSelectPanel title, GachaScreen gacha, PenguinListScreen penguins)
        {
            var menu = title.transform.Find("Menu");
            var coin = CreateLabel(menu, "Coins", string.Empty, MenuFontSize, CoinPosition, CoinSize);
            coin.alignment = TextAnchor.MiddleRight;
            var stage = CreateLabel(menu, "Stage", string.Empty, MenuFontSize, new Vector2(0f, StageRowY), StageTextSize);
            var prev = CreateTitleButton(menu, "PrevStage", "◀", new Vector2(-StageArrowX, StageRowY), StageArrowSize, ArrowColor, MenuFontSize);
            var next = CreateTitleButton(menu, "NextStage", "▶", new Vector2(StageArrowX, StageRowY), StageArrowSize, ArrowColor, MenuFontSize);
            var gachaButton = CreateTitleButton(menu, "Gacha", "ガチャ", GachaButtonPosition, MenuButtonSize, GachaColor, MenuFontSize);
            var penguinsButton = CreateTitleButton(menu, "Penguins", "ペンギン", PenguinsButtonPosition, MenuButtonSize, PenguinsColor, MenuFontSize);

            var titleMenu = title.gameObject.AddComponent<TitleMenu>();
            var so = new SerializedObject(titleMenu);
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_stageText").objectReferenceValue = stage;
            so.FindProperty("_prevStageButton").objectReferenceValue = prev;
            so.FindProperty("_nextStageButton").objectReferenceValue = next;
            so.FindProperty("_gachaButton").objectReferenceValue = gachaButton;
            so.FindProperty("_penguinsButton").objectReferenceValue = penguinsButton;
            so.FindProperty("_gacha").objectReferenceValue = gacha;
            so.FindProperty("_penguins").objectReferenceValue = penguins;
            so.ApplyModifiedProperties();
            return titleMenu;
        }

        /// <summary>画面全体をふさぐ下地（後ろのタイトルを押せないように）と、上の見出し</summary>
        private static GameObject CreateScreen(Transform canvas, string name, string heading)
        {
            var root = UIDialogBuilder.CreateUIObject(name, canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            root.AddComponent<Image>().color = ScreenBackColor;
            CreateLabel(root.transform, "Heading", heading, ScreenTitleFontSize, ScreenTitlePosition, ScreenTitleSize).fontStyle = FontStyle.Bold;
            return root;
        }

        private static GachaScreen CreateGachaScreen(Transform canvas)
        {
            var root = CreateScreen(canvas, "Gacha", "ガチャ");
            var coin = CreateLabel(root.transform, "Coins", string.Empty, MenuFontSize, CoinPosition, CoinSize);
            coin.alignment = TextAnchor.MiddleRight;
            var glow = CreateCircle(root.transform, "Glow", CapsulePosition, GlowSize, GlowColor);
            var capsule = CreateCircle(root.transform, "Capsule", CapsulePosition, CapsuleSize, Color.white);
            var penguin = CreateCircle(root.transform, "Penguin", CapsulePosition, CapsuleSize, Color.white);
            var stars = CreateLabel(root.transform, "Stars", string.Empty, StarsFontSize, StarsPosition, StarsSize);
            var name = CreateLabel(root.transform, "Name", string.Empty, MenuFontSize, NamePosition, NameSize);
            var info = CreateLabel(root.transform, "Info", string.Empty, SmallFontSize, InfoPosition, InfoSize);
            var pull = CreateTitleButton(root.transform, "Pull", string.Empty, PullPosition, PullSize, GachaColor, CardFontSize);
            var back = CreateTitleButton(root.transform, "Back", "もどる", BackPosition, BackSize, CancelColor, MenuFontSize);

            var screen = root.AddComponent<GachaScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_infoText").objectReferenceValue = info;
            so.FindProperty("_pullButton").objectReferenceValue = pull;
            so.FindProperty("_pullLabel").objectReferenceValue = pull.GetComponentInChildren<Text>();
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_capsule").objectReferenceValue = capsule.rectTransform;
            so.FindProperty("_capsuleImage").objectReferenceValue = capsule;
            so.FindProperty("_glow").objectReferenceValue = glow;
            so.FindProperty("_starsText").objectReferenceValue = stars;
            so.FindProperty("_penguin").objectReferenceValue = penguin.rectTransform;
            so.FindProperty("_penguinImage").objectReferenceValue = penguin;
            so.FindProperty("_nameText").objectReferenceValue = name;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return screen;
        }

        private static PenguinListScreen CreatePenguinListScreen(Transform canvas)
        {
            var root = CreateScreen(canvas, "Penguins", string.Empty);
            var back = CreateTitleButton(root.transform, "Back", "もどる", BackPosition, BackSize, CancelColor, MenuFontSize);
            var count = CreateLabel(root.transform, "Count", string.Empty, MenuFontSize, CountPosition, CountSize);
            var sort = CreateTitleButton(root.transform, "Sort", string.Empty, SortPosition, SortSize, ArrowColor, SmallFontSize);
            var coin = CreateLabel(root.transform, "Coins", string.Empty, MenuFontSize, CoinPosition, CoinSize);
            coin.alignment = TextAnchor.MiddleRight;
            CreateLabel(root.transform, "PartyLabel", "編成（枠を選んでから、下の子を「編成に入れる」）", SmallFontSize, PartyLabelPosition, PartyLabelSize);

            var slots = new PenguinCard[PartySlotX.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = CreatePenguinCard(root.transform, $"Slot{i + 1}");
                PlaceCenter(slots[i].GetComponent<RectTransform>(), new Vector2(PartySlotX[i], PartyRowY), PenguinCardSize);
            }

            var content = CreateCardGrid(root.transform);
            var template = CreatePenguinCard(content, "CardTemplate");
            template.gameObject.SetActive(false);
            var detail = CreateLabel(root.transform, "Detail", string.Empty, SmallFontSize, DetailPosition, DetailSize);
            detail.alignment = TextAnchor.UpperLeft;
            var setParty = CreateTitleButton(root.transform, "SetParty", string.Empty, SetPartyPosition, DetailButtonSize, StartColor, SmallFontSize);
            var sell = CreateTitleButton(root.transform, "Sell", string.Empty, SellPosition, DetailButtonSize, SoloColor, SmallFontSize);

            var screen = root.AddComponent<PenguinListScreen>();
            var so = new SerializedObject(screen);
            var slotsProperty = so.FindProperty("_partySlots");
            slotsProperty.arraySize = slots.Length;
            for (var i = 0; i < slots.Length; i++)
            {
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            so.FindProperty("_cardTemplate").objectReferenceValue = template;
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_countText").objectReferenceValue = count;
            so.FindProperty("_detailText").objectReferenceValue = detail;
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

        /// <summary>左に種類の色の丸、右に★・名前・パワー。編成にいる子は右上に「編成」</summary>
        private static PenguinCard CreatePenguinCard(Transform parent, string name)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            obj.GetComponent<RectTransform>().sizeDelta = PenguinCardSize;
            var frame = obj.AddComponent<Image>();
            frame.color = CardFrameColor;
            var button = obj.AddComponent<Button>();
            button.targetGraphic = frame;

            var body = CreateCircle(obj.transform, "Body", new Vector2(-PenguinCardSize.x * 0.5f + CardBodySize * 0.5f + CardSpacing, 0f), CardBodySize, Color.white);

            var labelObj = UIDialogBuilder.CreateUIObject("Label", obj.transform);
            var labelRect = labelObj.GetComponent<RectTransform>();
            UIDialogBuilder.SetStretchAll(labelRect);
            labelRect.offsetMin = new Vector2(CardBodySize + CardSpacing * 2f, 0f);
            var label = AddText(labelObj, string.Empty, PenguinCardFontSize);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.raycastTarget = false;

            var markObj = UIDialogBuilder.CreateUIObject("PartyMark", obj.transform);
            var markRect = markObj.GetComponent<RectTransform>();
            markRect.anchorMin = markRect.anchorMax = markRect.pivot = Vector2.one;
            markRect.anchoredPosition = new Vector2(-CardSpacing, -CardSpacing * 0.5f);
            markRect.sizeDelta = new Vector2(100f, 40f);
            var mark = AddText(markObj, "編成", PenguinCardFontSize);
            mark.alignment = TextAnchor.UpperRight;
            mark.color = PartyMarkColor;
            mark.raycastTarget = false;
            markObj.SetActive(false);

            var card = obj.AddComponent<PenguinCard>();
            var so = new SerializedObject(card);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_partyMark").objectReferenceValue = markObj;
            so.ApplyModifiedProperties();
            return card;
        }

        /// <summary>仮の見た目の丸（ペンギン・カプセル・光）。下のボタンを押す邪魔をしない</summary>
        private static Image CreateCircle(Transform parent, string name, Vector2 position, float size, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, Vector2.one * size);
            var image = obj.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSpritePath);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
