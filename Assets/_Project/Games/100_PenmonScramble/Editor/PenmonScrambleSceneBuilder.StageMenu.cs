using PenmonScramble.Common.Editor;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ステージ中のメニュー（StageMenu）: 後ろの幕・上の帯（見出し・ステージ名・エリアのつぶ）・左の斜めの札・音量の板・右の編成の欄・確認の窓。
    /// 札・帯・バーは HUD と同じ斜めの板の絵（HudArt）と部品（HudPlates）を使い回す
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Color MenuDimColor = new Color(0.02f, 0.03f, 0.10f, 0.55f);
        private static readonly Color MenuBandColor = new Color(0.02f, 0.03f, 0.10f, 0.9f);
        private static readonly Color MenuNoteColor = new Color(1f, 1f, 1f, 0.75f);
        private static readonly Color CardShadowColor = new Color(0f, 0f, 0f, 0.4f);
        private static readonly Color SettingsCardColor = new Color(0.55f, 0.45f, 0.85f);
        private static readonly Color VolumeHighlightColor = new Color(1f, 0.82f, 0.25f, 0.28f);

        // ---- 上の帯 ----
        private const float MenuBandHeight = 280f;
        private static readonly Vector2 MenuIconPosition = new Vector2(100f, -82f);
        private const float MenuIconSize = 96f;
        private static readonly Vector2 MenuHeadingPosition = new Vector2(520f, -82f);
        private static readonly Vector2 MenuHeadingSize = new Vector2(720f, 100f);
        private const int MenuHeadingFontSize = 64;
        private static readonly Vector2 MenuNotePosition = new Vector2(520f, -150f);
        private static readonly Vector2 MenuNoteSize = new Vector2(720f, 44f);
        private const int MenuNoteFontSize = 30;
        private static readonly Vector2 MenuStagePlatePosition = new Vector2(-320f, -82f);
        private static readonly Vector2 MenuStagePlateSize = new Vector2(560f, 72f);
        private const float MenuStageTextPaddingLeft = 40f;
        private const int MenuStageFontSize = 34;
        private static readonly Vector2 MenuBandSlideFrom = new Vector2(0f, 160f);

        // ---- 左の札（並べる場所は StageMenu が実行時に決める） ----
        private static readonly Vector2 MenuCardSize = new Vector2(560f, 116f);
        private const int MenuCardFontSize = 56;
        private const float MenuCardLabelPaddingLeft = 96f;
        private const float MenuCardArrowX = 56f;
        private const int MenuCardArrowFontSize = 44;
        private static readonly Vector2 MenuCardGlowGrow = new Vector2(44f, 28f);
        private static readonly Vector2 MenuCardShadowOffset = new Vector2(10f, -12f);
        private static readonly Vector2 MenuCardSlideFrom = new Vector2(-1100f, 0f);
        // 確認の札は短いので、文字をまん中に置く（左の ▶ と右の斜めの部分を避ける）
        private const float MenuCardCenteredPadding = 64f;

        // ---- 音量の板（「設定」の札の右） ----
        private static readonly Vector2 VolumePanelPosition = new Vector2(1080f, -110f);
        private static readonly Vector2 VolumePanelSize = new Vector2(480f, 330f);
        private static readonly Vector2 VolumeSlideFrom = new Vector2(-180f, 0f);
        private static readonly Vector2 VolumeTagPosition = new Vector2(-120f, 165f);
        private static readonly Vector2 VolumeTagSize = new Vector2(200f, 60f);
        private const int VolumeTagFontSize = 32;
        private static readonly float[] VolumeRowYs = { 50f, -90f };
        private static readonly Vector2 VolumeMenuRowSize = new Vector2(400f, 120f);
        private static readonly Vector2 VolumeHighlightSize = new Vector2(450f, 132f);

        // ---- 右の編成の欄（右端から測る） ----
        private static readonly Vector2 PartyPanelPosition = new Vector2(-340f, -20f);
        private static readonly Vector2 PartyPanelSize = new Vector2(520f, 520f);
        private static readonly Vector2 PartySlideFrom = new Vector2(520f, 0f);
        private const float PartyRowSpacing = 170f;
        private static readonly Vector2 PartyRowSize = new Vector2(520f, 150f);
        private static readonly Vector2 PartyDiscPosition = new Vector2(-190f, 0f);
        private const float PartyDiscDiameter = 128f;
        private const float PartyActiveRingRatio = 1.28f;
        private static readonly Vector2 PartyActiveTagSize = new Vector2(128f, 36f);
        private const int PartyActiveTagFontSize = 22;
        private const string PartyActiveLabel = "出ている";
        private const int PartyCrossFontSize = 100;
        private static readonly Vector2 PartyStripPosition = new Vector2(50f, 0f);
        private static readonly Vector2 PartyStripSize = new Vector2(420f, 104f);
        private static readonly Vector2 PartyNamePosition = new Vector2(70f, 24f);
        private static readonly Vector2 PartyNameSize = new Vector2(330f, 44f);
        private const int PartyNameFontSize = 32;
        private static readonly Vector2 PartyHpBarPosition = new Vector2(70f, -22f);
        private static readonly Vector2 PartyHpBarSize = new Vector2(360f, 36f);
        private const int PartyHpFontSize = 24;
        private const float PartyHpTextPaddingLeft = 72f;
        private const float PartyHpTextPaddingRight = 24f;

        // ---- 確認の窓 ----
        private static readonly Vector2 MenuConfirmPanelSize = new Vector2(1000f, 360f);
        private static readonly Vector2 MenuConfirmPanelPosition = new Vector2(0f, 20f);
        private static readonly Vector2 MenuConfirmHeadingPosition = new Vector2(-250f, 180f);
        private static readonly Vector2 MenuConfirmHeadingSize = new Vector2(380f, 84f);
        private const int MenuConfirmHeadingFontSize = 44;
        private static readonly Vector2 MenuConfirmMessagePosition = new Vector2(0f, 40f);
        private static readonly Vector2 MenuConfirmMessageSize = new Vector2(760f, 150f);
        private const int MenuConfirmMessageFontSize = 38;
        private static readonly Vector2 MenuConfirmCardSize = new Vector2(330f, 100f);
        private const float MenuConfirmCardX = 200f;
        private const float MenuConfirmCardY = -100f;
        private const int MenuConfirmCardFontSize = 44;
        private static readonly Vector2 MenuConfirmCardSlideFrom = new Vector2(0f, -180f);

        // 色はほかのファイルの static readonly を使うので、ファイルをまたいだ初期化の順に左右されないよう、使うときに作る
        private static (string Label, Color Color)[] MenuCards() => new[]
        {
            ("つづける", StartColor),
            ("やり直す", SoloColor),
            ("遊び方", OnlineColor),
            ("設定", SettingsCardColor),
            ("タイトルへ", ResetColor),
        };

        private static StageMenu CreateStageMenu(Transform canvas, Button openButton, HowToPlayScreen howToPlay)
        {
            var root = CreateStretchGroup(canvas, "StageMenuScreen");
            // 遊び方はメニューから開いて上に重ねるので、メニューは遊び方より奥に置く
            root.transform.SetSiblingIndex(howToPlay.transform.GetSiblingIndex());
            var backdrop = CreateMenuBackdrop(root.transform);
            var rootGroup = root.AddComponent<CanvasGroup>();
            var content = CreateStretchGroup(root.transform, "Content");
            var safe = CreateStretchGroup(content.transform, "SafeArea");
            safe.AddComponent<SafeAreaFitter>();

            var menu = new GameObject("StageMenu").AddComponent<StageMenu>();
            var so = new SerializedObject(menu);
            CreateMenuBand(safe.transform, so);
            SetObjectArray(so.FindProperty("_cards"), CreateMenuCards(safe.transform));
            so.FindProperty("_volume").objectReferenceValue = CreateMenuVolume(safe.transform);
            so.FindProperty("_party").objectReferenceValue = CreateMenuParty(safe.transform);
            // 確認の窓は札より手前に出すので、後に作る
            so.FindProperty("_confirm").objectReferenceValue = CreateMenuConfirm(content.transform);
            so.FindProperty("_openButton").objectReferenceValue = openButton;
            so.FindProperty("_root").objectReferenceValue = rootGroup;
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_backdrop").objectReferenceValue = backdrop;
            so.FindProperty("_howToPlay").objectReferenceValue = howToPlay;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return menu;
        }

        /// <summary>ぼかした絵（ソロだけ実行時に入れる）の上に暗い幕。後ろの HUD を押せないよう、幕で受け止める</summary>
        private static StageMenuBackdrop CreateMenuBackdrop(Transform parent)
        {
            var root = CreateStretchGroup(parent, "Backdrop");
            var blurred = CreateStretchGroup(root.transform, "Blurred").AddComponent<RawImage>();
            blurred.raycastTarget = false;
            blurred.enabled = false;
            var dim = CreateFullScreenBacking(root.transform, "Dim", MenuDimColor).GetComponent<Image>();

            var backdrop = root.AddComponent<StageMenuBackdrop>();
            var so = new SerializedObject(backdrop);
            so.FindProperty("_blurred").objectReferenceValue = blurred;
            so.FindProperty("_dim").objectReferenceValue = dim;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return backdrop;
        }

        /// <summary>上の帯: 左に「‖ いちじていし」（協力は「メニュー」とその下の一言）、右にステージ名とエリアのつぶ</summary>
        private static void CreateMenuBand(Transform safe, SerializedObject menu)
        {
            var band = CreateHudRect(safe, "Band", new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            band.anchorMin = new Vector2(0f, 1f);
            band.anchorMax = Vector2.one;
            band.pivot = new Vector2(0.5f, 1f);
            band.sizeDelta = new Vector2(0f, MenuBandHeight);
            CreateEdgeFade(band, "Fade", true, MenuBandHeight, MenuBandColor);
            SetSlideIn(band.gameObject, MenuBandSlideFrom, 0f);

            var icon = CreateMenuIcon(band, "Icon", MenuIconSize).GetComponent<RectTransform>();
            icon.anchorMin = icon.anchorMax = TopLeft;
            icon.anchoredPosition = MenuIconPosition;

            var heading = CreateMenuBandText(band, "Heading", MenuHeadingPosition, MenuHeadingSize, MenuHeadingFontSize);
            var note = CreateMenuBandText(band, "Note", MenuNotePosition, MenuNoteSize, MenuNoteFontSize);
            note.color = MenuNoteColor;
            menu.FindProperty("_headingText").objectReferenceValue = heading;
            menu.FindProperty("_noteText").objectReferenceValue = note;

            var plate = CreateFramedSlant(band, "Stage", HudBackColor, Vector2.one, MenuStagePlatePosition, MenuStagePlateSize);
            var stage = CreateHudLabel(plate.transform, "Name", MenuStageFontSize, TextAnchor.MiddleLeft, MenuStageTextPaddingLeft, 0f);
            stage.color = HudGoldColor;
            menu.FindProperty("_stageText").objectReferenceValue = stage;
            menu.FindProperty("_areaPips").objectReferenceValue = CreateAreaPips(plate.rectTransform);
        }

        private static Text CreateMenuBandText(RectTransform band, string name, Vector2 position, Vector2 size, int fontSize)
        {
            var rect = CreateHudRect(band, name, TopLeft, position, size);
            var text = CreateHudLabel(rect, "Text", fontSize, TextAnchor.MiddleLeft, 0f, 0f);
            return text;
        }

        private static Object[] CreateMenuCards(Transform safe)
        {
            var defs = MenuCards();
            var cards = new Object[defs.Length];
            for (var i = 0; i < defs.Length; i++)
            {
                var (label, color) = defs[i];
                cards[i] = CreateMenuCard(safe, $"Card{i + 1}", label, color, new Vector2(0f, 0.5f), Vector2.zero, MenuCardSize, MenuCardFontSize, MenuCardSlideFrom, centered: false);
            }

            return cards;
        }

        /// <summary>
        /// 札1枚（StageMenuCard）: 押す当たり（透明）の中に、前へ出て大きくなる体（光・影・色の札・▶・文字）。
        /// 押す当たりは動かさない体の外に置き、札が大きくなってもとなりの札の当たりと重ならないようにする
        /// </summary>
        private static StageMenuCard CreateMenuCard(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, Vector2 slideFrom, bool centered)
        {
            var root = CreateHudRect(parent, name, anchor, position, size);
            var hit = AddImage(root.gameObject, null, Color.clear, true);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            // キーボード・コントローラーでは StageMenu が自分で選ぶ（UI の選択が動くと「決定」が2回届く）
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var body = CreateHudRect(root, "Body", Middle, Vector2.zero, size);
            var glow = CreateSlant(body, "Glow", HudSlantPath, new Color(HudGoldColor.r, HudGoldColor.g, HudGoldColor.b, 0f), Middle, Vector2.zero, size + MenuCardGlowGrow);
            CreateSlant(body, "Shadow", HudSlantPath, CardShadowColor, Middle, MenuCardShadowOffset, size);
            var plate = CreateFramedSlant(body, "Plate", color, Middle, Vector2.zero, size);
            var arrow = CreateHudLabel(plate.transform, "Arrow", MenuCardArrowFontSize, TextAnchor.MiddleCenter, 0f, 0f);
            arrow.text = "▶";
            arrow.color = HudGoldColor;
            var arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0f, 0.5f);
            arrowRect.sizeDelta = new Vector2(size.y, size.y);
            arrowRect.anchoredPosition = new Vector2(MenuCardArrowX, 0f);
            var text = centered
                ? CreateHudLabel(plate.transform, "Label", fontSize, TextAnchor.MiddleCenter, MenuCardCenteredPadding, MenuCardCenteredPadding * 0.5f)
                : CreateHudLabel(plate.transform, "Label", fontSize, TextAnchor.MiddleLeft, MenuCardLabelPaddingLeft, 0f);
            text.text = label;
            text.GetComponent<Outline>().effectColor = Color.Lerp(color, Color.black, 1f - ButtonLabelDarken);

            var card = root.gameObject.AddComponent<StageMenuCard>();
            var so = new SerializedObject(card);
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_plate").objectReferenceValue = plate;
            so.FindProperty("_glow").objectReferenceValue = glow;
            so.FindProperty("_arrow").objectReferenceValue = arrow.gameObject;
            so.FindProperty("_label").objectReferenceValue = text;
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_slideFrom").vector2Value = slideFrom;
            so.ApplyModifiedProperties();
            return card;
        }

        /// <summary>「設定」の札の右にすべり出る板: 小さな「音量」の札と、BGM・効果音のつまみ（後ろに選んでいる行の光）</summary>
        private static StageMenuVolume CreateMenuVolume(Transform safe)
        {
            var panel = CreatePanel(safe, "Volume", Vector2.zero, VolumePanelSize);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = VolumePanelPosition;
            SetSlideIn(panel, VolumeSlideFrom, 0f);
            var tag = CreateFramedSlant(rect, "Tag", SettingsCardColor, Middle, VolumeTagPosition, VolumeTagSize);
            CreateHudLabel(tag.transform, "Text", VolumeTagFontSize, TextAnchor.MiddleCenter, 0f, 0f).text = "音量";

            var labels = new[] { "BGM", "効果音" };
            var sliders = new Object[labels.Length];
            var highlights = new Object[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var highlight = CreateSlant(rect, $"Highlight{i + 1}", HudSlantPath, VolumeHighlightColor, Middle, new Vector2(0f, VolumeRowYs[i]), VolumeHighlightSize);
                highlight.enabled = false;
                highlights[i] = highlight;
                sliders[i] = CreateMenuVolumeSlider(rect, i == 0 ? "BgmVolume" : "SeVolume", labels[i], VolumeRowYs[i]);
            }

            var volume = panel.AddComponent<StageMenuVolume>();
            var so = new SerializedObject(volume);
            SetObjectArray(so.FindProperty("_sliders"), sliders);
            SetObjectArray(so.FindProperty("_rowHighlights"), highlights);
            so.ApplyModifiedProperties();
            panel.SetActive(false);
            return volume;
        }

        private static Slider CreateMenuVolumeSlider(Transform panel, string name, string label, float y)
        {
            var slider = UIDialogBuilder.CreateVolumeSlider(name, panel, label);
            var row = slider.transform.parent.GetComponent<RectTransform>();
            PlaceCenter(row, new Vector2(0f, y), VolumeMenuRowSize);
            var labelText = row.Find("Label").GetComponent<Text>();
            labelText.fontSize = MenuFontSize - 8;
            labelText.fontStyle = FontStyle.Bold;
            slider.fillRect.GetComponent<Image>().color = VolumeFillColor;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        /// <summary>右の編成の欄: 3体を上から並べる（StageMenuParty）。開くたびに右からすべり込む</summary>
        private static StageMenuParty CreateMenuParty(Transform safe)
        {
            var panel = CreateHudRect(safe, "Party", new Vector2(1f, 0.5f), PartyPanelPosition, PartyPanelSize);
            SetSlideIn(panel.gameObject, PartySlideFrom, 0.05f);
            var party = panel.gameObject.AddComponent<StageMenuParty>();
            var so = new SerializedObject(party);
            var rows = so.FindProperty("_rows");
            rows.arraySize = MemberSlotCount;
            for (var i = 0; i < MemberSlotCount; i++)
            {
                CreateMenuPartyRow(panel, i, rows.GetArrayElementAtIndex(i));
            }

            so.ApplyModifiedProperties();
            return party;
        }

        /// <summary>1体分: 左に丸い顔（場に出ている子は金の輪と札）、右の斜めの帯に名前と HP のバー</summary>
        private static void CreateMenuPartyRow(RectTransform panel, int index, SerializedProperty row)
        {
            var root = CreateHudRect(panel, $"Member{index + 1}", Middle, new Vector2(0f, PartyRowSpacing * (1 - index)), PartyRowSize);
            CreateFramedSlant(root, "Strip", HudBackColor, Middle, PartyStripPosition, PartyStripSize);
            var name = CreateHudLabel(CreateHudRect(root, "NameArea", Middle, PartyNamePosition, PartyNameSize), "Name", PartyNameFontSize, TextAnchor.MiddleLeft, 0f, 0f);
            var hp = CreateHudBar(root, "Hp", Middle, PartyHpBarPosition, PartyHpBarSize, HpFillColor, colorByRatio: true);
            var hpLabel = CreateHudLabel(hp.transform, "Label", PartyHpFontSize, TextAnchor.MiddleLeft, HpLabelPaddingLeft, 0f);
            hpLabel.text = "HP";
            hpLabel.color = HudGoldColor;
            var hpText = CreateHudLabel(hp.transform, "Text", PartyHpFontSize, TextAnchor.MiddleRight, PartyHpTextPaddingLeft, PartyHpTextPaddingRight);

            var size = Vector2.one * PartyDiscDiameter;
            var disc = CreateHudRect(root, "Disc", Middle, PartyDiscPosition, size);
            var active = CreateHudRect(disc, "Active", Middle, Vector2.zero, size);
            CreateHudImage(active, "Ring", TouchRingPath, HudGoldColor, Middle, Vector2.zero, size * PartyActiveRingRatio);
            CreateHudImage(disc, "Shadow", TouchCirclePath, ShadowColor, Middle, ShadowOffset, size);
            CreateHudImage(disc, "Rim", TouchCirclePath, HudRimColor, Middle, Vector2.zero, size);
            var (face, portrait) = AddPortraitFace(disc, PartyDiscDiameter);
            var cross = CreateHudLabel(disc, "Cross", PartyCrossFontSize, TextAnchor.MiddleCenter, 0f, 0f);
            cross.text = "×";
            FitLineHeight(cross);
            cross.color = SlotCrossColor;
            // 札は顔の下に重ねるので、顔より後に作る
            active.SetAsLastSibling();
            var tag = CreateFramedSlant(active, "Tag", HudGoldColor, Middle, new Vector2(0f, -PartyDiscDiameter * 0.5f), PartyActiveTagSize);
            CreateHudLabel(tag.transform, "Text", PartyActiveTagFontSize, TextAnchor.MiddleCenter, 0f, 0f).text = PartyActiveLabel;

            row.FindPropertyRelative("Root").objectReferenceValue = root.gameObject;
            row.FindPropertyRelative("Disc").objectReferenceValue = face;
            row.FindPropertyRelative("Face").objectReferenceValue = portrait;
            row.FindPropertyRelative("Name").objectReferenceValue = name;
            row.FindPropertyRelative("Hp").objectReferenceValue = hp;
            row.FindPropertyRelative("HpText").objectReferenceValue = hpText;
            row.FindPropertyRelative("Cross").objectReferenceValue = cross.gameObject;
            row.FindPropertyRelative("ActiveMark").objectReferenceValue = active.gameObject;
        }

        /// <summary>
        /// 確認の窓（StageMenuConfirm）: 暗い幕・角の丸い板・左上の斜めの見出しの札・文・「もどる」と「（やること）」の斜めの札。
        /// 板まで斜めにすると、背の高い板では斜めの部分と縁が太くぼやけるので、板だけはほかの窓と同じ角の丸い板にする
        /// </summary>
        private static StageMenuConfirm CreateMenuConfirm(Transform content)
        {
            var root = CreateFullScreenBacking(content, "Confirm", ModalDimColor);
            var panel = CreatePanel(root.transform, "Panel", MenuConfirmPanelPosition, MenuConfirmPanelSize);
            var panelRect = panel.GetComponent<RectTransform>();
            SetSlideIn(panel, ModalSlideFrom, 0f);
            var headingPlate = CreateFramedSlant(panelRect, "Heading", ResetColor, Middle, MenuConfirmHeadingPosition, MenuConfirmHeadingSize);
            var heading = CreateHudLabel(headingPlate.transform, "Text", MenuConfirmHeadingFontSize, TextAnchor.MiddleCenter, 0f, 0f);
            var message = CreateLabel(panelRect, "Message", string.Empty, MenuConfirmMessageFontSize, MenuConfirmMessagePosition, MenuConfirmMessageSize);

            var cancel = CreateMenuCard(panelRect, "Cancel", "もどる", CancelColor, Middle, new Vector2(-MenuConfirmCardX, MenuConfirmCardY), MenuConfirmCardSize, MenuConfirmCardFontSize, MenuConfirmCardSlideFrom, centered: true);
            var ok = CreateMenuCard(panelRect, "Ok", string.Empty, ResetColor, Middle, new Vector2(MenuConfirmCardX, MenuConfirmCardY), MenuConfirmCardSize, MenuConfirmCardFontSize, MenuConfirmCardSlideFrom, centered: true);

            var confirm = root.AddComponent<StageMenuConfirm>();
            var so = new SerializedObject(confirm);
            so.FindProperty("_heading").objectReferenceValue = heading;
            so.FindProperty("_message").objectReferenceValue = message;
            SetObjectArray(so.FindProperty("_cards"), new Object[] { cancel, ok });
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return confirm;
        }
    }
}
