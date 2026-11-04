using System;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// オンラインの所: 打つ操作の行き先（ひとり・ホストはその場で打ち、ゲストはホストへ送る）、ほかの人の番に見せる手元
    /// （ホストはゲストから届いたもの、ゲストはスナップショットで届いたもの）、ホストがゲストの入力を受けて打たせる所。
    /// ゲストが打ってからホストの球が動き出すまで・ほかの人が振り下ろした後は、構え直さずに振り抜いたまま待つ（SwingHeld）
    /// </summary>
    public partial class GolfRoundPlay
    {
        // 打ってから、ホストから球が動き出したと届くまで待つ上限（届かなければ打ち直せるようにする）
        private const float AwaitShotSeconds = 1.5f;

        private readonly GolfAim[] _remoteAims = new GolfAim[GolfRules.MaxPlayers];
        private float _swingHold;
        private bool _remoteStriking;

        /// <summary>オンラインのホストが結果で「コースを選ぶ」を押した（全員で待ちの窓へ戻る。GolfOnlinePlay が受ける）</summary>
        public event Action RoomRequested;

        public GolfRound Round => _round;

        /// <summary>番の人の手元（ホストがスナップショットに載せて、ほかの人に見せる）</summary>
        public GolfAim CurrentAim => _round != null ? DisplayAim : default;

        /// <summary>番の人の手元。自分の番は手元の操作、ほかの人の番はその人のもの</summary>
        private GolfAim DisplayAim
        {
            get
            {
                if (_control.IsLocalTurn)
                {
                    return _control.Aim;
                }

                if (_round.IsMirror)
                {
                    return _round.RemoteAim;
                }

                return _remoteAims[World.CurrentPlayer].WithStriking(_view.Shooter.Striking);
            }
        }

        private bool SwingHeld => _swingHold > 0f;

        private void BindShots()
        {
            _control.StrikeStarted += OnStrikeStarted;
            _control.Impact += OnImpact;
            _control.BoostRequested += OnBoostRequested;
        }

        private void OnStrikeStarted(int player, GolfShot shot)
        {
            if (_mode.IsGuest)
            {
                _mode.Sender.Send(GolfInput.ForStrike(new GolfAim(shot.Member, shot.Angle, shot.Power, false, true, shot.Special, shot.Curve)));
            }
        }

        /// <summary>クラブが当たった。ゲストは球が動き出したとホストから届くまで、振り抜いたまま待つ</summary>
        private void OnImpact(int player, GolfShot shot)
        {
            if (_mode.IsGuest)
            {
                _swingHold = AwaitShotSeconds;
                return;
            }

            _round.Shoot(player, shot);
        }

        private void OnBoostRequested(int player, float turn)
        {
            if (_mode.IsGuest)
            {
                _mode.Sender.Send(GolfInput.ForBoost(turn));
                return;
            }

            _round.Boost(player, turn);
        }

        /// <summary>ゲストの自分の番: 狙っている様子をホストへ（ホストがほかの人に見せる。送る間隔は送る側が決める）</summary>
        private void SendLocalAim()
        {
            if (_mode.IsGuest && _control.IsLocalTurn && World.Phase == GolfHolePhase.Aiming && !SwingHeld && !_control.Striking)
            {
                _mode.Sender.SendAim(_control.Aim);
            }
        }

        /// <summary>
        /// 待つ間を減らす。ゲストは、ほかの人が振り下ろし始めたと届いたら、同じように振り下ろして見せる
        /// </summary>
        private void TickSwingHold(float dt)
        {
            _swingHold = Mathf.Max(0f, _swingHold - dt);
            if (_round.IsMirror)
            {
                var remote = _round.RemoteAim;
                var striking = !_control.IsLocalTurn && World.Phase == GolfHolePhase.Aiming && remote.Striking;
                if (striking && !_remoteStriking)
                {
                    _view.Shooter.Strike(remote.Power, null);
                    _swingHold = AwaitShotSeconds;
                }

                _remoteStriking = striking;
            }

            _control.Blocked = SwingHeld;
        }

        /// <summary>打った・番が替わった・ホールが始まった: 待つのは終わり</summary>
        private void ResetSwingHold()
        {
            _swingHold = 0f;
            _remoteStriking = false;
            _control.Blocked = false;
        }

        /// <summary>
        /// ホストのみ: ゲストの入力。番の人からだけ受ける。振り下ろしはホストでも振り下ろして、クラブが当たった瞬間に打つ
        /// （ひとりで打つときと同じ間合いにするため）
        /// </summary>
        public void ApplyRemoteInput(int seat, GolfInput input)
        {
            if (_round == null || _round.IsMirror || seat < 0 || seat >= _round.PlayerCount)
            {
                return;
            }

            switch (input.Kind)
            {
                case GolfInputKind.Aim:
                    if (IsRemoteAiming(seat))
                    {
                        FollowRemoteAim(seat, input.Aim);
                    }

                    break;
                case GolfInputKind.Strike:
                    if (IsRemoteAiming(seat) && CanAct)
                    {
                        FollowRemoteAim(seat, input.Aim);
                        var shot = input.Aim.ToShot();
                        _view.Shooter.Strike(shot.Power, () => _round.Shoot(seat, shot));
                    }

                    break;
                case GolfInputKind.Boost:
                    _round.Boost(seat, input.BoostTurn);
                    break;
            }
        }

        private bool IsRemoteAiming(int seat)
        {
            return World.Phase == GolfHolePhase.Aiming && World.CurrentPlayer == seat && !_view.Shooter.Striking;
        }

        private void FollowRemoteAim(int seat, GolfAim aim)
        {
            World.SetAim(aim.Angle);
            _round.Teams[seat].Select(aim.Member);
            _remoteAims[seat] = aim;
        }

        /// <summary>ホストのみ: ゲストが抜けた。球を消して打順から外す（残りで続ける）</summary>
        public void RemovePlayer(int seat)
        {
            if (_round == null || _round.IsMirror || _round.IsRemoved(seat))
            {
                return;
            }

            _round.RemovePlayer(seat);
            _hud.Flash($"{GolfSeats.Label(seat)} が抜けました", BadColor, MessageSeconds);
        }

        /// <summary>番が始まった人の手元は、まだ何も届いていない状態から</summary>
        private void ResetRemoteAim(int player)
        {
            if (player >= 0 && player < _remoteAims.Length)
            {
                _remoteAims[player] = GolfAim.Idle(_round.Teams[player].Selected, World.AimAngle);
            }
        }

        private void RequestRoom() => RoomRequested?.Invoke();
    }
}
