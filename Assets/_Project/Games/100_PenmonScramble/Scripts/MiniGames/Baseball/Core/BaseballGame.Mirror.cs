using System;
using System.Collections.Generic;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 試合の今の様子（カウント・走者・点・イニング・打順・投げる子・魔球と必殺打の残り・直前の走者の動き）。
    /// オンラインではホストがこれを送り、ゲストの試合へそのまま書き込む（ゲストは試合を計算しない）
    /// </summary>
    public sealed class BaseballGameState
    {
        public int Inning;
        public bool IsTop;
        public bool IsHalfOver;
        public bool IsOver;
        public bool EndedByMercy;
        public int Pitches;
        public int PitchesThisPlate;
        public int Balls;
        public int Strikes;
        public int Outs;
        public int[] Batter = new int[BaseballRules.TeamCount];
        public int[] Pitcher = new int[BaseballRules.TeamCount];

        /// <summary>チームごとのイニングの点（1回から）</summary>
        public List<int>[] InningRuns = { new List<int>(), new List<int>() };

        /// <summary>チームごとの、子ごとの魔球・必殺打を使ったか</summary>
        public bool[][] MagicUsed = { Array.Empty<bool>(), Array.Empty<bool>() };
        public bool[][] HitUsed = { Array.Empty<bool>(), Array.Empty<bool>() };

        /// <summary>一塁・二塁・三塁の走者の打順（いなければ BaseballBases.Empty）</summary>
        public int[] Runners = { BaseballBases.Empty, BaseballBases.Empty, BaseballBases.Empty };

        public List<BaseballRunnerMove> LastMoves = new List<BaseballRunnerMove>();
    }

    public sealed partial class BaseballGame
    {
        public BaseballGameState Capture()
        {
            var state = new BaseballGameState
            {
                Inning = Inning,
                IsTop = IsTop,
                IsHalfOver = IsHalfOver,
                IsOver = IsOver,
                EndedByMercy = EndedByMercy,
                Pitches = Pitches,
                PitchesThisPlate = _pitchesThisPlate,
                Balls = Count.Balls,
                Strikes = Count.Strikes,
                Outs = Count.Outs,
            };
            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                state.Batter[team] = _batter[team];
                state.Pitcher[team] = _pitcher[team];
                state.InningRuns[team].AddRange(_inningRuns[team]);
                state.MagicUsed[team] = (bool[])_magicUsed[team].Clone();
                state.HitUsed[team] = (bool[])_hitUsed[team].Clone();
            }

            for (var b = 0; b < BaseballRules.BaseCount; b++)
            {
                state.Runners[b] = Bases.RunnerOn(b + 1);
            }

            state.LastMoves.AddRange(_moves);
            return state;
        }

        /// <summary>
        /// 写し（ゲスト）: ホストの様子をそのまま書き込む。打順の人数が違う・壊れた値は、読める所だけ入れる（試合を止めないため）
        /// </summary>
        public void Restore(BaseballGameState state)
        {
            Inning = Math.Max(1, state.Inning);
            IsTop = state.IsTop;
            IsHalfOver = state.IsHalfOver;
            IsOver = state.IsOver;
            EndedByMercy = state.EndedByMercy;
            Pitches = state.Pitches;
            _pitchesThisPlate = state.PitchesThisPlate;
            Count.Restore(state.Balls, state.Strikes, state.Outs);
            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                _batter[team] = Slot(state.Batter[team], team);
                _pitcher[team] = Slot(state.Pitcher[team], team);
                _inningRuns[team].Clear();
                _inningRuns[team].AddRange(state.InningRuns[team]);
                CopyFlags(state.MagicUsed[team], _magicUsed[team]);
                CopyFlags(state.HitUsed[team], _hitUsed[team]);
            }

            for (var b = 0; b < BaseballRules.BaseCount; b++)
            {
                Bases.Place(b + 1, state.Runners[b]);
            }

            _moves.Clear();
            _moves.AddRange(state.LastMoves);
        }

        private int Slot(int slot, int team) => slot >= 0 && slot < _lineupSizes[team] ? slot : 0;

        private static void CopyFlags(bool[] from, bool[] to)
        {
            for (var i = 0; i < to.Length; i++)
            {
                to[i] = from != null && i < from.Length && from[i];
            }
        }
    }
}
