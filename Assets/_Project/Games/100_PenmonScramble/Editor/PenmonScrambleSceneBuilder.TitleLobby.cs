using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Common.Profile;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトルのロビー（§2・§9.6）: 上に名前の札・コイン・設定、左の列にガチャ・ペンモン・遊び方、
    /// 右下にステージのカードと「みんなで遊ぶ」「ひとりで遊ぶ」。まん中は後ろのジオラマの自分の編成が見えるよう空けておく。
    /// 部品は3つの組（上・左・右下）に分け、ロビーに入るたびに外から時間差ですべり込ませる（UiSlideIn）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string SoloLabel = "ひとりで遊ぶ";
        private const string OnlineLabel = "みんなで遊ぶ\n<size=28>最大4人で協力</size>";
        private const string NameHintLabel = "タップで変更";

        // 上の札
        private static readonly Vector2 NamePlatePosition = new Vector2(-690f, 466f);
        private static readonly Vector2 NamePlateSize = new Vector2(460f, 104f);
        private static readonly Color NamePlateColor = new Color(0.14f, 0.24f, 0.50f);
        private static readonly Vector2 LeaderFacePosition = new Vector2(-175f, 10f);
        private const float LeaderFaceSize = 136f;
        private static readonly Vector2 PlateNamePosition = new Vector2(40f, 12f);
        private static readonly Vector2 PlateNameSize = new Vector2(290f, 60f);
        private const int PlateNameFontSize = 44;
        private static readonly Vector2 PlateHintPosition = new Vector2(40f, -30f);
        private static readonly Vector2 PlateHintSize = new Vector2(290f, 30f);
        private const int PlateHintFontSize = 22;
        private static readonly Color PlateHintColor = new Color(0.75f, 0.85f, 1f);
        private static readonly Vector2 CoinPillPosition = new Vector2(560f, 466f);
        private static readonly Vector2 CoinPillSize = new Vector2(330f, 92f);
        private static readonly Vector2 CoinIconPosition = new Vector2(-122f, 0f);
        private const float CoinIconSize = 84f;
        private static readonly Vector2 CoinTextPosition = new Vector2(30f, 0f);
        private static readonly Vector2 CoinTextSize = new Vector2(220f, 80f);
        private const int CoinFontSize = 48;
        private static readonly Color CoinTextColor = new Color(1f, 0.92f, 0.6f);
        private static readonly Vector2 SettingsButtonPosition = new Vector2(830f, 466f);
        private const float SettingsButtonSize = 100f;
        private static readonly Color SettingsColor = new Color(0.40f, 0.47f, 0.62f);

        // 左の列
        private const float NavX = -800f;
        private static readonly float[] NavY = { 220f, 20f, -180f };
        private const float NavButtonSize = 150f;
        private const string HelpGlyph = "？";

        // 右下: ステージのカード（◀ ▶ で選ぶ）とボタン
        private static readonly Vector2 StageCardPosition = new Vector2(590f, -150f);
        private static readonly Vector2 StageCardSize = new Vector2(640f, 210f);
        private const float StageArrowCardX = 265f;
        private static readonly Vector2 StageCardArrowSize = new Vector2(90f, 150f);
        private static readonly Vector2 StageNoPosition = new Vector2(0f, 60f);
        private static readonly Vector2 StageNameLabelPosition = new Vector2(0f, 4f);
        private static readonly Vector2 StageBadgePosition = new Vector2(0f, -60f);
        private static readonly Vector2 StageTextSize = new Vector2(420f, 70f);
        private const int StageNoFontSize = 32;
        private const int StageNameFontSize = 60;
        private const int StageBadgeFontSize = 30;
        private static readonly Color StageNoColor = new Color(0.6f, 0.85f, 1f);
        private static readonly Color StageBadgeColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Vector2 OnlineButtonPosition = new Vector2(400f, -372f);
        private static readonly Vector2 OnlineButtonSize = new Vector2(270f, 150f);
        // 「みんなで遊ぶ」の6文字がボタンの内側（幅 - 余白）に1行で入る大きさ
        private const int OnlineButtonFontSize = 38;
        private static readonly Vector2 SoloButtonPosition = new Vector2(740f, -372f);
        private static readonly Vector2 SoloButtonSize = new Vector2(390f, 170f);
        private const int SoloButtonFontSize = 60;

        // ロビーに入るときのすべり込み: 上は上から、左の列は左から、右下は右から。少しずつ遅らせる
        private static readonly Vector2 TopSlideFrom = new Vector2(0f, 220f);
        private static readonly Vector2 NavSlideFrom = new Vector2(-360f, 0f);
        private static readonly Vector2 PlaySlideFrom = new Vector2(600f, 0f);
        private const float SlideStagger = 0.06f;

        /// <summary>
        /// ロビーの中身と、設定・名前の入力を作り、TitleMenu（ロビー）・TitleFlow（スタート → ロビー）・ModeSelectPanel のメニューをつなぐ。
        /// ガチャ・ペンモン・遊び方の画面はタイトルより手前に作るので、それらができてから呼ぶ
        /// </summary>
        private static TitleMenu CreateTitleMenu(ModeSelectPanel title, GachaScreen gacha, PenguinListScreen penguins, HowToPlayScreen howToPlay)
        {
            var root = title.transform;
            var lobby = root.Find($"{MenuGroupName}/{LobbyName}");
            var top = CreateSlideGroup(lobby, "Top", TopSlideFrom, 0f);
            var nameButton = CreateNamePlate(top, out var nameText, out var leaderFace);
            var coinText = CreateCoinPill(top);
            var settingsButton = CreateIconButton(top, "Settings", null, SettingsButtonPosition, SettingsButtonSize, SettingsColor, UiSprite(GearIconPath));

            var nav = CreateSlideGroup(lobby, "Nav", NavSlideFrom, SlideStagger);
            var gachaButton = CreateIconButton(nav, "Gacha", "ガチャ", new Vector2(NavX, NavY[0]), NavButtonSize, GachaColor, UiSprite(CapsuleIconPath));
            var penguinsButton = CreateIconButton(nav, "Penguins", "ペンモン", new Vector2(NavX, NavY[1]), NavButtonSize, PenguinsColor, LoadArtSprite(PenguinPortraitPath(1), CreatureArtMaxSize));
            var howToPlayButton = CreateIconButton(nav, "HowToPlay", "遊び方", new Vector2(NavX, NavY[2]), NavButtonSize, HowToPlayColor, null, HelpGlyph);

            var play = CreateSlideGroup(lobby, "Play", PlaySlideFrom, SlideStagger * 2f);
            var card = CreateStageCard(play);
            var onlineButton = CreateTitleButton(play, "Online", OnlineLabel, OnlineButtonPosition, OnlineButtonSize, OnlineColor, OnlineButtonFontSize);
            var soloButton = CreateTitleButton(play, "Solo", SoloLabel, SoloButtonPosition, SoloButtonSize, SoloColor, SoloButtonFontSize);
            var online = CreateOnlinePopup(lobby, out var host, out var join, out var codeInput, out var onlineClose);

            // 窓は接続待ちより手前に出す。名前の入力は設定の「変更」からも開くので、設定より手前
            var nameEntry = CreateNameEntryDialog(root);
            var settings = CreateSettingsScreen(root, nameEntry);
            nameEntry.transform.SetAsLastSibling();
            AddUpdateNotice(title.gameObject);

            WireModeSelectMenu(title, soloButton, host, join, codeInput);
            var menu = title.gameObject.AddComponent<TitleMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("_nameButton").objectReferenceValue = nameButton;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_leaderFace").objectReferenceValue = leaderFace;
            so.FindProperty("_coinText").objectReferenceValue = coinText;
            so.FindProperty("_settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("_gachaButton").objectReferenceValue = gachaButton;
            so.FindProperty("_penguinsButton").objectReferenceValue = penguinsButton;
            so.FindProperty("_howToPlayButton").objectReferenceValue = howToPlayButton;
            so.FindProperty("_stageNoText").objectReferenceValue = card.No;
            so.FindProperty("_stageNameText").objectReferenceValue = card.Name;
            so.FindProperty("_stageBadgeText").objectReferenceValue = card.Badge;
            so.FindProperty("_prevStageButton").objectReferenceValue = card.Prev;
            so.FindProperty("_nextStageButton").objectReferenceValue = card.Next;
            so.FindProperty("_onlineButton").objectReferenceValue = onlineButton;
            so.FindProperty("_onlinePanel").objectReferenceValue = online;
            so.FindProperty("_onlineCloseButton").objectReferenceValue = onlineClose;
            so.FindProperty("_gacha").objectReferenceValue = gacha;
            so.FindProperty("_penguins").objectReferenceValue = penguins;
            so.FindProperty("_howToPlay").objectReferenceValue = howToPlay;
            so.FindProperty("_settings").objectReferenceValue = settings;
            so.FindProperty("_nameEntry").objectReferenceValue = nameEntry;
            so.ApplyModifiedProperties();

            AddTitleFlow(title, lobby.gameObject, menu, nameEntry, gacha);
            return menu;
        }

        private static void WireModeSelectMenu(ModeSelectPanel title, Button solo, Button host, Button join, InputField codeInput)
        {
            var so = new SerializedObject(title);
            so.FindProperty("_npcButton").objectReferenceValue = solo;
            so.FindProperty("_hostButton").objectReferenceValue = host;
            so.FindProperty("_joinButton").objectReferenceValue = join;
            so.FindProperty("_codeInput").objectReferenceValue = codeInput;
            so.ApplyModifiedProperties();
        }

        private static void AddTitleFlow(ModeSelectPanel title, GameObject lobby, TitleMenu menu, NameEntryDialog nameEntry, GachaScreen gacha)
        {
            var splash = title.transform.Find(SplashName);
            var flow = title.gameObject.AddComponent<TitleFlow>();
            var so = new SerializedObject(flow);
            so.FindProperty("_splash").objectReferenceValue = splash.gameObject;
            so.FindProperty("_startButton").objectReferenceValue = splash.GetComponent<Button>();
            so.FindProperty("_promptText").objectReferenceValue = splash.Find("Prompt").GetComponent<Text>();
            so.FindProperty("_lobby").objectReferenceValue = lobby;
            so.FindProperty("_logo").objectReferenceValue = title.transform.Find("Logo").GetComponent<TitleLogoMotion>();
            so.FindProperty("_splashLogoPosition").vector2Value = LogoPosition;
            so.FindProperty("_lobbyLogoPosition").vector2Value = LogoLobbyPosition;
            so.FindProperty("_lobbyLogoScale").floatValue = LogoLobbyScale;
            so.FindProperty("_menu").objectReferenceValue = menu;
            so.FindProperty("_nameEntry").objectReferenceValue = nameEntry;
            so.FindProperty("_gacha").objectReferenceValue = gacha;
            so.ApplyModifiedProperties();
        }

        /// <summary>画面いっぱいの入れ物。中の部品をまとめてすべり込ませる</summary>
        private static Transform CreateSlideGroup(Transform parent, string name, Vector2 from, float delay)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            SetSlideIn(obj, from, delay);
            return obj.transform;
        }

        /// <summary>左上の名前の札: 編成の先頭の子の顔と、自分の名前。押すと名前を変えられる</summary>
        private static Button CreateNamePlate(Transform parent, out Text nameText, out Image leaderFace)
        {
            var plate = CreateTitleButton(parent, "NamePlate", string.Empty, NamePlatePosition, NamePlateSize, NamePlateColor, MenuFontSize);
            var face = UIDialogBuilder.CreateUIObject("Leader", plate.transform);
            PlaceCenter(face.GetComponent<RectTransform>(), LeaderFacePosition, Vector2.one * LeaderFaceSize);
            leaderFace = face.AddComponent<Image>();
            leaderFace.preserveAspect = true;
            leaderFace.raycastTarget = false;
            // 顔は TitleMenu が編成から入れる。それまでは白い四角を出さない
            leaderFace.enabled = false;

            nameText = CreateLabel(plate.transform, "Name", string.Empty, PlateNameFontSize, PlateNamePosition, PlateNameSize);
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            var hint = CreateLabel(plate.transform, "Hint", NameHintLabel, PlateHintFontSize, PlateHintPosition, PlateHintSize);
            hint.alignment = TextAnchor.MiddleLeft;
            hint.color = PlateHintColor;
            return plate;
        }

        private static Text CreateCoinPill(Transform parent)
        {
            var pill = CreatePanel(parent, "Coins", CoinPillPosition, CoinPillSize).transform;
            var icon = UIDialogBuilder.CreateUIObject("Icon", pill);
            PlaceCenter(icon.GetComponent<RectTransform>(), CoinIconPosition, Vector2.one * CoinIconSize);
            var image = icon.AddComponent<Image>();
            image.sprite = UiSprite(CoinIconPath);
            image.raycastTarget = false;

            var text = CreateLabel(pill, "Value", string.Empty, CoinFontSize, CoinTextPosition, CoinTextSize);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleRight;
            text.color = CoinTextColor;
            return text;
        }

        private readonly struct StageCard
        {
            public StageCard(Text no, Text name, Text badge, Button prev, Button next)
            {
                No = no;
                Name = name;
                Badge = badge;
                Prev = prev;
                Next = next;
            }

            public Text No { get; }
            public Text Name { get; }
            public Text Badge { get; }
            public Button Prev { get; }
            public Button Next { get; }
        }

        /// <summary>遊ぶステージ: 「ワールド 1 - 2」・名前・クリア済み（か大ボス）を、◀ ▶ で選ぶ</summary>
        private static StageCard CreateStageCard(Transform parent)
        {
            var card = CreatePanel(parent, "StageCard", StageCardPosition, StageCardSize).transform;
            var no = CreateLabel(card, "No", string.Empty, StageNoFontSize, StageNoPosition, StageTextSize);
            no.color = StageNoColor;
            var name = CreateLabel(card, "Name", string.Empty, StageNameFontSize, StageNameLabelPosition, StageTextSize);
            name.fontStyle = FontStyle.Bold;
            var badge = CreateLabel(card, "Badge", string.Empty, StageBadgeFontSize, StageBadgePosition, StageTextSize);
            badge.fontStyle = FontStyle.Bold;
            badge.color = StageBadgeColor;
            var prev = CreateTitleButton(card, "PrevStage", "◀", new Vector2(-StageArrowCardX, 0f), StageCardArrowSize, ArrowColor, MenuFontSize);
            var next = CreateTitleButton(card, "NextStage", "▶", new Vector2(StageArrowCardX, 0f), StageCardArrowSize, ArrowColor, MenuFontSize);
            return new StageCard(no, name, badge, prev, next);
        }
    }
}
