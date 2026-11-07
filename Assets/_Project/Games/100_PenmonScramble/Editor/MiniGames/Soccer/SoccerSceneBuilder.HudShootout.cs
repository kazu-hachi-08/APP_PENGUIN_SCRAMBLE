using PenmonScramble.Soccer.Core;
using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// PK戦の HUD: 点の下の ○× の表（チームごとに色の丸の札と5つの丸）と、下のまん中のためゲージ（上の赤い所は強いが外れることがある）・
    /// 持ち時間・狙い方の一言（SoccerHud.Shootout.cs が出し入れする）
    /// </summary>
    public static partial class SoccerSceneBuilder
    {
        private static readonly Vector2 BoardLabelPosition = new Vector2(0f, -170f);
        private static readonly Vector2 BoardLabelSize = new Vector2(400f, 48f);
        private const int BoardLabelFontSize = 34;
        private const float BoardFirstRowY = -222f;
        private const float BoardRowStep = 56f;
        private const float BoardTeamX = -190f;
        private const float BoardFirstMarkX = -110f;
        private const float BoardMarkStep = 62f;
        private const float BoardMarkSize = 48f;
        private const int BoardMarkFontSize = 34;
        private static readonly Vector2 BoardPlateSize = new Vector2(500f, 170f);
        private static readonly Vector2 BoardPlatePosition = new Vector2(0f, -232f);

        private static readonly Vector2 PenaltyHintPosition = new Vector2(0f, 120f);
        private static readonly Vector2 PenaltyHintSize = new Vector2(1300f, 56f);
        private const int PenaltyHintFontSize = 34;
        private static readonly Vector2 PenaltyGaugePosition = new Vector2(0f, 190f);
        private static readonly Vector2 PenaltyGaugeSize = new Vector2(620f, 40f);
        private const float PenaltyGaugePadding = 8f;
        private static readonly Color PenaltyGaugeColor = new Color(1f, 0.85f, 0.25f);
        private static readonly Color PenaltyRedZoneColor = new Color(1f, 0.25f, 0.2f, 0.55f);
        private static readonly Vector2 PenaltyTimerPosition = new Vector2(400f, 190f);
        private static readonly Vector2 PenaltyTimerSize = new Vector2(120f, 90f);
        private const int PenaltyTimerFontSize = 64;

        /// <summary>点の下の ○× の表。暗い板の上に「PK戦」、赤・青の行（チームの色の丸と5つの丸）</summary>
        private static void CreateShootoutBoard(Transform safe, SerializedObject so)
        {
            var board = B.CreateStretchGroup(safe, "ShootoutBoard");
            B.CreateHudImage(board.transform, "Plate", null, B.HudBackColor, TopCenter, BoardPlatePosition, BoardPlateSize);
            var label = B.CreateLabel(board.transform, "Label", string.Empty, BoardLabelFontSize, Vector2.zero, BoardLabelSize);
            Place(label.rectTransform, TopCenter, BoardLabelPosition, BoardLabelSize);
            label.fontStyle = FontStyle.Bold;

            var marks = new Object[SoccerHud.ShootoutMarksPerTeam * SoccerRules.TeamCount];
            var texts = new Object[marks.Length];
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                var y = BoardFirstRowY - BoardRowStep * team;
                B.CreateHudImage(board.transform, "Team", B.TouchCirclePath, SoccerColors.Team(team), TopCenter, new Vector2(BoardTeamX, y), Vector2.one * BoardMarkSize * 0.7f);
                for (var i = 0; i < SoccerHud.ShootoutMarksPerTeam; i++)
                {
                    var slot = team * SoccerHud.ShootoutMarksPerTeam + i;
                    var position = new Vector2(BoardFirstMarkX + BoardMarkStep * i, y);
                    var mark = B.CreateHudImage(board.transform, $"Mark{slot}", B.TouchCirclePath, Color.white, TopCenter, position, Vector2.one * BoardMarkSize);
                    var text = B.CreateLabel(mark.transform, "Text", string.Empty, BoardMarkFontSize, Vector2.zero, Vector2.one * BoardMarkSize);
                    text.fontStyle = FontStyle.Bold;
                    marks[slot] = mark;
                    texts[slot] = text;
                }
            }

            so.FindProperty("_shootoutBoard").objectReferenceValue = board;
            so.FindProperty("_shootoutLabel").objectReferenceValue = label;
            B.SetObjectArray(so.FindProperty("_shootoutMarks"), marks);
            B.SetObjectArray(so.FindProperty("_shootoutMarkTexts"), texts);
            board.SetActive(false);
        }

        /// <summary>下のまん中: ためゲージ（暗い板・赤い所・たまる金色の棒）と、その右の持ち時間、上の狙い方の一言</summary>
        private static void CreatePenaltyPanel(Transform safe, SerializedObject so)
        {
            var panel = B.CreateStretchGroup(safe, "PenaltyPanel");
            var hint = B.CreateLabel(panel.transform, "Hint", string.Empty, PenaltyHintFontSize, Vector2.zero, PenaltyHintSize);
            Place(hint.rectTransform, BottomCenter, PenaltyHintPosition, PenaltyHintSize);
            hint.fontStyle = FontStyle.Bold;

            var gaugeGroup = B.CreateStretchGroup(panel.transform, "Gauge");
            B.CreateHudImage(gaugeGroup.transform, "Back", null, B.HudBackColor, BottomCenter, PenaltyGaugePosition, PenaltyGaugeSize + Vector2.one * PenaltyGaugePadding);
            var fill = B.CreateHudImage(gaugeGroup.transform, "Fill", null, PenaltyGaugeColor, BottomCenter, PenaltyGaugePosition, PenaltyGaugeSize);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            // 赤い所（PenaltyRedZone より上）を、棒の右の端に重ねる
            var redWidth = PenaltyGaugeSize.x * (1f - SoccerBalance.PenaltyRedZone);
            var redPosition = PenaltyGaugePosition + new Vector2((PenaltyGaugeSize.x - redWidth) * 0.5f, 0f);
            B.CreateHudImage(gaugeGroup.transform, "RedZone", null, PenaltyRedZoneColor, BottomCenter, redPosition, new Vector2(redWidth, PenaltyGaugeSize.y));

            var timer = B.CreateLabel(panel.transform, "Timer", string.Empty, PenaltyTimerFontSize, Vector2.zero, PenaltyTimerSize);
            Place(timer.rectTransform, BottomCenter, PenaltyTimerPosition, PenaltyTimerSize);
            timer.fontStyle = FontStyle.Bold;

            so.FindProperty("_penaltyPanel").objectReferenceValue = panel;
            so.FindProperty("_penaltyGauge").objectReferenceValue = fill;
            so.FindProperty("_penaltyGaugeGroup").objectReferenceValue = gaugeGroup;
            so.FindProperty("_penaltyTimer").objectReferenceValue = timer;
            so.FindProperty("_penaltyHint").objectReferenceValue = hint;
            panel.SetActive(false);
        }
    }
}
