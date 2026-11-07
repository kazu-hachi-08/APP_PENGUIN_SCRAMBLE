using System;
using System.Collections.Generic;
using System.Threading;

namespace PenmonScramble.Core
{
    /// <summary>試験場で測る条件1つ（ステージ × 難易度 × 人数 × うまさ × 編成）と、種を変えて回した結果のまとめ</summary>
    public sealed class BalanceCase
    {
        private readonly List<BalanceRunResult> _results = new List<BalanceRunResult>();

        public BalanceCase(StageDef stage, Difficulty difficulty, int players, BotSkill skill, BalanceParty party)
        {
            Stage = stage;
            Difficulty = difficulty;
            Players = players;
            Skill = skill;
            Party = party;
        }

        public StageDef Stage { get; }
        public Difficulty Difficulty { get; }
        public int Players { get; }
        public BotSkill Skill { get; }
        public BalanceParty Party { get; }

        public float ClearRate => Average(r => r.Cleared ? 1f : 0f);

        /// <summary>クリアできた回の平均の秒数（クリアが無ければ全部の平均）</summary>
        public float ClearSeconds => HasClear ? Average(r => r.Seconds, r => r.Cleared) : Average(r => r.Seconds);
        public float Seconds => Average(r => r.Seconds);
        public float DamageRatio => Average(r => r.DamageRatio);
        public float Fainted => Average(r => r.Fainted);
        public float Downs => Average(r => r.Downs);
        public float BossSeconds => Average(r => r.BossSeconds);
        public float BossHits => Average(r => r.BossHits);
        public float RoundsWon => Average(r => r.RoundsWon);
        public float Defeated => Average(r => r.Defeated);

        private bool HasClear => _results.Exists(r => r.Cleared);

        internal void Add(BalanceRunResult result)
        {
            lock (_results)
            {
                _results.Add(result);
            }
        }

        private float Average(Func<BalanceRunResult, float> value, Func<BalanceRunResult, bool> filter = null)
        {
            var sum = 0f;
            var count = 0;
            foreach (var result in _results)
            {
                if (filter == null || filter(result))
                {
                    sum += value(result);
                    count++;
                }
            }

            return count > 0 ? sum / count : 0f;
        }
    }

    /// <summary>試験場の回し方</summary>
    public sealed class BalanceLabOptions
    {
        /// <summary>1つの条件を何回（種を変えて）回すか。多いほど割合が正確になる（20 回で ±10% ほど）</summary>
        public int Seeds { get; set; } = 20;

        public bool Story { get; set; } = true;
        public bool Rush { get; set; } = true;

        /// <summary>いくつも同時に回す（エディタ・コンソールで速くするため。結果は種で決まるので変わらない）</summary>
        public bool Parallel { get; set; } = true;

        /// <summary>途中でやめる（エディタの進み具合の窓で「キャンセル」を押したとき）。やめたら残りは回さない</summary>
        public CancellationToken Cancel { get; set; }
    }

    /// <summary>
    /// バランスの試験場: ステージ（1-1〜5-3・ボスラッシュ×3難易度）× 人数 × 編成 × 種 をボットで回して、表にまとめる。
    /// 目標と並べて、どこを直すべきかを見る
    /// </summary>
    public static partial class BalanceLab
    {
        /// <summary>ストーリーは「ひとりで遊ぶ」専用。ボスラッシュは 1〜4 人</summary>
        private const int StoryPlayers = 1;

        public static List<BalanceCase> Run(BalanceLabOptions options, Action<float> progress = null)
        {
            var cases = CreateCases(options);
            var jobs = new List<(BalanceCase Case, int Seed)>();
            foreach (var c in cases)
            {
                for (var seed = 1; seed <= options.Seeds; seed++)
                {
                    jobs.Add((c, seed));
                }
            }

            var done = 0;
            void RunJob(int i)
            {
                if (options.Cancel.IsCancellationRequested)
                {
                    return;
                }

                var (c, seed) = jobs[i];
                var run = new BalanceRun(c.Stage, c.Difficulty, c.Players, c.Skill, c.Party, SeedOf(c, seed));
                c.Add(run.Run());
                var finished = Interlocked.Increment(ref done);
                progress?.Invoke((float)finished / jobs.Count);
            }

            if (options.Parallel)
            {
                System.Threading.Tasks.Parallel.For(0, jobs.Count, RunJob);
            }
            else
            {
                for (var i = 0; i < jobs.Count; i++)
                {
                    RunJob(i);
                }
            }

            return cases;
        }

        public static List<BalanceCase> CreateCases(BalanceLabOptions options)
        {
            var cases = new List<BalanceCase>();
            if (options.Story)
            {
                AddStoryCases(cases);
            }

            if (options.Rush)
            {
                AddRushCases(cases);
            }

            return cases;
        }

        /// <summary>全ステージを、ふつうの人・うまい人で。1-1 は無料3回の子でも、5-3 は強化を進めた子でも測る</summary>
        private static void AddStoryCases(List<BalanceCase> cases)
        {
            for (var no = 1; no <= StageTable.Count; no++)
            {
                var stage = StageTable.Get(no);
                foreach (var party in StoryParties(stage))
                {
                    foreach (var skill in new[] { BotSkill.Casual, BotSkill.Expert })
                    {
                        cases.Add(new BalanceCase(stage, Difficulty.Normal, StoryPlayers, skill, party));
                    }
                }
            }
        }

        private static IEnumerable<BalanceParty> StoryParties(StageDef stage)
        {
            if (stage.No == 1)
            {
                yield return BalanceParty.Starter;
            }

            yield return BalanceParty.Story(stage.World);
            if (stage.No == StageTable.Count)
            {
                yield return BalanceParty.Advanced(stage.World);
            }
        }

        private static void AddRushCases(List<BalanceCase> cases)
        {
            for (var no = RushTable.FirstNo; no <= RushTable.LastNo; no++)
            {
                var stage = RushTable.Get(no);
                for (var d = 0; d < Balance.DifficultyCount; d++)
                {
                    var difficulty = (Difficulty)d;
                    for (var players = 1; players <= Balance.MaxPlayers; players++)
                    {
                        foreach (var skill in new[] { BotSkill.Casual, BotSkill.Expert })
                        {
                            cases.Add(new BalanceCase(stage, difficulty, players, skill, BalanceParty.ForRush(stage.World, difficulty)));
                        }
                    }
                }
            }
        }

        /// <summary>条件ごとに違う種にする（同じ種だと、条件をまたいで同じ編成・同じ敵の並びばかりになる）</summary>
        private static int SeedOf(BalanceCase c, int seed)
        {
            return unchecked(seed * 7919 + c.Stage.No * 104729 + (int)c.Difficulty * 1299709 + c.Players * 15485863);
        }
    }
}
