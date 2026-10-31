using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Profile;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトルの設定（なまえ・BGM・効果音）と、名前の入力の窓（初回と変更で共通。共通基盤の NameEntryDialog）。
    /// どちらもタイトルの中に置くので、ステージが始まると一緒に消える
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string SettingsHeading = "設定";
        private const string NameRowLabel = "なまえ";
        private const string ChangeNameLabel = "変更";
        private const string CloseLabel = "とじる";
        private const string NameHint = "協力プレイで頭の上に出ます（6文字まで）";
        private const string NamePlaceholder = "なまえ";
        private const string DecideLabel = "決定";
        private const string CancelNameLabel = "やめる";

        private static readonly Vector2 SettingsPanelSize = new Vector2(980f, 720f);
        private static readonly Vector2 NameRowLabelPosition = new Vector2(-330f, 170f);
        private static readonly Vector2 NameRowLabelSize = new Vector2(200f, 80f);
        private static readonly Vector2 NameValuePosition = new Vector2(-10f, 170f);
        private static readonly Vector2 NameValueSize = new Vector2(420f, 90f);
        private const int NameValueFontSize = 52;
        private static readonly Vector2 ChangeNamePosition = new Vector2(320f, 170f);
        private static readonly Vector2 ChangeNameSize = new Vector2(200f, 96f);
        private static readonly Vector2 VolumeRowSize = new Vector2(780f, 140f);
        private const float BgmRowY = 25f;
        private const float SeRowY = -125f;
        private static readonly Color VolumeFillColor = new Color(1f, 0.62f, 0.25f);
        private static readonly Vector2 SettingsClosePosition = new Vector2(0f, -285f);
        private static readonly Vector2 SettingsCloseSize = new Vector2(360f, 110f);
        private static readonly Color ChangeNameColor = new Color(0.20f, 0.55f, 0.90f);

        private static readonly Vector2 NamePanelSize = new Vector2(980f, 600f);
        private static readonly Vector2 NameHintPosition = new Vector2(0f, 135f);
        private static readonly Vector2 NameHintSize = new Vector2(900f, 60f);
        private const int NameHintFontSize = 34;
        private static readonly Vector2 NameInputPosition = new Vector2(0f, 20f);
        private static readonly Vector2 NameInputSize = new Vector2(620f, 130f);
        private static readonly Vector2 NameButtonsPosition = new Vector2(0f, -180f);
        private static readonly Vector2 NameButtonsSize = new Vector2(900f, 120f);
        private static readonly Vector2 NameButtonSize = new Vector2(340f, 110f);
        private const float NameButtonSpacing = 40f;

        private static SettingsScreen CreateSettingsScreen(Transform parent, NameEntryDialog nameEntry)
        {
            var root = CreateModal(parent, "Settings", SettingsPanelSize, SettingsHeading, out var panel, out _);
            CreateLabel(panel, "NameLabel", NameRowLabel, MenuFontSize, NameRowLabelPosition, NameRowLabelSize).alignment = TextAnchor.MiddleLeft;
            var nameValue = CreateLabel(panel, "NameValue", string.Empty, NameValueFontSize, NameValuePosition, NameValueSize);
            nameValue.fontStyle = FontStyle.Bold;
            var changeName = CreateTitleButton(panel, "ChangeName", ChangeNameLabel, ChangeNamePosition, ChangeNameSize, ChangeNameColor, MenuFontSize);
            var bgm = CreateSettingsSlider(panel, "BgmVolume", "BGM", BgmRowY);
            var se = CreateSettingsSlider(panel, "SeVolume", "効果音", SeRowY);
            var close = CreateTitleButton(panel, "Close", CloseLabel, SettingsClosePosition, SettingsCloseSize, CancelColor, MenuFontSize);

            var screen = root.AddComponent<SettingsScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_nameText").objectReferenceValue = nameValue;
            so.FindProperty("_changeNameButton").objectReferenceValue = changeName;
            so.FindProperty("_bgmSlider").objectReferenceValue = bgm;
            so.FindProperty("_seSlider").objectReferenceValue = se;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_nameEntry").objectReferenceValue = nameEntry;
            so.ApplyModifiedProperties();
            return screen;
        }

        /// <summary>ステージ中のメニューと同じ音量のつまみ（共通基盤）を、この窓の中の決まった場所に置く</summary>
        private static Slider CreateSettingsSlider(Transform panel, string name, string label, float y)
        {
            var slider = UIDialogBuilder.CreateVolumeSlider(name, panel, label);
            var row = slider.transform.parent.GetComponent<RectTransform>();
            PlaceCenter(row, new Vector2(0f, y), VolumeRowSize);
            var labelText = row.Find("Label").GetComponent<Text>();
            labelText.fontSize = MenuFontSize - 4;
            labelText.fontStyle = FontStyle.Bold;
            slider.fillRect.GetComponent<Image>().color = VolumeFillColor;
            return slider;
        }

        /// <summary>
        /// 名前の入力。ブラウザ版は Unity の入力欄で日本語に変換できないので、入力欄の上に透明なボタンを被せて
        /// ブラウザの入力ダイアログを出す（NameEntryDialog が、ブラウザ版のときだけ出す）
        /// </summary>
        private static NameEntryDialog CreateNameEntryDialog(Transform parent)
        {
            var root = CreateModal(parent, "NameEntry", NamePanelSize, string.Empty, out var panel, out var heading);
            CreateLabel(panel, "Hint", NameHint, NameHintFontSize, NameHintPosition, NameHintSize);
            var input = CreateInputField(panel, "NameInput", NamePlaceholder, NameInputPosition, NameInputSize);
            var webCover = CreateWebInputCover(input.transform);

            var buttons = UIDialogBuilder.CreateUIObject("Buttons", panel);
            PlaceCenter(buttons.GetComponent<RectTransform>(), NameButtonsPosition, NameButtonsSize);
            // 初回は「やめる」を隠すので、残った「決定」がまん中に寄るよう並べて置く
            var layout = buttons.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = NameButtonSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var cancel = CreateTitleButton(buttons.transform, "Cancel", CancelNameLabel, Vector2.zero, NameButtonSize, CancelColor, MenuFontSize);
            var decide = CreateTitleButton(buttons.transform, "Decide", DecideLabel, Vector2.zero, NameButtonSize, SoloColor, MenuFontSize);

            var dialog = root.AddComponent<NameEntryDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("_inputField").objectReferenceValue = input;
            so.FindProperty("_decideButton").objectReferenceValue = decide;
            so.FindProperty("_webInputCover").objectReferenceValue = webCover;
            so.FindProperty("_cancelButton").objectReferenceValue = cancel;
            so.FindProperty("_headingText").objectReferenceValue = heading;
            so.ApplyModifiedProperties();
            return dialog;
        }

        private static Button CreateWebInputCover(Transform input)
        {
            var obj = UIDialogBuilder.CreateUIObject("WebInputCover", input);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            obj.AddComponent<Image>().color = Color.clear;
            var button = obj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }
    }
}
