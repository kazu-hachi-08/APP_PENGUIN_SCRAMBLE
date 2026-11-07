using System;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>CPU の曲げ方の型</summary>
    public enum BaseballBendPlan
    {
        /// <summary>曲げない</summary>
        Straight,
        /// <summary>外へ逃げる（右打ちの打者から遠い、一塁側）</summary>
        Away,
        /// <summary>内へ食い込む（打者の側、三塁側）</summary>
        Inside,
        /// <summary>曲げられる区間の後半だけ曲げる（打つ側が読みにくい）</summary>
        Late,
    }

    /// <summary>CPU の1球の組み立て（球種・曲げ方の型・ホームの線で通したい所）</summary>
    public readonly struct BaseballPitchPlan
    {
        public BaseballPitchPlan(BaseballPitchType type, BaseballBendPlan bend, float targetX)
        {
            Type = type;
            Bend = bend;
            TargetX = targetX;
        }

        public BaseballPitchType Type { get; }
        public BaseballBendPlan Bend { get; }

        /// <summary>ホームの線を通したい横の位置（ホームの真ん中が 0。右が正）</summary>
        public float TargetX { get; }
    }

    /// <summary>CPU のうまさ（★ごとの値はフェーズ 3 の試合の確認で決める）</summary>
    public readonly struct BaseballAiSkill
    {
        public BaseballAiSkill(float timingErrorSeconds, float takeBallChance, float aimError)
        {
            TimingErrorSeconds = timingErrorSeconds;
            TakeBallChance = takeBallChance;
            AimError = aimError;
        }

        /// <summary>振る時刻のぶれ（秒。ばらつきの標準偏差）。強さの一番の差</summary>
        public float TimingErrorSeconds { get; }

        /// <summary>ゾーンの外の球を見送る確率</summary>
        public float TakeBallChance { get; }

        /// <summary>投げるときにゾーンの端を狙うぶれ（マス。標準偏差）</summary>
        public float AimError { get; }
    }

    /// <summary>
    /// CPU（Docs/MINIGAME_BASEBALL_PLAN.md「CPU」の最初の形。フェーズ 2 の試しの打席用。強さ・バント・投げる子の選び方はフェーズ 3 で足す）。
    /// 投げる: 球種と曲げ方の型を乱数で選び、曲げられる区間が終わるまでにねらいの所へ寄るよう曲げる入力を出す。
    /// 打つ: 曲げられる区間が終わって球の通る所が決まったら、見送るか振るかを決め、ジャストの時刻にぶれを足した時刻に振る
    /// （前へ来る速さは球種で一定なので、時刻は読める。ぶれは曲げられた球・速い球ほど大きい）
    /// </summary>
    public sealed class BaseballAi
    {
        private readonly Random _random;
        private readonly BaseballAiSkill _skill;
        private bool _decided;
        private bool _willSwing;
        private float _swingLead;

        public BaseballAi(int seed, BaseballAiSkill skill)
        {
            _random = new Random(seed);
            _skill = skill;
        }

        // ---- 投げる ----

        public BaseballPitchPlan PlanPitch(BaseballFieldDef field)
        {
            var type = (BaseballPitchType)_random.Next(3);
            var bend = PickBend();
            if (bend == BaseballBendPlan.Straight)
            {
                return new BaseballPitchPlan(type, bend, 0f);
            }

            var side = bend == BaseballBendPlan.Inside ? -1f
                : bend == BaseballBendPlan.Away ? 1f
                : _random.Next(2) == 0 ? -1f : 1f;
            var target = _random.NextDouble() < BaseballBalance.AiWasteChance
                ? side * (field.ZoneHalfWidth + BaseballBalance.AiWasteMargin)
                : side * field.ZoneHalfWidth * BaseballBalance.AiEdgeAim + Gaussian() * _skill.AimError;
            return new BaseballPitchPlan(type, bend, target);
        }

        private BaseballBendPlan PickBend()
        {
            var roll = _random.NextDouble();
            if (roll < BaseballBalance.AiStraightChance)
            {
                return BaseballBendPlan.Straight;
            }

            if (roll < BaseballBalance.AiStraightChance + BaseballBalance.AiLateChance)
            {
                return BaseballBendPlan.Late;
            }

            return _random.Next(2) == 0 ? BaseballBendPlan.Away : BaseballBendPlan.Inside;
        }

        /// <summary>
        /// この歩の曲げる入力（−1〜1）。曲げられる区間の終わりの横の位置がねらいに来るよう、残りの時間で要る横の加速を求める
        /// （区間の後はまっすぐ来るので、区間の終わりの位置がそのままホームの線の位置になる）
        /// </summary>
        public static float Bend(BaseballPitch pitch, BaseballPitchPlan plan, BaseballFieldDef field)
        {
            if (!pitch.CanBend || plan.Bend == BaseballBendPlan.Straight)
            {
                return 0f;
            }

            var remain = (pitch.Position.Y - pitch.BendEndY) / pitch.ForwardSpeed;
            var total = (field.Mound.Y - pitch.BendEndY) / pitch.ForwardSpeed;
            if (plan.Bend == BaseballBendPlan.Late && remain > total * BaseballBalance.AiLateFraction)
            {
                return 0f;
            }

            var needed = plan.TargetX - pitch.Position.X - pitch.LateralSpeed * remain;
            var accel = 2f * needed / Math.Max(remain * remain, 0.0001f);
            return Math.Clamp(accel / pitch.Spec.BendAccel, -1f, 1f);
        }

        // ---- 打つ ----

        /// <summary>次の球のために読みを消す</summary>
        public void ResetBatter()
        {
            _decided = false;
            _willSwing = false;
        }

        /// <summary>
        /// この歩で振るか。振るなら swingTime に、この歩の始めから振り始めるまでの秒を入れる
        /// （人の入力と同じく歩の中の時刻まで渡すので、歩の刻みでジャストを外さない）
        /// </summary>
        public bool TrySwing(BaseballBoard board, out float swingTime)
        {
            swingTime = 0f;
            var pitch = board.Pitch;
            if (board.Phase != BaseballBoardPhase.Pitching || !pitch.IsActive || board.Bat.HasAttempted)
            {
                return false;
            }

            if (!_decided)
            {
                // 曲げられる区間の間は通る所がまだ決まらないので待つ
                if (pitch.CanBend)
                {
                    return false;
                }

                Decide(board);
            }

            if (!_willSwing)
            {
                return false;
            }

            var toBat = (pitch.Position.Y - BaseballBalance.BallRadius - board.Field.BatPivot.Y) / pitch.ForwardSpeed;
            swingTime = toBat - _swingLead;
            if (swingTime >= BaseballRules.StepSeconds)
            {
                return false;
            }

            // 読みが遅れて振る時刻を過ぎていたら、すぐ振る（前の歩までさかのぼって振ったことにはしない）
            swingTime = Math.Max(swingTime, 0f);
            return true;
        }

        private void Decide(BaseballBoard board)
        {
            _decided = true;
            var pitch = board.Pitch;
            var inZone = Math.Abs(pitch.Position.X) <= board.Field.ZoneHalfWidth;
            _willSwing = inZone || _random.NextDouble() >= _skill.TakeBallChance;
            var bent = Math.Abs(pitch.Position.X - board.Field.Mound.X);
            var difficulty = BaseballBalance.AiTimingDifficulty(pitch.Type) * (1f + BaseballBalance.AiBendDifficulty * bent);
            _swingLead = BaseballBat.SecondsToSquare + Gaussian() * _skill.TimingErrorSeconds * difficulty;
        }

        /// <summary>標準正規分布の乱数（Box–Muller）</summary>
        private float Gaussian()
        {
            var u1 = 1.0 - _random.NextDouble();
            var u2 = _random.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }
    }
}
