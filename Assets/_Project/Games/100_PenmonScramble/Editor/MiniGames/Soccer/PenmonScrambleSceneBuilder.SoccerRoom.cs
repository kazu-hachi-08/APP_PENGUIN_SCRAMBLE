using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 「みんなで遊ぶ」の待ちの窓に出す、サッカーの選択（SoccerRoomPanel）: スタジアムの板（◀ ▶・番号・名前）、
    /// 対戦 / 協力の札、席（P1〜P4）の赤・青の札、CPU の強さと仕掛けの一言（閉じていれば鍵の板）、報酬、ホスト・ゲストへの一言。
    /// 置き場所と板の形はボスラッシュ・ゴルフの板と同じ
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly string[] SoccerRoomModeLabels = { "対戦", "協力" };
        private static readonly Color[] SoccerRoomModeColors = { new Color(0.92f, 0.36f, 0.32f), new Color(0.30f, 0.62f, 0.92f) };
        private const float SoccerRoomModeY = 62f;
        private const float SoccerRoomModeSpacing = 300f;
        private static readonly Vector2 SoccerRoomModeSize = new Vector2(260f, 84f);
        private static readonly Vector2 SoccerRoomModeMarkPadding = new Vector2(16f, 16f);
        private const int SoccerRoomModeFontSize = 40;

        private const float SoccerRoomSeatY = -48f;
        private const float SoccerRoomSeatSpacing = 230f;
        private static readonly Vector2 SoccerRoomSeatSize = new Vector2(206f, 80f);
        private const int SoccerRoomSeatFontSize = 34;

        private static readonly Vector2 SoccerRoomLayoutPosition = new Vector2(0f, -135f);
        private static readonly Vector2 SoccerRoomLayoutSize = new Vector2(900f, 56f);
        private const int SoccerRoomLayoutFontSize = 32;
        private static readonly Vector2 SoccerRoomLockPosition = new Vector2(0f, -135f);
        private static readonly Vector2 SoccerRoomLockSize = new Vector2(900f, 80f);
        private static readonly Vector2 SoccerRoomLockIconPosition = new Vector2(-380f, 0f);
        private const float SoccerRoomLockIconSize = 60f;
        private static readonly Vector2 SoccerRoomLockTextSize = new Vector2(760f, 70f);
        private const int SoccerRoomLockFontSize = 32;
        private static readonly Vector2 SoccerRoomRewardIconPosition = new Vector2(-420f, -212f);
        private static readonly Vector2 SoccerRoomRewardPosition = new Vector2(40f, -212f);
        private static readonly Vector2 SoccerRoomRewardSize = new Vector2(860f, 84f);
        private const int SoccerRoomRewardFontSize = 28;

        private static SoccerRoomPanel CreateSoccerRoomPanel(ModeSelectPanel title, OnlineSession session, Transform waitingPanel)
        {
            var card = UIDialogBuilder.CreateUIObject("Soccer", waitingPanel);
            PlaceCenter(card.GetComponent<RectTransform>(), RushCardPosition, RushCardSize);

            var panel = title.gameObject.AddComponent<SoccerRoomPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_modeSelect").objectReferenceValue = title;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_card").objectReferenceValue = card;
            CreateSoccerRoomStadiumRow(card.transform, so);
            CreateSoccerRoomModes(card.transform, so);
            CreateSoccerRoomSeats(card.transform, so);
            CreateSoccerRoomLayout(card.transform, so);
            CreateSoccerRoomReward(card.transform, so);
            so.ApplyModifiedProperties();
            card.SetActive(false);
            return panel;
        }

        /// <summary>「スタジアム 1」と名前の板を、◀ ▶ ではさむ（◀ ▶ はホストだけ。ゴルフのコースの板と同じ形）</summary>
        private static void CreateSoccerRoomStadiumRow(Transform card, SerializedObject so)
        {
            var plate = CreatePanel(card, "Stadium", new Vector2(0f, RushStageY), RushStagePlateSize).transform;
            var no = CreateLabel(plate, "No", string.Empty, RushStageNoFontSize, RushStageNoPosition, RushStageTextSize);
            no.color = StageNoColor;
            var name = CreateLabel(plate, "Name", string.Empty, RushStageNameFontSize, RushStageNamePosition, RushStageTextSize);
            name.fontStyle = FontStyle.Bold;
            var prev = CreateTitleButton(card, "PrevStadium", "◀", new Vector2(-RushArrowX, RushStageY), RushArrowSize, ArrowColor, MenuFontSize);
            var next = CreateTitleButton(card, "NextStadium", "▶", new Vector2(RushArrowX, RushStageY), RushArrowSize, ArrowColor, MenuFontSize);
            so.FindProperty("_stadiumNoText").objectReferenceValue = no;
            so.FindProperty("_stadiumNameText").objectReferenceValue = name;
            so.FindProperty("_prevButton").objectReferenceValue = prev;
            so.FindProperty("_nextButton").objectReferenceValue = next;
        }

        /// <summary>
        /// 対戦 / 協力の札（並びは SoccerOnlineMode の番号）。選んでいる札は後ろに白い縁を光らせて大きく（ボスラッシュの難易度の札と同じ見た目）。
        /// ゲストは押せないが、選ばれた札は濃く見せたいので、ボタンの「押せないときは薄く」は切る
        /// </summary>
        private static void CreateSoccerRoomModes(Transform card, SerializedObject so)
        {
            var count = SoccerRoomModeLabels.Length;
            var slots = new Object[count];
            var marks = new Object[count];
            var buttons = new Object[count];
            for (var i = 0; i < count; i++)
            {
                var position = new Vector2((i - (count - 1) * 0.5f) * SoccerRoomModeSpacing, SoccerRoomModeY);
                var slot = CreateHudRect(card, $"Mode{i}", Middle, position, SoccerRoomModeSize);
                slots[i] = slot.gameObject.AddComponent<CanvasGroup>();
                var mark = UIDialogBuilder.CreateUIObject("Selected", slot);
                PlaceCenter(mark.GetComponent<RectTransform>(), Vector2.zero, SoccerRoomModeSize + SoccerRoomModeMarkPadding);
                AddSliced(mark, RoundRectPath, Color.white, CornerScale(ButtonCornerRadius + SoccerRoomModeMarkPadding.y * 0.5f)).raycastTarget = false;
                marks[i] = mark;
                var button = CreateTitleButton(slot, "Button", SoccerRoomModeLabels[i], Vector2.zero, SoccerRoomModeSize, SoccerRoomModeColors[i], SoccerRoomModeFontSize);
                KeepOpaqueWhenDisabled(button);
                buttons[i] = button;
            }

            SetObjectArray(so.FindProperty("_modeSlots"), slots);
            SetObjectArray(so.FindProperty("_modeMarks"), marks);
            SetObjectArray(so.FindProperty("_modeButtons"), buttons);
        }

        /// <summary>席（P1〜P4）の札。色と字（「P2  青」）は SoccerRoomPanel がチームに合わせて入れる</summary>
        private static void CreateSoccerRoomSeats(Transform card, SerializedObject so)
        {
            var count = SoccerRoomChoice.MaxSeats;
            var buttons = new Object[count];
            var texts = new Object[count];
            for (var i = 0; i < count; i++)
            {
                var position = new Vector2((i - (count - 1) * 0.5f) * SoccerRoomSeatSpacing, SoccerRoomSeatY);
                var button = CreateTitleButton(card, $"Seat{i}", $"P{i + 1}", position, SoccerRoomSeatSize, SoccerColors.Team(SoccerTeamPlan.DefaultTeam(i)), SoccerRoomSeatFontSize);
                KeepOpaqueWhenDisabled(button);
                buttons[i] = button;
                texts[i] = button.GetComponentInChildren<UnityEngine.UI.Text>();
            }

            SetObjectArray(so.FindProperty("_seatButtons"), buttons);
            SetObjectArray(so.FindProperty("_seatTexts"), texts);
        }

        /// <summary>CPU の強さと仕掛けの一言。スタジアムが閉じていれば、上に鍵の板を重ねる</summary>
        private static void CreateSoccerRoomLayout(Transform card, SerializedObject so)
        {
            var layout = CreateLabel(card, "Layout", string.Empty, SoccerRoomLayoutFontSize, SoccerRoomLayoutPosition, SoccerRoomLayoutSize);
            layout.fontStyle = FontStyle.Bold;
            so.FindProperty("_layoutText").objectReferenceValue = layout;

            var lockPlate = UIDialogBuilder.CreateUIObject("Lock", card);
            PlaceCenter(lockPlate.GetComponent<RectTransform>(), SoccerRoomLockPosition, SoccerRoomLockSize);
            AddSliced(lockPlate, RoundRectPath, RushLockColor, CornerScale(PanelCornerRadius)).raycastTarget = false;
            CreateHudImage(lockPlate.transform, "Icon", LockIconPath, RushLockIconColor, Middle, SoccerRoomLockIconPosition, Vector2.one * SoccerRoomLockIconSize).preserveAspect = true;
            var lockText = CreateLabel(lockPlate.transform, "Text", string.Empty, SoccerRoomLockFontSize, RushLockTextPosition, SoccerRoomLockTextSize);
            lockText.fontStyle = FontStyle.Bold;
            lockPlate.SetActive(false);
            so.FindProperty("_lock").objectReferenceValue = lockPlate;
            so.FindProperty("_lockText").objectReferenceValue = lockText;
        }

        /// <summary>報酬（出た分・勝ち・点・初めて勝ったボーナス）と、ホスト・ゲストへの一言</summary>
        private static void CreateSoccerRoomReward(Transform card, SerializedObject so)
        {
            CreateHudImage(card, "CoinIcon", CoinIconPath, Color.white, Middle, SoccerRoomRewardIconPosition, Vector2.one * RushRewardIconSize).preserveAspect = true;
            var reward = CreateLabel(card, "Reward", string.Empty, SoccerRoomRewardFontSize, SoccerRoomRewardPosition, SoccerRoomRewardSize);
            reward.alignment = TextAnchor.MiddleLeft;
            reward.color = CoinTextColor;
            var note = CreateLabel(card, "Note", string.Empty, RushNoteFontSize, RushNotePosition, RushNoteSize);
            note.color = PlateHintColor;
            so.FindProperty("_rewardText").objectReferenceValue = reward;
            so.FindProperty("_noteText").objectReferenceValue = note;
        }

        /// <summary>ゲストは押せないが、選ばれた札・チームの色は濃いまま見せる</summary>
        private static void KeepOpaqueWhenDisabled(UnityEngine.UI.Button button)
        {
            var motion = new SerializedObject(button.GetComponent<UiButtonMotion>());
            motion.FindProperty("_disabledAlpha").floatValue = 1f;
            motion.ApplyModifiedProperties();
        }
    }
}
