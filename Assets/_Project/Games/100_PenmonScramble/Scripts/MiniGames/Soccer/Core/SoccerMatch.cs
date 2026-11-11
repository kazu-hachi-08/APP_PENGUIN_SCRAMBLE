using System;
using System.Collections.Generic;

namespace PenmonScramble.Soccer.Core
{
    public enum SoccerMatchPhase
    {
        /// <summary>キックオフの並びで「キックオフ！」を見せている（ボールに触れない・時間は止まる）</summary>
        Kickoff,
        /// <summary>試合中（時間が減る。カットインの間だけ止まる）</summary>
        Playing,
        /// <summary>点が入って「ゴール！」を見せている（ボールは網へ転がる・時間は止まる）</summary>
        GoalPause,
        /// <summary>時間切れの笛を見せている</summary>
        TimeUp,
        /// <summary>同点で PK戦</summary>
        Shootout,
        Finished,
    }

    /// <summary>
    /// 1試合の流れ: キックオフ → 3分 → 時間切れ。点が入ったら「ゴール！」の間をおいて、
    /// 取られた側のキックオフで並び直す。時間はボールが動いている間も止まらず、演出（キックオフの合図・ゴール・カットイン）の間だけ止まる。
    /// 時間切れで同点なら PK戦（SoccerShootout）。ピッチの上の計算は SoccerWorld、子の入力（人・AI）は呼ぶ側が決めて渡す
    /// </summary>
    public sealed partial class SoccerMatch
    {
        private const float Dt = SoccerRules.StepSeconds;

        private readonly int _seed;
        private int _nextKickoff;
        private int _shownSeconds;

        /// <param name="seed">PK戦の乱数の種（キーパーの乱数は SoccerWorld の種）</param>
        public SoccerMatch(SoccerWorld world, int seed, float seconds = SoccerRules.MatchSeconds)
        {
            World = world;
            _seed = seed;
            TimeLeft = seconds;
            _shownSeconds = (int)Math.Ceiling(seconds);
            Winner = -1;
            Phase = SoccerMatchPhase.Kickoff;
        }

        public SoccerWorld World { get; }

        public SoccerMatchPhase Phase { get; private set; }

        /// <summary>今の段になってからの秒</summary>
        public float PhaseSeconds { get; private set; }

        /// <summary>残りの秒（0 で時間切れ）</summary>
        public float TimeLeft { get; private set; }

        /// <summary>PK戦。始まるまで null</summary>
        public SoccerShootout Shootout { get; private set; }

        /// <summary>勝ったチーム。終わるまで −1</summary>
        public int Winner { get; private set; }

        /// <summary>PK戦で決まった</summary>
        public bool WonByShootout { get; private set; }

        public bool IsFinished => Phase == SoccerMatchPhase.Finished;

        /// <summary>子が動ける（人の切り替え・AI の考えを進める）</summary>
        public bool IsPlaying => Phase == SoccerMatchPhase.Playing;

        /// <summary>1本目のキックオフ（赤）の並びから始める</summary>
        public void Begin(List<SoccerEvent> events)
        {
            _nextKickoff = SoccerRules.Red;
            World.Kickoff(SoccerRules.Red, events);
            Enter(SoccerMatchPhase.Kickoff);
        }

        /// <summary>1歩進める。inputs は World.Players と同じ並び（試合中は子の入力、PK戦では SoccerShootout が読む）</summary>
        public void Step(IReadOnlyList<SoccerInput> inputs, List<SoccerEvent> events)
        {
            PhaseSeconds += Dt;
            switch (Phase)
            {
                case SoccerMatchPhase.Kickoff:
                    if (PhaseSeconds >= SoccerRules.KickoffWaitSeconds)
                    {
                        Enter(SoccerMatchPhase.Playing);
                        events.Add(new SoccerEvent(SoccerEventKind.PlayStart, SoccerBall.NoPlayer, _nextKickoff, World.Ball.Position));
                    }

                    break;
                case SoccerMatchPhase.Playing:
                    StepPlaying(inputs, events);
                    break;
                case SoccerMatchPhase.GoalPause:
                    // 入れたボールが網へ転がるのを見せる（子は止まっていく）
                    World.Step(null, events);
                    if (PhaseSeconds >= SoccerRules.GoalPauseSeconds)
                    {
                        AfterGoal(events);
                    }

                    break;
                case SoccerMatchPhase.TimeUp:
                    if (PhaseSeconds >= SoccerRules.TimeUpSeconds)
                    {
                        AfterTimeUp(events);
                    }

                    break;
                case SoccerMatchPhase.Shootout:
                    Shootout.Step(inputs, events);
                    if (Shootout.IsFinished)
                    {
                        Finish(Shootout.Winner, true, events);
                    }

                    break;
            }
        }

