using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 20種類のペンギンと40個の技の定義表（§5.3）。数値を変えるときはここを直す
    /// </summary>
    public static partial class SpeciesTable
    {
        public const int Count = 20;

        // ★ごとの基本の倍率（§5.3「★2・★3 は HP・攻撃の倍率が少し高い」）。個性の分はそれぞれの行で足し引きする
        private const float Hp1 = 1.2f, Hp2 = 1.35f, Hp3 = 1.5f;
        private const float Atk1 = 1.0f, Atk2 = 1.15f, Atk3 = 1.3f;
        private const float Speed = 4.5f;

        private static readonly SpeciesDef[] All =
        {
            // ---- 氷 ----
            new SpeciesDef(1, "アデリン", Element.Ice, 1, Hp1, Atk1, Speed,
                Melee("ペンギンパンチ", 8f, 0.25f, 1.0f, 60f, 5f),
                Dash("腹すべり", 22f, 2.5f, 4.0f, 14f, 9f)),
            new SpeciesDef(2, "ヒゲジイ", Element.Ice, 1, Hp1 + 0.1f, Atk1, Speed - 0.3f,
                Melee("ヒゲビンタ", 11f, 0.35f, 1.1f, 70f, 7f),
                Spread("つららおとし", 14f, 2.2f, 7f, 11f, 0.25f, 6f, 3, 30f)),
            new SpeciesDef(3, "ジェンツ", Element.Ice, 1, Hp1 - 0.1f, Atk1, Speed + 1f,
                Shot("氷のつぶて", 7f, 0.3f, 7f, 13f, 0.2f, 3f, false),
                Dash("氷上ダッシュ", 18f, 2.2f, 6.5f, 18f, 7f)),
            new SpeciesDef(4, "キングス", Element.Ice, 2, Hp2, Atk2, Speed,
                Melee("王のつえ", 11f, 0.35f, 1.4f, 90f, 6f),
                Dash("王の行進", 28f, 3.0f, 4.0f, 10f, 16f)),
            new SpeciesDef(5, "オヤブン", Element.Ice, 3, Hp3 + 0.2f, Atk3, Speed - 0.6f,
                Nova("ボディプレス", 12f, 0.5f, 1.5f, 6f),
                Drop("氷河の牙", 55f, 3.5f, 7f, 1.6f, 0.6f, 1, 12f)),

            // ---- 水 ----
            new SpeciesDef(6, "フンボ", Element.Water, 1, Hp1 + 0.1f, Atk1, Speed - 0.3f,
                Shot("水てっぽう", 8f, 0.3f, 7f, 12f, 0.22f, 3f, false),
                Zone("ぷかぷか泡", 5f, 3.5f, 2.0f, 1.4f, 3.0f, 0.3f, 1f, 0f)),
            new SpeciesDef(7, "ケープン", Element.Water, 1, Hp1, Atk1, Speed,
                Spread("しぶき", 5f, 0.35f, 4.5f, 11f, 0.18f, 3f, 3, 40f),
                Dash("腹ダイブ", 22f, 2.5f, 4.5f, 14f, 9f)),
            new SpeciesDef(8, "マゼラ", Element.Water, 2, Hp2, Atk2, Speed + 0.3f,
                Melee("水流キック", 11f, 0.3f, 1.1f, 55f, 7f),
                Zone("うず潮", 6f, 4.0f, 2.5f, 1.8f, 3.0f, 0.3f, 0f, 3f)),
            new SpeciesDef(9, "ガラパ", Element.Water, 2, Hp2, Atk2, Speed,
                Nova("汗しぶき", 8f, 0.35f, 1.2f, 4f),
                Drop("間欠泉", 35f, 2.8f, 6f, 1.3f, 0.5f, 2, 10f)),
            new SpeciesDef(10, "ロワイヤル", Element.Water, 3, Hp3, Atk3, Speed,
                Shot("噴水ショット", 10f, 0.3f, 9f, 14f, 0.25f, 4f, true),
                Dash("ビッグウェーブ", 32f, 3.0f, 6.0f, 12f, 12f, 0.6f)),

            // ---- 風 ----
            new SpeciesDef(11, "イワトビ", Element.Wind, 1, Hp1 - 0.1f, Atk1 + 0.1f, Speed + 0.3f,
                Dash("とびげり", 9f, 0.45f, 1.6f, 14f, 6f),
                Nova("ジャンプ着地", 20f, 2.2f, 1.8f, 10f)),
            new SpeciesDef(12, "フィヨル", Element.Wind, 1, Hp1, Atk1, Speed,
                Spread("羽ばたき", 6f, 0.4f, 4f, 10f, 0.25f, 5f, 3, 35f),
                Zone("つむじ風", 5f, 3.5f, 1.8f, 1.4f, 3.0f, 0.25f, 2f, 0f)),
            new SpeciesDef(13, "スネア", Element.Wind, 1, Hp1, Atk1, Speed,
                Melee("羽ドラム", 6f, 0.18f, 1.0f, 50f, 3f),
                Shot("衝撃波", 16f, 2.0f, 10f, 6f, 0.5f, 9f, true)),
            new SpeciesDef(14, "マカロ", Element.Wind, 2, Hp2, Atk2, Speed + 0.3f,
                Nova("スピン", 7f, 0.3f, 1.2f, 5f),
                Dash("大回転", 26f, 2.8f, 5.0f, 10f, 11f, 0.4f)),
            new SpeciesDef(15, "キガシラ", Element.Wind, 3, Hp3, Atk3, Speed,
                Spread("かまいたち", 7f, 0.4f, 7f, 14f, 0.2f, 4f, 5, 60f),
                Zone("天空の竜巻", 9f, 4.5f, 2.5f, 2.6f, 4.0f, 0.25f, 4f, 0f)),

            // ---- 雷 ----
            new SpeciesDef(16, "コガタン", Element.Thunder, 1, Hp1 - 0.2f, Atk1, Speed + 0.8f,
                Melee("ビリビリタッチ", 9f, 0.25f, 0.9f, 60f, 4f),
                Nova("放電", 22f, 2.5f, 2.2f, 9f)),
            new SpeciesDef(17, "ハネジロ", Element.Thunder, 1, Hp1, Atk1, Speed,
                Shot("電気ボール", 9f, 0.35f, 8f, 10f, 0.3f, 4f, false),
                Zone("避雷針", 8f, 4.0f, 2.0f, 1.6f, 4.0f, 0.4f, 3f, 0f)),
            new SpeciesDef(18, "シュレタ", Element.Thunder, 2, Hp2, Atk2, Speed,
                Shot("アンテナビーム", 8f, 0.3f, 10f, 18f, 0.18f, 2f, true),
                Drop("電波ジャック", 28f, 3.0f, 8f, 1.2f, 0.6f, 3, 9f)),
            new SpeciesDef(19, "エレペン", Element.Thunder, 2, Hp2, Atk2, Speed + 0.3f,
                Melee("しびれ尾", 10f, 0.3f, 1.4f, 85f, 5f),
                Dash("稲妻ダッシュ", 26f, 2.5f, 6.0f, 20f, 10f)),
            new SpeciesDef(20, "ライジン", Element.Thunder, 3, Hp3, Atk3, Speed,
                Spread("雷太鼓", 7f, 0.35f, 6f, 13f, 0.22f, 4f, 3, 25f),
                Drop("天の裁き", 40f, 4.0f, 16f, 1.4f, 0.7f, 5, 10f)),
        };

        public static IReadOnlyList<SpeciesDef> Species => All;

        /// <summary>番号（1〜20）から引く。範囲外は null（通信で壊れた値が届いたときに落ちないように）</summary>
        public static SpeciesDef Get(int no)
        {
            return no >= 1 && no <= All.Length ? All[no - 1] : null;
        }

        /// <summary>プレイヤーの1体分（§5.1）。HP ＝ HP の倍率 × パワー、攻撃 ＝ 攻撃の倍率 × パワー / 100</summary>
        public static UnitStats PlayerStats(SpeciesDef species, int power)
        {
            return new UnitStats
            {
                SpeciesNo = species.No,
                Element = species.Element,
                Radius = Balance.PenguinRadius,
                MoveSpeed = species.MoveSpeed,
                MaxHp = Math.Max(1, (int)MathF.Round(species.HpRate * power)),
                Attack = species.AttackRate * power / 100f,
                Skills = new[] { species.Skill1, species.Skill2 },
            };
        }

        /// <summary>野生の敵（§4.7）。技1だけを、待ち時間を長くして使う。HP は協力の人数で増える</summary>
        public static UnitStats EnemyStats(SpeciesDef species, int playerCount)
        {
            return new UnitStats
            {
                SpeciesNo = species.No,
                Element = species.Element,
                Radius = Balance.PenguinRadius,
                MoveSpeed = species.MoveSpeed * Balance.EnemySpeedScale,
                MaxHp = EnemyMaxHp(species, playerCount),
                Attack = species.AttackRate * Balance.EnemyAttackScale,
                Skills = new[] { species.Skill1 },
                CooldownScale = Balance.EnemyCooldownScale,
            };
        }

        public static int EnemyMaxHp(SpeciesDef species, int playerCount)
        {
            return Math.Max(1, (int)MathF.Round(species.HpRate * Balance.EnemyPower * Balance.EnemyHpScale(playerCount)));
        }
    }
}
