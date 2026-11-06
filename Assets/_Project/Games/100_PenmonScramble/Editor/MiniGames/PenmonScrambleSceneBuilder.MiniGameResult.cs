using PenmonScramble.MiniGames.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ミニゲームの結果の欄（MiniGameResultPanel）: 右の板（結果の点・ベストの一言・コインの内訳・合計）と、下の3枚の札
    /// （もう一度・コース（スタジアム）を選ぶ・ロビーへ）。左は各ミニゲームが使う（ゴルフはスコアカード、サッカーは点の板）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Vector2 MiniResultCenter = new Vector2(0.5f, 0.5f);
        private static readonly Color MiniResultGold = new Color(1f, 0.82f, 0.25f);
        private static readonly Color MiniResultNoteColor = new Color(0.75f, 0.85f, 1f);
        private static readonly Vector2 MiniResultPanelSlideFrom = new Vector2(0f, -120f);
        private static readonly Vector2 MiniResultCardSlideFrom = new Vector2(0f, -200f);
        private static readonly Vector2 MiniResultPanelPosition = new Vector2(500f, 60f);
        private static readonly Vector2 MiniResultPanelSize = new Vector2(720f, 600f);
        private static readonly Vector2 MiniResultScorePosition = new Vector2(0f, 210f);
        private static readonly Vector2 MiniResultScoreSize = new Vector2(660f, 110f);
        private const int MiniResultScoreFontSize = 76;
        // ベストの一言は2行まで（サッカーは得失点差の下にベスト）。1行なら真ん中に出る
        private static readonly Vector2 MiniResultBestPosition = new Vector2(0f, 115f);
        private static readonly Vector2 MiniResultBestSize = new Vector2(660f, 100f);
        private const int MiniResultBestFontSize = 40;
        private static readonly Vector2 MiniResultLabelsPosition = new Vector2(-90f, -50f);
        private static readonly Vector2 MiniResultLabelsSize = new Vector2(440f, 230f);
        private static readonly Vector2 MiniResultValuesPosition = new Vector2(220f, -50f);
        private static readonly Vector2 MiniResultValuesSize = new Vector2(180f, 230f);
        private const int MiniResultRewardFontSize = 34;
        private static readonly Vector2 MiniResultCoinPosition = new Vector2(0f, -230f);
        private static readonly Vector2 MiniResultCoinSize = new Vector2(660f, 80f);
        private const int MiniResultCoinFontSize = 52;
        private const float MiniResultCardY = -430f;
        private const float MiniResultCardSpacing = 520f;
        private static readonly Vector2 MiniResultCardSize = new Vector2(460f, 116f);
        private const int MiniResultCardFontSize = 50;
        private const int MiniResultNoteFontSize = 38;

        /// <param name="coursesLabel">真ん中の札の字（「コースを選ぶ」「スタジアムを選ぶ」）</param>
        /// <param name="guestNote">オンラインのゲストに、札2枚の代わりに出す一言</param>
        internal static MiniGameResultPanel CreateMiniGameResultPanel(Transform canvas, string coursesLabel, string guestNote)
        {
            var root = CreateStretchGroup(canvas, "Result");
            var panel = CreatePanel(root.transform, "Panel", MiniResultPanelPosition, MiniResultPanelSize);
            SetSlideIn(panel, MiniResultPanelSlideFrom, 0.1f);
            var score = CreateLabel(panel.transform, "Score", string.Empty, MiniResultScoreFontSize, MiniResultScorePosition, MiniResultScoreSize);
            score.fontStyle = FontStyle.Bold;
            var best = CreateLabel(panel.transform, "Best", string.Empty, MiniResultBestFontSize, MiniResultBestPosition, MiniResultBestSize);
            best.fontStyle = FontStyle.Bold;
            var labels = CreateLabel(panel.transform, "RewardLabels", string.Empty, MiniResultRewardFontSize, MiniResultLabelsPosition, MiniResultLabelsSize);
            labels.alignment = TextAnchor.UpperLeft;
            var values = CreateLabel(panel.transform, "RewardValues", string.Empty, MiniResultRewardFontSize, MiniResultValuesPosition, MiniResultValuesSize);
            values.alignment = TextAnchor.UpperRight;
            var coins = CreateLabel(panel.transform, "CoinTotal", string.Empty, MiniResultCoinFontSize, MiniResultCoinPosition, MiniResultCoinSize);
            coins.fontStyle = FontStyle.Bold;
            coins.color = MiniResultGold;

            var defs = new[] { ("もう一度", StartColor), (coursesLabel, OnlineColor), ("ロビーへ", CancelColor) };
            var cards = new Object[defs.Length];
            for (var i = 0; i < defs.Length; i++)
            {
                var (label, color) = defs[i];
                var position = new Vector2((i - (defs.Length - 1) * 0.5f) * MiniResultCardSpacing, MiniResultCardY);
                cards[i] = CreateMenuCard(root.transform, $"Card{i + 1}", label, color, MiniResultCenter, position, MiniResultCardSize, MiniResultCardFontSize, MiniResultCardSlideFrom, centered: true);
            }

            // オンラインのゲストは「もう一度」「コースを選ぶ」の代わりに一言（札2枚分の幅に出す）
            var note = CreateLabel(root.transform, "Note", guestNote, MiniResultNoteFontSize,
                new Vector2(-MiniResultCardSpacing * 0.5f, MiniResultCardY), new Vector2(MiniResultCardSpacing * 2f - 80f, MiniResultCardSize.y));
            note.fontStyle = FontStyle.Bold;
            note.color = MiniResultNoteColor;
            note.gameObject.SetActive(false);

            var result = root.AddComponent<MiniGameResultPanel>();
            var so = new SerializedObject(result);
            so.FindProperty("_note").objectReferenceValue = note;
            so.FindProperty("_score").objectReferenceValue = score;
            so.FindProperty("_best").objectReferenceValue = best;
            so.FindProperty("_rewardLabels").objectReferenceValue = labels;
            so.FindProperty("_rewardValues").objectReferenceValue = values;
            so.FindProperty("_coinTotal").objectReferenceValue = coins;
            SetObjectArray(so.FindProperty("_cards"), cards);
            so.ApplyModifiedProperties();
            root.SetActive(false);
            return result;
        }
    }
}
