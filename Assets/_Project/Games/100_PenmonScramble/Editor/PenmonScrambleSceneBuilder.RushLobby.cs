using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 「みんなで遊ぶ」の待ちの窓の右に置く、ボスラッシュの選択（RushLobby）: ステージのカード（◀ ▶・3戦の顔・鍵）、
    /// 難易度の3つの札、報酬のコイン。見出しは遊ぶものの札（RoomModes.cs）が受け持つ
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly string[] RushRoundLabels = { "1戦目 巨大ペンモン", "2戦目 大ボス", "3戦目 本気" };

        // 待ちの窓の中の置き場所（窓のまん中から測る）
        private static readonly Vector2 RushCardPosition = new Vector2(270f, 60f);
        private static readonly Vector2 RushCardSize = new Vector2(1000f, 600f);

        // カードの中（カードのまん中から測る）
        private const float RushStageY = 170f;
        private static readonly Vector2 RushStagePlateSize = new Vector2(680f, 110f);
        private const float RushArrowX = 420f;
        private static readonly Vector2 RushArrowSize = new Vector2(90f, 110f);
        private static readonly Vector2 RushStageNoPosition = new Vector2(0f, 30f);
        private static readonly Vector2 RushStageNamePosition = new Vector2(0f, -18f);
        private static readonly Vector2 RushStageTextSize = new Vector2(640f, 56f);
        private const int RushStageNoFontSize = 28;
        private const int RushStageNameFontSize = 50;

        private const float RushRoundY = 25f;
        private const float RushColumnSpacing = 300f;
        private static readonly Vector2 RushRoundSize = new Vector2(280f, 170f);
        private const float RushFaceY = 14f;
        private const float RushGiantFaceSize = 116f;
        private const float RushGiantFaceX = 52f;
        private const float RushBossFaceSize = 136f;
        private static readonly Vector2 RushRoundLabelPosition = new Vector2(0f, -62f);
        private static readonly Vector2 RushRoundLabelSize = new Vector2(266f, 40f);
        private const int RushRoundLabelFontSize = 26;
        private static readonly Color RushSeriousColor = new Color(1f, 0.45f, 0.4f);

        private static readonly Vector2 RushLockSize = new Vector2(900f, 180f);
        private static readonly Color RushLockColor = new Color(0.02f, 0.03f, 0.10f, 0.82f);
        private static readonly Vector2 RushLockIconPosition = new Vector2(-340f, 0f);
        private const float RushLockIconSize = 90f;
        private static readonly Vector2 RushLockTextPosition = new Vector2(60f, 0f);
        private static readonly Vector2 RushLockTextSize = new Vector2(720f, 150f);
        private const int RushLockFontSize = 36;
        private static readonly Color RushLockIconColor = new Color(1f, 0.85f, 0.3f);

        private const float RushDifficultyY = -125f;
        private static readonly Vector2 RushDifficultySize = new Vector2(260f, 92f);
        private static readonly Vector2 RushDifficultyMarkPadding = new Vector2(18f, 18f);
        private const int RushDifficultyFontSize = 40;
        private static readonly Vector2 RushDifficultyLockPosition = new Vector2(104f, 26f);
        private const float RushDifficultyLockSize = 48f;

        private static readonly Vector2 RushRewardIconPosition = new Vector2(-330f, -218f);
        private const float RushRewardIconSize = 56f;
        private static readonly Vector2 RushRewardPosition = new Vector2(110f, -218f);
        private static readonly Vector2 RushRewardSize = new Vector2(820f, 56f);
        private const int RushRewardFontSize = 36;
        private static readonly Vector2 RushNotePosition = new Vector2(0f, -276f);
        private static readonly Vector2 RushNoteSize = new Vector2(900f, 44f);
        private const int RushNoteFontSize = 28;

        /// <summary>待ちの窓の板（panel）の中にカードを作り、タイトルの土台に RushLobby を付けてつなぐ</summary>
        private static RushLobby CreateRushLobby(ModeSelectPanel title, Transform waitingPanel)
        {
            var card = UIDialogBuilder.CreateUIObject("Rush", waitingPanel);
            PlaceCenter(card.GetComponent<RectTransform>(), RushCardPosition, RushCardSize);

            var lobby = title.gameObject.AddComponent<RushLobby>();
            var so = new SerializedObject(lobby);
            so.FindProperty("_modeSelect").objectReferenceValue = title;
            so.FindProperty("_card").objectReferenceValue = card;
            CreateRushStageRow(card.transform, so);
            CreateRushRounds(card.transform, so);
            CreateRushDifficulties(card.transform, so);
            CreateRushReward(card.transform, so);
            so.ApplyModifiedProperties();
            card.SetActive(false);
            return lobby;
        }

        /// <summary>「ステージ 1」と名前の板を、◀ ▶ ではさむ（◀ ▶ はホストだけ）</summary>
        private static void CreateRushStageRow(Transform card, SerializedObject so)
        {
            var plate = CreatePanel(card, "Stage", new Vector2(0f, RushStageY), RushStagePlateSize).transform;
            var no = CreateLabel(plate, "No", string.Empty, RushStageNoFontSize, RushStageNoPosition, RushStageTextSize);
            no.color = StageNoColor;
            var name = CreateLabel(plate, "Name", string.Empty, RushStageNameFontSize, RushStageNamePosition, RushStageTextSize);
            name.fontStyle = FontStyle.Bold;
            var prev = CreateTitleButton(card, "PrevStage", "◀", new Vector2(-RushArrowX, RushStageY), RushArrowSize, ArrowColor, MenuFontSize);
            var next = CreateTitleButton(card, "NextStage", "▶", new Vector2(RushArrowX, RushStageY), RushArrowSize, ArrowColor, MenuFontSize);
            so.FindProperty("_stageNoText").objectReferenceValue = no;
            so.FindProperty("_stageNameText").objectReferenceValue = name;
            so.FindProperty("_prevButton").objectReferenceValue = prev;
            so.FindProperty("_nextButton").objectReferenceValue = next;
        }

        /// <summary>3戦の顔を並べる（1戦目は巨大ペンモン2体を少し重ねる）。ステージが閉じていれば、上に鍵の板を重ねる</summary>
        private static void CreateRushRounds(Transform card, SerializedObject so)
        {
            var faces = new Object[4];
            for (var round = 0; round < RushRoundLabels.Length; round++)
            {
                var slot = CreatePanel(card, $"Round{round + 1}", new Vector2((round - 1) * RushColumnSpacing, RushRoundY), RushRoundSize).transform;
                if (round == 0)
                {
                    faces[0] = CreateRushFace(slot, "GiantA", new Vector2(-RushGiantFaceX, RushFaceY), RushGiantFaceSize);
                    faces[1] = CreateRushFace(slot, "GiantB", new Vector2(RushGiantFaceX, RushFaceY), RushGiantFaceSize);
                }
                else
                {
                    faces[round + 1] = CreateRushFace(slot, "Boss", new Vector2(0f, RushFaceY), RushBossFaceSize);
                }

                var label = CreateLabel(slot, "Label", RushRoundLabels[round], RushRoundLabelFontSize, RushRoundLabelPosition, RushRoundLabelSize);
                label.fontStyle = FontStyle.Bold;
                label.color = round == RushRoundLabels.Length - 1 ? RushSeriousColor : Color.white;
            }

            SetObjectArray(so.FindProperty("_faces"), faces);

            var lockPlate = UIDialogBuilder.CreateUIObject("Lock", card);
            PlaceCenter(lockPlate.GetComponent<RectTransform>(), new Vector2(0f, RushRoundY), RushLockSize);
            AddSliced(lockPlate, RoundRectPath, RushLockColor, CornerScale(PanelCornerRadius)).raycastTarget = false;
            CreateHudImage(lockPlate.transform, "Icon", LockIconPath, RushLockIconColor, Middle, RushLockIconPosition, Vector2.one * RushLockIconSize).preserveAspect = true;
            var lockText = CreateLabel(lockPlate.transform, "Text", string.Empty, RushLockFontSize, RushLockTextPosition, RushLockTextSize);
            lockText.fontStyle = FontStyle.Bold;
            lockPlate.SetActive(false);
            so.FindProperty("_lock").objectReferenceValue = lockPlate;
            so.FindProperty("_lockText").objectReferenceValue = lockText;
        }

        private static Image CreateRushFace(Transform slot, string name, Vector2 position, float size)
        {
            var face = CreateHudImage(slot, name, null, Color.white, Middle, position, Vector2.one * size);
            face.preserveAspect = true;
            return face;
        }

        /// <summary>
        /// 難易度の3つの札（ふつう 緑 / むずかしい だいだい / げきむず 赤）。選んでいる札は後ろに白い縁を光らせ、入れ物ごと大きく濃くする。
        /// 薄くするのは入れ物（RushLobby）が受け持つので、ボタン自身の「押せないときは薄く」は切る（ゲストは押せないが、選ばれた札は濃く見せたい）
        /// </summary>
        private static void CreateRushDifficulties(Transform card, SerializedObject so)
        {
            var slots = new Object[Balance.DifficultyCount];
            var marks = new Object[Balance.DifficultyCount];
            var buttons = new Object[Balance.DifficultyCount];
            var locks = new Object[Balance.DifficultyCount];
            for (var i = 0; i < Balance.DifficultyCount; i++)
            {
                var difficulty = (Difficulty)i;
                var slot = CreateHudRect(card, $"Difficulty{i}", Middle, new Vector2((i - 1) * RushColumnSpacing, RushDifficultyY), RushDifficultySize);
                slots[i] = slot.gameObject.AddComponent<CanvasGroup>();
                var mark = UIDialogBuilder.CreateUIObject("Selected", slot);
                PlaceCenter(mark.GetComponent<RectTransform>(), Vector2.zero, RushDifficultySize + RushDifficultyMarkPadding);
                AddSliced(mark, RoundRectPath, Color.white, CornerScale(ButtonCornerRadius + RushDifficultyMarkPadding.y * 0.5f)).raycastTarget = false;
                marks[i] = mark;

                var button = CreateTitleButton(slot, "Button", DifficultyNames.NameOf(difficulty), Vector2.zero, RushDifficultySize, DifficultyNames.ColorOf(difficulty), RushDifficultyFontSize);
                var motion = new SerializedObject(button.GetComponent<UiButtonMotion>());
                motion.FindProperty("_disabledAlpha").floatValue = 1f;
                motion.ApplyModifiedProperties();
                buttons[i] = button;

                var lockIcon = CreateHudImage(slot, "Lock", LockIconPath, RushLockIconColor, Middle, RushDifficultyLockPosition, Vector2.one * RushDifficultyLockSize);
                lockIcon.preserveAspect = true;
                locks[i] = lockIcon.gameObject;
            }

            SetObjectArray(so.FindProperty("_difficultySlots"), slots);
            SetObjectArray(so.FindProperty("_difficultyMarks"), marks);
            SetObjectArray(so.FindProperty("_difficultyButtons"), buttons);
            SetObjectArray(so.FindProperty("_difficultyLocks"), locks);
        }

        /// <summary>選んだ難易度の報酬のコインと、ホスト・ゲストへの一言</summary>
        private static void CreateRushReward(Transform card, SerializedObject so)
        {
            CreateHudImage(card, "CoinIcon", CoinIconPath, Color.white, Middle, RushRewardIconPosition, Vector2.one * RushRewardIconSize).preserveAspect = true;
            var reward = CreateLabel(card, "Reward", string.Empty, RushRewardFontSize, RushRewardPosition, RushRewardSize);
            reward.fontStyle = FontStyle.Bold;
            reward.alignment = TextAnchor.MiddleLeft;
            reward.color = CoinTextColor;
            var note = CreateLabel(card, "Note", string.Empty, RushNoteFontSize, RushNotePosition, RushNoteSize);
            note.color = PlateHintColor;
            so.FindProperty("_rewardText").objectReferenceValue = reward;
            so.FindProperty("_noteText").objectReferenceValue = note;
        }
    }
}
