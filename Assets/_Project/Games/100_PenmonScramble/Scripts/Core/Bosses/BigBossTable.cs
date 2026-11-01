using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 大ボス5体の定義表。各ワールドの3ステージ目の最後と、ボスラッシュに出る。数値を変えるときはここを直す
    /// </summary>
    public static class BigBossTable
    {
        public const int SkuaNo = 101;
        public const int LeopardSealNo = 102;
        public const int OrcaNo = 103;
        public const int FulmarNo = 104;
        public const int SharkNo = 105;

        private static readonly BigBossDef[] All =
        {
            // 空から急降下と、羽の雨。速いが体は小さめ。本気では羽の雨が2回続けて降る
            new BigBossDef(SkuaNo, "オオトウゾクカモメ", Element.Wind, 1.0f, 0.9f, 2.4f, 1.6f, new BossRage(BossAttackKind.Rain, 1, simultaneous: false),
                new BossAttackDef(BossAttackKind.Charge, 0.8f, Dash(30f, 9f, 8f, 16f, 0.2f)),
                new BossAttackDef(BossAttackKind.Rain, 0.9f, Circles(20f, 5f, 0.9f, 3))),
            // 体当たりと、氷の下から飛び出す。もぐっている間は攻撃が効かない。本気では飛び出しが3回続く
            new BigBossDef(LeopardSealNo, "ヒョウアザラシ", Element.Ice, 1.2f, 1.0f, 1.8f, 1.5f, new BossRage(BossAttackKind.Ambush, 2, simultaneous: false),
                new BossAttackDef(BossAttackKind.Charge, 0.8f, Dash(32f, 10f, 7f, 13f, 0.2f)),
                new BossAttackDef(BossAttackKind.Ambush, 1.1f, Circles(38f, 10f, 1.6f, 0))),
            // 波と、闘技場を柵まで横切る突進（走る距離は柵で決まるので 0）。一番硬く、休みが短い。本気では波が2本同時
            new BigBossDef(OrcaNo, "シャチ", Element.Water, 1.4f, 1.2f, 2.0f, 1.3f, new BossRage(BossAttackKind.Wave, 1, simultaneous: true),
                new BossAttackDef(BossAttackKind.Wave, 1.0f, Circles(30f, 8f, 0f, 0)),
                new BossAttackDef(BossAttackKind.Charge, 1.0f, Dash(40f, 12f, 0f, 20f, 0.3f), crossScreen: true),
                new BossAttackDef(BossAttackKind.Charge, 0.8f, Dash(34f, 10f, 7f, 15f, 0.3f))),
            // 突風で柵へ押し流し、動く竜巻で吸い込み、低く急降下する。カモメより大きく重い。本気では竜巻が2つ増える
            new BigBossDef(FulmarNo, "オオフルマカモメ", Element.Wind, 1.5f, 1.1f, 2.0f, 1.5f, new BossRage(BossAttackKind.Vortex, 2, simultaneous: true),
                new BossAttackDef(BossAttackKind.Gust, 1.0f, new SkillDef
                {
                    Shape = SkillShape.Melee, Power = 24f, Knockback = 6f, Range = 9f, HalfAngleDegrees = 35f, DashSpeed = 10f, Duration = 1.1f,
                }),
                new BossAttackDef(BossAttackKind.Vortex, 1.0f, new SkillDef
                {
                    Shape = SkillShape.Zone, Power = 7f, AreaRadius = 2f, Count = 3, Duration = 4f, Interval = 0.5f, Pull = 2.2f, ChaseSpeed = 1.1f,
                }),
                new BossAttackDef(BossAttackKind.Charge, 0.8f, Dash(30f, 9f, 9f, 14f, 0.25f))),
            // 背びれだけで床を走り回り（攻撃が効かない）、足元から大口で食いつき（溶岩のしぶきが残る）、浮き上がって突っ込む。
            // 一番硬い最後の大ボス。本気では背びれが2本になる
            new BigBossDef(SharkNo, "サメ", Element.Water, 1.6f, 1.25f, 2.1f, 1.3f, new BossRage(BossAttackKind.Fin, 1, simultaneous: true),
                new BossAttackDef(BossAttackKind.Fin, 0.9f, new SkillDef
                {
                    Shape = SkillShape.Dash, Power = 26f, Knockback = 10f, Range = 9f, DashSpeed = 10f, AreaRadius = -0.55f, Count = 2,
                }),
                new BossAttackDef(BossAttackKind.Ambush, 1.1f, Circles(36f, 10f, 2f, 0), leftover: new SkillDef
                {
                    Shape = SkillShape.Zone, Power = 5f, AreaRadius = 0.9f, Count = 3, Duration = 3f, Interval = 0.5f,
                }),
                new BossAttackDef(BossAttackKind.Charge, 1.0f, Dash(36f, 11f, 9f, 14f, 0.3f))),
        };

        /// <summary>大ボスの数（顔の絵の数）</summary>
        public static int Count => All.Length;

        /// <summary>番号（101〜）から引く。大ボスでなければ null</summary>
        public static BigBossDef Get(int no)
        {
            var index = no - SkuaNo;
            return index >= 0 && index < All.Length ? All[index] : null;
        }

        /// <summary>HP は協力の人数と、ワールドの倍率で増える（雑魚と同じ考え方）</summary>
        /// <param name="serious">本気（ボスラッシュの3戦目）。HP 半分を切ると吠えて攻撃が激しくなる</param>
        public static UnitStats Stats(BigBossDef boss, int playerCount, int world, bool serious = false)
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
                IsSerious = serious,
            };
        }

        internal static SkillDef Dash(float power, float knockback, float distance, float speed, float extraWidth) => new SkillDef
        {
            Shape = SkillShape.Dash, Power = power, Knockback = knockback, Range = distance, DashSpeed = speed, AreaRadius = extraWidth,
        };

        internal static SkillDef Circles(float power, float knockback, float radius, int extraCount) => new SkillDef
        {
            Shape = SkillShape.Drop, Power = power, Knockback = knockback, AreaRadius = radius, Count = extraCount,
        };
    }
}
