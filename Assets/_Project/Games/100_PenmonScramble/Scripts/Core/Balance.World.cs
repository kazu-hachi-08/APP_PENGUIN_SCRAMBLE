using System;

namespace PenmonScramble.Core
{
    /// <summary>ワールドごとの敵の強さと、大ボスの調整値</summary>
    public static partial class Balance
    {
        public const int WorldCount = 3;

        // ワールドごとの敵の HP・攻撃の倍率。添字はワールド-1。
        // ガチャのパワーの範囲（PowerMins / PowerMaxes の真ん中 100 / 200 / 340）に合わせて、先のワールドでも手ごたえを残す
        private static readonly float[] WorldEnemyScales = { 1f, 2f, 3.4f };

        public static float WorldEnemyScale(int world)
        {
            return WorldEnemyScales[Math.Clamp(world, 1, WorldCount) - 1];
        }

        // 大ボス
        /// <summary>大ボスの HP ＝ 種類の HP の倍率 × EnemyPower × これ × 人数の倍率 × ワールドの倍率。ステージのボス（BossHpScale）より硬い</summary>
        public const float BigBossHpScale = 30f;
        /// <summary>大ボスの攻撃 ＝ これ × ワールドの倍率。技の威力に掛ける</summary>
        public const float BigBossAttack = 1f;
        /// <summary>大ボスが飛ばされる強さの割合。攻撃の予告の場所がずれないよう、ほとんど動かさない</summary>
        public const float BigBossKnockbackScale = 0.1f;
        /// <summary>攻撃と攻撃の間は、一番近いプレイヤーからこれだけ離れた所まで寄る（マス）。くっついて押し合いにならないように</summary>
        public const float BigBossKeepDistance = 3f;
        /// <summary>
        /// 波の帯の幅の半分（マス）。帯は狙ったプレイヤーの高さを中心にする。
        /// 闘技場の上半分・下半分を丸ごと流すと、柵ぎわにいたときに予告の間に逃げきれないため、どこにいても 2.5 マスほど歩けば抜けられる太さにする
        /// </summary>
        public const float BigBossWaveHalfWidth = 2f;

        /// <summary>最初の攻撃までの秒数。出てきてすぐ襲ってこないように</summary>
        public const float BigBossFirstAttackDelay = 2f;
    }
}
