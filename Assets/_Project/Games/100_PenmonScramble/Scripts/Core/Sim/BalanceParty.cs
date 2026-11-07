using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// バランスの試験場で連れていく編成の想定（その場面に来る人が持っていそうな3体）。
    /// パワー ＝ 引いたときのパワー（そのワールドのガチャの範囲 × ★の倍率）から、上限（上限解放の回数で決まる）までを Growth の割合だけ育てた値
    /// </summary>
    public sealed class BalanceParty
    {
        private BalanceParty(string name, int gachaWorld, int[] stars, int limitBreak, float growth)
        {
            Name = name;
            GachaWorld = gachaWorld;
            Stars = stars;
            LimitBreak = limitBreak;
            Growth = growth;
        }

        public string Name { get; }

        /// <summary>何ワールド目までを解放したときのガチャで引いた子か</summary>
        public int GachaWorld { get; }

        /// <summary>3体の★。null なら初回の無料ガチャと同じ確率で引く</summary>
        public IReadOnlyList<int> Stars { get; }

        public int LimitBreak { get; }

        /// <summary>引いたときのパワーから上限までの、どこまで育てたか（0 で強化なし、1 で上限まで）</summary>
        public float Growth { get; }

        /// <summary>1-1: 無料の3回で引いた子（強化なし）</summary>
        public static BalanceParty Starter => new BalanceParty("無料3回の子", 1, null, 0, 0f);

        /// <summary>そのワールドのガチャで ★2 を混ぜた3体を、少し強化した（上限の 3 割ほど）</summary>
        public static BalanceParty Story(int world) => new BalanceParty($"W{world} のガチャ＋少し強化", world, StoryStars(world), 0, 0.3f);

        /// <summary>5-3: 強化を進めた子（上限解放1回の上限の 8 割）</summary>
        public static BalanceParty Advanced(int world) => new BalanceParty($"W{world} のガチャ＋強化を進めた", world, new[] { 3, 2, 2 }, 1, 0.8f);

        /// <summary>ボスラッシュ むずかしい: ふつうの編成と同じ子を、上限解放を何回か（2回）して上限まで育てた</summary>
        public static BalanceParty LimitBroken(int world) => new BalanceParty($"W{world} のガチャ＋上限解放2回", world, StoryStars(world), 2, 1f);

        /// <summary>
        /// ボスラッシュ げきむず: ふつうの編成の★を1つずつ上げた子（★3 中心。ワールド4・5では ★3 の3体）を、上限まで育てた。
        /// どのワールドでも、ふつう → げきむず の編成の強さの開きをそろえるため、★はふつうの編成から決める
        /// </summary>
        public static BalanceParty Maxed(int world) => new BalanceParty($"W{world} の★3 中心を上限まで", world, RaiseStars(StoryStars(world)), Balance.MaxLimitBreak, 1f);

        /// <summary>ボスラッシュの難易度ごとに、その難易度に挑む人の編成</summary>
        public static BalanceParty ForRush(int world, Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Hard:
                    return LimitBroken(world);
                case Difficulty.Extreme:
                    return Maxed(world);
                default:
                    return Story(world);
            }
        }

        // 先のワールドほど、それまでに引いた回数が多く ★2・★3 がそろっている
        private static int[] StoryStars(int world)
        {
            switch (world)
            {
                case 1:
                    return new[] { 2, 1, 1 };
                case 2:
                case 3:
                    return new[] { 2, 2, 1 };
                default:
                    return new[] { 3, 2, 2 };
            }
        }

        private static int[] RaiseStars(int[] stars)
        {
            var raised = new int[stars.Length];
            for (var i = 0; i < stars.Length; i++)
            {
                raised[i] = Math.Min(Balance.MaxStars, stars[i] + 1);
            }

            return raised;
        }

        /// <summary>この想定の3体を、random で種類とガチャのパワーを決めて作る</summary>
        public UnitStats[] Create(Random random)
        {
            var party = new UnitStats[Balance.PartySize];
            var used = new List<int>();
            for (var i = 0; i < party.Length; i++)
            {
                var stars = Stars != null ? Stars[i] : Balance.StarsFromRoll(random.Next(100));
                var species = PickSpecies(stars, used, random);
                used.Add(species.No);
                party[i] = SpeciesTable.PlayerStats(species, PowerOf(stars, random));
            }

            return party;
        }

        /// <summary>引いたときのパワーを決め、上限まで Growth の割合だけ育てる</summary>
        public int PowerOf(int stars, Random random)
        {
            var roll = random.Next(Balance.PowerMin(GachaWorld), Balance.PowerMax(GachaWorld) + 1);
            var basePower = (int)MathF.Round(roll * Balance.StarPowerScale(stars));
            var cap = Balance.PowerCap(basePower, LimitBreak);
            return basePower + (int)MathF.Round((cap - basePower) * Growth);
        }

        /// <summary>3体の平均パワー（コインの流れの表で、強化のコインの目安に使う）</summary>
        public int TypicalPower()
        {
            var total = 0f;
            for (var i = 0; i < Balance.PartySize; i++)
            {
                var stars = Stars != null ? Stars[i] : 1;
                var basePower = (Balance.PowerMin(GachaWorld) + Balance.PowerMax(GachaWorld)) * 0.5f * Balance.StarPowerScale(stars);
                var cap = Balance.PowerCap((int)MathF.Round(basePower), LimitBreak);
                total += basePower + (cap - basePower) * Growth;
            }

            return (int)MathF.Round(total / Balance.PartySize);
        }

        private static SpeciesDef PickSpecies(int stars, List<int> used, Random random)
        {
            var pool = new List<SpeciesDef>();
            foreach (var species in SpeciesTable.Species)
            {
                if (species.Stars == stars && !used.Contains(species.No))
                {
                    pool.Add(species);
                }
            }

            return pool[random.Next(pool.Count)];
        }
    }
}
