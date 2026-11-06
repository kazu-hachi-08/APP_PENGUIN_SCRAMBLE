using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// PK戦の1本: 並べる・蹴った瞬間に決まり方を決める・ボールとキーパーを飛ばして見せる。
    /// ためすぎ（赤い所）と高い所は外れることがある。守る向きが合えば、弱い球ほど・低い球ほど止める。向きが違えば入る
    /// </summary>
    public sealed partial class SoccerShootout
    {
        // 待っている子を並べる所（真ん中の線の両側に、チームごとに縦に並ぶ）
        private const float WaitingX = 1.6f;
        private const float WaitingSpacing = 1.3f;

        // 決まった球はゴールの網の奥のこの手前で止まる
        private const float NetInset = 0.35f;

        private Vec2 _flightFrom;
        private Vec2 _flightTo;
        private float _flightHeight;
        private float _flightSeconds;
        private float _keeperTargetY;

        private SoccerGoal TargetGoal => _world.Pitch.GoalOf(KeeperTeam);

        private SoccerKeeper Keeper => _world.Keepers.Count > KeeperTeam ? _world.Keepers[KeeperTeam] : null;

        private Vec2 Spot
        {
            get
            {
                var goal = TargetGoal;
                return new Vec2(goal.LineX - goal.Outward * SoccerBalance.PenaltyDistance, 0f);
            }
        }

        /// <summary>蹴る子をボールの後ろに、ほかの子を真ん中の線の両側に並べ、キーパーを構えさせる</summary>
        private void StartKick(List<SoccerEvent> events)
        {
            Kicker = NextKicker(KickingTeam);
            AimSide = 0;
            AimHigh = false;
            Charge01 = 0f;
            IsCharging = false;
            _released = false;
            KeeperSide = 0;
            PlaceEveryone();
            Enter(SoccerShootoutPhase.Ready);
            events.Add(new SoccerEvent(SoccerEventKind.PenaltyReady, Kicker, KickingTeam, Spot, KickNumber));
        }

        private void PlaceEveryone()
        {
            var goal = TargetGoal;
            var spot = Spot;
            var waiting = new int[SoccerRules.TeamCount];
            foreach (var player in _world.Players)
            {
                player.ResetActions();
                player.Velocity = Vec2.Zero;
                if (player.Index == Kicker)
                {
                    player.Position = spot - new Vec2(goal.Outward * SoccerBalance.PenaltyRunUp, 0f);
                    player.Facing = new Vec2(goal.Outward, 0f);
                    continue;
                }

                var row = waiting[player.Team]++;
                var y = (row - 1) * WaitingSpacing;
                player.Position = new Vec2(-SoccerPitchDef.AttackDirection(player.Team) * WaitingX, y);
                player.Facing = new Vec2(goal.Outward, 0f);
            }

            foreach (var keeper in _world.Keepers)
            {
                keeper.Reset();
            }

            _world.Ball.Place(spot);
        }

        /// <summary>
        /// 蹴った瞬間に決まり方を決める: ためすぎ・高い所は外れることがある。外れなければ、守る向きが合っていれば止める割合で止める
        /// </summary>
        private void Kick(float power, SoccerInput keeperInput, List<SoccerEvent> events)
        {
            KeeperSide = SideOf(keeperInput.Move.X);
            IsCharging = false;
            Charge01 = power;
            var kicker = _world.Players[Kicker];
            kicker.ResetCharge();

            LastResult = Decide(power);
            _flightFrom = _world.Ball.Position;
            var height = AimHigh ? SoccerBalance.PenaltyHighHeight : 0f;
            switch (LastResult)
            {
                case SoccerPenaltyResult.Missed:
                    _flightTo = MissPoint(out height);
                    break;
                case SoccerPenaltyResult.Saved:
                    _flightTo = new Vec2(Keeper?.LineX ?? TargetGoal.LineX, TargetPoint(AimSide, height, false).Y);
                    break;
                default:
                    _flightTo = TargetPoint(AimSide, height, true);
                    break;
            }

            _flightHeight = height;
            var speed = SoccerBalance.PenaltyMinSpeed + (SoccerBalance.PenaltyMaxSpeed - SoccerBalance.PenaltyMinSpeed) * power;
            _flightSeconds = Math.Max(Dt, Vec2.Distance(_flightFrom, _flightTo) / speed);
            StartKeeperDive();
            Enter(SoccerShootoutPhase.Flight);
            events.Add(new SoccerEvent(SoccerEventKind.Kick, Kicker, KickingTeam, _flightFrom, power, AimHigh));
            events.Add(new SoccerEvent(SoccerEventKind.PenaltyKick, Kicker, KickingTeam, _flightFrom, power, AimHigh));
        }

        private SoccerPenaltyResult Decide(float power)
        {
            var miss = power > SoccerBalance.PenaltyRedZone
                ? (power - SoccerBalance.PenaltyRedZone) / (1f - SoccerBalance.PenaltyRedZone) * SoccerBalance.PenaltyMaxMissChance
                : 0f;
            miss += AimHigh ? SoccerBalance.PenaltyHighMissChance : 0f;
            if (_random.NextDouble() < miss)
            {
                return SoccerPenaltyResult.Missed;
            }

            return KeeperSide == AimSide && _random.NextDouble() < SaveChance(power, AimHigh)
                ? SoccerPenaltyResult.Saved
                : SoccerPenaltyResult.Goal;
        }

        /// <summary>守る向きが合ったときに止める割合（弱いほど・低いほど止める）</summary>
        public static float SaveChance(float power, bool high)
        {
            var chance = SoccerBalance.PenaltySaveBase - power * SoccerBalance.PenaltySavePowerWeight - (high ? SoccerBalance.PenaltyHighSaveDrop : 0f);
            return Math.Clamp(chance, SoccerBalance.PenaltyMinSave, SoccerBalance.PenaltyMaxSave);
        }

        /// <summary>
        /// 蹴る子から見た向き（−1 左・0 真ん中・1 右）の、ゴールの口の中の所。intoNet なら網の奥（決まった球の止まる所）、
        /// そうでなければゴールの線の上
        /// </summary>
        private Vec2 TargetPoint(int side, float height, bool intoNet)
        {
            var goal = TargetGoal;
            var x = intoNet ? goal.LineX + goal.Outward * (goal.Depth - NetInset) : goal.LineX;
            return new Vec2(x, SideToY(side) * (goal.HalfWidth - SoccerBalance.PenaltyPostInset));
        }

        /// <summary>蹴る子から見た左右を、ピッチの Y の向きへ（蹴る子の左手側は、攻める向きを左へ 90° 回した向き）</summary>
        private float SideToY(int side) => -side * SoccerPitchDef.AttackDirection(KickingTeam);

        /// <summary>外れた球の行く所: 左右を狙ったなら柱の外、真ん中・高い所ならバーの上</summary>
        private Vec2 MissPoint(out float height)
        {
            var goal = TargetGoal;
            var x = goal.LineX + goal.Outward * (goal.Depth - NetInset);
            if (AimSide != 0 && !AimHigh)
            {
                height = 0f;
                return new Vec2(x, SideToY(AimSide) * (goal.HalfWidth + SoccerBalance.PenaltyWideOffset));
            }

            height = SoccerBalance.PenaltyOverHeight;
            return new Vec2(goal.LineX, SideToY(AimSide) * (goal.HalfWidth - SoccerBalance.PenaltyPostInset));
        }

        /// <summary>守る側の選んだ向きへ飛ぶ（真ん中ならその場で構えたまま）</summary>
        private void StartKeeperDive()
        {
            var keeper = Keeper;
            if (keeper == null)
            {
                return;
            }

            var limit = TargetGoal.HalfWidth - SoccerBalance.KeeperGuardInset;
            _keeperTargetY = Math.Clamp(SideToY(KeeperSide) * (TargetGoal.HalfWidth - SoccerBalance.PenaltyPostInset), -limit, limit);
            keeper.WillSave = LastResult == SoccerPenaltyResult.Saved;
            if (KeeperSide == 0)
            {
                return;
            }

            keeper.DiveTargetY = _keeperTargetY;
            keeper.DiveSide = Math.Sign(_keeperTargetY);
            keeper.Enter(SoccerKeeperState.Dive);
        }

        /// <summary>ボールを狙った所へまっすぐ飛ばし、キーパーを飛ぶ先へ動かす。着いたら結果を出す</summary>
        private void StepFlight(List<SoccerEvent> events)
        {
            var t = Math.Min(1f, PhaseSeconds / _flightSeconds);
            var ball = _world.Ball;
            ball.Position = _flightFrom + (_flightTo - _flightFrom) * t;
            ball.Height = _flightHeight * t;
            ball.Velocity = (_flightTo - _flightFrom) / _flightSeconds;
            MoveKeeper();
            if (t < 1f)
            {
                return;
            }

            ball.Velocity = Vec2.Zero;
            Finish(events);
        }

        private void MoveKeeper()
        {
            var keeper = Keeper;
            if (keeper == null || keeper.State != SoccerKeeperState.Dive)
            {
                return;
            }

            var y = keeper.Position.Y;
            var step = Math.Clamp(_keeperTargetY - y, -SoccerBalance.PenaltyKeeperDiveSpeed * Dt, SoccerBalance.PenaltyKeeperDiveSpeed * Dt);
            keeper.Position = new Vec2(keeper.LineX, y + step);
            keeper.Speed = step / Dt;
        }

        private void Finish(List<SoccerEvent> events)
        {
            _results[KickingTeam].Add(LastResult);
            var goal = TargetGoal;
            var ball = _world.Ball;
            switch (LastResult)
            {
                case SoccerPenaltyResult.Goal:
                    events.Add(new SoccerEvent(SoccerEventKind.NetHit, SoccerBall.NoPlayer, goal.Team, ball.Position, SoccerBalance.PenaltyMaxSpeed));
                    events.Add(new SoccerEvent(SoccerEventKind.PenaltyGoal, Kicker, KickingTeam, ball.Position));
                    break;
                case SoccerPenaltyResult.Saved:
                    ball.KeeperTeam = KeeperTeam;
                    events.Add(new SoccerEvent(SoccerEventKind.KeeperCatch, Kicker, KeeperTeam, ball.Position));
                    events.Add(new SoccerEvent(SoccerEventKind.PenaltySave, Kicker, KickingTeam, ball.Position));
                    break;
                default:
                    events.Add(new SoccerEvent(SoccerEventKind.PenaltyMiss, Kicker, KickingTeam, ball.Position));
                    break;
            }

            Keeper?.Enter(Keeper.State == SoccerKeeperState.Dive ? SoccerKeeperState.Down : SoccerKeeperState.Guard);
            Enter(SoccerShootoutPhase.Result);
        }
    }
}
