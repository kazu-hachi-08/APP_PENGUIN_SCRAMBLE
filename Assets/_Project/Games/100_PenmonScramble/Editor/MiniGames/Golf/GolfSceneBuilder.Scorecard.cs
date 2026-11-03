using PenmonScramble.Game;
using PenmonScramble.Golf.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ホールの間のスコアカード（GolfScorecard）と、結果の右の欄（GolfResultPanel）。
    /// 板・札はほかの画面と同じ部品（角の丸い板・メニューの斜めの札）。結果ではスコアカードを左へ寄せ、右に内訳、下に3枚の札
    /// </summary>
    public static partial class GolfSceneBuilder
    {
        private static readonly Color ScorecardDimColor = new Color(0.02f, 0.03f, 0.10f, 0.55f);
        private static readonly Color HeaderRowColor = new Color(1f, 0.82f, 0.25f);

        // ---- スコアカード ----
        private static readonly Vector2 ScorecardSize = new Vector2(1000f, 600f);
        private static readonly Vector2 ScorecardCenter = new Vector2(0f, 70f);
        private static readonly Vector2 ScorecardDocked = new Vector2(-420f, 60f);
        private static readonly Vector2 ScorecardTitlePosition = new Vector2(0f, 236f);
        private static readonly Vector2 ScorecardTitleSize = new Vector2(900f, 90f);
        private const int ScorecardTitleFontSize = 60;
        private const float FirstRowY = 150f;
        private const float RowStep = 70f;
        private static readonly float[] ColumnXs = { -330f, -130f, 20f, 170f, 350f };
        private static readonly float[] ColumnWidths = { 260f, 150f, 150f, 150f, 240f };
        private const float CellHeight = 60f;
        private const int CellFontSize = 38;
        private static readonly Vector2 PlayerPlateSize = new Vector2(940f, 60f);
        private static readonly Vector2 NextCardPosition = new Vector2(0f, -330f);
        private static readonly Vector2 NextCardSize = new Vector2(520f, 116f);
        private const int NextCardFontSize = 52;
        private static readonly Vector2 PanelSlideFrom = new Vector2(0f, -120f);
        private static readonly Vector2 CardSlideFrom = new Vector2(0f, -200f);

        // ---- 結果の右の欄 ----
        private static readonly Vector2 ResultPanelPosition = new Vector2(500f, 60f);
        private static readonly Vector2 ResultPanelSize = new Vector2(720f, 600f);
        private static readonly Vector2 ResultScorePosition = new Vector2(0f, 210f);
        private static readonly Vector2 ResultScoreSize = new Vector2(660f, 110f);
        private const int ResultScoreFontSize = 76;
        private static readonly Vector2 ResultBestPosition = new Vector2(0f, 120f);
        private static readonly Vector2 ResultBestSize = new Vector2(660f, 60f);
        private const int ResultBestFontSize = 40;
        private static readonly Vector2 RewardLabelsPosition = new Vector2(-90f, -50f);
        private static readonly Vector2 RewardLabelsSize = new Vector2(440f, 230f);
        private static readonly Vector2 RewardValuesPosition = new Vector2(220f, -50f);
        private static readonly Vector2 RewardValuesSize = new Vector2(180f, 230f);
        private const int RewardFontSize = 34;
        private static readonly Vector2 CoinTotalPosition = new Vector2(0f, -230f);
        private static readonly Vector2 CoinTotalSize = new Vector2(660f, 80f);
        private const int CoinTotalFontSize = 52;
        private const float ResultCardY = -430f;
        private const float ResultCardSpacing = 520f;
        private static readonly Vector2 ResultCardSize = new Vector2(460f, 116f);
        private const int ResultCardFontSize = 50;

        private static GolfScorecard CreateScorecard(Transform canvas)
        {
            var root = B.CreateFullScreenBacking(canvas, "Scorecard", ScorecardDimColor);
            var panel = B.CreatePanel(root.transform, "Panel", ScorecardCenter, ScorecardSize);
            B.SetSlideIn(panel, PanelSlideFrom, 0f);
            var title = B.CreateLabel(panel.transform, "Title", string.Empty, ScorecardTitleFontSize, ScorecardTitlePosition, ScorecardTitleSize);
            title.fontStyle = FontStyle.Bold;

            var plates = new Object[Golf.Core.GolfRules.MaxPlayers];
            for (var seat = 0; seat < plates.Length; seat++)
            {
                var y = FirstRowY - RowStep * (GolfScorecard.FirstPlayerRow + seat);
                var plate = B.CreateHudImage(panel.transform, $"Seat{seat + 1}", B.RoundRectPath, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, y), PlayerPlateSize);
                Rounded(plate, PlayerPlateSize.y * 0.5f);
                plates[seat] = plate;
            }

            var cells = new Object[GolfScorecard.Rows * GolfScorecard.Columns];
            for (var row = 0; row < GolfScorecard.Rows; row++)
            {
                for (var column = 0; column < GolfScorecard.Columns; column++)
                {
                    var position = new Vector2(ColumnXs[column], FirstRowY - RowStep * row);
                    var cell = B.CreateLabel(panel.transform, $"Cell{row}_{column}", string.Empty, CellFontSize, position, new Vector2(ColumnWidths[column], CellHeight));
                    cell.fontStyle = row < GolfScorecard.FirstPlayerRow ? FontStyle.Bold : FontStyle.Normal;
                    if (row == GolfScorecard.HeaderRow)
                    {
                        cell.color = HeaderRowColor;
                    }

                    cells[row * GolfScorecard.Columns + column] = cell;
                }
            }

            var next = B.CreateMenuCard(root.transform, "Next", string.Empty, B.StartColor, Center, NextCardPosition, NextCardSize, NextCardFontSize, CardSlideFrom, centered: true);

            var scorecard = root.AddComponent<GolfScorecard>();
            var so = new SerializedObject(scorecard);
            so.FindProperty("_panel").objectReferenceValue = panel.GetComponent<RectTransform>();
            so.FindProperty("_title").objectReferenceValue = title;
            B.SetObjectArray(so.FindProperty("_cells"), cells);
            B.SetObjectArray(so.FindProperty("_playerPlates"), plates);
            so.FindProperty("_nextCard").objectReferenceValue = next;
            so.FindProperty("_centerPosition").vector2Value = ScorecardCenter;
            so.FindProperty("_dockedPosition").vector2Value = ScorecardDocked;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return scorecard;
        }

        /// <summary>結果の右の欄と下の3枚の札。スコアカードより手前に置く（スコアカードの暗い幕の上に出す）</summary>
        private static GolfResultPanel CreateResultPanel(Transform canvas)
        {
            var root = B.CreateStretchGroup(canvas, "Result");
            var panel = B.CreatePanel(root.transform, "Panel", ResultPanelPosition, ResultPanelSize);
            B.SetSlideIn(panel, PanelSlideFrom, 0.1f);
            var score = B.CreateLabel(panel.transform, "Score", string.Empty, ResultScoreFontSize, ResultScorePosition, ResultScoreSize);
            score.fontStyle = FontStyle.Bold;
            var best = B.CreateLabel(panel.transform, "Best", string.Empty, ResultBestFontSize, ResultBestPosition, ResultBestSize);
            best.fontStyle = FontStyle.Bold;
            var labels = B.CreateLabel(panel.transform, "RewardLabels", string.Empty, RewardFontSize, RewardLabelsPosition, RewardLabelsSize);
            labels.alignment = TextAnchor.UpperLeft;
            var values = B.CreateLabel(panel.transform, "RewardValues", string.Empty, RewardFontSize, RewardValuesPosition, RewardValuesSize);
            values.alignment = TextAnchor.UpperRight;
            var coins = B.CreateLabel(panel.transform, "CoinTotal", string.Empty, CoinTotalFontSize, CoinTotalPosition, CoinTotalSize);
            coins.fontStyle = FontStyle.Bold;
            coins.color = HudGold;

            var defs = new[] { ("もう一度", B.StartColor), ("コースを選ぶ", B.OnlineColor), ("ロビーへ", B.CancelColor) };
            var cards = new Object[defs.Length];
            for (var i = 0; i < defs.Length; i++)
            {
                var (label, color) = defs[i];
                var position = new Vector2((i - (defs.Length - 1) * 0.5f) * ResultCardSpacing, ResultCardY);
                cards[i] = B.CreateMenuCard(root.transform, $"Card{i + 1}", label, color, Center, position, ResultCardSize, ResultCardFontSize, CardSlideFrom, centered: true);
            }

            var result = root.AddComponent<GolfResultPanel>();
            var so = new SerializedObject(result);
            so.FindProperty("_score").objectReferenceValue = score;
            so.FindProperty("_best").objectReferenceValue = best;
            so.FindProperty("_rewardLabels").objectReferenceValue = labels;
            so.FindProperty("_rewardValues").objectReferenceValue = values;
            so.FindProperty("_coinTotal").objectReferenceValue = coins;
            B.SetObjectArray(so.FindProperty("_cards"), cards);
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return result;
        }

        /// <summary>
        /// ステージ中のメニュー（本編の StageMenu の見た目）。札は つづける・ホールをやり直す・設定・ミニゲームへ
        /// （遊び方はゴルフのページができるフェーズ 7 で足す。いまは札を出さない）。右の編成の欄は作らない
        /// </summary>
        private static StageMenu CreateMenu(Transform canvas, Button openButton)
        {
            var labels = new[] { "つづける", "ホールをやり直す", "遊び方", "設定", "ミニゲームへ" };
            var menu = B.CreateStageMenu(canvas, openButton, null, labels, withParty: false);
            var so = new SerializedObject(menu);
            so.FindProperty("_restartLabel").stringValue = "ホールをやり直す";
            so.FindProperty("_restartSoloMessage").stringValue = "このホールをはじめからやり直します。\nこのホールで使った特殊ショットは戻ります";
            so.FindProperty("_exitLabel").stringValue = "ミニゲームへ";
            so.FindProperty("_exitSoloMessage").stringValue = "コースをやめてミニゲームの画面に戻ります。\nいまのスコアは残りません";
            so.ApplyModifiedProperties();
            return menu;
        }
    }
}
