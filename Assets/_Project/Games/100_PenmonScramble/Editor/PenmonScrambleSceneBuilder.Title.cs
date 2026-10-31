using PenmonScramble.Common.AppUpdate;
using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトル: 後ろで動く乱戦のジオラマとロゴの上に、「タップしてスタート」の画面とロビーの2段を重ねる。
    /// 接続の流れは共通基盤の ModeSelectPanel をそのまま使い、見た目だけここで作る。
    /// ここではタイトルの土台・スタートの画面・接続待ちの窓・「みんなで遊ぶ」の窓を作る。ロビーの中身と画面どうしのつなぎは
    /// PenmonScrambleSceneBuilder.TitleLobby.cs、ジオラマとロゴは TitleArt.cs、設定と名前の入力は Settings.cs
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string MenuGroupName = "Menu";
        private const string LobbyName = "Lobby";
        private const string SplashName = "Splash";

        private const string HostLabel = "部屋を作る\n<size=34>コードを友だちに送って\n最大4人で協力</size>";
        private const string JoinCaption = "コードで参加";
        private const string JoinLabel = "参加する";
        private const string CodePlaceholder = "参加コード";
        private const string CopyLabel = "コードをコピー";
        private const string StartLabel = "開始";
        private const string CancelLabel = "キャンセル";
        private const string OnlineHeading = "みんなで遊ぶ";
        private const string CloseGlyph = "×";
        // Relay の参加コードは英数字で、これより長くはならない
        private const int RoomCodeLimit = 12;

        private static readonly Color SoloColor = new Color(1f, 0.55f, 0.30f);
        private static readonly Color OnlineColor = new Color(0.20f, 0.55f, 0.90f);
        private static readonly Color JoinColor = new Color(1f, 0.75f, 0.30f);
        private static readonly Color StartColor = new Color(0.30f, 0.75f, 0.40f);
        private static readonly Color CancelColor = new Color(0.45f, 0.50f, 0.60f);
        private static readonly Color PlaceholderColor = new Color(0.55f, 0.55f, 0.60f);
        private static readonly Color InputFrameColor = new Color(0.96f, 0.97f, 1f);

        // 「タップしてスタート」の文字。ロゴの下・ジオラマの乱戦の手前に置く
        private static readonly Vector2 StartPromptPosition = new Vector2(0f, -360f);
        private static readonly Vector2 StartPromptSize = new Vector2(1400f, 110f);
        private const int StartPromptFontSize = 64;
        private static readonly Color StartPromptOutlineColor = new Color(0.05f, 0.08f, 0.25f);
        private static readonly Vector2 StartPromptOutlineDistance = new Vector2(4f, -4f);

        // 「みんなで遊ぶ」の窓: 左に「部屋を作る」の大きなボタン、右にコードの入力と「参加する」
        private static readonly Vector2 OnlinePanelSize = new Vector2(1180f, 600f);
        private static readonly Vector2 HostPosition = new Vector2(-280f, -40f);
        private static readonly Vector2 HostSize = new Vector2(480f, 340f);
        private const int HostFontSize = 58;
        private static readonly Vector2 JoinCaptionPosition = new Vector2(280f, 95f);
        private static readonly Vector2 JoinCaptionSize = new Vector2(480f, 60f);
        private const int JoinCaptionFontSize = 40;
        private static readonly Vector2 CodeInputPosition = new Vector2(280f, -10f);
        private static readonly Vector2 CodeInputSize = new Vector2(480f, 120f);
        private static readonly Vector2 JoinPosition = new Vector2(280f, -150f);
        private static readonly Vector2 JoinSize = new Vector2(480f, 120f);
        private const int JoinFontSize = 52;
        private const float CloseButtonSize = 90f;
        private const float CloseButtonInset = 20f;

        // 接続待ちの窓: 上に状態と参加コード（文面は ModeSelectPanel が入れる）、下に「コピー」「開始」「キャンセル」
        private static readonly Vector2 WaitingPanelSize = new Vector2(1180f, 560f);
        private static readonly Vector2 WaitingPanelPosition = new Vector2(0f, -30f);
        private static readonly Vector2 StatusPosition = new Vector2(0f, 60f);
        private static readonly Vector2 StatusSize = new Vector2(1100f, 380f);
        private const float WaitButtonsY = -195f;
        private const float WaitButtonsSpacing = 390f;
        private static readonly Vector2 WaitButtonSize = new Vector2(360f, 120f);

        private const int CardFontSize = 56;
        private const int CodeFontSize = 52;
        private const int StatusFontSize = 44;
        private const int WaitButtonFontSize = 48;
        private const float InputPadding = 16f;

        /// <summary>
        /// タイトルの土台。ModeSelectPanel の接続待ちの部品はここでつなぎ、メニューの部品（ひとりで・部屋を作る・参加）は
        /// ロビーを作るときにつなぐ（CreateTitleMenu）
        /// </summary>
        private static ModeSelectPanel CreateTitlePanel(Transform canvas, OnlineSession session, Camera stageCamera)
        {
            // 後ろの HUD やボタンを押せないよう、画面全体をふさぐ（透明なので後ろのジオラマは見える）
            var root = CreateFullScreenBacking(canvas, "Title", Color.clear);
            CreateTitleDiorama(root, stageCamera);
            CreateTitleFades(root.transform);
            CreateTitleLogo(root.transform);
            CreateTitleSplash(root.transform);

            var menu = CreateStretchGroup(root.transform, MenuGroupName);
            var lobby = CreateStretchGroup(menu.transform, LobbyName);
            var waiting = CreateTitleWaiting(root.transform, out var status, out var copy, out var start, out var cancel);

            var panel = root.AddComponent<ModeSelectPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_menuGroup").objectReferenceValue = menu;
            so.FindProperty("_waitingGroup").objectReferenceValue = waiting;
            so.FindProperty("_statusText").objectReferenceValue = status;
            so.FindProperty("_cancelButton").objectReferenceValue = cancel;
            so.FindProperty("_copyButton").objectReferenceValue = copy;
            so.FindProperty("_copyButtonLabel").objectReferenceValue = copy.GetComponentInChildren<Text>();
            so.FindProperty("_startButton").objectReferenceValue = start;
            so.ApplyModifiedProperties();
            return panel;
        }

        /// <summary>
        /// Windows 版の「新しい版が あります」。タイトルの子の最後に置いてメニューや窓より手前に出し、
        /// タイトルを離れたら一緒に消えるようにする
        /// </summary>
        private static void AddUpdateNotice(GameObject title)
        {
            var dialog = UIDialogBuilder.CreateCommonDialog(title.transform);
            var notice = title.AddComponent<UpdateNotice>();
            var so = new SerializedObject(notice);
            so.FindProperty("_dialog").objectReferenceValue = dialog;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// 起動したときの「タップしてスタート」。画面全体が透明なボタンで、どこを押しても始まる（キーとコントローラーは TitleFlow が見る）
        /// </summary>
        private static GameObject CreateTitleSplash(Transform parent)
        {
            var splash = CreateFullScreenBacking(parent, SplashName, Color.clear);
            splash.AddComponent<Button>().transition = Selectable.Transition.None;

            var prompt = CreateLabel(splash.transform, "Prompt", string.Empty, StartPromptFontSize, StartPromptPosition, StartPromptSize);
            prompt.fontStyle = FontStyle.Bold;
            var outline = prompt.GetComponent<Outline>();
            outline.effectColor = StartPromptOutlineColor;
            outline.effectDistance = StartPromptOutlineDistance;
            prompt.gameObject.AddComponent<CanvasGroup>();
            prompt.gameObject.AddComponent<UiPulse>();
            return splash;
        }

        /// <summary>接続待ち。ロビーは隠れて、ロゴとジオラマの上に窓だけが出る</summary>
        private static GameObject CreateTitleWaiting(Transform parent, out Text status, out Button copy, out Button start, out Button cancel)
        {
            var waiting = CreateStretchGroup(parent, "Waiting");
            var panel = CreatePanel(waiting.transform, "Panel", WaitingPanelPosition, WaitingPanelSize).transform;
            status = CreateLabel(panel, "Status", string.Empty, StatusFontSize, StatusPosition, StatusSize);

            copy = CreateTitleButton(panel, "Copy", CopyLabel, new Vector2(-WaitButtonsSpacing, WaitButtonsY), WaitButtonSize, OnlineColor, WaitButtonFontSize);
            start = CreateTitleButton(panel, "Start", StartLabel, new Vector2(0f, WaitButtonsY), WaitButtonSize, StartColor, WaitButtonFontSize);
            cancel = CreateTitleButton(panel, "Cancel", CancelLabel, new Vector2(WaitButtonsSpacing, WaitButtonsY), WaitButtonSize, CancelColor, WaitButtonFontSize);
            copy.gameObject.SetActive(false);
            start.gameObject.SetActive(false);

            waiting.SetActive(false);
            return waiting;
        }

        /// <summary>「みんなで遊ぶ」の窓。ModeSelectPanel の Menu の中に置くので、接続待ちになると一緒に隠れる</summary>
        private static GameObject CreateOnlinePopup(Transform parent, out Button host, out Button join, out InputField codeInput, out Button close)
        {
            var popup = CreateModal(parent, "Online", OnlinePanelSize, OnlineHeading, out var panel, out _);
            host = CreateTitleButton(panel, "Host", HostLabel, HostPosition, HostSize, OnlineColor, HostFontSize);
            CreateLabel(panel, "JoinCaption", JoinCaption, JoinCaptionFontSize, JoinCaptionPosition, JoinCaptionSize).fontStyle = FontStyle.Bold;
            codeInput = CreateInputField(panel, "CodeInput", CodePlaceholder, CodeInputPosition, CodeInputSize);
            codeInput.characterValidation = InputField.CharacterValidation.Alphanumeric;
            codeInput.characterLimit = RoomCodeLimit;
            join = CreateTitleButton(panel, "Join", JoinLabel, JoinPosition, JoinSize, JoinColor, JoinFontSize);
            close = CreateCloseButton(panel, OnlinePanelSize);
            return popup;
        }

        /// <summary>窓の右上の ×</summary>
        private static Button CreateCloseButton(Transform panel, Vector2 panelSize)
        {
            var offset = panelSize * 0.5f - Vector2.one * (CloseButtonSize * 0.5f + CloseButtonInset);
            return CreateTitleButton(panel, "Close", CloseGlyph, offset, Vector2.one * CloseButtonSize, CancelColor, WaitButtonFontSize);
        }

        /// <summary>文字の入力欄。白い枠に大きな文字で、スマホでも押しやすくする</summary>
        private static InputField CreateInputField(Transform parent, string name, string placeholderText, Vector2 position, Vector2 size)
        {
            var frame = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(frame.GetComponent<RectTransform>(), position, size);
            var image = AddSliced(frame, RoundRectPath, InputFrameColor, CornerScale(PanelCornerRadius * 0.6f));

            var placeholder = CreateInputText(frame.transform, "Placeholder", placeholderText, PlaceholderColor);
            var text = CreateInputText(frame.transform, "Text", string.Empty, TextColor);
            text.fontStyle = FontStyle.Bold;

            var input = frame.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private static Text CreateInputText(Transform parent, string name, string value, Color color)
        {
            var obj = CreateStretchGroup(parent, name);
            var rect = obj.GetComponent<RectTransform>();
            rect.offsetMin = new Vector2(InputPadding, 0f);
            rect.offsetMax = new Vector2(-InputPadding, 0f);
            var text = AddText(obj, value, CodeFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            return text;
        }

        private static Text CreateLabel(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, size);
            var text = AddText(obj, value, fontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            // 背景の絵の上でも読めるよう、白い文字を濃い色で縁取る
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = LabelOutlineColor;
            outline.effectDistance = LabelOutlineDistance;
            // 文字は飾りなので、下のボタンを押す邪魔をしない
            text.raycastTarget = false;
            return text;
        }

        private static void PlaceCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
