using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// スタジアムの仕掛け: 押し流す帯（水流・風）・間欠泉・壁のバンパー。氷の床は転がり（Roll）と歩き（Walk）の中で地面の種類から効かせる。
    /// どれも時刻（Time）だけで様子が決まるので、ゲストは届いた時刻から同じ様子を描ける。
    /// 必殺シュートの軌道とキーパー・子が持っているボールには、押し流す帯は効かない（間欠泉だけは足元のボールも跳ね上げる）
    /// </summary>
    public sealed partial class SoccerWorld
    {
        // 間欠泉ごとの、最後に跳ね上げた噴き出しの回（1回噴く間に1度だけ跳ね上げる）
        private long[] _geyserFired;

        /// <summary>離れたボールを、入っている帯の向きへ押す（押す向きに MaxSpeed より遅い間だけ）</summary>
        private void PushByFlows()
        {
            foreach (var flow in Pitch.Flows)
            {
                if (!flow.Contains(Ball.Position))
                {
                    continue;
                }

                var direction = flow.DirectionAt(Time);
                if (direction.IsNearlyZero)
                {
                    continue;
                }

                var scale = 1f;
                if (Ball.Height > SoccerBalance.CurrentMaxHeight)
                {
                    if (flow.Kind == SoccerFlowKind.Current)
                    {
                        continue;
                    }

                    scale = SoccerBalance.WindAirScale;
                }

                var limit = flow.MaxSpeed * scale;
                var along = Vec2.Dot(Ball.Velocity, direction);
                if (along < limit)
                {
                    Ball.Velocity += direction * Math.Min(flow.Accel * scale * Dt, limit - along);
                }
            }
        }

        /// <summary>帯の中の子が流される速さ（凍っている子は流されない）</summary>
        private Vec2 FlowDrift(SoccerPlayer player)
        {
            var drift = Vec2.Zero;
            if (player.State == SoccerPlayerState.Frozen)
            {
                return drift;
            }

            foreach (var flow in Pitch.Flows)
            {
                if (flow.Contains(player.Position))
                {
                    drift += flow.DirectionAt(Time) * SoccerBalance.PlayerDrift(flow.Kind);
                }
            }

            return drift;
        }

        /// <summary>
        /// 噴いている間欠泉の口の上にある地面のボールを跳ね上げる。持っている子の足元からも離す（ドリブルで踏み込むと取られる）。
        /// キーパーが持っている・必殺シュートが飛んでいる・点が入った後は何もしない
        /// </summary>
        private void EruptGeysers(List<SoccerEvent> events)
        {
            var geysers = Pitch.Geysers;
            if (geysers.Count == 0 || Ball.KeeperTeam != SoccerBall.NoTeam || Special != null || IsGoalScored || Ball.IsAirborne)
            {
                return;
            }

            _geyserFired ??= CreateFiredMarks(geysers.Count);
            for (var i = 0; i < geysers.Count; i++)
            {
                var geyser = geysers[i];
                if (!geyser.IsErupting(Time) || !geyser.Contains(Ball.Position) || _geyserFired[i] == geyser.Eruption(Time))
                {
                    continue;
                }

                _geyserFired[i] = geyser.Eruption(Time);
                Launch(geyser, events);
                return;
            }
        }

        private static long[] CreateFiredMarks(int count)
        {
            var marks = new long[count];
            for (var i = 0; i < count; i++)
            {
                marks[i] = long.MinValue;
            }

            return marks;
        }

        private void Launch(SoccerGeyser geyser, List<SoccerEvent> events)
        {
            var holder = Ball.Holder;
            if (holder != SoccerBall.NoPlayer)
            {
                // 持っていた子はためを捨てる。落ちてくるまでは、その子も含めて誰も取れない
                _players[holder].ResetCharge();
                Ball.Holder = SoccerBall.NoPlayer;
            }

            var away = Ball.Position - geyser.Center;
            // 口の真ん中なら、攻める向きに関係のない決まった向き（上）へ押す（同じ入力なら同じ結果に）
            var outward = away.IsNearlyZero ? new Vec2(0f, 1f) : away.Normalized;
            Ball.Velocity = Ball.Velocity * SoccerBalance.GeyserKeepSpeed + outward * SoccerBalance.GeyserPushSpeed;
            Ball.VerticalSpeed = SoccerBalance.GeyserUpSpeed;
            Ball.NoPickupSeconds = SoccerBalance.GeyserNoPickupSeconds;
            Ball.IdleSeconds = 0f;
            _trajectoryChanged = true;
            events.Add(new SoccerEvent(SoccerEventKind.GeyserLaunch, holder, -1, geyser.Center));
        }

        /// <summary>
        /// バンパーの跳ね返り: 壁に向かう速さ into を強めて返す（下限・上限あり）。壁に沿った速さはそのまま
        /// </summary>
        private static Vec2 BumperBounce(Vec2 velocity, Vec2 normal, float into)
        {
            var tangent = velocity + normal * into;
            var outSpeed = Math.Max(into * SoccerBalance.BumperBounce, SoccerBalance.BumperMinSpeed);
            return (tangent + normal * outSpeed).ClampLength(SoccerBalance.BumperMaxSpeed);
        }
    }
}
