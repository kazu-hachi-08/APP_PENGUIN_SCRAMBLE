using PenmonScramble.Core;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 大ボスの動きの状態。BigBossMotion.Apply はこれだけから姿勢を決める（スクショでも同じ流れで撮れるように）。
    /// 1回きりの動き（Strike・Emerge・Hit・Blink）は始まってからの割合（0〜1、1 で終わり）。切り替わる状態（Move・Telegraph・Dash・Submerge）は 0〜1 でなめらかに
    /// </summary>
    public struct BigBossPose
    {
        /// <summary>向き（度。0 で +Z、時計回り）</summary>
        public float Yaw;

        /// <summary>移動している度合いと、羽ばたき・這う・泳ぐ動きの周期の進み（周）</summary>
        public float Move;
        public float Phase;

        /// <summary>攻撃の予告の構え（赤い範囲が出ている間）と、その攻撃の種類</summary>
        public float Telegraph;
        public BossAttackKind TelegraphKind;

        /// <summary>攻撃が出た瞬間の動き（羽を打ち下ろす・波を起こす・かみつく）</summary>
        public float Strike;
        public BossAttackKind StrikeKind;

        /// <summary>突進中</summary>
        public float Dash;

        /// <summary>もぐっている度合い（1 で床の下に隠れきる）と、飛び出す動き</summary>
        public float Submerge;
        public float Emerge;

        public float Hit;

        public bool Defeated;
        public float Defeat;

        public float Blink;
        public float Time;

        public static BigBossPose Idle => new BigBossPose { Strike = 1f, Emerge = 1f, Hit = 1f, Blink = 1f };
    }
}
