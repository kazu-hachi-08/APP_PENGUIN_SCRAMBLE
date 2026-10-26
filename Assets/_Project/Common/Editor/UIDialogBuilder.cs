using PenguinScramble.Common.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinScramble.Common.Editor
{
    /// <summary>
    /// 共通ダイアログ（確認・ポーズ・リザルト）のUIをシーンへ生成するエディタユーティリティ
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
            var msgText = msgObj.AddComponent<Text>();
            msgText.text = "メッセージ内容";
            msgText.fontSize = CommonMessageFontSize;
            msgText.alignment = TextAnchor.MiddleCenter;
            msgText.color = new Color(0.85f, 0.88f, 0.95f);

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

        // スマホで指で操作しやすいサイズ。項目の合計で高さは約830になる
        private const float PausePanelWidth = 820f;
        private const int PausePanelPadding = 30;
        private const float PauseItemSpacing = 20f;
        private const float PauseTitleHeight = 90f;
        private const int PauseTitleFontSize = 44;
        private const float PauseButtonHeight = 100f;
        private const int PauseButtonFontSize = 36;

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

        // リザルトも全ゲーム共通で毎回目にするため、確認ダイアログと同じ基準で大きくする
        private const float ResultPanelWidth = 820f;
        private const float ResultPanelHeight = 600f;
        private const int ResultTitleFontSize = 48;
        private const int ResultScoreFontSize = 52;
        private const int ResultDetailFontSize = 36;
        private const float ResultButtonWidth = 340f;
        private const float ResultButtonHeight = 100f;
        private const int ResultButtonFontSize = 36;

        // 3つのダイアログで同じ作り方のパネル（中央基準・固定サイズ）を1か所にまとめる
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

        // ラベル系の太字・中央寄せ文字。設定順は呼び出し前と同じ（text → fontSize → 太字 → 中央 → color）
        private static Text AddBoldCenteredText(GameObject obj, string content, int fontSize, Color color)
        {
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
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
            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

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
            var text = labelObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = VolumeLabelFontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;

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
