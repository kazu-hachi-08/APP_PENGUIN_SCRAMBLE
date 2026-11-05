using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    public enum GolfHolePhase
    {
        /// <summary>打順の人が狙っている（持ち時間が減る）</summary>
        Aiming,
        /// <summary>球が転がっている（全部止まるまで待つ）</summary>
        Rolling,
        /// <summary>全員が終わった</summary>
        Finished,
    }

    /// <summary>
    /// 1ホールの進行（打順・打数・持ち時間・上限・カップイン・池の戻し）。
    /// 作った時点で最初の人の番が始まっている。表示と音へは Happened で起きたことを渡す
    /// </summary>
    public sealed partial class GolfHoleWorld
    {
        // 描画が止まった後などに、溜まった時間を一気に進めて固まらないようにする上限
        private const float MaxCatchUpSeconds = 0.25f;

        private readonly GolfBallSim _sim;
        private readonly List<GolfBall> _balls = new List<GolfBall>();
        private readonly int[] _strokes;
        private readonly bool[] _done;
        private IReadOnlyList<int> _order;
        private readonly List<GolfEvent> _stepEvents = new List<GolfEvent>();
        private int _orderIndex;
        private float _accumulator;
        private float _rollSeconds;

        public event Action<GolfEvent> Happened;

        public GolfHoleDef Hole { get; }
        public IReadOnlyList<GolfBall> Balls => _balls;
        public int PlayerCount => _balls.Count;
        public GolfHolePhase Phase { get; private set; }

        /// <summary>いま打つ（または打った球が転がっている）人</summary>
        public int CurrentPlayer => _order[_orderIndex];

        /// <summary>このホールの打つ順（席の番号）</summary>
        public IReadOnlyList<int> TurnOrder => _order;

        /// <summary>いまの人が向いている向き（ラジアン。0 が +X、左回り）。持ち時間切れの自動の1打もこの向き</summary>
        public float AimAngle { get; private set; }

        public float ShotTimeLeft { get; private set; }

        /// <summary>
        /// ホールが始まってからの秒。動く物・間欠泉はこの時刻だけで決まる（オンラインはホストの時刻を写す）。
        /// 転がっている間は球と同じ決まった刻みで進む
        /// </summary>
        public float Time { get; private set; }

        /// <param name="turnOrder">打つ順の席の番号（0 〜 人数−1 を1回ずつ）。GolfTurnOrder で作る</param>
        /// <param name="absent">もう抜けた席（オンライン）。球を置かず、はじめから終わった扱いにする</param>
        public GolfHoleWorld(GolfHoleDef hole, IReadOnlyList<int> turnOrder, IReadOnlyCollection<int> absent = null)
        {
            ValidateOrder(turnOrder);
            Hole = hole;
            _order = turnOrder;
            _sim = new GolfBallSim(hole);
            _strokes = new int[turnOrder.Count];
            _done = new bool[turnOrder.Count];
            for (var player = 0; player < turnOrder.Count; player++)
            {
                _balls.Add(new GolfBall(player, TeePosition(hole, player, turnOrder.Count)));
            }

            foreach (var seat in absent ?? Array.Empty<int>())
            {
                if (seat >= 0 && seat < turnOrder.Count)
                {
                    _done[seat] = true;
                    _balls[seat].Active = false;
                }
            }

            BeginTurn(FirstWaiting());
        }

        /// <summary>コースの確かめ用: 球1つを from に置き、時刻 time から始める（GolfCourseCheck）</summary>
        internal static GolfHoleWorld ForCheck(GolfHoleDef hole, Vec2 from, float time)
        {
            var world = new GolfHoleWorld(hole, new[] { 0 });
            world._balls[0].Place(from);
            world._balls[0].ReturnPoint = from;
            world.Time = time;
            return world;
        }

        /// <summary>最後の1打が転がった秒（上限まで転がったか、をコースの確かめで見る）</summary>
        internal float LastRollSeconds => _rollSeconds;

        /// <summary>打つ順で最初の、まだ終わっていない人の並びの位置（全員終わっていれば 0）</summary>
        private int FirstWaiting()
        {
            for (var index = 0; index < _order.Count; index++)
            {
                if (!_done[_order[index]])
                {
                    return index;
                }
            }

            return 0;
        }

        /// <summary>
        /// 抜けた人（オンラインの切断）の球を消して、打順から外す。狙っている途中ならその場で次の人へ。
        /// 転がっている途中なら、止まってから次の人へ（打った球の結果は残す）
        /// </summary>
        public void RemovePlayer(int player)
        {
            if (player < 0 || player >= PlayerCount || !_balls[player].Active && _done[player])
            {
                return;
            }

            _balls[player].Stop();
            _balls[player].Active = false;
            _done[player] = true;
            if (Phase != GolfHolePhase.Aiming || player != CurrentPlayer)
            {
                return;
            }

            if (Array.TrueForAll(_done, done => done))
            {
                FinishHole();
                return;
            }

            NextTurn();
        }

        public int Strokes(int player) => _strokes[player];

        /// <summary>このホールを終えた（カップに入った・上限で終わった）</summary>
        public bool IsDone(int player) => _done[player];

        public bool HoledOut(int player) => _balls[player].InCup;

        /// <summary>ティーに全員の球を並べる位置。カップの方向と直角に少しずつずらす（重ならないように）</summary>
        public static Vec2 TeePosition(GolfHoleDef hole, int player, int playerCount)
        {
            var forward = (hole.Cup - hole.Tee).Normalized;
            var side = new Vec2(forward.Y, -forward.X);
            return hole.Tee + side * ((player - (playerCount - 1) * 0.5f) * GolfRules.TeeSpacing);
        }

        /// <summary>球からカップを向く角度（番の始めの向き）</summary>
        public float AngleToCup(int player)
        {
            var toCup = Hole.Cup - _balls[player].Position;
            return MathF.Atan2(toCup.Y, toCup.X);
        }

        public static Vec2 Direction(float radians) => new Vec2(MathF.Cos(radians), MathF.Sin(radians));

        /// <summary>ゲージの強さ（0〜1）から打ち出す速さ</summary>
        public static float ShotSpeed(float power)
        {
            var t = Math.Clamp(power, 0f, 1f);
            return GolfBalance.MinShotSpeed + (GolfBalance.MaxShotSpeed - GolfBalance.MinShotSpeed) * t;
        }

        /// <summary>狙う向きを変える（狙っている間だけ）</summary>
        public void SetAim(float radians)
        {
            if (Phase == GolfHolePhase.Aiming)
            {
                AimAngle = radians;
            }
        }

        /// <summary>打つ。番でない人・狙っている間でないときは何もせず false</summary>
        public bool Shoot(int player, float radians, float power) => Shoot(player, radians, power, GolfSpecial.None);

        /// <summary>特殊ショットつきで打つ（1コース1回の管理は GolfRound。ここでは渡された効き目を載せるだけ）</summary>
        public bool Shoot(int player, float radians, float power, GolfSpecial special)
        {
            if (Phase != GolfHolePhase.Aiming || player != CurrentPlayer)
            {
                return false;
            }

            AimAngle = radians;
            // 打った球に弾かれて池に落ちた球も、打つ前の位置へ戻せるよう、全部の球の位置を覚える
            foreach (var ball in _balls)
            {
                ball.ReturnPoint = ball.Position;
            }

            var shooter = _balls[player];
            shooter.Launch(Direction(radians) * ShotSpeed(power));
            _strokes[player]++;
            Phase = GolfHolePhase.Rolling;
            _accumulator = 0f;
            _rollSeconds = 0f;
            Raise(new GolfEvent(GolfEventKind.Shot, player, shooter.Position, Math.Clamp(power, 0f, 1f)));
            if (!special.IsNone)
            {
                shooter.ApplySpecial(special);
                Raise(new GolfEvent(GolfEventKind.Special, player, shooter.Position, special.Stars, (int)special.Kind));
            }

            return true;
        }

        /// <summary>いまの人が雷の加速を使える（打った球がまだ転がっていて、まだ使っていない）</summary>
        public bool CanBoost(int player)
        {
            if (Phase != GolfHolePhase.Rolling || player != CurrentPlayer)
            {
                return false;
            }

            var ball = _balls[player];
            return ball.BoostReady && ball.IsInPlay && ball.Velocity.Length >= GolfSpecialBalance.BoostMinSpeed;
        }

        /// <summary>雷の加速で変えられる角度の上限（ラジアン）。使えなければ 0</summary>
        public float BoostMaxTurn(int player)
        {
            return CanBoost(player) ? GolfSpecialBalance.BoostMaxTurn(_balls[player].BoostStars) : 0f;
        }

        /// <summary>
        /// 雷: その場で一瞬加速し、いまの進む向きから turn（ラジアン。正で左回り）だけ向きを変える。
        /// 変えられる角度は★で決まり、それより大きい値は丸める
        /// </summary>
        public bool Boost(int player, float turn)
        {
            if (!CanBoost(player))
            {
                return false;
            }

            var ball = _balls[player];
            var max = GolfSpecialBalance.BoostMaxTurn(ball.BoostStars);
            var speed = ball.Velocity.Length + GolfSpecialBalance.BoostSpeed(ball.BoostStars);
            ball.Velocity = ball.Velocity.Normalized.Rotated(Math.Clamp(turn, -max, max)) * speed;
            ball.Velocity = ball.Velocity.ClampLength(GolfBalance.MaxBallSpeed);
            ball.BoostReady = false;
            Raise(new GolfEvent(GolfEventKind.Boost, player, ball.Position, ball.BoostStars));
            return true;
        }

        /// <summary>時間を進める。狙っている間は持ち時間が減り、転がっている間は決まった刻みで球を動かす</summary>
        public void Tick(float seconds)
        {
            switch (Phase)
            {
                case GolfHolePhase.Aiming:
                    TickAim(seconds);
                    break;
                case GolfHolePhase.Rolling:
                    TickRoll(seconds);
                    break;
                default:
                    Time += seconds;
                    break;
            }
        }

        /// <summary>
        /// 時刻だけを進める（ホールの全体を見せている間・振り下ろしている間など、持ち時間を減らさないとき）。
        /// 動く物は止めずに動かし続ける。転がっている間は球と一緒に Tick で進むので何もしない
        /// </summary>
        public void Idle(float seconds)
        {
            if (Phase != GolfHolePhase.Rolling)
            {
                Time += seconds;
            }
        }

        private void TickAim(float seconds)
        {
            Time += seconds;
            ShotTimeLeft -= seconds;
            if (ShotTimeLeft <= 0f)
            {
                ShotTimeLeft = 0f;
                Shoot(CurrentPlayer, AimAngle, GolfRules.AutoShotPower);
            }
        }

        private void TickRoll(float seconds)
        {
            _accumulator = Math.Min(_accumulator + seconds, MaxCatchUpSeconds);
            while (_accumulator >= GolfBalance.StepSeconds && Phase == GolfHolePhase.Rolling)
            {
                _accumulator -= GolfBalance.StepSeconds;
                StepOnce();
            }
        }

        private void StepOnce()
        {
            _stepEvents.Clear();
            _sim.Step(_balls, _stepEvents, Time);
            Time += GolfBalance.StepSeconds;
            _rollSeconds += GolfBalance.StepSeconds;
            RaiseStepEvents();
            if (_sim.AllStopped(_balls, Time) || _rollSeconds >= GolfBalance.MaxRollSeconds)
            {
                EndShot();
            }
        }

        private void RaiseStepEvents()
        {
            foreach (var e in _stepEvents)
            {
                // 打った本人が落ちたら 1 打足す。弾かれて落ちた人は戻すだけ（自分で打っていないので）
                if (e.Kind == GolfEventKind.Fell && e.Player == CurrentPlayer)
                {
                    _strokes[e.Player]++;
                }

                Raise(e);
            }
        }

        private void EndShot()
        {
            foreach (var ball in _balls)
            {
                ball.Stop();
            }

            // 動く物の通る範囲で止まった球はよけておき（狙っている間に動く物がすり抜けないように）、
            // 凍った道の上で止まった球は、氷が消えて池に落ちる
            _stepEvents.Clear();
            _sim.ClearSweeps(_balls, _stepEvents);
            _sim.SettleHazards(_balls, _stepEvents);
            RaiseStepEvents();

            // 弾かれてカップに入った人も、そこで終わり
            for (var player = 0; player < PlayerCount; player++)
            {
                if (!_done[player] && _balls[player].InCup)
                {
                    _done[player] = true;
                }
            }

            GiveUpIfOutOfStrokes(CurrentPlayer);
            if (Array.TrueForAll(_done, done => done))
            {
                FinishHole();
                return;
            }

            NextTurn();
        }

        private void FinishHole()
        {
            Phase = GolfHolePhase.Finished;
            Raise(new GolfEvent(GolfEventKind.HoleFinished, -1, Hole.Cup));
        }

        /// <summary>上限まで打っても入らなければ「上限 ＋ 1」打で終わり、球をコースから外す</summary>
        private void GiveUpIfOutOfStrokes(int player)
        {
            if (_done[player] || _strokes[player] < Hole.StrokeLimit)
            {
                return;
            }

            _strokes[player] = Hole.StrokeLimit + 1;
            _done[player] = true;
            _balls[player].Active = false;
            Raise(new GolfEvent(GolfEventKind.GaveUp, player, _balls[player].Position));
        }

        /// <summary>1打ずつ交代。終わった人は飛ばす</summary>
        private void NextTurn()
        {
            for (var k = 1; k <= _order.Count; k++)
            {
                var index = (_orderIndex + k) % _order.Count;
                if (!_done[_order[index]])
                {
                    BeginTurn(index);
                    return;
                }
            }
        }

        private void BeginTurn(int orderIndex)
        {
            _orderIndex = orderIndex;
            Phase = GolfHolePhase.Aiming;
            ShotTimeLeft = GolfRules.ShotSeconds;
            AimAngle = AngleToCup(CurrentPlayer);
            Raise(new GolfEvent(GolfEventKind.TurnStart, CurrentPlayer, _balls[CurrentPlayer].Position));
        }

        private void Raise(GolfEvent e) => Happened?.Invoke(e);

        private static void ValidateOrder(IReadOnlyList<int> order)
        {
            if (order == null || order.Count < 1 || order.Count > GolfRules.MaxPlayers)
            {
                throw new ArgumentException("打つ順は 1〜4 人", nameof(order));
            }

            var seen = new bool[order.Count];
            foreach (var seat in order)
            {
                if (seat < 0 || seat >= order.Count || seen[seat])
                {
                    throw new ArgumentException("打つ順は 0〜人数−1 を1回ずつ", nameof(order));
                }

                seen[seat] = true;
            }
        }
    }
}
