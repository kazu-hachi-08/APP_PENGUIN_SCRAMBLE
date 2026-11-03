using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.UI;
using PenmonScramble.Game;
using PenmonScramble.Golf.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ゴルフの HUD（GolfHud）: 左上にホールの札・何打目・打順の表、上のまん中に持ち時間の輪、右上に「もどる」、
    /// 左下に打つ子の3体のカード、右にパワーゲージ、スマホでは右下に打つ大ボタン、まん中に知らせ、PC では下に操作の説明。
    /// 板・ボタン・顔の丸は本編の HUD と同じ部品（PenmonScrambleSceneBuilder）。画面のボタンの無い所はドラッグで狙う（GolfDragPad）
    /// </summary>
    public static partial class GolfSceneBuilder
    {
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 RightMiddle = new Vector2(1f, 0.5f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private static readonly Color HudGold = new Color(1f, 0.82f, 0.25f);
        private static readonly Color DarkFill = new Color(0f, 0f, 0f, 0.45f);

        // ---- 左上: ホールの札・何打目・打順の表（左上の角から測る） ----
        private static readonly Vector2 HolePlatePosition = new Vector2(290f, -78f);
        private static readonly Vector2 HolePlateSize = new Vector2(500f, 100f);
        private const int HoleNameFontSize = 40;
        private const int ParFontSize = 34;
        private static readonly Vector2 StrokePlatePosition = new Vector2(260f, -166f);
        private static readonly Vector2 StrokePlateSize = new Vector2(440f, 60f);
        private const int StrokeFontSize = 28;
        private const int TurnRowCount = 4;
        private static readonly Vector2 FirstRowPosition = new Vector2(230f, -240f);
        private static readonly Vector2 RowSize = new Vector2(380f, 54f);
        private const float RowSpacing = 62f;
        private const float RowSeatSize = 30f;
        private const int RowFontSize = 28;

        // ---- 上のまん中: 持ち時間の輪 ----
        private static readonly Vector2 TimerPosition = new Vector2(0f, -86f);
        private const float TimerSize = 116f;
        private const int TimerFontSize = 48;

        // ---- 右上: もどる ----
        private static readonly Vector2 BackPlayPosition = new Vector2(-130f, -66f);
        private static readonly Vector2 BackPlaySize = new Vector2(220f, 96f);
        private const int BackPlayFontSize = 40;

        // ---- 左下: 打つ子のカード ----
        private const int CardCount = 3;
        private static readonly Vector2 FirstCardPosition = new Vector2(130f, 170f);
        private const float CardSpacing = 190f;
        private const float CardDiameter = 150f;
        private const float CardRingRatio = 1.16f;
        private const float CardStarsY = -0.66f;
        private const int CardStarsFontSize = 30;
        private static readonly Vector2 CardCaptionPosition = new Vector2(320f, 300f);
        private static readonly Vector2 CardCaptionSize = new Vector2(500f, 50f);
        private const int CardCaptionFontSize = 28;
        private const string CardCaption = "打つ子（K / ○ で交代）";

        // ---- 右: パワーゲージ ----
        private static readonly Vector2 GaugePosition = new Vector2(-96f, 60f);
        private static readonly Vector2 GaugeSize = new Vector2(70f, 520f);
        private const float GaugeInset = 8f;
        private static readonly Vector2 GaugeNeedleSize = new Vector2(96f, 10f);
        private static readonly Vector2 GaugeTickSize = new Vector2(20f, 4f);
        private static readonly Vector2 GaugeLabelSize = new Vector2(160f, 50f);
        private const int GaugeLabelFontSize = 28;

        // ---- 右下: 打つ大ボタン（スマホだけ） ----
        private static readonly Vector2 ShootPosition = new Vector2(-330f, 260f);
        private const float ShootSize = 250f;
        private const int ShootFontSize = 60;
        private static readonly Color ShootColor = new Color(1f, 0.55f, 0.2f);

        // ---- まん中: 知らせ・下: 操作の説明 ----
        private static readonly Vector2 BannerPosition = new Vector2(0f, 200f);
        private static readonly Vector2 BannerSize = new Vector2(1500f, 220f);
        private const int BannerFontSize = 80;
        private static readonly Vector2 HelpPosition = new Vector2(0f, 34f);
        private static readonly Vector2 HelpSize = new Vector2(1500f, 48f);
        private const int HelpFontSize = 26;
        private const string HelpText = "←→ / スティック: 向き　J / ×: ゲージ → もう一度で打つ　右スティック / 右ドラッグ: 見回す　Space / LB: やり直す　Esc: もどる";

        private readonly struct GolfHudParts
        {
            public readonly GolfHud Hud;
            public readonly GolfDragPad Pad;
            public readonly VirtualButton Shoot;
            public readonly Button Back;

            public GolfHudParts(GolfHud hud, GolfDragPad pad, VirtualButton shoot, Button back)
            {
                Hud = hud;
                Pad = pad;
                Shoot = shoot;
                Back = back;
            }
        }

        /// <summary>試しのホールを開くまで隠しておく（ミニゲーム画面から来たときは準備中の板だけ）</summary>
        private static GolfHudParts CreateHud(Transform canvas)
        {
            var root = UIDialogBuilder.CreateUIObject("GolfHud", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            // ドラッグを受ける板は、ほかの部品より奥（最初）に置く
            var pad = CreateDragPad(root.transform);
            var safe = B.CreateStretchGroup(root.transform, "Safe");
            safe.AddComponent<SafeAreaFitter>();
            var hud = root.AddComponent<GolfHud>();
            var so = new SerializedObject(hud);

            CreateHolePlates(safe.transform, so);
            CreateTurnRows(safe.transform, so);
            CreateTimer(safe.transform, so);
            CreateCards(safe.transform, so);
            CreateGauge(safe.transform, so);
            var shoot = CreateShootButton(safe.transform, so);
            CreateBanner(safe.transform, so);
            var help = B.CreateLabel(safe.transform, "Help", HelpText, HelpFontSize, Vector2.zero, HelpSize);
            Place(help.rectTransform, BottomCenter, HelpPosition, HelpSize);
            so.FindProperty("_help").objectReferenceValue = help.gameObject;
            var back = B.CreateTitleButton(safe.transform, "Back", "もどる", Vector2.zero, BackPlaySize, B.CancelColor, BackPlayFontSize);
            Place(back.GetComponent<RectTransform>(), TopRight, BackPlayPosition, BackPlaySize);
            back.navigation = new Navigation { mode = Navigation.Mode.None };

            so.ApplyModifiedProperties();
            root.SetActive(false);
            return new GolfHudParts(hud, pad, shoot, back);
        }

        private static GolfDragPad CreateDragPad(Transform parent)
        {
            var obj = UIDialogBuilder.CreateUIObject("DragPad", parent);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            // 見えないが、指を受けるために raycast は残す
            var image = obj.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            return obj.AddComponent<GolfDragPad>();
        }

        /// <summary>ホールの名前とパーの斜めの札、その下に何打目の小さな札</summary>
        private static void CreateHolePlates(Transform safe, SerializedObject so)
        {
            var parent = (RectTransform)safe;
            var plate = B.CreateFramedSlant(parent, "HolePlate", B.HudBackColor, TopLeft, HolePlatePosition, HolePlateSize);
            var name = B.CreateHudLabel(plate.transform, "Name", HoleNameFontSize, TextAnchor.MiddleLeft, 40f, 150f);
            var par = B.CreateHudLabel(plate.transform, "Par", ParFontSize, TextAnchor.MiddleRight, 40f, 40f);
            par.color = HudGold;
            var stroke = B.CreateFramedSlant(parent, "StrokePlate", B.HudBackColor, TopLeft, StrokePlatePosition, StrokePlateSize);
            var strokeText = B.CreateHudLabel(stroke.transform, "Text", StrokeFontSize, TextAnchor.MiddleLeft, 36f, 20f);
            so.FindProperty("_holeName").objectReferenceValue = name;
            so.FindProperty("_par").objectReferenceValue = par;
            so.FindProperty("_stroke").objectReferenceValue = strokeText;
        }

        /// <summary>打順の表: 1行ずつ、板・▶・席の色の丸・名前・打数</summary>
        private static void CreateTurnRows(Transform safe, SerializedObject so)
        {
            var rows = new Object[TurnRowCount];
            for (var i = 0; i < TurnRowCount; i++)
            {
                var rect = B.CreateHudRect(safe, $"Turn{i + 1}", TopLeft, FirstRowPosition + Vector2.down * (RowSpacing * i), RowSize);
                var plate = Rounded(B.CreateHudImage(rect, "Plate", B.RoundRectPath, DarkFill, Center, Vector2.zero, RowSize), RowSize.y * 0.5f);
                var marker = B.CreateHudLabel(rect, "Marker", RowFontSize, TextAnchor.MiddleLeft, -34f, 0f);
                marker.text = "▶";
                marker.color = HudGold;
                var seat = B.CreateHudImage(rect, "Seat", B.TouchCirclePath, Color.white, new Vector2(0f, 0.5f), new Vector2(34f, 0f), Vector2.one * RowSeatSize);
                var name = B.CreateHudLabel(rect, "Name", RowFontSize, TextAnchor.MiddleLeft, 62f, 0f);
                var strokes = B.CreateHudLabel(rect, "Strokes", RowFontSize, TextAnchor.MiddleRight, 0f, 20f);

                var row = rect.gameObject.AddComponent<GolfTurnRow>();
                var rowSo = new SerializedObject(row);
                rowSo.FindProperty("_plate").objectReferenceValue = plate;
                rowSo.FindProperty("_seat").objectReferenceValue = seat;
                rowSo.FindProperty("_name").objectReferenceValue = name;
                rowSo.FindProperty("_strokes").objectReferenceValue = strokes;
                rowSo.FindProperty("_marker").objectReferenceValue = marker.gameObject;
                rowSo.ApplyModifiedProperties();
                rect.gameObject.SetActive(false);
                rows[i] = row;
            }

            B.SetObjectArray(so.FindProperty("_rows"), rows);
        }

        /// <summary>持ち時間: 暗い丸に、減っていく白い輪と残りの秒</summary>
        private static void CreateTimer(Transform safe, SerializedObject so)
        {
            var root = B.CreateHudRect(safe, "Timer", TopCenter, TimerPosition, Vector2.one * TimerSize);
            var visual = B.CreateHudRect(root, "Visual", Center, Vector2.zero, Vector2.one * TimerSize);
            B.CreateHudImage(visual, "Back", B.TouchCirclePath, B.HudBackColor, Center, Vector2.zero, Vector2.one * TimerSize);
            var fill = B.CreateHudImage(visual, "Fill", B.TouchRingPath, Color.white, Center, Vector2.zero, Vector2.one * TimerSize);
            B.SetRadialFill(fill);
            var text = B.CreateHudLabel(visual, "Seconds", TimerFontSize, TextAnchor.MiddleCenter, 0f, 0f);
            so.FindProperty("_timer").objectReferenceValue = root.gameObject;
            so.FindProperty("_timerVisual").objectReferenceValue = visual;
            so.FindProperty("_timerFill").objectReferenceValue = fill;
            so.FindProperty("_timerText").objectReferenceValue = text;
            root.gameObject.SetActive(false);
        }

        /// <summary>打つ子のカード3枚（属性の色の丸に顔・★・選んでいる子は金の輪）と、上に小さな見出し</summary>
        private static void CreateCards(Transform safe, SerializedObject so)
        {
            var caption = B.CreateLabel(safe, "CardCaption", CardCaption, CardCaptionFontSize, Vector2.zero, CardCaptionSize);
            Place(caption.rectTransform, BottomLeft, CardCaptionPosition, CardCaptionSize);
            caption.fontStyle = FontStyle.Bold;

            var cards = new Object[CardCount];
            for (var i = 0; i < CardCount; i++)
            {
                cards[i] = CreateCard(safe, i, FirstCardPosition + Vector2.right * (CardSpacing * i));
            }

            B.SetObjectArray(so.FindProperty("_cards"), cards);
        }

        private static GolfMemberCard CreateCard(Transform safe, int index, Vector2 position)
        {
            var size = Vector2.one * CardDiameter;
            var rect = B.CreateHudRect(safe, $"Card{index + 1}", BottomLeft, position, size);
            // 押せる範囲は丸い透明の絵（見た目は Visual が持つ）
            var hit = B.AddImage(rect.gameObject, AssetDatabase.LoadAssetAtPath<Sprite>(B.TouchCirclePath), Color.clear, true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            // スティック・十字キーは狙う向きに使うので、UI の選択を動かさない
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var visual = B.CreateHudRect(rect, "Visual", Center, Vector2.zero, size);
            var ring = B.CreateHudImage(visual, "Ring", B.TouchCirclePath, HudGold, Center, Vector2.zero, size * CardRingRatio);
            B.CreateHudImage(visual, "Rim", B.TouchCirclePath, Color.white, Center, Vector2.zero, size);
            var (disc, face) = B.AddPortraitFace(visual, CardDiameter);
            var stars = B.CreateHudLabel(visual, "Stars", CardStarsFontSize, TextAnchor.MiddleCenter, 0f, 0f);
            stars.color = HudGold;
            var starsRect = stars.rectTransform;
            starsRect.anchorMin = starsRect.anchorMax = Center;
            starsRect.sizeDelta = new Vector2(CardDiameter, CardStarsFontSize * 1.5f);
            starsRect.anchoredPosition = new Vector2(0f, CardDiameter * CardStarsY);

            var card = rect.gameObject.AddComponent<GolfMemberCard>();
            var cardSo = new SerializedObject(card);
            cardSo.FindProperty("_button").objectReferenceValue = button;
            cardSo.FindProperty("_visual").objectReferenceValue = visual;
            cardSo.FindProperty("_disc").objectReferenceValue = disc;
            cardSo.FindProperty("_face").objectReferenceValue = face;
            cardSo.FindProperty("_stars").objectReferenceValue = stars;
            cardSo.FindProperty("_selectedRing").objectReferenceValue = ring.gameObject;
            cardSo.ApplyModifiedProperties();
            return card;
        }

        /// <summary>縦のパワーゲージ: 白い枠・暗い溝・下から伸びる中身・25% ごとの目盛り・いまの強さの白い線・上に「パワー」</summary>
        private static void CreateGauge(Transform safe, SerializedObject so)
        {
            var root = B.CreateHudRect(safe, "Gauge", RightMiddle, GaugePosition, GaugeSize);
            var frame = Rounded(B.CreateHudImage(root, "Frame", B.RoundRectPath, Color.white, Center, Vector2.zero, GaugeSize), GaugeSize.x * 0.5f);
            var trackSize = GaugeSize - Vector2.one * (GaugeInset * 2f);
            var track = Rounded(B.CreateHudImage(frame.rectTransform, "Track", B.RoundRectPath, B.HudBackColor, Center, Vector2.zero, trackSize), trackSize.x * 0.5f);

            var fill = B.CreateHudImage(track.rectTransform, "Fill", null, Color.white, Center, Vector2.zero, Vector2.zero);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fillRect.localScale = new Vector3(1f, 0f, 1f);
            foreach (var tick in new[] { 0.25f, 0.5f, 0.75f })
            {
                B.CreateHudImage(track.rectTransform, "Tick", null, new Color(1f, 1f, 1f, 0.6f), new Vector2(1f, tick), new Vector2(-GaugeTickSize.x * 0.5f, 0f), GaugeTickSize);
            }

            var needle = Rounded(B.CreateHudImage(track.rectTransform, "Needle", B.RoundRectPath, Color.white, new Vector2(0.5f, 0f), Vector2.zero, GaugeNeedleSize), GaugeNeedleSize.y * 0.5f);
            var label = B.CreateLabel(root, "Label", "パワー", GaugeLabelFontSize, new Vector2(0f, GaugeSize.y * 0.5f + GaugeLabelSize.y * 0.5f), GaugeLabelSize);
            label.fontStyle = FontStyle.Bold;

            var view = root.gameObject.AddComponent<GolfGaugeView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_fill").objectReferenceValue = fillRect;
            viewSo.FindProperty("_fillImage").objectReferenceValue = fill;
            viewSo.FindProperty("_needle").objectReferenceValue = needle.rectTransform;
            viewSo.FindProperty("_frame").objectReferenceValue = frame.rectTransform;
            viewSo.ApplyModifiedProperties();
            so.FindProperty("_gauge").objectReferenceValue = view;
            root.gameObject.SetActive(false);
        }

        /// <summary>スマホだけの打つ大ボタン（押すと沈む厚みのある丸。本編の技のボタンと同じ部品）。タッチできない端末では隠す</summary>
        private static VirtualButton CreateShootButton(Transform safe, SerializedObject so)
        {
            var group = B.CreateStretchGroup(safe, "Touch");
            group.AddComponent<CanvasGroup>();
            var visibility = group.AddComponent<TouchControlsVisibility>();
            var parts = B.CreateTouchButton(group.transform, "Shoot", ShootPosition, ShootSize, ShootColor);
            B.CreateHudImage(parts.Top, "Gloss", B.TouchGlossPath, Color.white, Center, Vector2.zero, Vector2.one * ShootSize * 0.88f);
            var label = B.CreateTouchLabel(parts.Top, "打つ", ShootFontSize);
            label.rectTransform.anchoredPosition = Vector2.zero;
            B.DisableRaycasts(parts.Visual);
            so.FindProperty("_touch").objectReferenceValue = visibility;
            return parts.Button;
        }

        private static void CreateBanner(Transform safe, SerializedObject so)
        {
            var text = B.CreateLabel(safe, "Banner", string.Empty, BannerFontSize, BannerPosition, BannerSize);
            text.fontStyle = FontStyle.Bold;
            text.gameObject.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -8f);
            var group = text.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var banner = text.gameObject.AddComponent<GolfBanner>();
            var bannerSo = new SerializedObject(banner);
            bannerSo.FindProperty("_text").objectReferenceValue = text;
            bannerSo.FindProperty("_group").objectReferenceValue = group;
            bannerSo.ApplyModifiedProperties();
            so.FindProperty("_banner").objectReferenceValue = banner;
            text.gameObject.SetActive(false);
        }

        /// <summary>角の丸い板の絵を、角の丸みが radius に見えるよう敷く</summary>
        private static Image Rounded(Image image, float radius)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = B.CornerScale(radius);
            return image;
        }

        /// <summary>anchor の点から position だけずらした所に置く（CreateLabel などはまん中に置くので、留める角を付け替える）</summary>
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Center;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
