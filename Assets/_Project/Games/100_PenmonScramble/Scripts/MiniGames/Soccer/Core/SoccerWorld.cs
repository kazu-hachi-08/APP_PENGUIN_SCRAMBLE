using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ピッチの上の計算（子の移動・ボールの転がり・壁の跳ね返り・浮いた球・持つ / 離す・シュート・ゴールの判定）。
    /// SoccerRules.StepSeconds ずつ進める（同じ入力なら同じ結果）。表示と音へは起きたこと（SoccerEvent）で渡す。
    /// 試合の流れ（時間・ゴールの演出・PK戦）はここに持たない（フェーズ 4 の SoccerMatch）。
    /// 点が入ったら Kickoff を呼ぶまでボールは誰も持てない（網の中で転がるだけ）。
    /// キーパーの動きと止める判定は SoccerWorld.Keeper.cs
    /// </summary>
    public sealed partial class SoccerWorld
    {
        private const float Dt = SoccerRules.StepSeconds;

        // 壁の線の真上にいるとき（押し出す向きが決められない）の扱いの境目
        private const float Epsilon = 1e-6f;

        private readonly SoccerPlayer[] _players;
        private readonly int[] _score = new int[SoccerRules.TeamCount];

        /// <param name="redPlayers">赤のフィールドの子の数（試しのピッチは 1）</param>
        /// <param name="bluePlayers">青のフィールドの子の数</param>
        /// <param name="keepers">両チームにキーパーを置くか（ボールの動きだけを確かめるテストでは置かない）</param>
        /// <param name="seed">キーパーが止めるかの乱数の種（オンラインではホストだけが計算する）</param>
        public SoccerWorld(SoccerPitchDef pitch, int redPlayers, int bluePlayers, bool keepers = false, int seed = 0)
        {
            Pitch = pitch;
            _random = new Random(seed);
            _keepers = keepers ? CreateKeepers(pitch) : new SoccerKeeper[0];
            _players = new SoccerPlayer[redPlayers + bluePlayers];
            for (var i = 0; i < _players.Length; i++)
            {
                _players[i] = new SoccerPlayer(i, i < redPlayers ? SoccerRules.Red : SoccerRules.Blue);
            }

            Kickoff(SoccerRules.Red, null);
        }

        public SoccerPitchDef Pitch { get; }

        public SoccerBall Ball { get; } = new SoccerBall();

        public IReadOnlyList<SoccerPlayer> Players => _players;

        /// <summary>始まってからの秒</summary>
        public float Time { get; private set; }

        /// <summary>点が入って、次のキックオフを待っている</summary>
        public bool IsGoalScored { get; private set; }

        public int Score(int team) => _score[team];

        /// <summary>全員をキックオフの並びに戻し、ボールを真ん中に置く</summary>
        /// <param name="events">null なら何も足さない（作った直後など）</param>
        public void Kickoff(int kickingTeam, List<SoccerEvent> events)
        {
            var countInTeam = new int[SoccerRules.TeamCount];
            foreach (var player in _players)
            {
                var index = countInTeam[player.Team]++;
                player.Position = Pitch.KickoffSpot(player.Team, index, player.Team == kickingTeam);
                player.Velocity = Vec2.Zero;
                player.Facing = new Vec2(SoccerPitchDef.AttackDirection(player.Team), 0f);
                player.ResetCharge();
            }

            foreach (var keeper in _keepers)
            {
                keeper.Reset();
            }

            Ball.Place(Vec2.Zero);
            Ball.LastToucher = SoccerBall.NoPlayer;
            IsGoalScored = false;
            _trajectoryChanged = true;
            events?.Add(new SoccerEvent(SoccerEventKind.Kickoff, SoccerBall.NoPlayer, kickingTeam, Vec2.Zero));
        }

        /// <summary>1歩進める。inputs は Players と同じ並び（足りない子は何もしない）。起きたことは events に足す</summary>
        public void Step(IReadOnlyList<SoccerInput> inputs, List<SoccerEvent> events)
        {
            Time += Dt;
            foreach (var player in _players)
            {
                MovePlayer(player, InputOf(inputs, player.Index));
            }

            StepKeepers(events);
            CarryBall(events);
            foreach (var player in _players)
            {
                UpdateShot(player, InputOf(inputs, player.Index), events);
            }

            if (!Ball.IsHeld)
            {
                StepFreeBall(events);
            }

            KeepersTouchBall(events);
            CheckGoal(events);
            TryPickup(events);
            UpdateIdle(events);
        }

        private static SoccerInput InputOf(IReadOnlyList<SoccerInput> inputs, int index)
        {
            return inputs != null && index < inputs.Count ? inputs[index] : SoccerInput.None;
        }

        /// <summary>
        /// 入力の向きへ歩く速さを近づける（急に止まる・曲がるときも少しだけなめらかに）。
        /// 種類の速さは効かせない。ボールを持っている子だけ少し遅い
        /// </summary>
        private void MovePlayer(SoccerPlayer player, SoccerInput input)
        {
            var move = input.Move.ClampLength(1f);
            var speed = SoccerBalance.RunSpeed * (Ball.Holder == player.Index ? SoccerBalance.CarrySpeedScale : 1f);
            var target = move * speed;
            var change = (target - player.Velocity).ClampLength(SoccerBalance.RunAccel * Dt);
            player.Velocity += change;
            player.Position += player.Velocity * Dt;
            if (!move.IsNearlyZero)
            {
                player.Facing = move.Normalized;
            }

            PushPlayerOutOfWalls(player);
            PushPlayerOutOfKeepers(player);
        }

        /// <summary>子は壁・ゴールの口・網のどれも通れない（ゴールの中へは入らない）</summary>
        private void PushPlayerOutOfWalls(SoccerPlayer player)
        {
            var position = player.Position;
            var velocity = player.Velocity;
            foreach (var wall in Pitch.Walls)
            {
                if (TryPushOut(wall, SoccerBalance.PlayerRadius, ref position, out var normal))
                {
                    var into = Vec2.Dot(velocity, normal);
                    if (into < 0f)
                    {
                        velocity -= normal * into;
                    }
                }
            }

            player.Position = position;
            player.Velocity = velocity;
        }

        /// <summary>
        /// 半径 radius の円が壁に食い込んでいれば外へ押し出す。normal は壁から円へ向かう向き。
        /// 円の中心が線の真上なら、外周の内向き（左回りの左手側）へ押す
        /// </summary>
        private static bool TryPushOut(SoccerWall wall, float radius, ref Vec2 position, out Vec2 normal)
        {
            var closest = MiniGameGeometry.ClosestOnSegment(wall.A, wall.B, position);
            var away = position - closest;
            var distanceSquared = away.LengthSquared;
            if (distanceSquared >= radius * radius)
            {
                normal = Vec2.Zero;
                return false;
            }

            var distance = MathF.Sqrt(distanceSquared);
            if (distance > Epsilon)
            {
                normal = away / distance;
            }
            else
            {
                var along = (wall.B - wall.A).Normalized;
                normal = new Vec2(-along.Y, along.X);
            }

            position = closest + normal * radius;
            return true;
        }
    }
}
