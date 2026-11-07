using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// キーパー: ゴールの口の前を横に動き、シュートが来たら止めるか決めて飛びつく（SoccerKeeperSave）。
    /// 取ったら少し持ってから、近くの味方の足元へ投げる。止めないと決めた球が体に当たったらはじく。
    /// 止めるかはボールの道筋（蹴った・跳ね返った）ごとに1回だけ乱数で決める（同じ種なら同じ結果）
    /// </summary>
    public sealed partial class SoccerWorld
    {
        private readonly SoccerKeeper[] _keepers;
        private readonly Random _random;

        // ボールの道筋が変わった（蹴った・壁で跳ねた・はじいた・投げた）。キーパーは決め直す
        private bool _trajectoryChanged;

        public IReadOnlyList<SoccerKeeper> Keepers => _keepers;

        private static SoccerKeeper[] CreateKeepers(SoccerPitchDef pitch)
        {
            var keepers = new SoccerKeeper[SoccerRules.TeamCount];
            for (var team = 0; team < keepers.Length; team++)
            {
                var goal = pitch.GoalOf(team);
                keepers[team] = new SoccerKeeper(team, goal.LineX - goal.Outward * SoccerBalance.KeeperLineOffset, goal.Outward);
            }

            return keepers;
        }

        private void StepKeepers(List<SoccerEvent> events)
        {
            foreach (var keeper in _keepers)
            {
                keeper.StateSeconds += Dt;
                keeper.CatchLockSeconds = Math.Max(0f, keeper.CatchLockSeconds - Dt);
                if (_trajectoryChanged)
                {
                    keeper.Decided = false;
                }

                StepKeeper(keeper, events);
            }

            _trajectoryChanged = false;
        }

        private void StepKeeper(SoccerKeeper keeper, List<SoccerEvent> events)
        {
            switch (keeper.State)
            {
                case SoccerKeeperState.Guard:
                    if (!TryDive(keeper, events))
                    {
                        Guard(keeper);
                    }

                    break;
                case SoccerKeeperState.Dive:
                    Dive(keeper);
                    break;
                case SoccerKeeperState.Down:
                    keeper.Speed = 0f;
                    if (keeper.StateSeconds >= SoccerBalance.KeeperDownSeconds)
                    {
                        keeper.Enter(SoccerKeeperState.Guard);
                    }

                    break;
                case SoccerKeeperState.Hold:
                    HoldBall(keeper);
                    if (keeper.StateSeconds >= SoccerBalance.KeeperHoldSeconds)
                    {
                        Throw(keeper, events);
                    }

                    break;
            }
        }

        /// <summary>シュートが来ていればその通る所へ、来ていなければゴールの真ん中とボールの間へ寄る</summary>
        private void Guard(SoccerKeeper keeper)
        {
            var targetY = Predict(keeper, out var prediction) ? prediction.Y : Ball.Position.Y * SoccerBalance.KeeperFollowRatio;
            MoveAlongLine(keeper, targetY, SoccerBalance.KeeperGuardSpeed);
        }

        /// <summary>
        /// シュートがキーパーの線に近づいたら、止めるかを1回だけ決めて飛びつく。
        /// 止めると決めたら通る所へ、止めないと決めたら指先が届かない所へ（真正面なら反対へ）飛ぶ
        /// </summary>
        private bool TryDive(SoccerKeeper keeper, List<SoccerEvent> events)
        {
            if (keeper.Decided || !Predict(keeper, out var prediction) || prediction.Seconds > SoccerBalance.KeeperDiveLeadSeconds)
            {
                return false;
            }

            var capability = SoccerKeeperSave.DiveCapability(prediction.Seconds);
            var need = SoccerKeeperSave.Need(keeper.Position.Y, prediction.Y);
            var chance = Special != null
                ? SoccerBalance.KeeperSpecialSave(Special.Stars)
                : SoccerKeeperSave.Chance(capability, need, prediction.Height, Pitch.GoalOf(keeper.Team).BarHeight);

            keeper.Decided = true;
            keeper.WillSave = _random.NextDouble() < chance;
            keeper.DiveTargetY = ClampToLine(keeper, keeper.WillSave ? prediction.Y : MissTarget(keeper, prediction.Y));
            keeper.DiveSide = keeper.DiveTargetY >= keeper.Position.Y ? 1f : -1f;
            keeper.Speed = 0f;
            keeper.Enter(SoccerKeeperState.Dive);
            var at = new Vec2(keeper.LineX, keeper.DiveTargetY);
            events.Add(new SoccerEvent(SoccerEventKind.KeeperDive, SoccerBall.NoPlayer, keeper.Team, at, keeper.DiveSide, keeper.WillSave));
            return true;
        }

        /// <summary>止めないときの飛びつく先: 球の縁からすき間を空けた手前。球が体の正面なら、反対の側へ読み違える</summary>
        private float MissTarget(SoccerKeeper keeper, float ballY)
        {
            var offset = ballY - keeper.Position.Y;
            var clear = SoccerBalance.KeeperCatchRadius + SoccerBalance.BallRadius + SoccerBalance.KeeperMissGap;
            if (Math.Abs(offset) < clear)
            {
                var side = _random.NextDouble() < 0.5 ? -1f : 1f;
                return keeper.Position.Y + side * SoccerBalance.KeeperDiveReach;
            }

            return keeper.Position.Y + Math.Sign(offset) * (Math.Abs(offset) - clear);
        }

        /// <summary>
        /// 気づくまでは動かず、その後は飛びつく先へ速く動く。止めると決めた球には間に合う速さにする
        /// （運よく止める球は、速さを上げたファインセーブに見せる）。着いたか長くたったら倒れる
        /// </summary>
        private void Dive(SoccerKeeper keeper)
        {
            if (keeper.StateSeconds >= SoccerBalance.KeeperReactSeconds)
            {
                MoveAlongLine(keeper, keeper.DiveTargetY, DiveSpeed(keeper));
            }

            var arrived = Math.Abs(keeper.DiveTargetY - keeper.Position.Y) <= Epsilon;
            if ((arrived && keeper.StateSeconds >= SoccerBalance.KeeperReactSeconds) || keeper.StateSeconds >= SoccerBalance.KeeperDiveMaxSeconds)
            {
                keeper.Speed = 0f;
                keeper.Enter(SoccerKeeperState.Down);
            }
        }

        private float DiveSpeed(SoccerKeeper keeper)
        {
            if (!keeper.WillSave || !Predict(keeper, out var prediction))
            {
                return SoccerBalance.KeeperDiveSpeed;
            }

            var distance = Math.Abs(keeper.DiveTargetY - keeper.Position.Y);
            return Math.Max(SoccerBalance.KeeperDiveSpeed, distance / Math.Max(Dt, prediction.Seconds));
        }

        private void MoveAlongLine(SoccerKeeper keeper, float targetY, float speed)
        {
            var target = ClampToLine(keeper, targetY);
            var step = Math.Clamp(target - keeper.Position.Y, -speed * Dt, speed * Dt);
            keeper.Position = new Vec2(keeper.LineX, keeper.Position.Y + step);
            keeper.Speed = step / Dt;
        }

        private float ClampToLine(SoccerKeeper keeper, float y)
        {
            var limit = Pitch.GoalOf(keeper.Team).HalfWidth - SoccerBalance.KeeperGuardInset;
            return Math.Clamp(y, -limit, limit);
        }

        /// <summary>離れたボールがこのキーパーの守る口へ向かっていれば、その見込み（必殺シュートは飛び方を先へ進めて読む）</summary>
        private bool Predict(SoccerKeeper keeper, out SoccerShotPrediction prediction)
        {
            prediction = default;
            if (Ball.IsHeld || IsGoalScored)
            {
                return false;
            }

            if (Special != null)
            {
                return PredictSpecial(keeper, out prediction);
            }

            var decel = SoccerBalance.RollDecel * SoccerBalance.RollDecelScale(Pitch.GroundAt(Ball.Position));
            return SoccerKeeperSave.TryPredict(Ball, keeper.LineX, Pitch.GoalOf(keeper.Team), decel, out prediction);
        }

        /// <summary>
        /// ボールが手の届く所に来たら取る。止めないと決めた速い球は取らず、体に当たればはじく。
        /// ゴールへ運び込もうとする子の足元のボールも取る（キーパーはスライディングされない代わりに、近づきすぎると取られる）
        /// </summary>
        private void KeepersTouchBall(List<SoccerEvent> events)
        {
            foreach (var keeper in _keepers)
            {
                if (Ball.KeeperTeam != SoccerBall.NoTeam || IsGoalScored)
                {
                    return;
                }

                if (keeper.State == SoccerKeeperState.Hold || keeper.CatchLockSeconds > 0f || Ball.Height > SoccerBalance.KeeperReachHeight)
                {
                    continue;
                }

                var distance = Vec2.Distance(keeper.Position, Ball.Position);
                if (distance <= SoccerBalance.KeeperCatchRadius + SoccerBalance.BallRadius && MayCatch(keeper))
                {
                    Catch(keeper, events);
                }
                else if (!Ball.IsHeld && distance < SoccerBalance.KeeperRadius + SoccerBalance.BallRadius)
                {
                    Parry(keeper, events);
                }
            }
        }

        private bool MayCatch(SoccerKeeper keeper)
        {
            if (Ball.IsHeldByPlayer)
            {
                // 味方が持っているボールは取らない
                return keeper.State != SoccerKeeperState.Down && _players[Ball.Holder].Team != keeper.Team;
            }

            var gaveUp = keeper.Decided && !keeper.WillSave;
            return !gaveUp || Ball.Velocity.Length <= SoccerBalance.KeeperEasyCatchSpeed;
        }

        private void Catch(SoccerKeeper keeper, List<SoccerEvent> events)
        {
            var from = Ball.Holder;
            if (from != SoccerBall.NoPlayer)
            {
                _players[from].ResetCharge();
            }

            Ball.Holder = SoccerBall.NoPlayer;
            Ball.KeeperTeam = keeper.Team;
            Ball.Velocity = Vec2.Zero;
            Ball.VerticalSpeed = 0f;
            Ball.IdleSeconds = 0f;
            EndSpecial();
            keeper.Speed = 0f;
            keeper.Enter(SoccerKeeperState.Hold);
            HoldBall(keeper);
            events.Add(new SoccerEvent(SoccerEventKind.KeeperCatch, from, keeper.Team, Ball.Position));
        }

        /// <summary>体（丸）で跳ね返す。キーパーの体はボールの当たりでは動かない</summary>
        private void Parry(SoccerKeeper keeper, List<SoccerEvent> events)
        {
            var away = Ball.Position - keeper.Position;
            var normal = away.IsNearlyZero ? keeper.Facing : away.Normalized;
            var into = -Vec2.Dot(Ball.Velocity, normal);
            Ball.Position = keeper.Position + normal * (SoccerBalance.KeeperRadius + SoccerBalance.BallRadius);
            if (into <= 0f)
            {
                return;
            }

            Ball.Velocity += normal * (into * (1f + SoccerBalance.KeeperParryBounce));
            _trajectoryChanged = true;
            EndSpecial();
            events.Add(new SoccerEvent(SoccerEventKind.KeeperParry, SoccerBall.NoPlayer, keeper.Team, Ball.Position, into));
        }

        /// <summary>持っているボールは体の前・胸の高さ（ピッチの中の向き）</summary>
        private void HoldBall(SoccerKeeper keeper)
        {
            Ball.Position = keeper.Position + keeper.Facing * SoccerBalance.KeeperHoldDistance;
            Ball.Height = SoccerBalance.KeeperHoldHeight;
            Ball.Velocity = Vec2.Zero;
        }

        /// <summary>一番近い味方の足元へ、着いたときに少し勢いが残る速さで転がす。味方がいなければ前の、相手のいない側へ</summary>
        private void Throw(SoccerKeeper keeper, List<SoccerEvent> events)
        {
            var receiver = NearestOfTeam(keeper.Position, keeper.Team);
            var target = receiver != null ? receiver.Position : FallbackThrowTarget(keeper);
            var toTarget = target - keeper.Position;
            var direction = toTarget.IsNearlyZero ? keeper.Facing : toTarget.Normalized;
            var arrive = MathF.Sqrt(2f * SoccerBalance.RollDecel * toTarget.Length) + SoccerBalance.KeeperThrowArriveSpeed;
            var speed = Math.Clamp(arrive, SoccerBalance.KeeperThrowMinSpeed, SoccerBalance.KeeperThrowMaxSpeed);

            Ball.KeeperTeam = SoccerBall.NoTeam;
            Ball.Position = keeper.Position + direction * (SoccerBalance.KeeperRadius + SoccerBalance.BallRadius);
            Ball.Height = 0f;
            Ball.Velocity = direction * speed;
            Ball.IdleSeconds = 0f;
            keeper.CatchLockSeconds = SoccerBalance.KeeperThrowLockSeconds;
            keeper.Decided = false;
            keeper.Enter(SoccerKeeperState.Guard);
            _trajectoryChanged = true;
            events.Add(new SoccerEvent(SoccerEventKind.KeeperThrow, receiver?.Index ?? SoccerBall.NoPlayer, keeper.Team, Ball.Position));
        }

        private Vec2 FallbackThrowTarget(SoccerKeeper keeper)
        {
            var opponent = NearestOfTeam(keeper.Position, SoccerRules.Opponent(keeper.Team));
            var side = opponent != null && opponent.Position.Y > 0f ? -1f : 1f;
            var forward = keeper.Facing * SoccerBalance.KeeperThrowFallbackDistance;
            return keeper.Position + forward + new Vec2(0f, side * SoccerBalance.KeeperThrowFallbackSide);
        }

        private SoccerPlayer NearestOfTeam(Vec2 position, int team)
        {
            SoccerPlayer best = null;
            var bestDistance = float.MaxValue;
            foreach (var player in _players)
            {
                var distance = (player.Position - position).LengthSquared;
                if (player.Team == team && distance < bestDistance)
                {
                    best = player;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>ペンモンはキーパーを通り抜けない（キーパーは押されて動かない）</summary>
        private void PushPlayerOutOfKeepers(SoccerPlayer player)
        {
            foreach (var keeper in _keepers)
            {
                var away = player.Position - keeper.Position;
                var reach = SoccerBalance.KeeperRadius + SoccerBalance.PlayerRadius;
                if (away.LengthSquared >= reach * reach)
                {
                    continue;
                }

                var normal = away.IsNearlyZero ? keeper.Facing : away.Normalized;
                player.Position = keeper.Position + normal * reach;
                var into = Vec2.Dot(player.Velocity, normal);
                if (into < 0f)
                {
                    player.Velocity -= normal * into;
                }
            }
        }
    }
}
