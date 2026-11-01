using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>手ごたえの目標1つ（計画書 §7.2）と、測った値</summary>
    public readonly struct BalanceCheck
    {
        public BalanceCheck(string scene, string party, string goal, float measured, float min, float max, string unit)
        {
            Scene = scene;
            Party = party;
            Goal = goal;
            Measured = measured;
            Min = min;
            Max = max;
            Unit = unit;
        }

        public string Scene { get; }
        public string Party { get; }
        public string Goal { get; }
        public float Measured { get; }
        public float Min { get; }
        public float Max { get; }

        /// <summary>"%" なら割合（0〜1）を百分率で見せる</summary>
        public string Unit { get; }

        public bool IsMet => Measured >= Min && Measured <= Max;
    }

    /// <summary>
    /// 手ごたえの目標。ふつうの人＝ StageBot の Casual、うまい人＝ Expert。
    /// 割合の目標には、20 回ほど回したときのぶれ（±1 割ほど）を見込んで少し幅を持たせる
    /// </summary>
    public static partial class BalanceLab
    {
        public static List<BalanceCheck> Check(IReadOnlyList<BalanceCase> cases)
        {
            var checks = new List<BalanceCheck>();
            AddStoryChecks(cases, checks);
            AddRushChecks(cases, checks);
            return checks;
        }

        private static void AddStoryChecks(IReadOnlyList<BalanceCase> cases, List<BalanceCheck> checks)
        {
            var first = Find(cases, 1, BotSkill.Casual, BalanceParty.Starter.Name);
            if (first != null)
            {
                checks.Add(new BalanceCheck("1-1", first.Party.Name, "ふつうの人でクリアできる", first.ClearRate, 0.9f, 1f, "%"));
                checks.Add(new BalanceCheck("1-1", first.Party.Name, "HP の半分ほど削られる", first.DamageRatio, 0.3f, 0.7f, "%"));
            }

            for (var world = 1; world < Balance.WorldCount; world++)
            {
                var no = world * StageTable.StagesPerWorld;
                var boss = Find(cases, no, BotSkill.Casual, BalanceParty.Story(world).Name);
                if (boss != null)
                {
                    var scene = $"{world}-{StageTable.StagesPerWorld}";
                    checks.Add(new BalanceCheck(scene, boss.Party.Name, "ふつうの人で 6〜7 割クリア", boss.ClearRate, 0.55f, 0.8f, "%"));
                    checks.Add(new BalanceCheck(scene, boss.Party.Name, "大ボスに何回か当たる", boss.BossHits, 2f, 30f, "回"));
                }
            }

            var last = Find(cases, StageTable.Count, BotSkill.Casual, BalanceParty.Advanced(Balance.WorldCount).Name);
            if (last != null)
            {
                checks.Add(new BalanceCheck("5-3", last.Party.Name, "ふつうの人で半分ほど", last.ClearRate, 0.4f, 0.65f, "%"));
            }
        }

        private static void AddRushChecks(IReadOnlyList<BalanceCase> cases, List<BalanceCheck> checks)
        {
            for (var no = RushTable.FirstNo; no <= RushTable.LastNo; no++)
            {
                var name = RushTable.Get(no).Name;
                AddRushCheck(cases, checks, no, $"{name} ふつう", Difficulty.Normal, BotSkill.Casual, 2, "2〜4人で 7 割ほど", 0.6f, 0.85f);
                AddRushCheck(cases, checks, no, $"{name} むずかしい", Difficulty.Hard, BotSkill.Casual, 2, "2〜4人で 5 割ほど", 0.4f, 0.65f);
                AddRushCheck(cases, checks, no, $"{name} げきむず", Difficulty.Extreme, BotSkill.Expert, Balance.MaxPlayers, "うまい人4人で 3〜5 割", 0.25f, 0.55f);
            }
        }

        /// <summary>minPlayers〜4 人のクリア率の平均で見る</summary>
        private static void AddRushCheck(IReadOnlyList<BalanceCase> cases, List<BalanceCheck> checks, int no, string scene, Difficulty difficulty,
            BotSkill skill, int minPlayers, string goal, float min, float max)
        {
            var sum = 0f;
            var count = 0;
            string party = null;
            foreach (var c in cases)
            {
                if (c.Stage.No == no && c.Difficulty == difficulty && c.Skill == skill && c.Players >= minPlayers)
                {
                    sum += c.ClearRate;
                    count++;
                    party = c.Party.Name;
                }
            }

            if (count > 0)
            {
                checks.Add(new BalanceCheck(scene, party, goal, sum / count, min, max, "%"));
            }
        }

        private static BalanceCase Find(IReadOnlyList<BalanceCase> cases, int stageNo, BotSkill skill, string partyName)
        {
            foreach (var c in cases)
            {
                if (c.Stage.No == stageNo && c.Skill == skill && c.Party.Name == partyName)
                {
                    return c;
                }
            }

            return null;
        }
    }
}
