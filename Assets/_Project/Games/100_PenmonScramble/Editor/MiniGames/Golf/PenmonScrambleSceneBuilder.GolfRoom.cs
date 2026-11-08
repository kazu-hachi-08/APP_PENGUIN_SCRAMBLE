using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Golf.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 「みんなで遊ぶ」の待ちの窓に出す、ゴルフのコースの選択（GolfRoomPanel）: コースの板（◀ ▶・番号・名前）、
    /// ホールとパー、鍵の板、報酬（初回ボーナス・順位のボーナス）、ホスト・ゲストへの一言。置き場所はボスラッシュの板と同じ
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private static readonly Vector2 GolfRoomFlagPosition = new Vector2(-330f, 40f);
        private const float GolfRoomFlagSize = 120f;
        private static readonly Vector2 GolfRoomLayoutPosition = new Vector2(70f, 40f);
        private static readonly Vector2 GolfRoomLayoutSize = new Vector2(700f, 110f);
        // コースの中身は2行（ホールとパー / 仕掛けの一言）。2行が枠の高さに入る大きさ
        private const int GolfRoomLayoutFontSize = 36;
        private static readonly Vector2 GolfRoomLockPosition = new Vector2(0f, 40f);
        private static readonly Vector2 GolfRoomRewardIconPosition = new Vector2(-420f, -130f);
        private static readonly Vector2 GolfRoomRewardPosition = new Vector2(40f, -130f);
        private static readonly Vector2 GolfRoomRewardSize = new Vector2(860f, 150f);
        private const int GolfRoomRewardFontSize = 30;

        private static GolfRoomPanel CreateGolfRoomPanel(ModeSelectPanel title, OnlineSession session, Transform waitingPanel)
        {
            var card = UIDialogBuilder.CreateUIObject("Golf", waitingPanel);
            PlaceCenter(card.GetComponent<RectTransform>(), RushCardPosition, RushCardSize);

            var panel = title.gameObject.AddComponent<GolfRoomPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_modeSelect").objectReferenceValue = title;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_card").objectReferenceValue = card;
            CreateGolfRoomCourseRow(card.transform, so);
            CreateGolfRoomLayout(card.transform, so);
            CreateGolfRoomReward(card.transform, so);
            so.ApplyModifiedProperties();
            card.SetActive(false);
            return panel;
        }

        /// <summary>「コース 1」と名前の板を、◀ ▶ ではさむ（◀ ▶ はホストだけ。ボスラッシュのステージの板と同じ形）</summary>
        private static void CreateGolfRoomCourseRow(Transform card, SerializedObject so)
        {
            var plate = CreatePanel(card, "Course", new Vector2(0f, RushStageY), RushStagePlateSize).transform;
            var no = CreateLabel(plate, "No", string.Empty, RushStageNoFontSize, RushStageNoPosition, RushStageNoSize);
            no.color = StageNoColor;
            var name = CreateLabel(plate, "Name", string.Empty, RushStageNameFontSize, RushStageNamePosition, RushStageNameSize);
            name.fontStyle = FontStyle.Bold;
            var prev = CreateTitleButton(card, "PrevCourse", "◀", new Vector2(-RushArrowX, RushStageY), RushArrowSize, ArrowColor, MenuFontSize);
            var next = CreateTitleButton(card, "NextCourse", "▶", new Vector2(RushArrowX, RushStageY), RushArrowSize, ArrowColor, MenuFontSize);
            so.FindProperty("_courseNoText").objectReferenceValue = no;
            so.FindProperty("_courseNameText").objectReferenceValue = name;
            so.FindProperty("_prevButton").objectReferenceValue = prev;
            so.FindProperty("_nextButton").objectReferenceValue = next;
        }

        /// <summary>旗の絵とホールとパー。コースが閉じていれば、上に鍵の板を重ねる</summary>
        private static void CreateGolfRoomLayout(Transform card, SerializedObject so)
        {
            CreateHudImage(card, "Flag", MiniGameIconPath, Color.white, Middle, GolfRoomFlagPosition, Vector2.one * GolfRoomFlagSize).preserveAspect = true;
            var layout = CreateLabel(card, "Layout", string.Empty, GolfRoomLayoutFontSize, GolfRoomLayoutPosition, GolfRoomLayoutSize);
            layout.fontStyle = FontStyle.Bold;
            so.FindProperty("_layoutText").objectReferenceValue = layout;

            var lockPlate = UIDialogBuilder.CreateUIObject("Lock", card);
            PlaceCenter(lockPlate.GetComponent<RectTransform>(), GolfRoomLockPosition, RushLockSize);
            AddSliced(lockPlate, RoundRectPath, RushLockColor, CornerScale(PanelCornerRadius)).raycastTarget = false;
            CreateHudImage(lockPlate.transform, "Icon", LockIconPath, RushLockIconColor, Middle, RushLockIconPosition, Vector2.one * RushLockIconSize).preserveAspect = true;
            var lockText = CreateLabel(lockPlate.transform, "Text", string.Empty, RushLockFontSize, RushLockTextPosition, RushLockTextSize);
            lockText.fontStyle = FontStyle.Bold;
            lockPlate.SetActive(false);
            so.FindProperty("_lock").objectReferenceValue = lockPlate;
            so.FindProperty("_lockText").objectReferenceValue = lockText;
        }

        /// <summary>報酬（基本・パーより少ない打数・初回ボーナス・順位のボーナス）と、ホスト・ゲストへの一言</summary>
        private static void CreateGolfRoomReward(Transform card, SerializedObject so)
        {
            CreateHudImage(card, "CoinIcon", CoinIconPath, Color.white, Middle, GolfRoomRewardIconPosition, Vector2.one * RushRewardIconSize).preserveAspect = true;
            var reward = CreateLabel(card, "Reward", string.Empty, GolfRoomRewardFontSize, GolfRoomRewardPosition, GolfRoomRewardSize);
            reward.alignment = TextAnchor.MiddleLeft;
            reward.color = CoinTextColor;
            var note = CreateLabel(card, "Note", string.Empty, RushNoteFontSize, RushNotePosition, RushNoteSize);
            note.color = PlateHintColor;
            so.FindProperty("_rewardText").objectReferenceValue = reward;
            so.FindProperty("_noteText").objectReferenceValue = note;
        }
    }
}
