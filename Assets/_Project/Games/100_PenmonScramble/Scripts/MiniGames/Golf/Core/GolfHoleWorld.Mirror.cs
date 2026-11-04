using System;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// オンラインのゲストの写し: ホストのスナップショットを写すだけで、球の計算はしない。
    /// 起きたことは順に「打数・番・カップイン」だけを進めて（その出来事を見せる時点の打数や番の人で見せるため）、
    /// 最後にスナップショットの状態で全部を上書きする。球の位置は前の位置から届いた位置へ、1回の送る間隔をかけてなめらかに動かす
    /// </summary>
    public sealed partial class GolfHoleWorld
    {
        // これより大きく飛んだ球（池の戻し・ティーへの置き直し）は、なめらかに動かさずその場へ置く
        private const float SnapDistance = 2.5f;

        private Vec2[] _mirrorFrom;
        private Vec2[] _mirrorTo;
        private float[] _mirrorFromHeight;
        private float[] _mirrorToHeight;
        private float _mirrorBlend = 1f;

        /// <summary>出来事1つ分だけ写しを進める（その後に状態で上書きするので、ここは見せ方に要る所だけ）</summary>
        internal void MirrorEvent(GolfEvent e)
        {
            var player = e.Player;
            var valid = player >= 0 && player < PlayerCount;
            switch (e.Kind)
            {
                case GolfEventKind.Shot when valid:
                    _strokes[player]++;
                    Phase = GolfHolePhase.Rolling;
                    SetCurrent(player);
                    break;
                case GolfEventKind.Fell when valid && player == CurrentPlayer:
                    _strokes[player]++;
                    break;
                case GolfEventKind.CupIn when valid:
                    _balls[player].InCup = true;
                    break;
                case GolfEventKind.GaveUp when valid:
                    _strokes[player] = Hole.StrokeLimit + 1;
                    _done[player] = true;
                    _balls[player].Active = false;
                    break;
                case GolfEventKind.TurnStart when valid:
                    Phase = GolfHolePhase.Aiming;
                    SetCurrent(player);
                    break;
                case GolfEventKind.HoleFinished:
                    Phase = GolfHolePhase.Finished;
                    break;
            }
        }

        /// <param name="keepLocalAim">自分の番で狙っている途中なら、向きは手元のものを使う（届いた向きで手元の向きが戻らないように）</param>
        internal void MirrorState(GolfSnapshot snapshot, bool keepLocalAim)
        {
            if (snapshot.TurnOrder != null && snapshot.TurnOrder.Length == PlayerCount)
            {
                _order = (int[])snapshot.TurnOrder.Clone();
            }

            Phase = snapshot.Phase;
            SetCurrent(snapshot.CurrentPlayer);
            ShotTimeLeft = snapshot.ShotTimeLeft;
            if (!keepLocalAim)
            {
                AimAngle = snapshot.Aim.Angle;
            }

            for (var player = 0; player < PlayerCount && player < snapshot.PlayerCount; player++)
            {
                _strokes[player] = snapshot.Strokes[player];
                _done[player] = snapshot.Done[player];
                MirrorBall(player, snapshot.Balls[player]);
            }

            _mirrorBlend = 0f;
        }

        private void MirrorBall(int player, GolfBallState state)
        {
            EnsureMirrorBuffers();
            var ball = _balls[player];
            var jump = !ball.Active || !state.Active || Vec2.Distance(ball.Position, state.Position) > SnapDistance;
            _mirrorFrom[player] = jump ? state.Position : ball.Position;
            _mirrorFromHeight[player] = jump ? state.Height : ball.Height;
            _mirrorTo[player] = state.Position;
            _mirrorToHeight[player] = state.Height;
            ball.Position = _mirrorFrom[player];
            ball.Height = _mirrorFromHeight[player];
            ball.Velocity = state.Velocity;
            ball.VerticalSpeed = 0f;
            ball.InCup = state.InCup;
            ball.Active = state.Active;
            ball.Special = state.Special;
            ball.IceLeft = state.IceLeft;
            ball.BoostReady = state.BoostReady;
            ball.BoostStars = state.BoostStars;
        }

        /// <summary>写しの時間を進める: 持ち時間を減らし（次のスナップショットで直る）、球を届いた位置へ寄せる</summary>
        /// <param name="blendSeconds">届いた位置へ寄せきるまでの秒（スナップショットを送る間隔）</param>
        internal void TickMirror(float seconds, float blendSeconds)
        {
            if (Phase == GolfHolePhase.Aiming)
            {
                ShotTimeLeft = Math.Max(0f, ShotTimeLeft - seconds);
            }

            if (_mirrorTo == null || _mirrorBlend >= 1f)
            {
                return;
            }

            _mirrorBlend = blendSeconds > 0f ? Math.Min(1f, _mirrorBlend + seconds / blendSeconds) : 1f;
            for (var player = 0; player < PlayerCount; player++)
            {
                var ball = _balls[player];
                ball.Position = _mirrorFrom[player] + (_mirrorTo[player] - _mirrorFrom[player]) * _mirrorBlend;
                ball.Height = _mirrorFromHeight[player] + (_mirrorToHeight[player] - _mirrorFromHeight[player]) * _mirrorBlend;
            }
        }

        private void EnsureMirrorBuffers()
        {
            if (_mirrorTo != null)
            {
                return;
            }

            _mirrorFrom = new Vec2[PlayerCount];
            _mirrorTo = new Vec2[PlayerCount];
            _mirrorFromHeight = new float[PlayerCount];
            _mirrorToHeight = new float[PlayerCount];
            for (var player = 0; player < PlayerCount; player++)
            {
                _mirrorFrom[player] = _mirrorTo[player] = _balls[player].Position;
            }
        }

        /// <summary>番の人を席の番号で決める（打つ順の中の位置を探す）</summary>
        private void SetCurrent(int player)
        {
            for (var index = 0; index < _order.Count; index++)
            {
                if (_order[index] == player)
                {
                    _orderIndex = index;
                    return;
                }
            }
        }
    }
}
