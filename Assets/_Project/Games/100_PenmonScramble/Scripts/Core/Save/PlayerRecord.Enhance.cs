using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    public enum EnhanceResult
    {
        Ok,
        /// <summary>強化する子がいない・知らない種類</summary>
        NoTarget,
        NoMaterials,
        TooManyMaterials,
        /// <summary>編成の子・強化する子自身・いない子・重なった子が素材に入っている</summary>
        InvalidMaterial,
        /// <summary>上限まで上がっていて、上限解放もできない（素材を使っても何も変わらない）</summary>
        NoEffect,
        NotEnoughCoins,
    }

    /// <summary>強化した（する）ときの見込み。画面の「150 → 182」と、実行した結果の両方に使う</summary>
    public readonly struct EnhancePlan
    {
        public EnhancePlan(EnhanceResult result, int powerBefore, int powerAfter, int capBefore, int capAfter,
            int limitBreakBefore, int limitBreakAfter, int overflow, int coins, bool needsConfirm)
        {
            Result = result;
            PowerBefore = powerBefore;
            PowerAfter = powerAfter;
            CapBefore = capBefore;
            CapAfter = capAfter;
            LimitBreakBefore = limitBreakBefore;
            LimitBreakAfter = limitBreakAfter;
            Overflow = overflow;
            Coins = coins;
            NeedsConfirm = needsConfirm;
        }

        public EnhanceResult Result { get; }
        public int PowerBefore { get; }
        public int PowerAfter { get; }
        public int CapBefore { get; }
        public int CapAfter { get; }
        public int LimitBreakBefore { get; }
        public int LimitBreakAfter { get; }

        /// <summary>上限を超えて捨てられる分</summary>
        public int Overflow { get; }

        public int Coins { get; }

        /// <summary>★3 や強化する子より強い子が素材に入っている（売るのと同じ2回押しにする）</summary>
        public bool NeedsConfirm { get; }

        public bool CanEnhance => Result == EnhanceResult.Ok;
        public bool LimitBroken => LimitBreakAfter > LimitBreakBefore;
    }

    /// <summary>
    /// 強化と上限解放: どの子でも素材にしてパワーを足し、同じ種類を素材にすると上限が上がる。
    /// 上限解放を先に数えてから上限を決める（同じ種類を入れた分の上がった上限まで、ほかの素材のパワーも入る）
    /// </summary>
    public sealed partial class PlayerRecord
    {
        public static int PowerCap(OwnedPenguin penguin) => Balance.PowerCap(penguin.basePower, penguin.limitBreak);

        /// <summary>1体を素材にしたときに上がる量（上限で止まる前）</summary>
        public static int EnhanceGain(OwnedPenguin material)
        {
            var species = SpeciesTable.Get(material.speciesNo);
            return Balance.EnhanceGain(species?.Stars ?? 1, material.Power);
        }

        /// <summary>素材にできるか（強化する子の候補一覧で、押せる・押せないを決める）</summary>
        public bool CanBeMaterial(int targetId, OwnedPenguin material)
        {
            return material != null && material.id != targetId && !IsInParty(material.id) && IsUsable(material);
        }

        public static bool IsSameSpecies(OwnedPenguin target, OwnedPenguin material)
        {
            return target != null && material != null && target.speciesNo == material.speciesNo;
        }

        /// <summary>
        /// 素材を選ぶ一覧の並び: 素材にできる同じ種類（上限UP）→ 素材にできる子 → 編成の子（押せない）。
        /// それぞれの中は弱い順（いらない子から選びやすいように）
        /// </summary>
        public List<OwnedPenguin> MaterialCandidates(int targetId)
        {
            var target = Find(targetId);
            var list = Data.penguins.FindAll(p => p.id != targetId);
            list.Sort((a, b) =>
            {
                var order = CandidateGroup(target, a).CompareTo(CandidateGroup(target, b));
                if (order == 0)
                {
                    order = a.Power.CompareTo(b.Power);
                }

                return order != 0 ? order : a.id.CompareTo(b.id);
            });
            return list;
        }

        private int CandidateGroup(OwnedPenguin target, OwnedPenguin penguin)
        {
            if (!CanBeMaterial(target?.id ?? 0, penguin))
            {
                return 2;
            }

            return IsSameSpecies(target, penguin) ? 0 : 1;
        }

        /// <summary>強化したらどうなるかを計算する（記録は変えない）</summary>
        public EnhancePlan PlanEnhance(int targetId, IReadOnlyList<int> materialIds)
        {
            var target = Find(targetId);
            if (!IsUsable(target))
            {
                return new EnhancePlan(EnhanceResult.NoTarget, 0, 0, 0, 0, 0, 0, 0, 0, false);
            }

            var materials = CollectMaterials(targetId, materialIds, out var valid);
            var power = target.Power;
            var limitBreak = target.limitBreak;
            foreach (var material in materials)
            {
                if (IsSameSpecies(target, material) && limitBreak < Balance.MaxLimitBreak)
                {
                    limitBreak++;
                }
            }

            var gain = 0;
            var needsConfirm = false;
            foreach (var material in materials)
            {
                gain += EnhanceGain(material);
                needsConfirm |= IsPrecious(target, material);
            }

            var capBefore = PowerCap(target);
            var capAfter = Balance.PowerCap(target.basePower, limitBreak);
            // 強化の前から上限を超えている子（数値の調整で上限が下がったときなど）は、下げずにそのまま
            var powerAfter = Math.Max(power, Math.Min(power + gain, capAfter));
            var overflow = power + gain - powerAfter;
            var coins = materials.Count * Balance.EnhanceCoinsPerMaterial(power);
            var result = !valid ? EnhanceResult.InvalidMaterial
                : materials.Count == 0 ? EnhanceResult.NoMaterials
                : materials.Count > Balance.MaxEnhanceMaterials ? EnhanceResult.TooManyMaterials
                : powerAfter == power && limitBreak == target.limitBreak ? EnhanceResult.NoEffect
                : Data.coins < coins ? EnhanceResult.NotEnoughCoins
                : EnhanceResult.Ok;
            return new EnhancePlan(result, power, powerAfter, capBefore, capAfter, target.limitBreak, limitBreak, overflow, coins, needsConfirm);
        }

        /// <summary>強化する。素材は消え、コインを払う。できなければ記録を変えずに理由を返す。保存は呼んだ側で行う</summary>
        public EnhancePlan Enhance(int targetId, IReadOnlyList<int> materialIds)
        {
            var plan = PlanEnhance(targetId, materialIds);
            if (!plan.CanEnhance)
            {
                return plan;
            }

            var target = Find(targetId);
            foreach (var id in materialIds)
            {
                Data.penguins.Remove(Find(id));
            }

            Data.coins -= plan.Coins;
            target.limitBreak = plan.LimitBreakAfter;
            target.boost = plan.PowerAfter - target.basePower;
            return plan;
        }

        /// <summary>素材を集める。1体でも素材にできない子・重なった子がいれば valid を false にする</summary>
        private List<OwnedPenguin> CollectMaterials(int targetId, IReadOnlyList<int> materialIds, out bool valid)
        {
            valid = true;
            var materials = new List<OwnedPenguin>();
            foreach (var id in materialIds ?? Array.Empty<int>())
            {
                var material = Find(id);
                if (!CanBeMaterial(targetId, material) || materials.Contains(material))
                {
                    valid = false;
                    continue;
                }

                materials.Add(material);
            }

            return materials;
        }

        private static bool IsPrecious(OwnedPenguin target, OwnedPenguin material)
        {
            var species = SpeciesTable.Get(material.speciesNo);
            return (species != null && species.Stars >= Balance.MaxStars) || material.Power > target.Power;
        }
    }
}
