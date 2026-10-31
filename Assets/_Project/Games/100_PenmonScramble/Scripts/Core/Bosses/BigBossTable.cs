using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 大ボス3体の定義表。各ワールドの3ステージ目の最後に出る。数値を変えるときはここを直す
    /// </summary>
    public static class BigBossTable
    {
        public const int SkuaNo = 101;
        public const int LeopardSealNo = 102;
        public const int OrcaNo = 103;

        private static readonly BigBossDef[] All =
        {
            // 空から急降下と、羽の雨。速いが体は小さめ
            new BigBossDef(SkuaNo, "オオトウゾクカモメ", Element.Wind, 1.0f, 0.9f, 2.4f, 1.6f,
                new BossAttackDef(BossAttackKind.Charge, 0.8f, Dash(30f, 9f, 8f, 16f, 0.2f)),
                new BossAttackDef(BossAttackKind.Rain, 0.9f, Circles(20f, 5f, 0.9f, 3))),
            // 体当たりと、氷の下から飛び出す。もぐっている間は攻撃が効かない
            new BigBossDef(LeopardSealNo, "ヒョウアザラシ", Element.Ice, 1.2f, 1.0f, 1.8f, 1.5f,
                new BossAttackDef(BossAttackKind.Charge, 0.8f, Dash(32f, 10f, 7f, 13f, 0.2f)),
                new BossAttackDef(BossAttackKind.Ambush, 1.1f, Circles(38f, 10f, 1.6f, 0))),
            // 波と、闘技場を柵まで横切る突進（走る距離は柵で決まるので 0）。一番硬く、休みが短い
            new BigBossDef(OrcaNo, "シャチ", Element.Water, 1.4f, 1.2f, 2.0f, 1.3f,
                new BossAttackDef(BossAttackKind.Wave, 1.0f, Circles(30f, 8f, 0f, 0)),
                new BossAttackDef(BossAttackKind.Charge, 1.0f, Dash(40f, 12f, 0f, 20f, 0.3f), crossScreen: true),
                new BossAttackDef(BossAttackKind.Charge, 0.8f, Dash(34f, 10f, 7f, 15f, 0.3f))),
        };

        /// <summary>番号（101〜）から引く。大ボスでなければ null</summary>
        public static BigBossDef Get(int no)
        {
            var index = no - SkuaNo;
            return index >= 0 && index < All.Length ? All[index] : null;
        }

        /// <summary>HP は協力の人数と、ワールドの倍率で増える（雑魚と同じ考え方）</summary>
        public static UnitStats Stats(BigBossDef boss, int playerCount, int world)
        {
            var hp = boss.HpRate * Balance.EnemyPower * Balance.BigBossHpScale * Balance.EnemyHpScale(playerCount) * Balance.WorldEnemyScale(world);
            return new UnitStats
            {
                SpeciesNo = boss.No,
                Element = boss.Element,
                Radius = boss.Radius,
                MoveSpeed = boss.MoveSpeed,
                MaxHp = UnitStats.HpOf(hp),
                Attack = Balance.BigBossAttack * Balance.WorldEnemyScale(world),
                // 技は持たず、BigBoss の攻撃を順に出す
                Skills = Array.Empty<SkillDef>(),
                IsBoss = true,
                KnockbackScale = Balance.BigBossKnockbackScale,
                BigBoss = boss,
            };
        }

        private static SkillDef Dash(float power, float knockback, float distance, float speed, float extraWidth) => new SkillDef
        {
            Shape = SkillShape.Dash, Power = power, Knockback = knockback, Range = distance, DashSpeed = speed, AreaRadius = extraWidth,
        };

        private static SkillDef Circles(float power, float knockback, float radius, int extraCount) => new SkillDef
        {
            Shape = SkillShape.Drop, Power = power, Knockback = knockback, AreaRadius = radius, Count = extraCount,
        };
    }
}
