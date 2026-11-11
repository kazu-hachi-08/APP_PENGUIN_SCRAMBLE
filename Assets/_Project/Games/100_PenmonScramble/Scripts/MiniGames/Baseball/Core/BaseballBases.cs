using System;
using System.Collections.Generic;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>走者の1つの動き（表示の演出に使う。打者は From = 0、ホームへ還ったら To = ScoredBase）</summary>
    public readonly struct BaseballRunnerMove
    {
        /// <summary>還った（点が入った）ときの To</summary>
        public const int ScoredBase = BaseballRules.BaseCount + 1;

        public BaseballRunnerMove(int runner, int from, int to, bool out_)
        {
            Runner = runner;
            From = from;
            To = to;
            Out = out_;
        }

        /// <summary>走者の打順（チームの編成の何番目か）</summary>
        public int Runner { get; }

        /// <summary>動く前の塁（0 = 打者・1〜3 = 一塁〜三塁）</summary>
        public int From { get; }

        /// <summary>着いた塁（1〜3。ScoredBase なら還った。アウトなら向かっていた塁）</summary>
        public int To { get; }

        /// <summary>この走者はアウトになった（ゲッツーの一塁の走者）</summary>
        public bool Out { get; }

        public bool Scored => To == ScoredBase && !Out;

        public override string ToString() => $"{Runner}: {From}→{To}{(Out ? " out" : string.Empty)}";
    }

    /// <summary>打席の結果で走者が動いた結果（入った点と増えたアウト）</summary>
    public readonly struct BaseballBasesPlay
    {
        public BaseballBasesPlay(int runs, int outs)
        {
            Runs = runs;
            Outs = outs;
        }

        public int Runs { get; }

        /// <summary>この打席で増えたアウト（打者のアウトも含む。ゲッツーで 2）</summary>
        public int Outs { get; }
    }

    /// <summary>
    /// 塁の上の走者と、打席の結果での進み方。
    /// 安打は全員その塁の数だけ・四球は押し出される走者だけ・ゴロ（バントの犠打も）は全員1つ・フライは三塁の走者だけ還る（タッチアップ）。
    /// その打球で3アウトになったら点は入らず、走者も動かない。ゲッツーは一塁に走者がいて2アウト未満のときだけ一塁の走者もアウト
    /// </summary>
    public sealed class BaseballBases
    {
        public const int Empty = -1;

        // 0 = 一塁・1 = 二塁・2 = 三塁。中身は走者の打順（いなければ Empty）
        private readonly int[] _runners = { Empty, Empty, Empty };

        /// <summary>その塁（1〜3）の走者の打順（いなければ Empty）</summary>
        public int RunnerOn(int baseNumber) => _runners[Index(baseNumber)];

        public bool IsOccupied(int baseNumber) => RunnerOn(baseNumber) != Empty;

        public int RunnerCount
        {
            get
            {
                var count = 0;
                foreach (var runner in _runners)
                {
                    count += runner != Empty ? 1 : 0;
                }

                return count;
            }
        }

        public void Clear()
        {
            Array.Fill(_runners, Empty);
        }

        /// <summary>テストで走者を置く</summary>
        internal void Place(int baseNumber, int runner)
        {
            _runners[Index(baseNumber)] = runner;
        }

        /// <summary>
        /// 打席の結果で走者と打者を進める。outsBefore はこの打席の前のアウトの数（3アウト目で点を入れないため）。
        /// 動きは moves に足す（打者のアウトは足さない。打つ子の演出は打席の側で出すため）
        /// </summary>
        public BaseballBasesPlay Advance(BaseballPlateResult plate, BaseballHitResult hit, int batter, int outsBefore, List<BaseballRunnerMove> moves)
        {
            switch (plate)
            {
                case BaseballPlateResult.Strikeout:
                    return new BaseballBasesPlay(0, 1);
                case BaseballPlateResult.Walk:
                    return Walk(batter, moves);
                case BaseballPlateResult.Hit:
                    return Safe(batter, Bases(hit), moves);
                case BaseballPlateResult.Out:
                    return Out(hit, outsBefore, moves);
                default:
                    return new BaseballBasesPlay(0, 0);
            }
        }

        /// <summary>安打の進む塁の数（ホームランは 4）</summary>
        public static int Bases(BaseballHitResult hit)
        {
            return hit switch
            {
                BaseballHitResult.Single => 1,
                BaseballHitResult.Double => 2,
                BaseballHitResult.Triple => 3,
                BaseballHitResult.HomeRun => BaseballRunnerMove.ScoredBase,
                _ => throw new ArgumentOutOfRangeException(nameof(hit), hit, "安打ではない"),
            };
        }

        /// <summary>打者も走者も全員 count 塁ずつ進む（先の塁の走者から動かして、追い越さないように）</summary>
        private BaseballBasesPlay Safe(int batter, int count, List<BaseballRunnerMove> moves)
        {
            var runs = AdvanceRunners(count, moves);
            runs += Arrive(batter, 0, count, moves);
            return new BaseballBasesPlay(runs, 0);
        }

        /// <summary>四球: 打者は一塁へ。一塁から詰まっている走者だけ1つ押し出される</summary>
        private BaseballBasesPlay Walk(int batter, List<BaseballRunnerMove> moves)
        {
            var forced = 0;
            while (forced < BaseballRules.BaseCount && IsOccupied(forced + 1))
            {
                forced++;
            }

            var runs = 0;
            for (var b = forced; b >= 1; b--)
            {
                var runner = Take(b);
                runs += Arrive(runner, b, b + 1, moves);
            }

            Arrive(batter, 0, 1, moves);
            return new BaseballBasesPlay(runs, 0);
        }

        private BaseballBasesPlay Out(BaseballHitResult hit, int outsBefore, List<BaseballRunnerMove> moves)
        {
            if (hit == BaseballHitResult.DoublePlay && IsOccupied(1) && outsBefore < BaseballRules.OutsPerInning - 1)
            {
                return DoublePlay(outsBefore, moves);
            }

            if (outsBefore + 1 >= BaseballRules.OutsPerInning)
            {
                return new BaseballBasesPlay(0, 1);
            }

            if (hit == BaseballHitResult.FlyOut)
            {
                return new BaseballBasesPlay(TagUp(moves), 1);
            }

            // ゴロ（バントの犠打も）: 打者はアウト、走者は全員1つ進む
            return new BaseballBasesPlay(AdvanceRunners(1, moves), 1);
        }

        /// <summary>ゲッツー: 一塁の走者も二塁でアウト。3アウトにならなければ、ほかの走者は1つ進む</summary>
        private BaseballBasesPlay DoublePlay(int outsBefore, List<BaseballRunnerMove> moves)
        {
            const int outs = 2;
            var first = Take(1);
            moves.Add(new BaseballRunnerMove(first, 1, 2, true));
            if (outsBefore + outs >= BaseballRules.OutsPerInning)
            {
                return new BaseballBasesPlay(0, outs);
            }

            return new BaseballBasesPlay(AdvanceRunners(1, moves), outs);
        }

        /// <summary>フライ: 三塁の走者だけホームへ（ほかは進まない）</summary>
        private int TagUp(List<BaseballRunnerMove> moves)
        {
            if (!IsOccupied(3))
            {
                return 0;
            }

            var runner = Take(3);
            return Arrive(runner, 3, BaseballRunnerMove.ScoredBase, moves);
        }

        /// <summary>塁の上の走者を、先の塁から順に count 塁ずつ進める。還った人数を返す</summary>
        private int AdvanceRunners(int count, List<BaseballRunnerMove> moves)
        {
            var runs = 0;
            for (var b = BaseballRules.BaseCount; b >= 1; b--)
            {
                if (!IsOccupied(b))
                {
                    continue;
                }

                var runner = Take(b);
                runs += Arrive(runner, b, b + count, moves);
            }

            return runs;
        }

        /// <summary>走者を to の塁へ置く（三塁より先ならホームへ還って 1 を返す）</summary>
        private int Arrive(int runner, int from, int to, List<BaseballRunnerMove> moves)
        {
            var scored = to > BaseballRules.BaseCount;
            moves.Add(new BaseballRunnerMove(runner, from, scored ? BaseballRunnerMove.ScoredBase : to, false));
            if (scored)
            {
                return 1;
            }

            _runners[Index(to)] = runner;
            return 0;
        }

        private int Take(int baseNumber)
        {
            var runner = RunnerOn(baseNumber);
            _runners[Index(baseNumber)] = Empty;
            return runner;
        }

        private static int Index(int baseNumber)
        {
            if (baseNumber < 1 || baseNumber > BaseballRules.BaseCount)
            {
                throw new ArgumentOutOfRangeException(nameof(baseNumber), baseNumber, null);
            }

            return baseNumber - 1;
        }
    }
}
