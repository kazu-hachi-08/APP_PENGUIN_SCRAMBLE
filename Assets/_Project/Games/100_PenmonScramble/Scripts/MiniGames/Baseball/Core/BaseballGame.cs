using System;
using System.Collections.Generic;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>1球を試合に入れた結果（表示はこれを見て、走者の演出・「チェンジ」・試合の終わりを出す）</summary>
    public readonly struct BaseballGamePlay
    {
        public BaseballGamePlay(BaseballPitchOutcome outcome, BaseballPlateResult plate, int runs, bool halfOver, bool gameOver)
        {
            Outcome = outcome;
            Plate = plate;
            Runs = runs;
            HalfOver = halfOver;
            GameOver = gameOver;
        }

        public BaseballPitchOutcome Outcome { get; }
        public BaseballPlateResult Plate { get; }

        /// <summary>この球で入った点</summary>
        public int Runs { get; }

        /// <summary>3アウトになった（試合が終わっていなければ、見せた後に StartNextHalf で攻守を替える）</summary>
        public bool HalfOver { get; }

        public bool GameOver { get; }

        public bool PlateEnded => Plate != BaseballPlateResult.Continue;
    }

    /// <summary>
    /// 試合の流れ（Docs/MINIGAME_BASEBALL_PLAN.md「試合の流れ」）。3イニングの表（先攻）と裏（後攻）、3アウトで交代。
    /// 打順は編成の 1→2→3 をくり返し、イニングをまたいで続く。投げる子は守る側が打席ごとに選ぶ。
    /// 3回裏・サヨナラで後攻が勝っていたら、その時点で終わる。2回が終わって 10 点差なら早く終わる。3回が終わって同点なら引き分け。
    /// 試しの打席（Practice）は同じチームが打ち続け、3アウトでアウトと走者を戻すだけで終わらない
    /// </summary>
    public sealed class BaseballGame
    {
        private readonly int[] _lineupSizes;
        private readonly int[] _batter = new int[BaseballRules.TeamCount];
        private readonly int[] _pitcher = new int[BaseballRules.TeamCount];
        private readonly List<int>[] _inningRuns = { new List<int>(), new List<int>() };
        private readonly List<BaseballRunnerMove> _moves = new List<BaseballRunnerMove>();
        private readonly int _practiceTeam;
        private int _pitchesThisPlate;

        /// <param name="awayLineup">先攻のチームの人数（編成の数。1〜3）</param>
        /// <param name="homeLineup">後攻のチームの人数</param>
        public BaseballGame(int awayLineup, int homeLineup)
            : this(awayLineup, homeLineup, -1)
        {
        }

        private BaseballGame(int awayLineup, int homeLineup, int practiceTeam)
        {
            if (awayLineup < 1 || homeLineup < 1)
            {
                throw new ArgumentException("チームに子が1体もいない");
            }

            _lineupSizes = new[] { awayLineup, homeLineup };
            _practiceTeam = practiceTeam;
            Inning = 1;
            IsTop = practiceTeam != BaseballRules.Home;
            OpenInning();
        }

        /// <summary>試しの打席: battingTeam がずっと打つ（3アウトでアウトと走者を戻すだけ。試合は終わらない）</summary>
        public static BaseballGame Practice(int awayLineup, int homeLineup, int battingTeam)
        {
            return new BaseballGame(awayLineup, homeLineup, battingTeam);
        }

        public BaseballCount Count { get; } = new BaseballCount();
        public BaseballBases Bases { get; } = new BaseballBases();

        /// <summary>今のイニング（1 から）</summary>
        public int Inning { get; private set; }

        /// <summary>表（先攻が打つ）か</summary>
        public bool IsTop { get; private set; }

        public bool IsPractice => _practiceTeam >= 0;

        public int BattingTeam => IsTop ? BaseballRules.Away : BaseballRules.Home;
        public int FieldingTeam => IsTop ? BaseballRules.Home : BaseballRules.Away;

        /// <summary>3アウトになって、StartNextHalf を待っている</summary>
        public bool IsHalfOver { get; private set; }

        public bool IsOver { get; private set; }

        /// <summary>勝ったチーム（引き分け・まだ終わっていなければ −1）</summary>
        public int Winner
        {
            get
            {
                if (!IsOver || Score(BaseballRules.Away) == Score(BaseballRules.Home))
                {
                    return -1;
                }

                return Score(BaseballRules.Away) > Score(BaseballRules.Home) ? BaseballRules.Away : BaseballRules.Home;
            }
        }

        /// <summary>点差で早く終わった</summary>
        public bool EndedByMercy { get; private set; }

        /// <summary>投げた球の数（試合の長さの目安）</summary>
        public int Pitches { get; private set; }

        /// <summary>打席の始め（まだ1球も投げていない）。投げる子を選べるのはこの間だけ</summary>
        public bool IsPlateStart => _pitchesThisPlate == 0;

        /// <summary>直前の球で動いた走者（次の Apply まで）</summary>
        public IReadOnlyList<BaseballRunnerMove> LastMoves => _moves;

        public int LineupSize(int team) => _lineupSizes[team];

        /// <summary>そのチームの今の（次の）打者の打順</summary>
        public int BatterSlot(int team) => _batter[team];

        public int CurrentBatter => _batter[BattingTeam];

        /// <summary>そのチームの投げる子の打順</summary>
        public int PitcherSlot(int team) => _pitcher[team];

        public int CurrentPitcher => _pitcher[FieldingTeam];

        public int Score(int team)
        {
            var total = 0;
            foreach (var runs in _inningRuns[team])
            {
                total += runs;
            }

            return total;
        }

        /// <summary>そのイニングの点（スコアボード用。まだ打っていないイニングは −1）</summary>
        public int InningRuns(int team, int inning)
        {
            var list = _inningRuns[team];
            return inning >= 1 && inning <= list.Count ? list[inning - 1] : -1;
        }

        /// <summary>守る側が、この打席に投げる子を選ぶ（打席の始めだけ。前と同じ子でも良い）</summary>
        public void ChoosePitcher(int slot)
        {
            if (!IsPlateStart)
            {
                throw new InvalidOperationException("投げる子を選べるのは打席の始めだけ");
            }

            if (slot < 0 || slot >= _lineupSizes[FieldingTeam])
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
            }

            _pitcher[FieldingTeam] = slot;
        }

        /// <summary>1球の結果を入れる（カウント・走者・点・アウト・打順・試合の終わり）</summary>
        public BaseballGamePlay Apply(BaseballPitchOutcome outcome)
        {
            if (IsOver || IsHalfOver)
            {
                throw new InvalidOperationException("試合が終わった・攻守交代の後は StartNextHalf を待つ");
            }

            _moves.Clear();
            Pitches++;
            _pitchesThisPlate++;
            var outsBefore = Count.Outs;
            var plate = Count.Apply(outcome);
            if (plate == BaseballPlateResult.Continue)
            {
                return new BaseballGamePlay(outcome, plate, 0, false, false);
            }

            var play = Bases.Advance(plate, outcome.Hit, CurrentBatter, outsBefore, _moves);
            // カウントは打球のアウトを1つ数えている（三振・アウト）。ゲッツーの2つ目はここで足す
            var counted = plate == BaseballPlateResult.Strikeout || plate == BaseballPlateResult.Out ? 1 : 0;
            for (var i = counted; i < play.Outs; i++)
            {
                Count.AddOut();
            }

            AddRuns(play.Runs);
            NextBatter();
            CheckEnd();
            return new BaseballGamePlay(outcome, plate, play.Runs, IsHalfOver, IsOver);
        }

        /// <summary>攻守を替える（試しの打席は同じチームのまま、アウトと走者だけ戻す）</summary>
        public void StartNextHalf()
        {
            if (!IsHalfOver || IsOver)
            {
                throw new InvalidOperationException("3アウトになっていない・試合が終わっている");
            }

            IsHalfOver = false;
            Count.ClearOuts();
            Bases.Clear();
            if (IsPractice)
            {
                return;
            }

            if (IsTop)
            {
                IsTop = false;
            }
            else
            {
                IsTop = true;
                Inning++;
                OpenInning();
            }
        }

        private void OpenInning()
        {
            foreach (var runs in _inningRuns)
            {
                runs.Add(0);
            }
        }

        private void AddRuns(int runs)
        {
            var list = _inningRuns[BattingTeam];
            list[list.Count - 1] += runs;
        }

        private void NextBatter()
        {
            var team = BattingTeam;
            _batter[team] = (_batter[team] + 1) % _lineupSizes[team];
            _pitchesThisPlate = 0;
        }

        /// <summary>サヨナラ・点差・3アウト（3回の表で後攻が勝っている・3回裏が終わった）で終わるか</summary>
        private void CheckEnd()
        {
            if (IsPractice)
            {
                IsHalfOver = Count.IsInningOver;
                return;
            }

            var lead = Score(BaseballRules.Home) - Score(BaseballRules.Away);
            var last = Inning >= BaseballRules.Innings;
            if (!IsTop && last && lead > 0)
            {
                IsOver = true;
                return;
            }

            if (!IsTop && Inning >= BaseballRules.MercyInning && lead >= BaseballRules.MercyRuns)
            {
                EndByMercy();
                return;
            }

            if (!Count.IsInningOver)
            {
                return;
            }

            IsHalfOver = true;
            if (IsTop && last && lead > 0)
            {
                IsOver = true;
            }
            else if (!IsTop && last)
            {
                IsOver = true;
            }
            else if (!IsTop && Inning >= BaseballRules.MercyInning && Math.Abs(lead) >= BaseballRules.MercyRuns)
            {
                EndByMercy();
            }
        }

        private void EndByMercy()
        {
            IsOver = true;
            EndedByMercy = true;
        }
    }
}
