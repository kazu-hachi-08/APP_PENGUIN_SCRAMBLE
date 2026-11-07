using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// 人1人が動かす子の切り替え。チームの3体のうち1体を動かし、残りは AI が動かす。
    /// - 味方がボールを持ったら、その子へすぐ替わる（パスしたら受ける子へ・キーパーが投げたら受ける子へ）
    /// - 自分のチームが持っていないときは、ボールに一番早く着ける味方へ自動で替わる（替わり続けてガタつかないよう、
    ///   替わってから SwitchHoldSeconds は替わらず、今の子より SwitchMarginSeconds 以上早い子にだけ替わる。
    ///   自分が出したパスが転がっている間は替えない（受ける子を動かしているのに、出した子へ戻らないように）
    /// - 交代ボタンで、今の子を除いてボールに一番近い味方へ替わる
    /// 人が動かしている子（オンラインで同じチームのほかの人）へは替わらない。
    /// 替わった瞬間に押していたボタンは、離すまで新しい子へ届けない（パスのボタンでそのまま受けた子がダッシュしないように）
    /// </summary>
    public sealed class SoccerControl
    {
        private readonly Func<int, bool> _takenByOther;
        private float _sinceSwitch;
        private float _passFlight = -1f;
        private bool _maskShoot;
        private bool _maskPass;

        /// <param name="takenByOther">ほかの人が動かしている子なら true（ひとりなら null）</param>
        public SoccerControl(int team, int controlled, Func<int, bool> takenByOther = null)
        {
            Team = team;
            Controlled = controlled;
            _takenByOther = takenByOther ?? (_ => false);
        }

        public int Team { get; }

        /// <summary>動かしている子の番号（SoccerWorld.Players の番号）</summary>
        public int Controlled { get; private set; }

        /// <summary>交代ボタンで次に替わる子（ボタンに顔を出す）。替われる子がいなければ −1</summary>
        public int NextSwitchTarget(SoccerWorld world) => NearestMate(world, Controlled)?.Index ?? SoccerBall.NoPlayer;

        /// <summary>
        /// その歩の前に呼ぶ。前の歩で起きたこと（パス・キーパーの投げ）と今のボールの位置から動かす子を決め、
        /// 人の入力を動かす子への入力にして返す
        /// </summary>
        public SoccerInput Update(SoccerWorld world, SoccerInput raw, bool switchPressed, IReadOnlyList<SoccerEvent> lastEvents)
        {
            _sinceSwitch += SoccerRules.StepSeconds;
            UpdatePassFlight(world);
            var next = ChooseControlled(world, switchPressed, lastEvents);
            if (next != Controlled)
            {
                SwitchTo(next, raw);
            }

            _maskShoot &= raw.Shoot;
            _maskPass &= raw.Pass;
            return new SoccerInput(raw.Move, raw.Shoot && !_maskShoot, raw.Pass && !_maskPass, raw.Special);
        }

        /// <summary>
        /// オンラインのゲストの写し: ホストが決めた動かす子に合わせる（ボタンの中身・交代の顔を出すため。切り替えはホストが決める）
        /// </summary>
        public void Follow(int controlled)
        {
            if (controlled != SoccerBall.NoPlayer)
            {
                Controlled = controlled;
            }
        }

        private int ChooseControlled(SoccerWorld world, bool switchPressed, IReadOnlyList<SoccerEvent> lastEvents)
        {
            var handed = HandedTo(world, lastEvents, out var byPass);
            if (handed != SoccerBall.NoPlayer)
            {
                _passFlight = byPass ? 0f : -1f;
                return handed;
            }

            var holder = world.Ball.Holder;
            if (holder != SoccerBall.NoPlayer && world.Players[holder].Team == Team)
            {
                return CanTake(holder) ? holder : Controlled;
            }

            if (switchPressed)
            {
                return NearestMate(world, Controlled)?.Index ?? Controlled;
            }

            return AutoSwitch(world);
        }

        /// <summary>前の歩で味方へパスした・自分のキーパーが投げた、その受ける子</summary>
        private int HandedTo(SoccerWorld world, IReadOnlyList<SoccerEvent> lastEvents, out bool byPass)
        {
            byPass = false;
            if (lastEvents == null)
            {
                return SoccerBall.NoPlayer;
            }

            foreach (var e in lastEvents)
            {
                var pass = e.Kind == SoccerEventKind.Pass && e.Player == Controlled && e.Other != SoccerBall.NoPlayer;
                var thrown = e.Kind == SoccerEventKind.KeeperThrow && e.Team == Team && e.Player != SoccerBall.NoPlayer;
                var to = pass ? e.Other : thrown ? e.Player : SoccerBall.NoPlayer;
                if (to != SoccerBall.NoPlayer && CanTake(to) && world.Players[to].Team == Team)
                {
                    byPass = pass;
                    return to;
                }
            }

            return SoccerBall.NoPlayer;
        }

        /// <summary>自分が出したパスは、誰かが持つまで（長くても SwitchPassFlightSeconds）転がっている扱いにする</summary>
        private void UpdatePassFlight(SoccerWorld world)
        {
            if (_passFlight < 0f)
            {
                return;
            }

            _passFlight += SoccerRules.StepSeconds;
            if (world.Ball.IsHeld || _passFlight >= SoccerBalance.SwitchPassFlightSeconds)
            {
                _passFlight = -1f;
            }
        }

        /// <summary>
        /// 自分のチームが持っていない間、ボールに一番早く着ける味方へ替わる。
        /// すべっている間・自分のパスが転がっている間・味方のキーパーが持っている間は替えない
        /// </summary>
        private int AutoSwitch(SoccerWorld world)
        {
            var current = world.Players[Controlled];
            var busy = current.State == SoccerPlayerState.Slide || _passFlight >= 0f || world.Ball.KeeperTeam == Team;
            if (_sinceSwitch < SoccerBalance.SwitchHoldSeconds || busy)
            {
                return Controlled;
            }

            var nearest = NearestMate(world, SoccerBall.NoPlayer);
            if (nearest == null || nearest.Index == Controlled)
            {
                return Controlled;
            }

            var sooner = ReachScore(world, current) - ReachScore(world, nearest);
            return sooner >= SoccerBalance.SwitchMarginSeconds ? nearest.Index : Controlled;
        }

        /// <summary>ボールに一番早く着ける、替われる味方（except は除く）</summary>
        private SoccerPlayer NearestMate(SoccerWorld world, int except)
        {
            SoccerPlayer best = null;
            var bestScore = float.MaxValue;
            foreach (var player in world.Players)
            {
                if (player.Team != Team || player.Index == except || (player.Index != Controlled && !CanTake(player.Index)))
                {
                    continue;
                }

                var score = ReachScore(world, player);
                if (score < bestScore)
                {
                    best = player;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>ボールに着くまでの秒。転んでいる子は起き上がるまで動けないので後回しにする</summary>
        private static float ReachScore(SoccerWorld world, SoccerPlayer player)
        {
            var ball = world.Ball;
            var seconds = SoccerReach.Seconds(world.Pitch, ball.Position, ball.Velocity, ball.IsHeld, player.Position);
            return seconds + (player.IsDown ? SoccerBalance.SwitchDownPenaltySeconds : 0f);
        }

        private bool CanTake(int index) => index == Controlled || !_takenByOther(index);

        private void SwitchTo(int index, SoccerInput raw)
        {
            Controlled = index;
            _sinceSwitch = 0f;
            _maskShoot = raw.Shoot;
            _maskPass = raw.Pass;
        }
    }
}
