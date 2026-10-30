using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>部品ごとの動き（胴・頭・ヒレ・足・目）。体全体の動きは PenguinMotion.Figure.cs</summary>
    public sealed partial class PenguinMotion
    {
        private const float BreathRate = 0.6f;
        private const float BreathScale = 0.02f;
        // まばたきでひとみを縦につぶす割合
        private const float BlinkClose = 0.9f;
        // まばたきの中で目がいちばん閉じる時（割合）。閉じるのは速く、開くのはゆっくり
        private const float BlinkClosedAt = 0.4f;

        // 胴を 90° 傾けたときに持ち上げる高さ（傾きの sin に掛ける）
        private const float LeanLift = 0.16f;

        private const float WalkRoll = 9f;
        private const float WalkBob = 0.05f;
        private const float WalkLean = 6f;
        private const float Stride = 0.12f;
        private const float FootLift = 0.07f;
        private const float FlipperOpen = 22f;
        private const float FlipperFlap = 10f;

        // 技の流れ（割合）: ここまでかがむ → ここまで出す → 戻る。周囲の技の回転は出しどころから少し長め
        private const float AttackWindupEnd = 0.35f;
        private const float AttackActionEnd = 0.55f;
        private const float AttackSpinEnd = 0.85f;

        private const float HitLean = 25f;
        private const float HitHeadLean = 15f;
        private const float HitFlipper = 40f;
        private const float HitShake = 0.04f;
        private const float HitShakeRate = 60f;

        // 吹っ飛び中はヒレを広げてばたつかせる
        private const float KnockFlipperOpen = 70f;
        private const float KnockFlipperFlail = 25f;
        private const float KnockFlailRate = 8f;

        // すべる・倒れるときは顔を上げてカメラに見せ、足は後ろへ伸ばす
        private const float LieHeadLift = 65f;
        private const float LieFeetBack = 70f;
        // すべるときはヒレを体に沿わせる（休みの開きを打ち消す）
        private const float SlideFlipperHug = 10f;
        private const float SlideFlipperBack = 25f;
        // ダウン中のじたばた
        private const float DownFlipperOpen = 55f;
        private const float DownFlail = 35f;
        private const float DownFlailRate = 3f;
        private const float DownKick = 30f;
        private const float DownHeadShake = 12f;

        private void ApplyParts(PenguinPose pose, ref PartPose p)
        {
            var standing = 1f - Mathf.Max(pose.Slide, pose.Down);
            Breathe(pose, ref p);
            Walk(pose, pose.Walk * standing, ref p);
            Attack(pose, standing, ref p);
            SpecialParts(pose, ref p);
            Hit(pose, ref p);
            KnockParts(pose, ref p);
            SlideParts(pose, ref p);
            DownParts(pose, ref p);
            LiftWhenLeaning(ref p);
        }

        /// <summary>
        /// 胴は足元を軸に傾けるので、大きく前後へ傾くと卵形の下の端が床にめり込む。傾いた分だけ持ち上げる
        /// （前へ飛び出す・のけぞるときに少し跳ねたように見えてちょうどよい）
        /// </summary>
        private void LiftWhenLeaning(ref PartPose p)
        {
            var lean = Mathf.DeltaAngle(_rest.Body.Euler.x, p.Body.Euler.x);
            p.Body.Position.y += Mathf.Abs(Mathf.Sin(lean * Mathf.Deg2Rad)) * LeanLift;
        }

        private static void Breathe(PenguinPose pose, ref PartPose p)
        {
            var breath = Mathf.Sin(pose.Time * BreathRate * Mathf.PI * 2f) * BreathScale;
            p.Squash(-breath);
        }

        private void ApplyBlink(PenguinPose pose)
        {
            var close = 1f - Bell(pose.Blink, 0f, BlinkClosedAt, 1f) * BlinkClose;
            for (var i = 0; i < _rest.EyeScales.Length; i++)
            {
                var scale = _rest.EyeScales[i];
                _model.Eyes[i].localScale = new Vector3(scale.x, scale.y * close, scale.z);
            }
        }

        /// <summary>体を左右に揺らしながら跳ねるように歩く。足は前へ出すときだけ持ち上げる</summary>
        private static void Walk(PenguinPose pose, float weight, ref PartPose p)
        {
            if (weight <= 0f)
            {
                return;
            }

            var angle = pose.WalkPhase * Mathf.PI * 2f;
            var swing = Mathf.Sin(angle);
            var lift = Mathf.Cos(angle);
            p.Body.Euler += new Vector3(WalkLean, 0f, swing * WalkRoll) * weight;
            p.Body.Position += Vector3.up * (Mathf.Abs(swing) * WalkBob * weight);

            p.FootLeft.Position += new Vector3(0f, Mathf.Max(0f, lift) * FootLift, swing * Stride) * weight;
            p.FootRight.Position += new Vector3(0f, Mathf.Max(0f, -lift) * FootLift, -swing * Stride) * weight;
            p.OpenFlippers((FlipperOpen + Mathf.Sin(angle * 2f) * FlipperFlap) * weight);
        }

        /// <summary>技は「かがむ（予備動作）→ 出す → 戻る」。技の形（§4.2）ごとの体とヒレの使い方は PenguinMotion.AttackStyles.cs</summary>
        private static void Attack(PenguinPose pose, float weight, ref PartPose p)
        {
            if (pose.Attack >= 1f || weight <= 0f)
            {
                return;
            }

            var style = AttackStyle.Of(pose.AttackShape);
            var windup = Bell(pose.Attack, 0f, AttackWindupEnd, AttackActionEnd) * weight;
            var action = Bell(pose.Attack, AttackWindupEnd, AttackActionEnd, 1f) * weight;
            var spin = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(AttackWindupEnd, AttackSpinEnd, pose.Attack)) * weight;
            float Mix(Vector2 v) => v.x * windup + v.y * action;

            p.Squash(Mix(style.Squash));
            p.Body.Euler += new Vector3(Mix(style.Lean), Mix(style.Twist) + style.Spin * spin, 0f);
            p.Body.Position += new Vector3(0f, Mix(style.Lift), Mix(style.Lunge));
            p.Head.Euler += new Vector3(Mix(style.HeadLean), 0f, 0f);
            p.FlipperRight.Euler += new Vector3(Mix(style.Swing), 0f, Mix(style.Open));
            if (!style.OneFlipper)
            {
                p.FlipperLeft.Euler += new Vector3(Mix(style.Swing), 0f, -Mix(style.Open));
            }
        }

        /// <summary>必殺技: 大きくかがんでヒレを引き、跳んで回る間はヒレを上げる。着地でもう一度つぶれる（跳ぶ・回るのは体全体）</summary>
        private static void SpecialParts(PenguinPose pose, ref PartPose p)
        {
            if (pose.Special >= 1f)
            {
                return;
            }

            var crouch = Bell(pose.Special, 0f, SpecialCrouchEnd, SpecialJumpStart + SpecialBlend);
            var air = Bell(pose.Special, SpecialJumpStart, (SpecialJumpStart + SpecialJumpEnd) * 0.5f, SpecialJumpEnd + SpecialBlend);
            var land = Bell(pose.Special, SpecialJumpEnd - SpecialBlend, SpecialJumpEnd + SpecialBlend, 1f);
            p.Squash(crouch * SpecialCrouch - air * SpecialStretch + land * SpecialLand);
            p.SwingFlippers(crouch * SpecialFlipperWindup);
            p.OpenFlippers(air * SpecialFlipperRaise);
            p.Head.Euler += new Vector3(-air * SpecialHeadLift, 0f, 0f);
        }

        /// <summary>のけぞって震え、ヒレが跳ね上がる。すぐ強く、ゆっくり戻る</summary>
        private static void Hit(PenguinPose pose, ref PartPose p)
        {
            if (pose.Hit >= 1f)
            {
                return;
            }

            var recoil = (1f - pose.Hit) * (1f - pose.Hit);
            p.Body.Euler += new Vector3(-recoil * HitLean, 0f, 0f);
            p.Body.Position += Vector3.right * (Mathf.Sin(pose.Time * HitShakeRate) * HitShake * (1f - pose.Hit));
            p.Head.Euler += new Vector3(-recoil * HitHeadLean, 0f, 0f);
            p.OpenFlippers(recoil * HitFlipper);
        }

        private static void KnockParts(PenguinPose pose, ref PartPose p)
        {
            if (pose.Knock >= 1f)
            {
                return;
            }

            var flying = Mathf.Sin(pose.Knock * Mathf.PI);
            var flail = Mathf.Sin(pose.Time * KnockFlailRate * Mathf.PI * 2f) * KnockFlipperFlail;
            p.OpenFlippers((KnockFlipperOpen + flail) * flying);
        }

        /// <summary>お腹ですべる: 顔を上げ、ヒレは体に沿わせて後ろへ、足は後ろへ伸ばす（体を倒すのは体全体）</summary>
        private static void SlideParts(PenguinPose pose, ref PartPose p)
        {
            var w = pose.Slide * (1f - pose.Down);
            if (w <= 0f)
            {
                return;
            }

            p.Head.Euler += new Vector3(-LieHeadLift * w, 0f, 0f);
            p.OpenFlippers(-SlideFlipperHug * w);
            p.SwingFlippers(SlideFlipperBack * w);
            p.FootLeft.Euler += new Vector3(-LieFeetBack * w, 0f, 0f);
            p.FootRight.Euler += new Vector3(-LieFeetBack * w, 0f, 0f);
        }

        /// <summary>ダウン: 倒れたまま顔を上げ、ヒレを左右交互にじたばた、足もばたつかせる</summary>
        private static void DownParts(PenguinPose pose, ref PartPose p)
        {
            var w = pose.Down;
            if (w <= 0f)
            {
                return;
            }

            var angle = pose.Time * DownFlailRate * Mathf.PI * 2f;
            var flail = Mathf.Sin(angle);
            p.Head.Euler += new Vector3(-LieHeadLift * w, Mathf.Sin(angle * 0.5f) * DownHeadShake * w, 0f);
            p.FlipperLeft.Euler.z -= (DownFlipperOpen + flail * DownFlail) * w;
            p.FlipperRight.Euler.z += (DownFlipperOpen - flail * DownFlail) * w;
            p.FootLeft.Euler += new Vector3((-LieFeetBack + flail * DownKick) * w, 0f, 0f);
            p.FootRight.Euler += new Vector3((-LieFeetBack - flail * DownKick) * w, 0f, 0f);
        }
    }
}