        /// <summary>試合を飛ばして、いきなり PK戦から始める（開発用の PK戦の確認）</summary>
        public void StartShootoutNow(List<SoccerEvent> events) => BeginShootout(events);

        private void StepPlaying(IReadOnlyList<SoccerInput> inputs, List<SoccerEvent> events)
        {
            var frozen = World.IsCutIn;
            var blue = World.Score(SoccerRules.Blue);
            World.Step(inputs, events);
            if (!frozen)
            {
                TickClock(events);
            }

            if (World.IsGoalScored)
            {
                // 取られた側が次のキックオフ
                _nextKickoff = World.Score(SoccerRules.Blue) > blue ? SoccerRules.Red : SoccerRules.Blue;

                Enter(SoccerMatchPhase.GoalPause);
                return;
            }

            if (TimeLeft <= 0f)
            {
                BeginTimeUp(events);
            }
        }

        /// <summary>時間を減らし、残り CountdownFrom 秒からは1秒ごとに知らせる</summary>
        private void TickClock(List<SoccerEvent> events)
        {
            TimeLeft = Math.Max(0f, TimeLeft - Dt);
            var shown = (int)Math.Ceiling(TimeLeft);
            if (shown == _shownSeconds)
            {
                return;
            }

            _shownSeconds = shown;
            if (shown > 0 && shown <= SoccerRules.CountdownFrom)
            {
                events.Add(new SoccerEvent(SoccerEventKind.Countdown, SoccerBall.NoPlayer, -1, World.Ball.Position, shown));
            }
        }

        private void AfterGoal(List<SoccerEvent> events)
        {
            if (TimeLeft <= 0f)
            {
                BeginTimeUp(events);
                return;
            }

            World.Kickoff(_nextKickoff, events);
            Enter(SoccerMatchPhase.Kickoff);
        }

        private void BeginTimeUp(List<SoccerEvent> events)
        {
            Enter(SoccerMatchPhase.TimeUp);
            events.Add(new SoccerEvent(SoccerEventKind.TimeUp, SoccerBall.NoPlayer, -1, World.Ball.Position));
        }

        private void AfterTimeUp(List<SoccerEvent> events)
        {
            var red = World.Score(SoccerRules.Red);
            var blue = World.Score(SoccerRules.Blue);
            if (red == blue)
            {
                BeginShootout(events);
                return;
            }

            Finish(red > blue ? SoccerRules.Red : SoccerRules.Blue, false, events);
        }

        private void BeginShootout(List<SoccerEvent> events)
        {
            Shootout = new SoccerShootout(World, _seed);
            Enter(SoccerMatchPhase.Shootout);
            events.Add(new SoccerEvent(SoccerEventKind.ShootoutStart, SoccerBall.NoPlayer, -1, World.Ball.Position));
            Shootout.Begin(events);
        }

        private void Finish(int winner, bool byShootout, List<SoccerEvent> events)
        {
            Winner = winner;
            WonByShootout = byShootout;
            Enter(SoccerMatchPhase.Finished);
            events.Add(new SoccerEvent(SoccerEventKind.MatchEnd, SoccerBall.NoPlayer, winner, World.Ball.Position, 0f, byShootout));
        }

        private void Enter(SoccerMatchPhase phase)
        {
            Phase = phase;
            PhaseSeconds = 0f;
        }
    }
}
