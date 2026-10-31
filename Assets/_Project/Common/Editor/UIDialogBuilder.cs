using PenmonScramble.Common.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Common.Editor
{
    /// <summary>
    /// 共通ダイアログ（確認・ポーズ）のUIをシーンへ生成するエディタユーティリティ
    /// タイトルと各ゲームのSceneBuilderから共有して使う
    /// </summary>
    public static class UIDialogBuilder
    {
        // 確認ダイアログは名前決定や終了確認など重要な判断に使うため、PauseDialog と同じくスマホの指で押せる大きさにする。
        // 横画面(1920x1080, match 0.5)の縦に短い端末でも収まるよう、パネルの高さは PauseDialog より低く抑える
        private const float CommonPanelWidth = 820f;
        private const float CommonPanelHeight = 520f;
        private const int CommonTitleFontSize = 44;
        private const int CommonMessageFontSize = 36;
        private const float CommonButtonWidth = 320f;
        private const float CommonButtonHeight = 100f;
        private const int CommonButtonFontSize = 36;
        private const float DialogButtonSpacing = 30f;

        public static CommonDialog CreateCommonDialog(Transform parent)
        {
            var dialogObj = CreateUIObject("CommonDialog", parent);
            SetStretchAll(dialogObj.GetComponent<RectTransform>());

            var bgImg = dialogObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.65f);

            // パネル本体
            var panelObj = CreateCenteredPanel(dialogObj.transform, new Vector2(CommonPanelWidth, CommonPanelHeight), new Color(0.14f, 0.16f, 0.22f));

            // タイトル
            var titleObj = CreateUIObject("TitleText", panelObj.transform);
            SetAnchorRange(titleObj.GetComponent<RectTransform>(), new Vector2(0, 0.75f), new Vector2(1, 1));
            var titleText = AddBoldCenteredText(titleObj, "確認", CommonTitleFontSize, Color.white);

            // メッセージ
            var msgObj = CreateUIObject("MessageText", panelObj.transform);
            SetAnchorRange(msgObj.GetComponent<RectTransform>(), new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.75f));
            var msgText = AddText(msgObj, "メッセージ内容", CommonMessageFontSize, TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 0.95f));

            // ボタンエリア
            var btnAreaObj = CreateUIObject("ButtonArea", panelObj.transform);
            SetAnchorRange(btnAreaObj.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0.35f));
            var layout = btnAreaObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = DialogButtonSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var cancelBtnObj = CreateDialogButton(btnAreaObj.transform, "Btn_Cancel", "キャンセル", CommonButtonWidth, CommonButtonHeight,
                CommonButtonFontSize, new Color(0.35f, 0.38f, 0.45f));
            var confirmBtnObj = CreateDialogButton(btnAreaObj.transform, "Btn_Confirm", "OK", CommonButtonWidth, CommonButtonHeight,
                CommonButtonFontSize, new Color(0.18f, 0.55f, 0.9f));

            var dialog = dialogObj.AddComponent<CommonDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_messageText").objectReferenceValue = msgText;
            so.FindProperty("_confirmButton").objectReferenceValue = confirmBtnObj.GetComponent<Button>();
            so.FindProperty("_confirmButtonText").objectReferenceValue = confirmBtnObj.GetComponentInChildren<Text>();
            so.FindProperty("_cancelButton").objectReferenceValue = cancelBtnObj.GetComponent<Button>();
            so.FindProperty("_cancelButtonText").objectReferenceValue = cancelBtnObj.GetComponentInChildren<Text>();
            so.ApplyModifiedProperties();

            dialogObj.SetActive(false);
            return dialog;
        }

        // スマホで指で操作しやすいサイズ。ボタンは2列に並べて、横画面の低い高さに収める（合計で高さは約710）
        private const float PausePanelWidth = CommonPanelWidth;
        private const int PausePanelPadding = 30;
        private const float PauseItemSpacing = 20f;
        private const float PauseTitleHeight = 90f;
        private const int PauseTitleFontSize = 44;
        private const float PauseButtonHeight = 100f;
        private const int PauseButtonFontSize = 36;
        private const int PauseButtonColumns = 2;
        private const int PauseButtonRows = 2;
        private static readonly Color PauseResumeColor = new Color(0.18f, 0.55f, 0.9f);
        private static readonly Color PauseHelpColor = new Color(0.30f, 0.70f, 0.45f);
        private static readonly Color PauseRestartColor = new Color(0.95f, 0.55f, 0.25f);
        private static readonly Color PauseTitleButtonColor = new Color(0.35f, 0.38f, 0.45f);

        /// <summary>
        /// ポーズ: 見出し・ボタン（つづける / 遊び方 / やり直し / タイトルへ）・BGM と効果音の音量。
        /// 遊び方を持たないゲームは withHelp を false にする
        /// </summary>
        public static PauseDialog CreatePauseDialog(Transform parent, string heading, bool withHelp)
        {
            var dialogObj = CreateUIObject("PauseDialog", parent);
            SetStretchAll(dialogObj.GetComponent<RectTransform>());
            // 後ろのボタン（仮想スティックなど）を押せないよう、画面全体をふさぐ
            dialogObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            var contentHeight = PauseTitleHeight + PauseGridHeight + VolumeRowHeight * 2f;
            var panelHeight = contentHeight + PausePanelPadding * 2f + PauseItemSpacing * 3f;
            var panelObj = CreateCenteredPanel(dialogObj.transform, new Vector2(PausePanelWidth, panelHeight), new Color(0.14f, 0.16f, 0.22f));
            var layout = panelObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PausePanelPadding, PausePanelPadding, PausePanelPadding, PausePanelPadding);
            layout.spacing = PauseItemSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("TitleText", panelObj.transform);
            SetPreferredHeight(titleObj, PauseTitleHeight);
            AddBoldCenteredText(titleObj, heading, PauseTitleFontSize, Color.white);

            var grid = CreatePauseButtonGrid(panelObj.transform);
            var resume = CreatePauseButton(grid, "Btn_Resume", "つづける", PauseResumeColor);
            var help = withHelp ? CreatePauseButton(grid, "Btn_Help", "遊び方", PauseHelpColor) : null;
            var restart = CreatePauseButton(grid, "Btn_Restart", "やり直し", PauseRestartColor);
            var title = CreatePauseButton(grid, "Btn_Title", "タイトルへ", PauseTitleButtonColor);

            var bgm = CreateVolumeSlider("BgmVolume", panelObj.transform, "BGM");
            SetPreferredHeight(bgm.transform.parent.gameObject, VolumeRowHeight);
            var se = CreateVolumeSlider("SeVolume", panelObj.transform, "効果音");
            SetPreferredHeight(se.transform.parent.gameObject, VolumeRowHeight);

            var dialog = dialogObj.AddComponent<PauseDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("_resumeButton").objectReferenceValue = resume.GetComponent<Button>();
            so.FindProperty("_restartButton").objectReferenceValue = restart.GetComponent<Button>();
            so.FindProperty("_titleButton").objectReferenceValue = title.GetComponent<Button>();
            so.FindProperty("_helpButton").objectReferenceValue = help != null ? help.GetComponent<Button>() : null;
            so.FindProperty("_bgmSlider").objectReferenceValue = bgm;
            so.FindProperty("_seSlider").objectReferenceValue = se;
            so.ApplyModifiedProperties();

            dialogObj.SetActive(false);
            return dialog;
        }

        private static float PauseGridHeight => PauseButtonHeight * PauseButtonRows + PauseItemSpacing * (PauseButtonRows - 1);

        /// <summary>ボタンを2列に並べる。やり直しを隠したときは、残りが詰めて並ぶ</summary>
        private static Transform CreatePauseButtonGrid(Transform parent)
        {
            var gridObj = CreateUIObject("Buttons", parent);
            SetPreferredHeight(gridObj, PauseGridHeight);
            var grid = gridObj.AddComponent<GridLayoutGroup>();
            var innerWidth = PausePanelWidth - PausePanelPadding * 2f;
            grid.cellSize = new Vector2((innerWidth - PauseItemSpacing * (PauseButtonColumns - 1)) / PauseButtonColumns, PauseButtonHeight);
            grid.spacing = Vector2.one * PauseItemSpacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = PauseButtonColumns;
            grid.childAlignment = TextAnchor.UpperCenter;
            return gridObj.transform;
        }

        private static void SetPreferredHeight(GameObject obj, float height)
        {
            obj.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private const float VolumeRowHeight = 140f;
        private const float VolumeLabelHeight = 50f;
        private const int VolumeLabelFontSize = 32;
        private const float VolumeTrackHeight = 16f;
        private const float VolumeHandleSize = 52f;

        private static GameObject CreatePauseButton(Transform parent, string name, string label, Color color)
        {
            // 幅はレイアウトが決めるので 0 を渡す
            var btnObj = CreateButton(parent, name, label, 0, PauseButtonHeight, color);
            btnObj.GetComponentInChildren<Text>().fontSize = PauseButtonFontSize;
            return btnObj;
        }

        // 確認・ポーズのダイアログで同じ作り方のパネル（中央基準・固定サイズ）を1か所にまとめる
        private static GameObject CreateCenteredPanel(Transform parent, Vector2 size, Color color)
        {
            var panelObj = CreateUIObject("Panel", parent);
            var rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var img = panelObj.AddComponent<Image>();
            img.color = color;
            return panelObj;
        }

        // 伸縮範囲だけを指定し、余白は0に戻す（pivot は呼び出し側の値のまま）
        private static void SetAnchorRange(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // ラベル系の太字・中央寄せ文字。設定順は text → fontSize → 太字 → 中央 → color
        private static Text AddBoldCenteredText(GameObject obj, string content, int fontSize, Color color)
        {
            return AddText(obj, content, fontSize, TextAnchor.MiddleCenter, color, FontStyle.Bold);
        }

        // 文字の設定順（text → fontSize → fontStyle → alignment → color）は、生成結果の見た目を揃えるため固定する
        private static Text AddText(GameObject obj, string content, int fontSize, TextAnchor alignment, Color color, FontStyle style = FontStyle.Normal)
        {
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static GameObject CreateDialogButton(Transform parent, string name, string label, float width, float height, int fontSize, Color color)
        {
            var btnObj = CreateButton(parent, name, label, width, height, color);
            btnObj.GetComponentInChildren<Text>().fontSize = fontSize;
            return btnObj;
        }

        public static GameObject CreateButton(Transform parent, string name, string label, float width, float height, Color color)
        {
            var btnObj = CreateUIObject(name, parent);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var img = btnObj.AddComponent<Image>();
            img.color = color;

            btnObj.AddComponent<Button>();

            var textObj = CreateUIObject("Text", btnObj.transform);
            SetStretchAll(textObj.GetComponent<RectTransform>());
            AddText(textObj, label, 22, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);

            return btnObj;
        }

        public static GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        public static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// ラベル(上) + スライダー(下)の1行。
        /// 見た目のバーは細いまま、タッチ判定をスライダーの行全体に広げて指で掴みやすくする
        /// </summary>
        public static Slider CreateVolumeSlider(string name, Transform parent, string label)
        {
            var rowObj = CreateUIObject(name, parent);
            rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, VolumeRowHeight);

            var labelObj = CreateUIObject("Label", rowObj.transform);
            var lRect = labelObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0, 1);
            lRect.anchorMax = new Vector2(1, 1);
            lRect.pivot = new Vector2(0.5f, 1);
            lRect.sizeDelta = new Vector2(0, VolumeLabelHeight);
            AddText(labelObj, label, VolumeLabelFontSize, TextAnchor.MiddleLeft, Color.white);

            // 透明な Image をタッチ判定にする（Slider は自分に当たったレイキャストでしかドラッグを受け取らないため）
            var sliderObj = CreateUIObject("SliderBar", rowObj.transform);
            var sRect = sliderObj.GetComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = new Vector2(0, -VolumeLabelHeight);
            sliderObj.AddComponent<Image>().color = Color.clear;

            // つまみが両端ではみ出さないよう、バー・塗り・つまみの可動域をつまみの半径ぶん内側に寄せる
            float inset = VolumeHandleSize / 2f;

            var trackObj = CreateUIObject("Track", sliderObj.transform);
            SetHorizontalBand(trackObj.GetComponent<RectTransform>(), inset, VolumeTrackHeight);
            var trackImg = trackObj.AddComponent<Image>();
            trackImg.color = new Color(0.3f, 0.35f, 0.45f);
            trackImg.raycastTarget = false;

            var fillArea = CreateUIObject("Fill Area", sliderObj.transform);
            SetHorizontalBand(fillArea.GetComponent<RectTransform>(), inset, VolumeTrackHeight);
            var fill = CreateUIObject("Fill", fillArea.transform);
            var fRect = fill.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 1f);
            fillImg.raycastTarget = false;

            var handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
            SetHorizontalBand(handleArea.GetComponent<RectTransform>(), inset, VolumeHandleSize);
            var handle = CreateUIObject("Handle", handleArea.transform);
            var hRect = handle.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 0.5f);
            hRect.anchorMax = new Vector2(0, 0.5f);
            hRect.sizeDelta = new Vector2(VolumeHandleSize, VolumeHandleSize);
            var handleImg = handle.AddComponent<Image>();
            handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            handleImg.color = Color.white;

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fRect;
            slider.handleRect = hRect;
            slider.targetGraphic = handleImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            return slider;
        }

        /// <summary>親の縦中央に、左右を inset ずつ内側へ寄せた高さ height の帯を置く</summary>
        private static void SetHorizontalBand(RectTransform rect, float inset, float height)
        {
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, -height / 2f);
            rect.offsetMax = new Vector2(-inset, height / 2f);
        }
    }
}
