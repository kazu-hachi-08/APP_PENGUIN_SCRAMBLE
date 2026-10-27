using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトル: 「ひとりで遊ぶ」「部屋を作る」「コードで参加」。
    /// 接続の流れは共通基盤の ModeSelectPanel をそのまま使い、見た目だけここで作る（§2。仮の見た目）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string GameTitle = "乱戦！ペンモンスクランブル";
        private const string SoloLabel = "ひとりで遊ぶ";
        private const string HostLabel = "部屋を作る\n<size=36>最大4人で協力</size>";
        private const string JoinLabel = "参加する";
        private const string CodePlaceholder = "参加コード";
        private const string CopyLabel = "コードをコピー";
        private const string StartLabel = "開始";
        private const string CancelLabel = "キャンセル";
        // Relay の参加コードは英数字で、これより長くはならない
        private const int RoomCodeLimit = 12;

        private static readonly Color TitleBackColor = new Color(0.12f, 0.20f, 0.35f);
        private static readonly Color SoloColor = new Color(1f, 0.55f, 0.30f);
        private static readonly Color OnlineColor = new Color(0.20f, 0.55f, 0.90f);
        private static readonly Color JoinColor = new Color(1f, 0.75f, 0.30f);
        private static readonly Color StartColor = new Color(0.30f, 0.75f, 0.40f);
        private static readonly Color CancelColor = new Color(0.45f, 0.50f, 0.60f);
        private static readonly Color PlaceholderColor = new Color(0.55f, 0.55f, 0.60f);

        private static readonly Vector2 GameTitlePosition = new Vector2(0f, 330f);
        private static readonly Vector2 GameTitleSize = new Vector2(1700f, 200f);
        private static readonly Vector2 CardSize = new Vector2(640f, 220f);
        private static readonly Vector2 SoloPosition = new Vector2(-420f, 40f);
        private static readonly Vector2 HostPosition = new Vector2(420f, 40f);
        private static readonly Vector2 CodeInputPosition = new Vector2(320f, -180f);
        private static readonly Vector2 CodeInputSize = new Vector2(440f, 110f);
        private static readonly Vector2 JoinPosition = new Vector2(660f, -180f);
        private static readonly Vector2 JoinSize = new Vector2(220f, 110f);
        private static readonly Vector2 StatusPosition = new Vector2(0f, 60f);
        private static readonly Vector2 StatusSize = new Vector2(1400f, 440f);
        private const float WaitButtonsY = -320f;
        private const float WaitButtonsSpacing = 460f;
        private static readonly Vector2 WaitButtonSize = new Vector2(400f, 120f);

        private const int GameTitleFontSize = 110;
        private const int CardFontSize = 64;
        private const int CodeFontSize = 52;
        private const int StatusFontSize = 52;
        private const int WaitButtonFontSize = 48;
        private const float CodeInputPadding = 16f;

        private static ModeSelectPanel CreateTitlePanel(Transform canvas, OnlineSession session)
        {
            var root = UIDialogBuilder.CreateUIObject("Title", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            // 後ろの HUD やボタンを押せないよう、画面全体をふさぐ
            root.AddComponent<Image>().color = TitleBackColor;

            var title = CreateLabel(root.transform, "GameTitle", GameTitle, GameTitleFontSize, GameTitlePosition, GameTitleSize);
            title.fontStyle = FontStyle.Bold;

            var menu = CreateTitleMenu(root.transform, out var solo, out var host, out var join, out var codeInput);
            var waiting = CreateTitleWaiting(root.transform, out var status, out var copy, out var start, out var cancel);

            var panel = root.AddComponent<ModeSelectPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_menuGroup").objectReferenceValue = menu;
            so.FindProperty("_npcButton").objectReferenceValue = solo;
            so.FindProperty("_hostButton").objectReferenceValue = host;
            so.FindProperty("_joinButton").objectReferenceValue = join;
            so.FindProperty("_codeInput").objectReferenceValue = codeInput;
            so.FindProperty("_waitingGroup").objectReferenceValue = waiting;
            so.FindProperty("_statusText").objectReferenceValue = status;
            so.FindProperty("_cancelButton").objectReferenceValue = cancel;
            so.FindProperty("_copyButton").objectReferenceValue = copy;
            so.FindProperty("_copyButtonLabel").objectReferenceValue = copy.GetComponentInChildren<Text>();
            so.FindProperty("_startButton").objectReferenceValue = start;
            so.ApplyModifiedProperties();
            return panel;
        }

        private static GameObject CreateTitleMenu(Transform parent, out Button solo, out Button host, out Button join, out InputField codeInput)
        {
            var menu = UIDialogBuilder.CreateUIObject("Menu", parent);
            UIDialogBuilder.SetStretchAll(menu.GetComponent<RectTransform>());
            solo = CreateTitleButton(menu.transform, "Solo", SoloLabel, SoloPosition, CardSize, SoloColor, CardFontSize);
            host = CreateTitleButton(menu.transform, "Host", HostLabel, HostPosition, CardSize, OnlineColor, CardFontSize);
            codeInput = CreateCodeInput(menu.transform);
            join = CreateTitleButton(menu.transform, "Join", JoinLabel, JoinPosition, JoinSize, JoinColor, WaitButtonFontSize);
            return menu;
        }

        /// <summary>接続待ち。まん中に状態と参加コード（文面は ModeSelectPanel が入れる）、下に「コピー」「開始」「キャンセル」</summary>
        private static GameObject CreateTitleWaiting(Transform parent, out Text status, out Button copy, out Button start, out Button cancel)
        {
            var waiting = UIDialogBuilder.CreateUIObject("Waiting", parent);
            UIDialogBuilder.SetStretchAll(waiting.GetComponent<RectTransform>());
            status = CreateLabel(waiting.transform, "Status", string.Empty, StatusFontSize, StatusPosition, StatusSize);

            copy = CreateTitleButton(waiting.transform, "Copy", CopyLabel, new Vector2(-WaitButtonsSpacing, WaitButtonsY), WaitButtonSize, OnlineColor, WaitButtonFontSize);
            start = CreateTitleButton(waiting.transform, "Start", StartLabel, new Vector2(0f, WaitButtonsY), WaitButtonSize, StartColor, WaitButtonFontSize);
            cancel = CreateTitleButton(waiting.transform, "Cancel", CancelLabel, new Vector2(WaitButtonsSpacing, WaitButtonsY), WaitButtonSize, CancelColor, WaitButtonFontSize);
            copy.gameObject.SetActive(false);
            start.gameObject.SetActive(false);

            waiting.SetActive(false);
            return waiting;
        }

        /// <summary>参加コードの入力欄。白い枠に大きな文字で、スマホでも押しやすくする</summary>
        private static InputField CreateCodeInput(Transform parent)
        {
            var frame = UIDialogBuilder.CreateUIObject("CodeInput", parent);
            PlaceCenter(frame.GetComponent<RectTransform>(), CodeInputPosition, CodeInputSize);
            var image = frame.AddComponent<Image>();
            image.color = Color.white;

            var placeholder = CreateInputText(frame.transform, "Placeholder", CodePlaceholder, PlaceholderColor);
            var text = CreateInputText(frame.transform, "Text", string.Empty, TextColor);
            text.fontStyle = FontStyle.Bold;
            text.supportRichText = false;

            var input = frame.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterValidation = InputField.CharacterValidation.Alphanumeric;
            input.characterLimit = RoomCodeLimit;
            return input;
        }

        private static Text CreateInputText(Transform parent, string name, string value, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            var rect = obj.GetComponent<RectTransform>();
            UIDialogBuilder.SetStretchAll(rect);
            rect.offsetMin = new Vector2(CodeInputPadding, 0f);
            rect.offsetMax = new Vector2(-CodeInputPadding, 0f);
            var text = AddText(obj, value, CodeFontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            return text;
        }

        private static Button CreateTitleButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, int fontSize)
        {
            var obj = UIDialogBuilder.CreateButton(parent, name, label, size.x, size.y, color);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, size);
            obj.GetComponentInChildren<Text>().fontSize = fontSize;
            return obj.GetComponent<Button>();
        }

        private static Text CreateLabel(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            PlaceCenter(obj.GetComponent<RectTransform>(), position, size);
            var text = AddText(obj, value, fontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
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
