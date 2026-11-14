using PenmonScramble.Common.Editor;
using PenmonScramble.Common.Online;
using PenmonScramble.Baseball.Game;
using PenmonScramble.Game;
using PenmonScramble.Soccer.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 「みんなで遊ぶ」の待ちの窓の右側: 上に部屋の遊びの見出し（「ゴルフの部屋」。RoomModeSelector が入れる）、その下にその遊びの板
    /// （ボスラッシュは RushLobby.cs、ゴルフは MiniGames/Golf の GolfRoom.cs、サッカー・野球は MiniGames の TeamRoom.cs）。部屋に入っている間は、左に接続の状態（参加コード・人数）が寄る。
    /// 遊びは部屋を作った入口で決まるので、待ちの窓では選ばない
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        // 見出しは遊びの板の上の端に置く（窓のまん中から測る。板のまん中の x にそろえる）
        private static readonly Vector2 RoomHeadingPosition = new Vector2(270f, 312f);
        private static readonly Vector2 RoomHeadingSize = new Vector2(1000f, 80f);
        private const int RoomHeadingFontSize = 48;
        private static readonly Color RoomHeadingColor = new Color(1f, 0.85f, 0.45f);

        private static readonly Vector2 WaitingStatusBesidePosition = new Vector2(-520f, 60f);
        private static readonly Vector2 WaitingStatusBesideSize = new Vector2(500f, 460f);

        /// <summary>遊びの板を作って RoomModeSelector に並べる。先頭（ボスラッシュ）が、入口を通らずに部屋を開いたときの遊び</summary>
        private static RoomModeSelector CreateRoomModes(ModeSelectPanel title, OnlineSession session, Transform waitingPanel, Text status)
        {
            var rush = CreateRushLobby(title, waitingPanel);
            var golf = CreateGolfRoomPanel(title, session, waitingPanel);
            var soccer = CreateTeamRoomPanel<SoccerRoomPanel>(title, session, waitingPanel, "Soccer");
            var baseball = CreateTeamRoomPanel<BaseballRoomPanel>(title, session, waitingPanel, "Baseball");
            var modes = new MonoBehaviour[] { rush, golf, soccer, baseball };

            var selector = title.gameObject.AddComponent<RoomModeSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("_modeSelect").objectReferenceValue = title;
            SetObjectArray(so.FindProperty("_modes"), modes);
            so.FindProperty("_roomHeading").objectReferenceValue = CreateRoomHeading(waitingPanel);
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

        /// <summary>部屋の遊びの見出し。部屋に入るまでは隠しておく（RoomModeSelector が出す）</summary>
        private static Text CreateRoomHeading(Transform waitingPanel)
        {
            var heading = CreateLabel(waitingPanel, "RoomHeading", string.Empty, RoomHeadingFontSize, RoomHeadingPosition, RoomHeadingSize);
            heading.fontStyle = FontStyle.Bold;
            heading.color = RoomHeadingColor;
            heading.gameObject.SetActive(false);
            return heading;
        }

        /// <summary>「みんなで遊ぶ」の窓の見出し（どの遊びの部屋を作るか）をつなぐ。窓はロビーを作るときにできるので、遊びの板より後で</summary>
        private static void WireOnlineHeading(RoomModeSelector roomModes, Text heading)
        {
            var so = new SerializedObject(roomModes);
            so.FindProperty("_onlineHeading").objectReferenceValue = heading;
            so.ApplyModifiedProperties();
        }

        /// <summary>札の後ろに置く白い縁（選んでいる札だけ見せる。ボスラッシュの難易度・ミニゲームの対戦 / 協力の札で使う）。札より少し大きく、角の丸みも縁の分だけ広げる</summary>
        private static GameObject CreateSelectedMark(Transform slot, Vector2 size, Vector2 markPadding)
        {
            var mark = UIDialogBuilder.CreateUIObject("Selected", slot);
            PlaceCenter(mark.GetComponent<RectTransform>(), Vector2.zero, size + markPadding);
            AddSliced(mark, RoundRectPath, Color.white, CornerScale(ButtonCornerRadius + markPadding.y * 0.5f)).raycastTarget = false;
            return mark;
        }
    }
}
