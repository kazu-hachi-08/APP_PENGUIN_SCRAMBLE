using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 結果の画面（ResultScreen）: 右から暗くなるぼかし・紙ふぶき・右寄りの列（見出しと光の筋・ステージの名札・札・ボタン）・飛ぶコイン。
    /// 左側は空けて、寄ったカメラが映すペンモンを見せる（StageView.ShowResultStage）。札は HUD と同じ斜めの板。
    /// 光の筋とぼかしの絵も、ほかの UI の絵と同じく Rebuild のたびにコードで描いて Art/UI/ に置く
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string ResultRaysPath = UiArtFolder + "ResultRays.png";
        private const string ResultShadePath = UiArtFolder + "ResultShade.png";

        // 光の筋: 本数と、筋の太さ（0〜1。1 で隙間なし）、まん中のくぼみ（見出しの文字の後ろは明るくしすぎない）
        private const int RayCount = 14;
        private const float RayWidth = 0.45f;
        private const float RayInnerFade = 0.18f;
        private const int ShadeSpriteWidth = 256;

        // 右から暗くするぼかし（画面の右端から測る幅）
        private const float ResultShadeWidth = 1250f;
        // 札の列は画面の幅のこの割合の所を真ん中にする（左のペンモンと重ならないよう、縦横比が変わっても割合で置く）
        private const float ResultColumnX = 0.72f;
        private static readonly Vector2 ResultColumnSize = new Vector2(640f, 1080f);

        private static readonly Vector2 ResultHeadlinePosition = new Vector2(0f, 330f);
        private static readonly Vector2 ResultHeadlineSize = new Vector2(900f, 200f);
        private const int ResultHeadlineFontSize = 150;
        private static readonly Vector2 ResultHeadlineOutline = new Vector2(6f, -6f);
        private static readonly Vector2 ResultHeadlineShadow = new Vector2(0f, -12f);
        private const float ResultRaysSize = 780f;
        private static readonly Color ResultRaysColor = new Color(1f, 0.9f, 0.5f, 0.4f);
        private static readonly Vector2 ResultStagePlatePosition = new Vector2(0f, -122f);
        // 「ボスラッシュ 火山の闘技場 むずかしい」まで入る幅。入りきらなければ字を縮める
        private static readonly Vector2 ResultStagePlateSize = new Vector2(580f, 58f);
        private const float ResultStagePadding = 36f;
        private const int ResultStageFontSize = 30;

        // 札: 一番上の位置と間隔は ResultScreen（出さない札を詰めて並べる）
        private const float ResultRowTop = 110f;
        private const float ResultRowSpacing = 96f;
        // 一番下の札の位置。ボタン（ResultButtonsPosition）の上の端より札の半分だけ上
        private const float ResultRowBottom = -226f;
        private static readonly Vector2 ResultRowSize = new Vector2(600f, 80f);
        private static readonly Vector2 ResultRowSlideFrom = new Vector2(520f, 0f);
        private const float ResultRowLabelPadding = 44f;
        private const float ResultRowValuePadding = 48f;
        private const int ResultRowLabelFontSize = 34;
        private const int ResultRowValueFontSize = 46;
        private const float ResultCoinIconSize = 64f;
        private const float ResultCoinIconX = 60f;
        private const float ResultCoinLabelPadding = 104f;
        private static readonly Color ResultUnlockColor = new Color(0.22f, 0.68f, 0.45f);
        private const string DefeatedLabel = "倒した数";
        private const string RoundsLabel = "勝った戦い";
        private const string CoinLabel = "コイン";
        private const string BonusLabel = "初クリア ボーナス";

        private static readonly Vector2 ResultButtonsPosition = new Vector2(0f, -330f);
        private static readonly Vector2 ResultButtonsSize = new Vector2(640f, 120f);
        private static readonly Vector2 ResultButtonSize = new Vector2(300f, 110f);
        private const float ResultButtonX = 160f;
        private const int ResultButtonFontSize = 44;
        private static readonly Color RetryButtonColor = new Color(0.18f, 0.55f, 0.9f);
        private static readonly Color ResultTitleButtonColor = new Color(0.45f, 0.5f, 0.6f);
        private static readonly Vector2 ResultGuestNotePosition = new Vector2(0f, -440f);
        private static readonly Vector2 ResultGuestNoteSize = new Vector2(760f, 50f);
        private const int ResultGuestNoteFontSize = 30;

        private static void CreateResultArt()
        {
            WriteUiSprite(ResultRaysPath, DrawIcon(RaysPixel), 0);
            // 左が透明で右へ濃くなる横のぼかし（縦に伸ばして使う）
            WriteUiSprite(ResultShadePath, PaintTexture(ShadeSpriteWidth, 1, (x, _) =>
            {
                var t = (x + 0.5f) / ShadeSpriteWidth;
                return White(t * t * (3f - 2f * t));
            }), 0);
        }

        /// <summary>まん中から放射状に伸びる光の筋。外へ向かって薄く消え、まん中も少しくぼませる</summary>
        private static Color RaysPixel(Vector2 p, float pixel)
        {
            var r = p.magnitude;
            var angle = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) * RayCount;
            var wedge = Mathf.Abs(angle - Mathf.Round(angle)) * 2f;
            var ray = Mathf.Clamp01((RayWidth - wedge) / (pixel * RayCount));
            var fade = Mathf.Clamp01(1f - r) * Mathf.SmoothStep(0f, 1f, r / RayInnerFade);
            return White(ray * fade);
        }

        private static ResultScreen CreateResultScreen(Transform hud)
        {
            // 透明な下地で全体を受けて、押したら演出を飛ばす（ResultScreen.OnPointerClick）
            var root = CreateFullScreenBacking(hud, "Result", Color.clear);
            var screen = root.AddComponent<ResultScreen>();
            var so = new SerializedObject(screen);

            var shade = CreateHudRect(root.transform, "Shade", new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            shade.anchorMin = new Vector2(1f, 0f);
            shade.anchorMax = Vector2.one;
            shade.pivot = new Vector2(1f, 0.5f);
            shade.sizeDelta = new Vector2(ResultShadeWidth, 0f);
            so.FindProperty("_shade").objectReferenceValue = AddImage(shade.gameObject, UiSprite(ResultShadePath), Color.white, false);

            so.FindProperty("_confetti").objectReferenceValue = CreateStretchGroup(root.transform, "Confetti").AddComponent<ResultConfetti>();

            var column = CreateHudRect(root.transform, "Column", new Vector2(ResultColumnX, 0.5f), Vector2.zero, ResultColumnSize);
            CreateResultHeadline(column, so);
            CreateResultRows(column, so);
            CreateResultButtons(column, so);

            // コインは札の上を飛ぶので、列より後ろ（上）に置く
            var burst = CreateStretchGroup(root.transform, "CoinBurst").AddComponent<ResultCoinBurst>();
            var burstSo = new SerializedObject(burst);
            burstSo.FindProperty("_coinSprite").objectReferenceValue = UiSprite(CoinIconPath);
            burstSo.ApplyModifiedProperties();
            so.FindProperty("_coinBurst").objectReferenceValue = burst;

            so.FindProperty("_rowTop").floatValue = ResultRowTop;
            so.FindProperty("_rowSpacing").floatValue = ResultRowSpacing;
            so.FindProperty("_rowBottom").floatValue = ResultRowBottom;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return screen;
        }

        /// <summary>見出し（太字・濃い紺の太い縁取りと影）と後ろで回る光の筋、下にステージの名札</summary>
        private static void CreateResultHeadline(RectTransform column, SerializedObject screen)
        {
            var group = CreateHudRect(column, "Headline", Middle, ResultHeadlinePosition, ResultHeadlineSize);
            group.gameObject.AddComponent<CanvasGroup>();
            var headline = group.gameObject.AddComponent<ResultHeadline>();
            var rays = CreateHudImage(group, "Rays", ResultRaysPath, ResultRaysColor, Middle, Vector2.zero, Vector2.one * ResultRaysSize);

            var titleRect = CreateHudRect(group, "Title", Middle, Vector2.zero, ResultHeadlineSize);
            var title = AddText(titleRect.gameObject, string.Empty, ResultHeadlineFontSize);
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.raycastTarget = false;
            var outline = titleRect.gameObject.AddComponent<Outline>();
            outline.effectColor = HudRimColor;
            outline.effectDistance = ResultHeadlineOutline;
            var shadow = titleRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ButtonShadowColor;
            shadow.effectDistance = ResultHeadlineShadow;

            var plate = CreateFramedSlant(group, "StagePlate", HudBackColor, Middle, ResultStagePlatePosition, ResultStagePlateSize);
            var stageName = CreateHudLabel(plate.transform, "Text", ResultStageFontSize, TextAnchor.MiddleCenter, ResultStagePadding, ResultStagePadding);
            SetBestFit(stageName, ResultStageFontSize);

            var so = new SerializedObject(headline);
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_rays").objectReferenceValue = rays.rectTransform;
            so.FindProperty("_stagePlate").objectReferenceValue = plate.gameObject;
            so.FindProperty("_stageName").objectReferenceValue = stageName;
            so.ApplyModifiedProperties();
            screen.FindProperty("_headline").objectReferenceValue = headline;
        }

        /// <summary>
        /// 倒した数・勝った戦い（ボスラッシュ）・コイン・初クリア ボーナス・解放の札。位置は ResultScreen が出す札だけ詰めて決める
        /// </summary>
        private static void CreateResultRows(RectTransform column, SerializedObject screen)
        {
            screen.FindProperty("_defeatedRow").objectReferenceValue = CreateResultRow(column, "Defeated", HudBackColor, DefeatedLabel, ResultRowLabelPadding, false, out _);
            screen.FindProperty("_roundsRow").objectReferenceValue = CreateResultRow(column, "Rounds", HudBackColor, RoundsLabel, ResultRowLabelPadding, false, out _);

            var coin = CreateResultRow(column, "Coins", HudBackColor, CoinLabel, ResultCoinLabelPadding, false, out var coinPlate);
            var icon = CreateHudImage(coinPlate.rectTransform, "Icon", CoinIconPath, Color.white, new Vector2(0f, 0.5f), new Vector2(ResultCoinIconX, 0f), Vector2.one * ResultCoinIconSize);
            screen.FindProperty("_coinRow").objectReferenceValue = coin;
            screen.FindProperty("_coinIcon").objectReferenceValue = icon.rectTransform;

            screen.FindProperty("_bonusRow").objectReferenceValue = CreateResultRow(column, "Bonus", HudGoldColor, BonusLabel, ResultRowLabelPadding, false, out _);

            // 解放の札は数えないので、文字をまん中に1つだけ置き、出たあとゆっくりふくらませて目を引く
            var unlock = CreateResultRow(column, "Unlock", ResultUnlockColor, null, 0f, true, out var unlockPlate);
            var unlockText = CreateHudLabel(unlockPlate.transform, "Text", ResultRowLabelFontSize, TextAnchor.MiddleCenter, ResultRowLabelPadding, ResultRowLabelPadding);
            SetBestFit(unlockText, ResultRowLabelFontSize);
            screen.FindProperty("_unlockRow").objectReferenceValue = unlock;
            screen.FindProperty("_unlockText").objectReferenceValue = unlockText;
        }

        /// <param name="label">左の見出し。null なら見出しも数字も置かない（呼んだ側が文字を置く）</param>
        private static ResultRow CreateResultRow(RectTransform column, string name, Color color, string label, float labelPadding, bool pulse, out Image plate)
        {
            var root = CreateHudRect(column, name, Middle, Vector2.zero, ResultRowSize);
            root.gameObject.AddComponent<CanvasGroup>();
            plate = CreateFramedSlant(root, "Plate", color, Middle, Vector2.zero, ResultRowSize);
            Text value = null;
            if (label != null)
            {
                var labelText = CreateHudLabel(plate.transform, "Label", ResultRowLabelFontSize, TextAnchor.MiddleLeft, labelPadding, 0f);
                labelText.text = label;
                // 金の札の上では金の字が読めないので、白のままにする
                labelText.color = color == HudGoldColor ? Color.white : HudGoldColor;
                value = CreateHudLabel(plate.transform, "Value", ResultRowValueFontSize, TextAnchor.MiddleRight, 0f, ResultRowValuePadding);
            }

            var row = root.gameObject.AddComponent<ResultRow>();
            var so = new SerializedObject(row);
            so.FindProperty("_plate").objectReferenceValue = plate.rectTransform;
            so.FindProperty("_value").objectReferenceValue = value;
            so.FindProperty("_slideFrom").vector2Value = ResultRowSlideFrom;
            so.FindProperty("_pulse").boolValue = pulse;
            so.ApplyModifiedProperties();
            return row;
        }

        /// <summary>「もう一度」「タイトルへ」はメニューと同じボタン。ゲストには「もう一度」の代わりに待つことを知らせる</summary>
        private static void CreateResultButtons(RectTransform column, SerializedObject screen)
        {
            var group = CreateHudRect(column, "Buttons", Middle, ResultButtonsPosition, ResultButtonsSize);
            screen.FindProperty("_buttons").objectReferenceValue = group.gameObject.AddComponent<CanvasGroup>();
            var retry = CreateTitleButton(group, "Retry", "もう一度", new Vector2(-ResultButtonX, 0f), ResultButtonSize, RetryButtonColor, ResultButtonFontSize);
            var toTitle = CreateTitleButton(group, "Title", "タイトルへ", new Vector2(ResultButtonX, 0f), ResultButtonSize, ResultTitleButtonColor, ResultButtonFontSize);
            foreach (var button in new[] { retry, toTitle })
            {
                // キーボード・コントローラーの「決定」で押されないように（Space は交代、攻撃のボタンは ResultScreen が受ける）
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }

            screen.FindProperty("_retryButton").objectReferenceValue = retry;
            screen.FindProperty("_titleButton").objectReferenceValue = toTitle;

            var note = CreateLabel(column, "GuestNote", string.Empty, ResultGuestNoteFontSize, ResultGuestNotePosition, ResultGuestNoteSize);
            screen.FindProperty("_guestNote").objectReferenceValue = note;
        }
    }
}
