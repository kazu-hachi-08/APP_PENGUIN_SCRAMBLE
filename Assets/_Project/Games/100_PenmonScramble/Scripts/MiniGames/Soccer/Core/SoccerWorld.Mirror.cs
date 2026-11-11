namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// オンラインの写し: ホストはピッチの上の今を SoccerSnapshot に写し、ゲストは届いた物をそのまま書き込む
    /// （ゲストは計算しない。見た目・HUD はひとりのときと同じ SoccerWorld を読んで描く）
    /// </summary>
    public sealed partial class SoccerWorld
    {
        internal void CaptureTo(SoccerSnapshot snapshot)
        {
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                snapshot.Score[team] = _score[team];
                snapshot.Gauge[team] = _gauge[team];
            }

            snapshot.CutInSeconds = _cutInSeconds;
            snapshot.GoalScored = IsGoalScored;
            snapshot.WorldTime = Time;
            snapshot.Ball = CaptureBall();
            snapshot.Special = Special == null ? (SoccerSpecialSnap?)null : new SoccerSpecialSnap
            {
                Shooter = Special.Shooter,
                Team = Special.Team,
                Element = Special.Element,
                Stars = Special.Stars,
                Hidden = Special.IsHidden,
            };
            snapshot.Players = new SoccerPlayerSnap[_players.Length];
            for (var i = 0; i < _players.Length; i++)
            {
                snapshot.Players[i] = CapturePlayer(_players[i]);
            }

            snapshot.Keepers = new SoccerKeeperSnap[_keepers.Length];
            for (var i = 0; i < _keepers.Length; i++)
            {
                var keeper = _keepers[i];
                snapshot.Keepers[i] = new SoccerKeeperSnap
                {
                    Position = keeper.Position,
                    Speed = keeper.Speed,
                    State = keeper.State,
                    StateSeconds = keeper.StateSeconds,
                    DiveSide = keeper.DiveSide,
                };
            }
        }

        /// <summary>ゲストのみ: 届いた写しを書き込む。子・キーパーの数は呼ぶ側（SoccerMatch.ApplySnapshot）が先に確かめてから渡す。ここでは数の少ない方まで書く</summary>
        internal void ApplySnapshot(SoccerSnapshot snapshot)
        {
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                _score[team] = snapshot.Score[team];
                _gauge[team] = snapshot.Gauge[team];
            }

            _cutInSeconds = snapshot.CutInSeconds;
            IsGoalScored = snapshot.GoalScored;
            Time = snapshot.WorldTime;
            ApplyBall(snapshot.Ball);
            ApplySpecial(snapshot.Special);
            for (var i = 0; i < _players.Length && i < snapshot.Players.Length; i++)
            {
                ApplyPlayer(_players[i], snapshot.Players[i]);
            }

            for (var i = 0; i < _keepers.Length && i < snapshot.Keepers.Length; i++)
            {
                var keeper = _keepers[i];
                var state = snapshot.Keepers[i];
                keeper.Position = state.Position;
                keeper.Speed = state.Speed;
                keeper.State = state.State;
                keeper.StateSeconds = state.StateSeconds;
                keeper.DiveSide = state.DiveSide;
            }
        }

        private SoccerBallSnap CaptureBall()
        {
            return new SoccerBallSnap
            {
                Position = Ball.Position,
                Velocity = Ball.Velocity,
                Height = Ball.Height,
                VerticalSpeed = Ball.VerticalSpeed,
                Holder = Ball.Holder,
                KeeperTeam = Ball.KeeperTeam,
                LastToucher = Ball.LastToucher,
            };
        }

        private void ApplyBall(SoccerBallSnap state)
        {
            Ball.Position = state.Position;
            Ball.Velocity = state.Velocity;
            Ball.Height = state.Height;
            Ball.VerticalSpeed = state.VerticalSpeed;
            Ball.Holder = state.Holder >= 0 && state.Holder < _players.Length ? state.Holder : SoccerBall.NoPlayer;
            Ball.KeeperTeam = state.KeeperTeam == SoccerRules.Red || state.KeeperTeam == SoccerRules.Blue ? state.KeeperTeam : SoccerBall.NoTeam;
            Ball.LastToucher = state.LastToucher;
        }

        /// <summary>同じ必殺シュートが飛び続けている間は作り直さない（見た目が尾を引き継げるように）</summary>
        private void ApplySpecial(SoccerSpecialSnap? state)
        {
            if (state == null)
            {
                Special = null;
                return;
            }

            var s = state.Value;
            if (Special == null || Special.Shooter != s.Shooter || Special.Element != s.Element)
            {
                Special = new SoccerSpecialShot(s.Shooter, s.Team, s.Element, s.Stars);
            }

            Special.Flight.HiddenSeconds = s.Hidden ? 1f : 0f;
        }

        private static SoccerPlayerSnap CapturePlayer(SoccerPlayer player)
        {
            return new SoccerPlayerSnap
            {
                Position = player.Position,
                Velocity = player.Velocity,
                Facing = player.Facing,
                State = player.State,
                StateSeconds = player.StateSeconds,
                IsCharging = player.IsCharging,
                ChargeSeconds = player.ChargeSeconds,
                SlideCooldown = player.SlideCooldown,
                DashCooldown = player.DashCooldown,
                FrozenSeconds = player.FrozenSeconds,
            };
        }

        private static void ApplyPlayer(SoccerPlayer player, SoccerPlayerSnap state)
        {
            player.Position = state.Position;
            player.Velocity = state.Velocity;
            if (!state.Facing.IsNearlyZero)
            {
                player.Facing = state.Facing.Normalized;
            }

            player.State = state.State;
            player.StateSeconds = state.StateSeconds;
            player.IsCharging = state.IsCharging;
            player.ChargeSeconds = state.ChargeSeconds;
            player.SlideCooldown = state.SlideCooldown;
            player.DashCooldown = state.DashCooldown;
            player.FrozenSeconds = state.FrozenSeconds;
        }
    }
}
