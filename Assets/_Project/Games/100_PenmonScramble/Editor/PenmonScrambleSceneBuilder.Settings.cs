using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Profile;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトルの設定（なまえ・BGM・効果音・記録の初期化）と、名前の入力の窓（初回と変更で共通。共通基盤の NameEntryDialog）。
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
        private const string ResetRowLabel = "記録";
        private const string ResetRowHint = "最初の3体から やり直す";
        private const string ResetLabel = "初期化";
        private const string ResetHeading = "記録の初期化";
        private const string ResetMessage = "コイン・ペンモン・クリアの記録が消えて、\n無料ガチャ3回から やり直します。\n<size=34>名前と音量は残ります。元には戻せません</size>";
        private const string ResetConfirmLabel = "初期化する";
        private const string ResetArmedLabel = "もう一度押す";

        private static readonly Vector2 SettingsPanelSize = new Vector2(980f, 860f);
        private const float NameRowY = 235f;
        private const float RowLabelX = -330f;
        private static readonly Vector2 NameRowLabelSize = new Vector2(200f, 80f);
        private const float NameValueX = -10f;
        private static readonly Vector2 NameValueSize = new Vector2(420f, 90f);
        private const int NameValueFontSize = 52;
        private const float RowButtonX = 320f;
        private static readonly Vector2 RowButtonSize = new Vector2(200f, 96f);
        private static readonly Vector2 VolumeRowSize = new Vector2(780f, 140f);
        private const float BgmRowY = 90f;
        private const float SeRowY = -60f;
        private static readonly Color VolumeFillColor = new Color(1f, 0.62f, 0.25f);
        private const float ResetRowY = -200f;
        private const int ResetHintFontSize = 34;
        private static readonly Color ResetHintColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Vector2 SettingsClosePosition = new Vector2(0f, -330f);
        private static readonly Vector2 SettingsCloseSize = new Vector2(360f, 110f);
        private static readonly Color ChangeNameColor = new Color(0.20f, 0.55f, 0.90f);
        // 消す操作だと一目で分かるよう、ほかのボタンに無い赤にする
        private static readonly Color ResetColor = new Color(0.88f, 0.26f, 0.28f);

        private static readonly Vector2 ResetPanelSize = new Vector2(980f, 620f);
        private static readonly Vector2 ResetMessagePosition = new Vector2(0f, 50f);
        private static readonly Vector2 ResetMessageSize = new Vector2(900f, 240f);

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
            CreateRowLabel(panel, "NameLabel", NameRowLabel, NameRowY);
            var nameValue = CreateLabel(panel, "NameValue", string.Empty, NameValueFontSize, new Vector2(NameValueX, NameRowY), NameValueSize);
            nameValue.fontStyle = FontStyle.Bold;
            var changeName = CreateTitleButton(panel, "ChangeName", ChangeNameLabel, new Vector2(RowButtonX, NameRowY), RowButtonSize, ChangeNameColor, MenuFontSize);
            var bgm = CreateSettingsSlider(panel, "BgmVolume", "BGM", BgmRowY);
            var se = CreateSettingsSlider(panel, "SeVolume", "効果音", SeRowY);
            CreateRowLabel(panel, "ResetLabel", ResetRowLabel, ResetRowY);
            var hint = CreateLabel(panel, "ResetHint", ResetRowHint, ResetHintFontSize, new Vector2(NameValueX, ResetRowY), NameValueSize);
            hint.color = ResetHintColor;
            var reset = CreateTitleButton(panel, "Reset", ResetLabel, new Vector2(RowButtonX, ResetRowY), RowButtonSize, ResetColor, MenuFontSize);
            var close = CreateTitleButton(panel, "Close", CloseLabel, SettingsClosePosition, SettingsCloseSize, CancelColor, MenuFontSize);
            // 設定の窓より手前に開くよう、後に作る
            var confirm = CreateResetConfirm(parent, out var confirmButton, out var cancelButton);

            var screen = root.AddComponent<SettingsScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_nameText").objectReferenceValue = nameValue;
            so.FindProperty("_changeNameButton").objectReferenceValue = changeName;
            so.FindProperty("_bgmSlider").objectReferenceValue = bgm;
            so.FindProperty("_seSlider").objectReferenceValue = se;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_nameEntry").objectReferenceValue = nameEntry;
            so.FindProperty("_resetButton").objectReferenceValue = reset;
            so.FindProperty("_resetConfirm").objectReferenceValue = confirm;
            so.FindProperty("_resetConfirmButton").objectReferenceValue = confirmButton;
            so.FindProperty("_resetConfirmText").objectReferenceValue = confirmButton.GetComponentInChildren<Text>();
            so.FindProperty("_resetCancelButton").objectReferenceValue = cancelButton;
            so.FindProperty("_resetLabel").stringValue = ResetConfirmLabel;
            so.FindProperty("_resetArmedLabel").stringValue = ResetArmedLabel;
            so.ApplyModifiedProperties();
            return screen;
        }

        private static void CreateRowLabel(Transform panel, string name, string label, float y)
        {
            CreateLabel(panel, name, label, MenuFontSize, new Vector2(RowLabelX, y), NameRowLabelSize).alignment = TextAnchor.MiddleLeft;
        }

        /// <summary>初期化の確認。何が消えて何が残るかを書き、赤いボタンを2回押すと消える（SettingsScreen）</summary>
        private static GameObject CreateResetConfirm(Transform parent, out Button confirm, out Button cancel)
        {
            var root = CreateModal(parent, "ResetConfirm", ResetPanelSize, ResetHeading, out var panel, out _);
            CreateLabel(panel, "Message", ResetMessage, MenuFontSize - 4, ResetMessagePosition, ResetMessageSize).supportRichText = true;
            var buttons = CreateDialogButtonRow(panel);
            cancel = CreateTitleButton(buttons, "Cancel", CancelNameLabel, Vector2.zero, NameButtonSize, CancelColor, MenuFontSize);
            confirm = CreateTitleButton(buttons, "Confirm", ResetConfirmLabel, Vector2.zero, NameButtonSize, ResetColor, MenuFontSize);
            return root;
        }

        /// <summary>窓の下の、まん中に寄せて並べるボタンの列（名前の入力と初期化の確認で共通）</summary>
        private static Transform CreateDialogButtonRow(Transform panel)
        {
            var buttons = UIDialogBuilder.CreateUIObject("Buttons", panel);
            PlaceCenter(buttons.GetComponent<RectTransform>(), NameButtonsPosition, NameButtonsSize);
            var layout = buttons.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = NameButtonSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return buttons.transform;
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

            // 初回は「やめる」を隠すので、残った「決定」がまん中に寄るよう並べて置く
            var buttons = CreateDialogButtonRow(panel);
            var cancel = CreateTitleButton(buttons, "Cancel", CancelNameLabel, Vector2.zero, NameButtonSize, CancelColor, MenuFontSize);
            var decide = CreateTitleButton(buttons, "Decide", DecideLabel, Vector2.zero, NameButtonSize, SoloColor, MenuFontSize);

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
