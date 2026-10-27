using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    public enum PullResult
    {
        Ok,
        NotEnoughCoins,
        Full,
    }

    /// <summary>ガチャ（§7）: ★を確率で決め、その★の種類から1つ選び、パワーを解放したワールドの範囲で決める</summary>
    public sealed partial class PlayerRecord
    {
        public int FreePullsLeft => Math.Max(0, Balance.FreePulls - Data.freePullsUsed);
        public bool IsFull => Data.penguins.Count >= Balance.MaxPenguins;

        public PullResult CanPull()
        {
            if (IsFull)
            {
                return PullResult.Full;
            }

            return FreePullsLeft > 0 || Data.coins >= Balance.GachaCost ? PullResult.Ok : PullResult.NotEnoughCoins;
        }

        /// <summary>1回引く。引けなければ null。無料の回が残っていればコインは使わない</summary>
        public OwnedPenguin Pull(Random random)
        {
            if (CanPull() != PullResult.Ok)
            {
                return null;
            }

            if (FreePullsLeft > 0)
            {
                Data.freePullsUsed++;
            }
            else
            {
                Data.coins -= Balance.GachaCost;
            }

            var species = RollSpecies(random);
            var penguin = new OwnedPenguin(Data.nextPenguinId++, species.No, RollPower(species.Stars, random));
            Data.penguins.Add(penguin);
            // 初回の3回で、そのまま3体の編成ができるようにする
            FillParty();
            return penguin;
        }

        private static SpeciesDef RollSpecies(Random random)
        {
            var stars = Balance.StarsFromRoll(random.Next(100));
            var pool = new List<SpeciesDef>();
            foreach (var species in SpeciesTable.Species)
            {
                if (species.Stars == stars)
                {
                    pool.Add(species);
                }
            }

            return pool[random.Next(pool.Count)];
        }

        private int RollPower(int stars, Random random)
        {
            var worlds = WorldsUnlocked;
            var basePower = random.Next(Balance.PowerMin(worlds), Balance.PowerMax(worlds) + 1);
            return (int)MathF.Round(basePower * Balance.StarPowerScale(stars));
        }
    }
}
