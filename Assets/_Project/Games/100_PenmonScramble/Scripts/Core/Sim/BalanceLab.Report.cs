using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PenmonScramble.Core
{
    /// <summary>試験場の結果を Markdown の表にする（Docs/BALANCE_REPORT.md に書き出して、調整の前後を比べる）</summary>
    public static partial class BalanceLab
    {
        /// <summary>1回遊ぶごとに、戦い以外（ロビー・読み込み・結果の画面）にかかる秒数の目安。コインの流れの表で、1時間に遊べる回数に入れる</summary>
        public const float MenuSecondsPerPlay = 30f;

        private const float SecondsPerHour = 3600f;

        public static string Report(IReadOnlyList<BalanceCase> cases, int seeds)
        {
            var text = new StringBuilder();
            text.AppendLine("# バランスの試験場の結果");
            text.AppendLine();
            text.AppendLine($"> `Tools > PenmonScramble > Balance > Run` で作った表（1条件 {seeds} 回）。ふつうの人＝ボットの Casual（気づくまで 0.35 秒・2 割見落とす）、うまい人＝ Expert（0.15 秒・見落とさない）。");
            text.AppendLine("> ボットは技を出し続けるので、人より手数が多い。ここで合わせた後、実機で遊んで確かめる（計画書 §7.3）。");
            text.AppendLine();
            AppendChecks(text, Check(cases));
            AppendStory(text, cases);
            AppendRush(text, cases);
            AppendCoins(text, cases);
            return text.ToString();
        }

        private static void AppendChecks(StringBuilder text, List<BalanceCheck> checks)
        {
            var met = checks.FindAll(c => c.IsMet).Count;
            text.AppendLine($"## 目標との比べ（{met} / {checks.Count} がそろっている）");
            text.AppendLine();
            text.AppendLine("| 場面 | 編成 | 目標 | 測った値 | 目標の幅 | |");
            text.AppendLine("| --- | --- | --- | --- | --- | --- |");
            foreach (var c in checks)
            {
                text.AppendLine($"| {c.Scene} | {c.Party} | {c.Goal} | {Format(c.Measured, c.Unit)} | {Format(c.Min, c.Unit)}〜{Format(c.Max, c.Unit)} | {(c.IsMet ? "○" : "×")} |");
            }

            text.AppendLine();
        }

        private static void AppendStory(StringBuilder text, IReadOnlyList<BalanceCase> cases)
        {
            text.AppendLine("## ストーリー（ひとり）");
            text.AppendLine();
            text.AppendLine("| ステージ | 編成 | うまさ | クリア | 時間 | 受けたダメージ | 倒れた子 | ダウン | ボスの戦い | ボスに当たった |");
            text.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (var c in cases)
            {
                if (!RushTable.IsRush(c.Stage.No))
                {
                    text.AppendLine($"| {c.Stage.Name} | {c.Party.Name} | {SkillName(c.Skill)} | {Percent(c.ClearRate)} | {Seconds(c.ClearSeconds)} | {Percent(c.DamageRatio)} | {c.Fainted:0.0} | {c.Downs:0.0} | {Seconds(c.BossSeconds)} | {c.BossHits:0.0} |");
                }
            }

            text.AppendLine();
            text.AppendLine("受けたダメージは 3 体の最大 HP の合計に対する割合（100% で 3 体分）。時間はクリアできた回の平均。");
            text.AppendLine();
        }

        private static void AppendRush(StringBuilder text, IReadOnlyList<BalanceCase> cases)
        {
            text.AppendLine("## ボスラッシュ（クリア率 ふつうの人 / うまい人）");
            text.AppendLine();
            text.AppendLine("| ステージ | 難易度 | 編成 | 1人 | 2人 | 3人 | 4人 | 時間（3人・ふつう） | 勝った戦い（3人・ふつう） | ボスに当たった（3人・ふつう） |");
            text.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            for (var no = RushTable.FirstNo; no <= RushTable.LastNo; no++)
            {
                for (var d = 0; d < Balance.DifficultyCount; d++)
                {
                    AppendRushRow(text, cases, no, (Difficulty)d);
                }
            }

            text.AppendLine();
        }

        private static void AppendRushRow(StringBuilder text, IReadOnlyList<BalanceCase> cases, int no, Difficulty difficulty)
        {
            var row = new StringBuilder();
            BalanceCase sample = null;
            for (var players = 1; players <= Balance.MaxPlayers; players++)
            {
                var casual = FindRush(cases, no, difficulty, players, BotSkill.Casual);
                var expert = FindRush(cases, no, difficulty, players, BotSkill.Expert);
                if (casual == null || expert == null)
                {
                    return;
                }

                row.Append($" {Percent(casual.ClearRate)} / {Percent(expert.ClearRate)} |");
                if (players == 3)
                {
                    sample = casual;
                }
            }

            text.AppendLine($"| {RushTable.Get(no).Name} | {DifficultyName(difficulty)} | {sample.Party.Name} |{row} {Seconds(sample.ClearSeconds)} | {sample.RoundsWon:0.0} | {sample.BossHits:0.0} |");
        }

        /// <summary>
        /// コインの流れ: 1時間遊んだら、どこで何枚もらえて、ガチャを何回・強化（素材1体）を何回できるか。
        /// 強化1回 ＝ 素材にする子をガチャで引く 100 枚 ＋ 強化のコイン（強化する子のパワーで決まる）
        /// </summary>
        private static void AppendCoins(StringBuilder text, IReadOnlyList<BalanceCase> cases)
        {
            text.AppendLine("## コインの流れ（1回ごとに戦い以外で 30 秒かかるとして）");
            text.AppendLine();
            text.AppendLine("| 遊び方 | 1回のコイン（平均） | 1時間のコイン | ガチャ / 時 | 強化する子のパワー | 強化（素材1体）/ 時 |");
            text.AppendLine("| --- | --- | --- | --- | --- | --- |");
            for (var world = 1; world <= Balance.WorldCount; world++)
            {
                var stage = FindStoryReplay(cases, world);
                if (stage != null)
                {
                    var coins = stage.ClearRate * Balance.ClearCoins + (1f - stage.ClearRate) * Balance.FailCoinsPerEnemy * stage.Defeated;
                    AppendCoinRow(text, $"ストーリー {stage.Stage.Name} をくり返す", coins, stage.Seconds, stage.Party);
                }
            }

            for (var no = RushTable.FirstNo; no <= RushTable.LastNo; no++)
            {
                for (var d = 0; d < Balance.DifficultyCount; d++)
                {
                    // 目標と同じ条件（ふつう・むずかしいは ふつうの人3人、げきむずは うまい人4人）で見る
                    var difficulty = (Difficulty)d;
                    var extreme = difficulty == Difficulty.Extreme;
                    var players = extreme ? Balance.MaxPlayers : 3;
                    var c = FindRush(cases, no, difficulty, players, extreme ? BotSkill.Expert : BotSkill.Casual);
                    if (c != null)
                    {
                        var coins = c.ClearRate * Balance.RushClearCoins(no, difficulty)
                            + (1f - c.ClearRate) * Balance.RushFailCoins(no, difficulty, (int)MathF.Round(c.RoundsWon), (int)MathF.Round(c.Defeated));
                        var who = extreme ? "うまい人4人" : "ふつうの人3人";
                        AppendCoinRow(text, $"ボスラッシュ {RushTable.Get(no).Name} {DifficultyName(difficulty)}（{who}）", coins, c.Seconds, c.Party);
                    }
                }
            }

            text.AppendLine();
            text.AppendLine("ストーリーの初クリアは各ステージ 1 回だけ +" + Balance.FirstClearBonusCoins + "、ボスラッシュの初クリアは各難易度 1 回だけクリアと同じ枚数が上乗せされる（表には入れていない）。");
            text.AppendLine();
        }

        private static void AppendCoinRow(StringBuilder text, string name, float coinsPerPlay, float seconds, BalanceParty party)
        {
            var perHour = coinsPerPlay * SecondsPerHour / (seconds + MenuSecondsPerPlay);
            var power = party.TypicalPower();
            var enhanceCost = Balance.GachaCost + Balance.EnhanceCoinsPerMaterial(power);
            text.AppendLine($"| {name} | {coinsPerPlay:0} | {perHour:0} | {perHour / Balance.GachaCost:0.0} | {power} | {perHour / enhanceCost:0.0} |");
        }

        /// <summary>そのワールドで一番早く回せるステージ（同じワールドの編成で、クリア率 × 1回の速さが一番よいもの）</summary>
        private static BalanceCase FindStoryReplay(IReadOnlyList<BalanceCase> cases, int world)
        {
            BalanceCase best = null;
            var bestRate = 0f;
            foreach (var c in cases)
            {
                if (RushTable.IsRush(c.Stage.No) || c.Stage.World != world || c.Skill != BotSkill.Casual || c.Party.Name != BalanceParty.Story(world).Name)
                {
                    continue;
                }

                var rate = c.ClearRate / (c.Seconds + MenuSecondsPerPlay);
                if (rate > bestRate)
                {
                    bestRate = rate;
                    best = c;
                }
            }

            return best;
        }

        private static BalanceCase FindRush(IReadOnlyList<BalanceCase> cases, int no, Difficulty difficulty, int players, BotSkill skill)
        {
            foreach (var c in cases)
            {
                if (c.Stage.No == no && c.Difficulty == difficulty && c.Players == players && c.Skill == skill)
                {
                    return c;
                }
            }

            return null;
        }

        private static string Format(float value, string unit) => unit == "%" ? Percent(value) : value.ToString("0.#", CultureInfo.InvariantCulture) + unit;

        private static string Percent(float ratio) => (ratio * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";

        private static string Seconds(float seconds) => ((int)MathF.Round(seconds)).ToString(CultureInfo.InvariantCulture) + " 秒";

        private static string SkillName(BotSkill skill) => skill == BotSkill.Expert ? "うまい" : skill == BotSkill.Casual ? "ふつう" : "よけない";

        private static string DifficultyName(Difficulty difficulty) => difficulty == Difficulty.Hard ? "むずかしい" : difficulty == Difficulty.Extreme ? "げきむず" : "ふつう";
    }
}
