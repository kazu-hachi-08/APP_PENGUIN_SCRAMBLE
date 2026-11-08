using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 「みんなで遊ぶ」の待ちの窓に出す、チームで戦うミニゲーム（サッカー・野球）の選択（MiniGameTeamRoomPanel の子）: コースの板（◀ ▶・番号・名前）、
    /// 対戦 / 協力の札、席（P1〜P4）の赤・青の札、CPU の強さと仕掛けの一言（閉じていれば鍵の板）、報酬、ホスト・ゲストへの一言。
    /// 置き場所と板の形はボスラッシュ・ゴルフの板と同じ
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly string[] TeamRoomModeLabels = { "対戦", "協力" };
        private static readonly Color[] TeamRoomModeColors = { new Color(0.92f, 0.36f, 0.32f), new Color(0.30f, 0.62f, 0.92f) };
        private const float TeamRoomModeY = 62f;
        private const float TeamRoomModeSpacing = 300f;
        private static readonly Vector2 TeamRoomModeSize = new Vector2(260f, 84f);
        private static readonly Vector2 TeamRoomModeMarkPadding = new Vector2(16f, 16f);
        private const int TeamRoomModeFontSize = 40;

        private const float TeamRoomSeatY = -40f;
        private const float TeamRoomSeatSpacing = 230f;
        private static readonly Vector2 TeamRoomSeatSize = new Vector2(206f, 80f);
        private const int TeamRoomSeatFontSize = 34;

        // 強さと仕掛けの一言は2行（MiniGameGuides.TwoLines）。鍵の板はその2行の上に重ねる
        private static readonly Vector2 TeamRoomLayoutPosition = new Vector2(0f, -126f);
        private static readonly Vector2 TeamRoomLayoutSize = new Vector2(900f, 92f);
        private const int TeamRoomLayoutFontSize = 30;
        private static readonly Vector2 TeamRoomLockPosition = TeamRoomLayoutPosition;
        private static readonly Vector2 TeamRoomLockSize = TeamRoomLayoutSize;
        private static readonly Vector2 TeamRoomLockIconPosition = new Vector2(-380f, 0f);
        private const float TeamRoomLockIconSize = 60f;
        private static readonly Vector2 TeamRoomLockTextSize = new Vector2(760f, 70f);
        private const int TeamRoomLockFontSize = 32;
        private static readonly Vector2 TeamRoomRewardIconPosition = new Vector2(-420f, -216f);
        private static readonly Vector2 TeamRoomRewardPosition = new Vector2(40f, -216f);
        private static readonly Vector2 TeamRoomRewardSize = new Vector2(860f, 84f);
        private const int TeamRoomRewardFontSize = 28;

        /// <param name="name">板の名前（「Soccer」など。見分けるためだけ）</param>
        private static T CreateTeamRoomPanel<T>(ModeSelectPanel title, OnlineSession session, Transform waitingPanel, string name) where T : MiniGameTeamRoomPanel
        {
            var card = UIDialogBuilder.CreateUIObject(name, waitingPanel);
            PlaceCenter(card.GetComponent<RectTransform>(), RushCardPosition, RushCardSize);

            var panel = title.gameObject.AddComponent<T>();
            var so = new SerializedObject(panel);
            so.FindProperty("_modeSelect").objectReferenceValue = title;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_card").objectReferenceValue = card;
            CreateTeamRoomStadiumRow(card.transform, so);
            CreateTeamRoomModes(card.transform, so);
            CreateTeamRoomSeats(card.transform, so);
            CreateTeamRoomLayout(card.transform, so);
            CreateTeamRoomReward(card.transform, so);
            so.ApplyModifiedProperties();
            card.SetActive(false);
            return panel;
        }

        /// <summary>「スタジアム 1」などと名前の板を、◀ ▶ ではさむ（◀ ▶ はホストだけ。ゴルフのコースの板と同じ形）</summary>
        private static void CreateTeamRoomStadiumRow(Transform card, SerializedObject so)
        {
            var plate = CreatePanel(card, "Stadium", new Vector2(0f, RushStageY), RushStagePlateSize).transform;
            var no = CreateLabel(plate, "No", string.Empty, RushStageNoFontSize, RushStageNoPosition, RushStageNoSize);
            no.color = StageNoColor;
            var name = CreateLabel(plate, "Name", string.Empty, RushStageNameFontSize, RushStageNamePosition, RushStageNameSize);
            name.fontStyle = FontStyle.Bold;
            var prev = CreateTitleButton(card, "PrevStadium", "◀", new Vector2(-RushArrowX, RushStageY), RushArrowSize, ArrowColor, MenuFontSize);
            var next = CreateTitleButton(card, "NextStadium", "▶", new Vector2(RushArrowX, RushStageY), RushArrowSize, ArrowColor, MenuFontSize);
            so.FindProperty("_stadiumNoText").objectReferenceValue = no;
            so.FindProperty("_stadiumNameText").objectReferenceValue = name;
            so.FindProperty("_prevButton").objectReferenceValue = prev;
            so.FindProperty("_nextButton").objectReferenceValue = next;
        }

        /// <summary>
        /// 対戦 / 協力の札（並びは MiniGameOnlineMode の番号）。選んでいる札は後ろに白い縁を光らせて大きく（ボスラッシュの難易度の札と同じ見た目）。
        /// </summary>
        private static void CreateTeamRoomModes(Transform card, SerializedObject so)
        {
            var count = TeamRoomModeLabels.Length;
            var slots = new Object[count];
            var marks = new Object[count];
            var buttons = new Object[count];
            for (var i = 0; i < count; i++)
            {
                var position = new Vector2((i - (count - 1) * 0.5f) * TeamRoomModeSpacing, TeamRoomModeY);
                var slot = CreateHudRect(card, $"Mode{i}", Middle, position, TeamRoomModeSize);
                slots[i] = slot.gameObject.AddComponent<CanvasGroup>();
                marks[i] = CreateSelectedMark(slot, TeamRoomModeSize, TeamRoomModeMarkPadding);
                var button = CreateTitleButton(slot, "Button", TeamRoomModeLabels[i], Vector2.zero, TeamRoomModeSize, TeamRoomModeColors[i], TeamRoomModeFontSize);
                KeepOpaqueWhenDisabled(button);
                buttons[i] = button;
            }

            SetObjectArray(so.FindProperty("_modeSlots"), slots);
            SetObjectArray(so.FindProperty("_modeMarks"), marks);
            SetObjectArray(so.FindProperty("_modeButtons"), buttons);
        }

        /// <summary>席（P1〜P4）の札。色と字（「P2  青」）は MiniGameTeamRoomPanel がチームに合わせて入れる</summary>
        private static void CreateTeamRoomSeats(Transform card, SerializedObject so)
        {
            var count = MiniGameRoomChoice.MaxSeats;
            var buttons = new Object[count];
            var texts = new Object[count];
            for (var i = 0; i < count; i++)
            {
                var position = new Vector2((i - (count - 1) * 0.5f) * TeamRoomSeatSpacing, TeamRoomSeatY);
                var button = CreateTitleButton(card, $"Seat{i}", $"P{i + 1}", position, TeamRoomSeatSize, MiniGameTeamColors.Team(MiniGameTeams.DefaultTeam(i)), TeamRoomSeatFontSize);
                KeepOpaqueWhenDisabled(button);
                buttons[i] = button;
                texts[i] = button.GetComponentInChildren<UnityEngine.UI.Text>();
            }

            SetObjectArray(so.FindProperty("_seatButtons"), buttons);
            SetObjectArray(so.FindProperty("_seatTexts"), texts);
        }

        /// <summary>CPU の強さと仕掛けの一言。コースが閉じていれば、上に鍵の板を重ねる</summary>
        private static void CreateTeamRoomLayout(Transform card, SerializedObject so)
        {
            var layout = CreateLabel(card, "Layout", string.Empty, TeamRoomLayoutFontSize, TeamRoomLayoutPosition, TeamRoomLayoutSize);
            layout.fontStyle = FontStyle.Bold;
            so.FindProperty("_layoutText").objectReferenceValue = layout;

            var lockPlate = UIDialogBuilder.CreateUIObject("Lock", card);
            PlaceCenter(lockPlate.GetComponent<RectTransform>(), TeamRoomLockPosition, TeamRoomLockSize);
            AddSliced(lockPlate, RoundRectPath, RushLockColor, CornerScale(PanelCornerRadius)).raycastTarget = false;
            CreateHudImage(lockPlate.transform, "Icon", LockIconPath, RushLockIconColor, Middle, TeamRoomLockIconPosition, Vector2.one * TeamRoomLockIconSize).preserveAspect = true;
            var lockText = CreateLabel(lockPlate.transform, "Text", string.Empty, TeamRoomLockFontSize, RushLockTextPosition, TeamRoomLockTextSize);
            lockText.fontStyle = FontStyle.Bold;
            lockPlate.SetActive(false);
            so.FindProperty("_lock").objectReferenceValue = lockPlate;
            so.FindProperty("_lockText").objectReferenceValue = lockText;
        }

        /// <summary>報酬（出た分・勝ち・点・初めて勝ったボーナス）と、ホスト・ゲストへの一言</summary>
        private static void CreateTeamRoomReward(Transform card, SerializedObject so)
        {
            CreateHudImage(card, "CoinIcon", CoinIconPath, Color.white, Middle, TeamRoomRewardIconPosition, Vector2.one * RushRewardIconSize).preserveAspect = true;
            var reward = CreateLabel(card, "Reward", string.Empty, TeamRoomRewardFontSize, TeamRoomRewardPosition, TeamRoomRewardSize);
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
