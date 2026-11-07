using PenmonScramble.Common.Editor;
using PenmonScramble.Common.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 試合の HUD（BaseballHud.Match.cs）: 上のまん中の点の板（点・イニング・塁の3つのひし形）、
    /// 左の投げる子の札3枚（人が守る打席の始めだけ）、ゲームセットの欄（暗い幕・勝ち負けと点・もう一度・もどる）
    /// </summary>
    public static partial class BaseballSceneBuilder
    {
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);

        private static readonly Vector2 ScorePlatePosition = new Vector2(0f, -85f);
        private static readonly Vector2 ScorePlateSize = new Vector2(760f, 150f);
        private static readonly Vector2 ScoreTextPosition = new Vector2(0f, 30f);
        private static readonly Vector2 ScoreTextSize = new Vector2(740f, 72f);
        private const int ScoreFontSize = 52;
        private static readonly Vector2 InningTextPosition = new Vector2(-110f, -38f);
        private static readonly Vector2 InningTextSize = new Vector2(360f, 52f);
        private const int InningFontSize = 36;
        // 塁のひし形: 二塁が上、一塁が右、三塁が左（ホームから見た形）
        private static readonly Vector2 BaseDiamondCenter = new Vector2(170f, -40f);
        private static readonly Vector2[] BaseDiamondOffsets = { new Vector2(30f, -4f), new Vector2(0f, 22f), new Vector2(-30f, -4f) };
        private const float BaseDiamondSize = 28f;
        private static readonly Color BaseDiamondOff = new Color(0.22f, 0.24f, 0.30f);

        private const string PitcherHeading = "投げる子（↑↓）";
        private static readonly Vector2 PitcherHeadingPosition = new Vector2(190f, -285f);
        private static readonly Vector2 PitcherHeadingSize = new Vector2(320f, 50f);
        private const int PitcherHeadingFontSize = 30;
        private static readonly Vector2 PitcherCardFirst = new Vector2(190f, -355f);
        private const float PitcherCardStep = 92f;
        private static readonly Vector2 PitcherCardSize = new Vector2(300f, 80f);
        private const int PitcherCardFontSize = 32;
        private static readonly Color PitcherCardColor = new Color(0.35f, 0.42f, 0.55f);

        private static readonly Color EndCurtainColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Vector2 EndTextPosition = new Vector2(0f, 130f);
        private static readonly Vector2 EndTextSize = new Vector2(1600f, 480f);
        private const int EndFontSize = 130;
        private static readonly Vector2 RetryPosition = new Vector2(-240f, -250f);
        private static readonly Vector2 LeavePosition = new Vector2(240f, -250f);
        private static readonly Vector2 EndButtonSize = new Vector2(400f, 130f);
        private const int EndButtonFontSize = 52;
        private static readonly Color RetryColor = new Color(0.30f, 0.62f, 1f);
        private const string EndHint = "J / × でもう一度　Esc でもどる";
        private static readonly Vector2 EndHintPosition = new Vector2(0f, -360f);
        private static readonly Vector2 EndHintSize = new Vector2(1000f, 50f);
        private const int EndHintFontSize = 30;

        private static void CreateMatchHud(Transform safe, Transform root, SerializedObject so)
        {
            CreateScorePlate(safe, so);
            CreatePitcherCards(safe, so);
            CreateEndPanel(root, so);
        }

        /// <summary>点の板: 上の段に「先攻 点 − 点 後攻」、下の段にイニングと塁のひし形</summary>
        private static void CreateScorePlate(Transform safe, SerializedObject so)
        {
            var plate = B.CreateHudImage(safe, "ScorePlate", null, B.HudBackColor, TopCenter, ScorePlatePosition, ScorePlateSize);
            var score = B.CreateLabel(plate.transform, "Score", string.Empty, ScoreFontSize, ScoreTextPosition, ScoreTextSize);
            score.fontStyle = FontStyle.Bold;
            score.supportRichText = true;
            var inning = B.CreateLabel(plate.transform, "Inning", string.Empty, InningFontSize, InningTextPosition, InningTextSize);
            var lights = new Object[BaseDiamondOffsets.Length];
            for (var i = 0; i < lights.Length; i++)
            {
                var diamond = B.CreateHudImage(plate.transform, $"Base{i + 1}", null, BaseDiamondOff, Center,
                    BaseDiamondCenter + BaseDiamondOffsets[i], Vector2.one * BaseDiamondSize);
                diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                lights[i] = diamond;
            }

            so.FindProperty("_scoreText").objectReferenceValue = score;
            so.FindProperty("_inningText").objectReferenceValue = inning;
            B.SetObjectArray(so.FindProperty("_baseLights"), lights);
        }

        /// <summary>投げる子の札（カウントの板の下に縦に3枚。名前は試合の中で入れる）</summary>
        private static void CreatePitcherCards(Transform safe, SerializedObject so)
        {
            var group = B.CreateStretchGroup(safe, "PitcherCards");
            var heading = B.CreateLabel(group.transform, "Heading", PitcherHeading, PitcherHeadingFontSize, Vector2.zero, PitcherHeadingSize);
            Place(heading.rectTransform, TopLeft, PitcherHeadingPosition, PitcherHeadingSize);
            var buttons = new Object[3];
            var labels = new Object[3];
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = B.CreateTitleButton(group.transform, $"Pitcher{i}", string.Empty, Vector2.zero, PitcherCardSize, PitcherCardColor, PitcherCardFontSize);
                Place((RectTransform)button.transform, TopLeft, PitcherCardFirst + Vector2.down * (PitcherCardStep * i), PitcherCardSize);
                buttons[i] = button;
                labels[i] = button.GetComponentInChildren<Text>();
            }

            so.FindProperty("_pitcherCards").objectReferenceValue = group;
            B.SetObjectArray(so.FindProperty("_pitcherButtons"), buttons);
            B.SetObjectArray(so.FindProperty("_pitcherLabels"), labels);
        }

        /// <summary>ゲームセットの欄（画面全体の暗い幕の上。安全な範囲の外まで幕で覆う）</summary>
        private static void CreateEndPanel(Transform root, SerializedObject so)
        {
            var panel = UIDialogBuilder.CreateUIObject("EndPanel", root);
            UIDialogBuilder.SetStretchAll(panel.GetComponent<RectTransform>());
            panel.AddComponent<Image>().color = EndCurtainColor;
            var safe = B.CreateStretchGroup(panel.transform, "Safe");
            safe.AddComponent<SafeAreaFitter>();

            var text = B.CreateLabel(safe.transform, "Result", string.Empty, EndFontSize, EndTextPosition, EndTextSize);
            text.fontStyle = FontStyle.Bold;
            text.supportRichText = true;
            text.raycastTarget = false;
            var retry = B.CreateTitleButton(safe.transform, "Retry", "もう一度", RetryPosition, EndButtonSize, RetryColor, EndButtonFontSize);
            var leave = B.CreateTitleButton(safe.transform, "Leave", "もどる", LeavePosition, EndButtonSize, B.CancelColor, EndButtonFontSize);
            B.CreateLabel(safe.transform, "Hint", EndHint, EndHintFontSize, EndHintPosition, EndHintSize);

            so.FindProperty("_endPanel").objectReferenceValue = panel;
            so.FindProperty("_endText").objectReferenceValue = text;
            so.FindProperty("_retryButton").objectReferenceValue = retry;
            so.FindProperty("_leaveButton").objectReferenceValue = leave;
            panel.SetActive(false);
        }
    }
}
