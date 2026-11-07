using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 「みんなで遊ぶ」の待ちの窓の右側: 上に遊ぶものの札（ボスラッシュ / ゴルフ / サッカー。RoomModeSelector）、その下に選んだ遊びの板
    /// （ボスラッシュは RushLobby.cs、ゴルフは MiniGames/Golf の GolfRoom.cs、サッカーは MiniGames/Soccer の SoccerRoom.cs）。部屋に入っている間は、左に接続の状態（参加コード・人数）が寄る
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        // 札の並び（窓のまん中から測る）。遊びの板の上の端に置く（板の見出しの代わり）
        private static readonly Vector2 RoomTabsPosition = new Vector2(270f, 312f);
        private const float RoomTabSpacing = 330f;
        private static readonly Vector2 RoomTabSize = new Vector2(300f, 80f);
        private static readonly Vector2 RoomTabMarkPadding = new Vector2(14f, 14f);
        private const int RoomTabFontSize = 38;
        private static readonly Color RoomTabColor = new Color(0.95f, 0.62f, 0.20f);

        private static readonly Vector2 WaitingStatusBesidePosition = new Vector2(-520f, 60f);
        private static readonly Vector2 WaitingStatusBesideSize = new Vector2(500f, 460f);

        /// <summary>遊びの板を作って、札でつなぐ。札の並びはボスラッシュ・ゴルフ・サッカー（先頭が最初に選ばれている）</summary>
        private static RoomModeSelector CreateRoomModes(ModeSelectPanel title, OnlineSession session, Transform waitingPanel, Text status)
        {
            var rush = CreateRushLobby(title, waitingPanel);
            var golf = CreateGolfRoomPanel(title, session, waitingPanel);
            var soccer = CreateSoccerRoomPanel(title, session, waitingPanel);
            var modes = new MonoBehaviour[] { rush, golf, soccer };

            var selector = title.gameObject.AddComponent<RoomModeSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("_modeSelect").objectReferenceValue = title;
            SetObjectArray(so.FindProperty("_modes"), modes);
            CreateRoomTabs(waitingPanel, modes.Length, so);
            var statusRect = status.rectTransform;
            so.FindProperty("_status").objectReferenceValue = statusRect;
            so.FindProperty("_statusAlonePosition").vector2Value = statusRect.anchoredPosition;
            so.FindProperty("_statusAloneSize").vector2Value = statusRect.sizeDelta;
            so.FindProperty("_statusBesidePosition").vector2Value = WaitingStatusBesidePosition;
            so.FindProperty("_statusBesideSize").vector2Value = WaitingStatusBesideSize;
            so.ApplyModifiedProperties();

            // ホストひとりでも始められる（友達を待たずに練習できる）
            var titleSo = new SerializedObject(title);
            titleSo.FindProperty("_minPlayersToStart").intValue = 1;
            titleSo.ApplyModifiedProperties();
            return selector;
        }

        /// <summary>
        /// 札の列。選んでいる札は後ろに白い縁を光らせ、入れ物ごと大きく濃くする（ボスラッシュの難易度の札と同じ見た目）。
        /// 札の字は RoomModeSelector が遊びの名前を入れる。ゲストは押せないが、選ばれた札は濃く見せたいので、ボタンの「押せないときは薄く」は切る
        /// </summary>
        private static void CreateRoomTabs(Transform waitingPanel, int count, SerializedObject so)
        {
            var row = UIDialogBuilder.CreateUIObject("RoomTabs", waitingPanel);
            PlaceCenter(row.GetComponent<RectTransform>(), RoomTabsPosition, new Vector2(RoomTabSpacing * count, RoomTabSize.y));
            var slots = new Object[count];
            var marks = new Object[count];
            var tabs = new Object[count];
            for (var i = 0; i < count; i++)
            {
                var slot = CreateHudRect(row.transform, $"Tab{i}", Middle, new Vector2((i - (count - 1) * 0.5f) * RoomTabSpacing, 0f), RoomTabSize);
                slots[i] = slot.gameObject.AddComponent<CanvasGroup>();
                var mark = UIDialogBuilder.CreateUIObject("Selected", slot);
                PlaceCenter(mark.GetComponent<RectTransform>(), Vector2.zero, RoomTabSize + RoomTabMarkPadding);
                AddSliced(mark, RoundRectPath, Color.white, CornerScale(ButtonCornerRadius + RoomTabMarkPadding.y * 0.5f)).raycastTarget = false;
                marks[i] = mark;

                var button = CreateTitleButton(slot, "Button", string.Empty, Vector2.zero, RoomTabSize, RoomTabColor, RoomTabFontSize);
                var motion = new SerializedObject(button.GetComponent<UiButtonMotion>());
                motion.FindProperty("_disabledAlpha").floatValue = 1f;
                motion.ApplyModifiedProperties();
                tabs[i] = button;
            }

            row.SetActive(false);
            so.FindProperty("_tabRow").objectReferenceValue = row;
            SetObjectArray(so.FindProperty("_tabSlots"), slots);
            SetObjectArray(so.FindProperty("_tabMarks"), marks);
            SetObjectArray(so.FindProperty("_tabs"), tabs);
        }
    }
}
