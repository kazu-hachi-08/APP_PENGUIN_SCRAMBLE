using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンギンの動きの状態。PenguinMotion.Apply はこれだけから姿勢を決めるので、試作の場のスクショでは時刻を止めた姿勢を直接置ける。
    /// 1回きりの動き（Attack・Special・Hit・Knock・Blink・Kick・Throw・Catch）は始まってからの割合（0〜1、1 で終わり）。Defeat は Defeated の間だけ使う
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

        /// <summary>結果の画面で喜んで跳ねている（0〜1。切り替えはなめらかに）。CheerTime は喜び始めてからの秒（負の間は待つ）</summary>
        public float Cheer;
        public float CheerTime;

        /// <summary>
        /// 両ヒレで物（ゴルフのクラブなど）を体の前で持っている（0〜1。切り替えはなめらかに）。
        /// HoldSwing はその物を左右へ振る角度（度。正で右へ振り上げる、負で左へ振り抜く）
        /// </summary>
        public float Hold;
        public float HoldSwing;

        /// <summary>しくじって頭を抱えている（0〜1。切り替えはなめらかに）</summary>
        public float Despair;

        /// <summary>
        /// ボールを蹴る（ミニゲームのサッカー）。KickCharge はためて足を引いている深さ（0〜1。なめらかに追う）、
        /// Kick は振り抜きの割合（1回きり）、KickPower はその強さ（0〜1）、KickFrom は振り始めたときのため
        /// </summary>
        public float KickCharge;
        public float Kick;
        public float KickPower;
        public float KickFrom;

        /// <summary>
        /// 球を投げる（ミニゲームの野球）。ThrowCharge は振りかぶっている深さ（0〜1。なめらかに追う）、
        /// Throw は振り下ろしの割合（1回きり）、ThrowFrom は振り下ろし始めたときの振りかぶり
        /// </summary>
        public float ThrowCharge;
        public float Throw;
        public float ThrowFrom;

        /// <summary>守備の構え（0〜1。切り替えはなめらかに）と、球を捕る動きの割合（1回きり。ミニゲームの野球）</summary>
        public float Field;
        public float Catch;

        public float Blink;
        public float Time;

        public static PenguinPose Idle => new PenguinPose { Attack = 1f, Special = 1f, Hit = 1f, Knock = 1f, Blink = 1f, Kick = 1f, Throw = 1f, Catch = 1f };
    }
}
