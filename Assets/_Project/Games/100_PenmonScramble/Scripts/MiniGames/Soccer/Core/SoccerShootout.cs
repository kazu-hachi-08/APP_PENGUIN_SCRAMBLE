using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    public enum SoccerPenaltyResult
    {
        Goal,
        Saved,
        Missed,
    }

    public enum SoccerShootoutPhase
    {
        /// <summary>並べて何本目かを見せる</summary>
        Ready,
        /// <summary>蹴る子が狙ってためる（持ち時間あり）。守る側は飛ぶ向きを選ぶ</summary>
        Aim,
        /// <summary>ボールが飛んでいる・キーパーが飛んでいる</summary>
        Flight,
        /// <summary>決まった・止めた・外れたを見せる</summary>
        Result,
        Finished,
    }

    /// <summary>
    /// PK戦。3本ずつ交代で蹴り、3本で同点ならサドンデス（1本ずつ、差がついたら終わり）。
    /// 先に蹴るのは赤。蹴る子はチームの子の番号の順にくり返す。蹴る側は相手のゴールへ蹴り、相手のキーパーが守る。
    /// 入力は試合と同じ SoccerInput で、蹴る子（Kicker）と、守る側の決まった子（KeeperInputIndex）への入力を読む。
    /// スティックの向きは蹴る子の後ろから見た向き（X が右・Y が上）。守る側の選びは蹴った瞬間の向きで決まる。
    /// 見た目は SoccerWorld の子・ボール・キーパーを並べ直して見せる（SoccerWorld の計算は進めない）。
    /// 乱数は種から作る System.Random（オンラインではホストだけが計算する）。蹴った球の決まり方は SoccerShootout.Kick.cs
    /// </summary>
    public sealed partial class SoccerShootout
    {
        private const float Dt = SoccerRules.StepSeconds;

        private readonly SoccerWorld _world;
        private readonly Random _random;
        private readonly int _firstTeam;
        private readonly List<SoccerPenaltyResult>[] _results =
        {
            new List<SoccerPenaltyResult>(),
            new List<SoccerPenaltyResult>(),
        };

        // ため始めるには、構えてから一度ボタンを離していること（押しっぱなしのまま始まった1本で勝手にためないように）
        private bool _released;

        public SoccerShootout(SoccerWorld world, int seed, int firstTeam = SoccerRules.Red)
        {
            _world = world;
            _random = new Random(seed);
            _firstTeam = firstTeam;
            Winner = -1;
        }

        public SoccerShootoutPhase Phase { get; private set; } = SoccerShootoutPhase.Finished;

        /// <summary>今の段になってからの秒</summary>
        public float PhaseSeconds { get; private set; }

        public int KickingTeam { get; private set; }

        public int KeeperTeam => SoccerRules.Opponent(KickingTeam);

        /// <summary>蹴る子（SoccerWorld.Players の番号）</summary>
        public int Kicker { get; private set; } = SoccerBall.NoPlayer;

        /// <summary>今の1本は、そのチームの何本目か（1 から）</summary>
        public int KickNumber => _results[KickingTeam].Count + 1;

        /// <summary>今まで蹴った本数（両チームの合計）。1本ごとに増える（AI が新しい1本に気づくのに使う）</summary>
        public int TotalKicks => _results[0].Count + _results[1].Count;

        /// <summary>狙っている向き（蹴る子から見て −1 左・0 真ん中・1 右）と高さ。守る側の人には見せない</summary>
        public int AimSide { get; private set; }

        public bool AimHigh { get; private set; }

        /// <summary>ためゲージ（0〜1。PenaltyRedZone より上は赤い所）</summary>
        public float Charge01 { get; private set; }

        public bool IsCharging { get; private set; }

        /// <summary>狙う持ち時間の残り</summary>
        public float AimSecondsLeft => Phase == SoccerShootoutPhase.Aim ? Math.Max(0f, SoccerBalance.PenaltyAimSeconds - PhaseSeconds) : 0f;

        /// <summary>蹴った後の、守る側の選び（−1 左・0 真ん中・1 右。蹴る子から見て）と、この1本の結果</summary>
        public int KeeperSide { get; private set; }

        public SoccerPenaltyResult LastResult { get; private set; }

        public bool IsFinished => Phase == SoccerShootoutPhase.Finished && Winner >= 0;

        /// <summary>勝ったチーム。決まるまで −1</summary>
        public int Winner { get; private set; }

        public IReadOnlyList<SoccerPenaltyResult> Results(int team) => _results[team];

        public int Score(int team) => _results[team].FindAll(r => r == SoccerPenaltyResult.Goal).Count;

        /// <summary>3本ずつを過ぎてサドンデスに入っている</summary>
        public bool IsSuddenDeath => Math.Min(_results[0].Count, _results[1].Count) >= SoccerRules.PenaltyRounds;

        /// <summary>そのチームの人の入力を渡す子（蹴る側なら蹴る子、守る側なら KeeperInputIndex）</summary>
        public int InputIndex(int team) => team == KickingTeam ? Kicker : KeeperInputIndex(team);

        /// <summary>守る側の飛ぶ向きを読む子（チームの一番若い番号の子）</summary>
        public int KeeperInputIndex(int team)
        {
            foreach (var player in _world.Players)
            {
                if (player.Team == team)
                {
                    return player.Index;
                }
            }

            return SoccerBall.NoPlayer;
        }

        /// <summary>1本目の構えから始める</summary>
        public void Begin(List<SoccerEvent> events)
        {
            KickingTeam = _firstTeam;
            StartKick(events);
        }

        public void Step(IReadOnlyList<SoccerInput> inputs, List<SoccerEvent> events)
        {
            if (Phase == SoccerShootoutPhase.Finished)
            {
                return;
            }

            PhaseSeconds += Dt;
            switch (Phase)
            {
                case SoccerShootoutPhase.Ready:
                    if (PhaseSeconds >= SoccerBalance.PenaltyReadySeconds)
                    {
                        Enter(SoccerShootoutPhase.Aim);
                    }

                    break;
                case SoccerShootoutPhase.Aim:
                    StepAim(InputOf(inputs, Kicker), InputOf(inputs, KeeperInputIndex(KeeperTeam)), events);
                    break;
                case SoccerShootoutPhase.Flight:
                    StepFlight(events);
                    break;
                case SoccerShootoutPhase.Result:
                    if (PhaseSeconds >= SoccerBalance.PenaltyResultSeconds)
                    {
                        NextKick(events);
                    }

                    break;
            }
        }

        private static SoccerInput InputOf(IReadOnlyList<SoccerInput> inputs, int index)
        {
            return inputs != null && index >= 0 && index < inputs.Count ? inputs[index] : SoccerInput.None;
        }

        private void Enter(SoccerShootoutPhase phase)
        {
            Phase = phase;
            PhaseSeconds = 0f;
        }

        /// <summary>スティックで狙いを決め、押した瞬間からためて、離したら蹴る。持ち時間が過ぎたら、そのときの狙いで弱く蹴る</summary>
        private void StepAim(SoccerInput kicker, SoccerInput keeper, List<SoccerEvent> events)
        {
            ReadAim(kicker.Move);
            _released |= !kicker.Shoot;
            if (!IsCharging && kicker.Shoot && _released)
            {
                IsCharging = true;
            }

            if (IsCharging)
            {
                Charge01 = Math.Min(1f, Charge01 + Dt / SoccerBalance.PenaltyChargeSeconds);
                if (!kicker.Shoot)
                {
                    Kick(Charge01, keeper, events);
                    return;
                }
            }

            ShowKickerCharge();
            if (PhaseSeconds >= SoccerBalance.PenaltyAimSeconds)
            {
                Kick(SoccerBalance.PenaltyTimeoutPower, keeper, events);
            }
        }

        private void ReadAim(Vec2 move)
        {
            AimSide = SideOf(move.X);
            AimHigh = move.Y > SoccerBalance.PenaltyStickDeadZone;
            var kicker = _world.Players[Kicker];
            var target = TargetPoint(AimSide, AimHigh ? SoccerBalance.PenaltyHighHeight : 0f, false);
            var toTarget = target - kicker.Position;
            if (!toTarget.IsNearlyZero)
            {
                kicker.Facing = toTarget.Normalized;
            }
        }

        private static int SideOf(float x) => x > SoccerBalance.PenaltyStickDeadZone ? 1 : x < -SoccerBalance.PenaltyStickDeadZone ? -1 : 0;

        /// <summary>ためている姿（足を引く）を見た目へ渡す</summary>
        private void ShowKickerCharge()
        {
            var kicker = _world.Players[Kicker];
            kicker.IsCharging = IsCharging;
            kicker.ChargeSeconds = Charge01 * SoccerBalance.ShotChargeSeconds;
        }

        /// <summary>結果を見せ終えたら、決着か次の1本か</summary>
        private void NextKick(List<SoccerEvent> events)
        {
            if (TryDecide(out var winner))
            {
                Winner = winner;
                Enter(SoccerShootoutPhase.Finished);
                return;
            }

            KickingTeam = _results[_firstTeam].Count == _results[SoccerRules.Opponent(_firstTeam)].Count
                ? _firstTeam
                : SoccerRules.Opponent(_firstTeam);
            StartKick(events);
        }

        /// <summary>決着がついていれば勝ったチーム。いちばん多い組の数まで並んだら、乱数で決める（終わらないことが無いように）</summary>
        private bool TryDecide(out int winner)
        {
            var redKicks = _results[SoccerRules.Red].Count;
            var blueKicks = _results[SoccerRules.Blue].Count;
            if (IsDecided(redKicks, Score(SoccerRules.Red), blueKicks, Score(SoccerRules.Blue), out winner))
            {
                return true;
            }

            if (redKicks == blueKicks && redKicks >= SoccerRules.PenaltyMaxRounds)
            {
                winner = _random.Next(SoccerRules.TeamCount);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 本数と点だけで決着がついたか。3本ずつの間は、残りを全部決めても追いつけなくなったら終わり。
        /// 3本ずつを過ぎたら（サドンデス）、同じ本数を蹴って差がついたら終わり
        /// </summary>
        public static bool IsDecided(int redKicks, int redGoals, int blueKicks, int blueGoals, out int winner)
        {
            winner = -1;
            var rounds = SoccerRules.PenaltyRounds;
            if (redKicks <= rounds && blueKicks <= rounds)
            {
                if (redGoals > blueGoals + (rounds - blueKicks))
                {
                    winner = SoccerRules.Red;
                    return true;
                }

                if (blueGoals > redGoals + (rounds - redKicks))
                {
                    winner = SoccerRules.Blue;
                    return true;
                }
            }

            if (redKicks != blueKicks || redKicks < rounds || redGoals == blueGoals)
            {
                return false;
            }

            winner = redGoals > blueGoals ? SoccerRules.Red : SoccerRules.Blue;
            return true;
        }

        /// <summary>そのチームの、次に蹴る子（子の番号の順にくり返す）</summary>
        private int NextKicker(int team)
        {
            var members = new List<int>();
            foreach (var player in _world.Players)
            {
                if (player.Team == team)
                {
                    members.Add(player.Index);
                }
            }

            return members.Count > 0 ? members[_results[team].Count % members.Count] : SoccerBall.NoPlayer;
        }
    }
}
