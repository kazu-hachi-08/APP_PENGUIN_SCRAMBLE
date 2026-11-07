using PenmonScramble.Game;
using PenmonScramble.Golf.Game;
using PenmonScramble.MiniGames.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using B = PenmonScramble.Editor.PenmonScrambleSceneBuilder;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ホールの間のスコアカード（GolfScorecard）と、結果の右の欄（ミニゲームで共通の MiniGameResultPanel）。
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
        private static readonly Vector2 ScorecardNoteSize = new Vector2(1200f, 80f);
        private const int ScorecardNoteFontSize = 44;
        private static readonly Color NoteColor = new Color(0.75f, 0.85f, 1f);
        private static readonly Vector2 PanelSlideFrom = new Vector2(0f, -120f);
        private static readonly Vector2 CardSlideFrom = new Vector2(0f, -200f);

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
            // オンラインのゲストは、つぎの札の代わりに「ホストが進めるのを待っています」
            var note = B.CreateLabel(root.transform, "Note", string.Empty, ScorecardNoteFontSize, NextCardPosition, ScorecardNoteSize);
            note.fontStyle = FontStyle.Bold;
            note.color = NoteColor;
            note.gameObject.SetActive(false);

            var scorecard = root.AddComponent<GolfScorecard>();
            var so = new SerializedObject(scorecard);
            so.FindProperty("_panel").objectReferenceValue = panel.GetComponent<RectTransform>();
            so.FindProperty("_title").objectReferenceValue = title;
            B.SetObjectArray(so.FindProperty("_cells"), cells);
            B.SetObjectArray(so.FindProperty("_playerPlates"), plates);
            so.FindProperty("_nextCard").objectReferenceValue = next;
            so.FindProperty("_note").objectReferenceValue = note;
            so.FindProperty("_centerPosition").vector2Value = ScorecardCenter;
            so.FindProperty("_dockedPosition").vector2Value = ScorecardDocked;
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return scorecard;
        }

        /// <summary>結果の右の欄と下の3枚の札（ミニゲームで共通の部品）。スコアカードより手前に置く（スコアカードの暗い幕の上に出す）</summary>
        private static MiniGameResultPanel CreateResultPanel(Transform canvas)
        {
            return B.CreateMiniGameResultPanel(canvas, "コースを選ぶ", "ホストがコースを選ぶと、みんなで待ちの窓へ戻ります");
        }

        /// <summary>
        /// ステージ中のメニュー（本編の StageMenu の見た目）。札は つづける・ホールをやり直す・遊び方（ゴルフのページ）・設定・ミニゲームへ。
        /// 右の編成の欄は作らない
        /// </summary>
        private static StageMenu CreateMenu(Transform canvas, Button openButton, HowToPlayScreen howToPlay)
        {
            var labels = new[] { "つづける", "ホールをやり直す", "遊び方", "設定", "ミニゲームへ" };
            var menu = B.CreateStageMenu(canvas, openButton, howToPlay, labels, withParty: false);
            var so = new SerializedObject(menu);
            so.FindProperty("_restartLabel").stringValue = "ホールをやり直す";
            so.FindProperty("_restartSoloMessage").stringValue = "このホールをはじめからやり直します。\nこのホールで使った特殊ショットは戻ります";
            so.FindProperty("_exitLabel").stringValue = "ミニゲームへ";
            so.FindProperty("_exitSoloMessage").stringValue = "コースをやめてミニゲームの画面に戻ります。\nいまのスコアは残りません";
            so.FindProperty("_exitHostMessage").stringValue = "部屋を閉じてミニゲームの画面に戻ります。\nほかの人も全員ロビーに戻ります";
            so.FindProperty("_exitGuestMessage").stringValue = "部屋から抜けてミニゲームの画面に戻ります。\nいまのスコアは残りません";
            so.FindProperty("_coopNote").stringValue = "みんなで遊んでいる間は 止まりません";
            so.ApplyModifiedProperties();
            return menu;
        }
    }
}
