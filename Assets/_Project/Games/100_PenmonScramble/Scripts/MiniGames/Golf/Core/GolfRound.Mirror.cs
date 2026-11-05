using System;
using System.Collections.Generic;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// オンラインのゲストの写しのラウンド。ホストのスナップショット（GolfSnapshot）を写すだけで、打つ・次のホールへ進むはしない。
    /// 起きたことはホストと同じく Happened / HoleStarted / HoleCompleted で渡すので、見せ方はひとりで回るときと同じものを使える
    /// </summary>
    public sealed partial class GolfRound
    {
        /// <summary>オンラインのゲストの写し（打つ・進めるはホストだけ）</summary>
        public bool IsMirror { get; }

        /// <summary>写しのみ: ホストから届いた、番の人の手元（向き・ゲージ・特殊）</summary>
        public GolfAim RemoteAim { get; private set; }

        /// <summary>写しのラウンドを作る。teams はスナップショットの3体から作る</summary>
        public static GolfRound CreateMirror(GolfCourseDef course, IReadOnlyList<GolfTeam> teams)
        {
            return new GolfRound(course, teams, true);
        }

        /// <summary>写しのみ: 持ち時間を減らし、球を届いた位置へなめらかに寄せる</summary>
        public void TickMirror(float seconds, float blendSeconds)
        {
            if (IsMirror)
            {
                World.TickMirror(seconds, blendSeconds);
            }
        }

        /// <summary>
        /// 写しのみ: 届いたスナップショットを写す。起きたことを順に渡してから（ホールが替わっていればホールも始め直す）、状態で上書きする。
        /// 人数やホールの数が合わない（別のコース・別の版）ときは何もせず false
        /// </summary>
        /// <param name="localSeat">自分の席。自分の番で狙っている途中なら、向きと選んでいる子は手元のものを使う</param>
        public bool ApplySnapshot(GolfSnapshot snapshot, int localSeat)
        {
            if (!IsMirror || snapshot == null || snapshot.PlayerCount != PlayerCount || snapshot.Recorded.Length != HoleCount
                || snapshot.HoleIndex < 0 || snapshot.HoleIndex >= HoleCount)
            {
                return false;
            }

            // 手元の向きを残すのは、前から自分の番で狙い続けているときだけ（この間に番やホールが替わったら、届いた向きから始める）
            var wasAiming = localSeat >= 0 && World.Phase == GolfHolePhase.Aiming && World.CurrentPlayer == localSeat;
            var hole = HoleIndex;
            var turnChanged = false;
            CopyRemoved(snapshot);
            foreach (var e in snapshot.Events)
            {
                turnChanged |= e.Event.Kind == GolfEventKind.TurnStart || e.Event.Kind == GolfEventKind.Shot;
                ApplyEvent(e, snapshot);
            }

            if (snapshot.HoleIndex != HoleIndex)
            {
                MoveToHole(snapshot.HoleIndex, snapshot);
            }

            var keepLocal = wasAiming && !turnChanged && hole == HoleIndex
                && snapshot.CurrentPlayer == localSeat && snapshot.Phase == GolfHolePhase.Aiming;
            World.MirrorState(snapshot, keepLocal);
            for (var player = 0; player < PlayerCount; player++)
            {
                Teams[player].MirrorState(snapshot.Teams[player], keepLocal && player == localSeat);
            }

            var wasRecorded = _recorded[HoleIndex];
            CopyCard(snapshot);
            RemoteAim = snapshot.Aim;
            // 書き終えた知らせ（HoleFinished）を受け取りそこねていても、スコアカードへ進めるように
            if (!wasRecorded && _recorded[HoleIndex])
            {
                HoleCompleted?.Invoke(HoleIndex);
            }

            return true;
        }

        private void ApplyEvent(GolfNetEvent e, GolfSnapshot snapshot)
        {
            if (e.Hole < 0 || e.Hole >= HoleCount)
            {
                return;
            }

            if (e.Hole != HoleIndex)
            {
                MoveToHole(e.Hole, snapshot);
            }

            World.MirrorEvent(e.Event);
            OnHappened(e.Event);
        }

        /// <summary>ホストが次のホールへ進んだ。スコアカードを写してからホールを始める（打つ順は状態で届く）</summary>
        private void MoveToHole(int hole, GolfSnapshot snapshot)
        {
            CopyCard(snapshot);
            StartHole(hole);
        }

        private void CopyCard(GolfSnapshot snapshot)
        {
            var players = Math.Min(PlayerCount, snapshot.PlayerCount);
            for (var hole = 0; hole < HoleCount; hole++)
            {
                _recorded[hole] = snapshot.Recorded[hole];
                for (var player = 0; player < players; player++)
                {
                    _strokes[hole, player] = snapshot.Card[hole, player];
                }
            }
        }

        private void CopyRemoved(GolfSnapshot snapshot)
        {
            for (var player = 0; player < PlayerCount; player++)
            {
                var removed = snapshot.Removed[player];
                if (removed && !_removed[player])
                {
                    _absent.Add(player);
                }

                _removed[player] = removed;
            }
        }
    }
}
