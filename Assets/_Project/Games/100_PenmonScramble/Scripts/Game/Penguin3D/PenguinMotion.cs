using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 3D のペンギンの部品を動かす（歩く・技・被弾。フェーズ0の仮の動き）。
    /// 毎フレームの姿勢は PenguinPose だけから決まるので、試作の場のスクショでは時刻を止めた姿勢を Apply で直接置ける
    /// </summary>
    [RequireComponent(typeof(PenguinModel))]
    public sealed class PenguinMotion : MonoBehaviour
    {
        // 向きを変える速さ（度/秒）。速すぎるとカクッと見える
        private const float TurnSpeed = 720f;
        // 立ち止まり⇔歩きの切り替えにかける時間の逆数
        private const float WalkBlendRate = 6f;
        private const float MovingThreshold = 0.05f;
        // 1マス進むあいだの歩数（左右で1周）
        private const float CyclesPerUnit = 0.9f;

        private const float BreathRate = 0.6f;
        private const float BreathScale = 0.02f;

        private const float WalkRoll = 9f;
        private const float WalkBob = 0.05f;
        private const float WalkLean = 6f;
        private const float Stride = 0.12f;
        private const float FootLift = 0.07f;
        private const float FlipperOpen = 22f;
        private const float FlipperFlap = 10f;

        public const float AttackSeconds = 0.5f;
        // 技の流れ（割合）: ここまでかがむ → ここまで前へ伸びる → 戻る
        private const float AttackWindupEnd = 0.35f;
        private const float AttackLungeEnd = 0.55f;
        private const float CrouchSquash = 0.18f;
        private const float WindupLean = 12f;
        private const float LungeLean = 22f;
        private const float LungeDistance = 0.25f;
        private const float FlipperWindup = 35f;
        private const float FlipperSwing = 80f;

        public const float HitSeconds = 0.4f;
        private const float HitLean = 25f;
        private const float HitHeadLean = 15f;
        private const float HitFlipper = 40f;
        private const float HitShake = 0.04f;
        private const float HitShakeRate = 60f;

        private PenguinModel _model;
        private Rest _rest;
        private PenguinPose _pose = PenguinPose.Idle;

        /// <summary>床の上での速度（マス/秒）。向きと歩きの揺れに使う</summary>
        public Vector3 Velocity { get; set; }

        public void PlayAttack() => _pose.Attack = 0f;

        public void PlayHit() => _pose.Hit = 0f;

        private void Awake() => Bind();

        private void Bind()
        {
            if (_model != null)
            {
                return;
            }

            _model = GetComponent<PenguinModel>();
            _rest = new Rest(_model);
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            var speed = new Vector3(Velocity.x, 0f, Velocity.z).magnitude;
            var moving = speed > MovingThreshold;
            if (moving)
            {
                var target = Quaternion.LookRotation(new Vector3(Velocity.x, 0f, Velocity.z));
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, TurnSpeed * dt);
            }

            _pose.Walk = Mathf.MoveTowards(_pose.Walk, moving ? 1f : 0f, WalkBlendRate * dt);
            _pose.WalkPhase = Mathf.Repeat(_pose.WalkPhase + speed * CyclesPerUnit * dt, 1f);
            _pose.Attack = Mathf.Min(1f, _pose.Attack + dt / AttackSeconds);
            _pose.Hit = Mathf.Min(1f, _pose.Hit + dt / HitSeconds);
            _pose.Time += dt;
            Apply(_pose);
        }

        /// <summary>姿勢を部品に置く。毎回休みの姿勢から組み直すので、前のフレームの値が残らない</summary>
        public void Apply(PenguinPose pose)
        {
            Bind();
            var body = new Offset(_rest.Body);
            var head = new Offset(_rest.Head);
            var flipperL = new Offset(_rest.FlipperLeft);
            var flipperR = new Offset(_rest.FlipperRight);
            var footL = new Offset(_rest.FootLeft);
            var footR = new Offset(_rest.FootRight);

            Breathe(pose, ref body);
            Walk(pose, ref body, ref flipperL, ref flipperR, ref footL, ref footR);
            Attack(pose, ref body, ref flipperL, ref flipperR);
            Hit(pose, ref body, ref head, ref flipperL, ref flipperR);

            body.ApplyTo(_model.Body);
            head.ApplyTo(_model.Head);
            flipperL.ApplyTo(_model.FlipperLeft);
            flipperR.ApplyTo(_model.FlipperRight);
            footL.ApplyTo(_model.FootLeft);
            footR.ApplyTo(_model.FootRight);
        }

        private static void Breathe(PenguinPose pose, ref Offset body)
        {
            var breath = Mathf.Sin(pose.Time * BreathRate * Mathf.PI * 2f) * BreathScale;
            body.Scale = Vector3.Scale(body.Scale, new Vector3(1f - breath * 0.5f, 1f + breath, 1f - breath * 0.5f));
        }

        /// <summary>体を左右に揺らしながら跳ねるように歩く。足は前へ出すときだけ持ち上げる</summary>
        private static void Walk(PenguinPose pose, ref Offset body, ref Offset flipperL, ref Offset flipperR, ref Offset footL, ref Offset footR)
        {
            if (pose.Walk <= 0f)
            {
                return;
            }

            var angle = pose.WalkPhase * Mathf.PI * 2f;
            var swing = Mathf.Sin(angle);
            var lift = Mathf.Cos(angle);
            body.Euler += new Vector3(WalkLean, 0f, swing * WalkRoll) * pose.Walk;
            body.Position += Vector3.up * (Mathf.Abs(swing) * WalkBob * pose.Walk);

            footL.Position += new Vector3(0f, Mathf.Max(0f, lift) * FootLift, swing * Stride) * pose.Walk;
            footR.Position += new Vector3(0f, Mathf.Max(0f, -lift) * FootLift, -swing * Stride) * pose.Walk;

            var flap = Mathf.Sin(angle * 2f) * FlipperFlap;
            flipperL.Euler += new Vector3(0f, 0f, -(FlipperOpen + flap)) * pose.Walk;
            flipperR.Euler += new Vector3(0f, 0f, FlipperOpen + flap) * pose.Walk;
        }

        /// <summary>かがんでヒレを後ろへ引き、前へ伸びながらヒレを振り出す</summary>
        private static void Attack(PenguinPose pose, ref Offset body, ref Offset flipperL, ref Offset flipperR)
        {
            if (pose.Attack >= 1f)
            {
                return;
            }

            var crouch = Bell(pose.Attack, 0f, AttackWindupEnd, AttackLungeEnd);
            var lunge = Bell(pose.Attack, AttackWindupEnd, AttackLungeEnd, 1f);
            body.Scale = Vector3.Scale(body.Scale, new Vector3(1f + crouch * CrouchSquash * 0.6f, 1f - crouch * CrouchSquash, 1f + crouch * CrouchSquash * 0.6f));
            body.Euler += new Vector3(-crouch * WindupLean + lunge * LungeLean, 0f, 0f);
            body.Position += Vector3.forward * (lunge * LungeDistance);

            var flipper = new Vector3(crouch * FlipperWindup - lunge * FlipperSwing, 0f, 0f);
            flipperL.Euler += flipper;
            flipperR.Euler += flipper;
        }

        /// <summary>のけぞって震え、ヒレが跳ね上がる。すぐ強く、ゆっくり戻る</summary>
        private static void Hit(PenguinPose pose, ref Offset body, ref Offset head, ref Offset flipperL, ref Offset flipperR)
        {
            if (pose.Hit >= 1f)
            {
                return;
            }

            var recoil = (1f - pose.Hit) * (1f - pose.Hit);
            body.Euler += new Vector3(-recoil * HitLean, 0f, 0f);
            body.Position += Vector3.right * (Mathf.Sin(pose.Time * HitShakeRate) * HitShake * (1f - pose.Hit));
            head.Euler += new Vector3(-recoil * HitHeadLean, 0f, 0f);
            flipperL.Euler += new Vector3(0f, 0f, -recoil * HitFlipper);
            flipperR.Euler += new Vector3(0f, 0f, recoil * HitFlipper);
        }

        /// <summary>start で 0、peak で 1、end で 0 に戻る山（なめらかに）</summary>
        private static float Bell(float t, float start, float peak, float end)
        {
            if (t <= start || t >= end) return 0f;
            return t < peak ? Mathf.SmoothStep(0f, 1f, (t - start) / (peak - start)) : Mathf.SmoothStep(1f, 0f, (t - peak) / (end - peak));
        }

        /// <summary>組み立てたときの部品の位置・向き・大きさ</summary>
        private readonly struct Rest
        {
            public readonly Offset Body;
            public readonly Offset Head;
            public readonly Offset FlipperLeft;
            public readonly Offset FlipperRight;
            public readonly Offset FootLeft;
            public readonly Offset FootRight;

            public Rest(PenguinModel model)
            {
                Body = Offset.From(model.Body);
                Head = Offset.From(model.Head);
                FlipperLeft = Offset.From(model.FlipperLeft);
                FlipperRight = Offset.From(model.FlipperRight);
                FootLeft = Offset.From(model.FootLeft);
                FootRight = Offset.From(model.FootRight);
            }
        }

        /// <summary>部品1つ分の姿勢。休みの姿勢に動きを足していき、最後にまとめて置く</summary>
        private struct Offset
        {
            public Vector3 Position;
            public Vector3 Euler;
            public Vector3 Scale;

            public Offset(Offset rest)
            {
                Position = rest.Position;
                Euler = rest.Euler;
                Scale = rest.Scale;
            }

            public static Offset From(Transform t) => new Offset { Position = t.localPosition, Euler = t.localEulerAngles, Scale = t.localScale };

            public void ApplyTo(Transform t)
            {
                t.localPosition = Position;
                t.localRotation = Quaternion.Euler(Euler);
                t.localScale = Scale;
            }
        }
    }

    /// <summary>ペンギンの動きの状態。Attack・Hit は始まってからの割合（0〜1、1 で終わり）</summary>
    public struct PenguinPose
    {
        public float Walk;
        public float WalkPhase;
        public float Attack;
        public float Hit;
        public float Time;

        public static PenguinPose Idle => new PenguinPose { Attack = 1f, Hit = 1f };
    }
}
