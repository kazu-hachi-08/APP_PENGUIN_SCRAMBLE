using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンギンの動きの状態。PenguinMotion.Apply はこれだけから姿勢を決めるので、試作の場のスクショでは時刻を止めた姿勢を直接置ける。
    /// 1回きりの動き（Attack・Special・Hit・Knock・Blink）は始まってからの割合（0〜1、1 で終わり）。Defeat は Defeated の間だけ使う
    /// </summary>
    public struct PenguinPose
    {
        /// <summary>向き（度。0 で +Z、時計回り）</summary>
        public float Yaw;

        public float Walk;
        public float WalkPhase;

        public float Attack;
        public SkillShape AttackShape;

        /// <summary>必殺技・合体技</summary>
        public float Special;

        public float Hit;

        /// <summary>吹っ飛び。向き（根元から見た床の上の向き）と、その間に転がる回数</summary>
        public float Knock;
        public Vector3 KnockDirection;
        public float KnockTurns;

        /// <summary>突進中にお腹ですべる・ダウンして倒れている（0〜1。切り替えはなめらかに）</summary>
        public float Slide;
        public float Down;

        /// <summary>技の前の構えで膨らんでいる（0〜1。切り替えはなめらかに）</summary>
        public float WindUp;

        public bool Defeated;
        public float Defeat;

        public float Blink;
        public float Time;

        public static PenguinPose Idle => new PenguinPose { Attack = 1f, Special = 1f, Hit = 1f, Knock = 1f, Blink = 1f };
    }
}
