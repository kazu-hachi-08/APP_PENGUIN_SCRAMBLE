using PenmonScramble.Core;
using UnityEngine;
using static PenmonScramble.Game.ToyMotionMath;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 3D のペンギンの部品を動かす（立つ・歩く・技・必殺技・被弾・吹っ飛び・すべる・ダウン・撃破）。
    /// 状態は外（UnitView・試作の場）から受け取り、部品と体全体（Figure）だけを動かす。根元の位置・回転・大きさは呼び出し側のもの。
    /// 毎フレームの姿勢は PenguinPose だけから決まる（Apply）。部品ごとの動きは PenguinMotion.Parts.cs、体全体の動きは PenguinMotion.Figure.cs
    /// </summary>
    [RequireComponent(typeof(PenguinModel))]
    public sealed partial class PenguinMotion : MonoBehaviour, IUnitMotion
    {
        // 向きを変える速さ（度/秒）。速すぎるとカクッと見える
        private const float TurnSpeed = 720f;
        // 立ち止まり⇔歩き・すべる・倒れるの切り替えにかける時間の逆数
        private const float WalkBlendRate = 6f;
        private const float SlideBlendRate = 10f;
        private const float DownBlendRate = 5f;
        // 構えの 0.4 秒のうちに膨らみきって見えるよう速めにする
        private const float WindUpBlendRate = 10f;
        private const float MovingThreshold = 0.05f;
        // 1マス進むあいだの歩数（左右で1周）。速く歩くほど揺れも速くなる
        private const float CyclesPerUnit = 0.9f;

        public const float AttackSeconds = 0.5f;
        public const float SpecialSeconds = 0.9f;
        public const float HitSeconds = 0.4f;
        public const float DefeatSeconds = 0.6f;
        private const float BlinkSeconds = 0.15f;
        private const float BlinkIntervalMin = 2f;
        private const float BlinkIntervalMax = 5f;

        // 吹っ飛びの長さ（秒）の範囲。Core の減速から求めた時間を、短すぎて見えない・長すぎて間延びしないように丸める
        private const float KnockSecondsMin = 0.25f;
        private const float KnockSecondsMax = 1f;
        // 転がる回数は飛ぶ距離をこの半径の輪が転がる回数にする（体の半径より少し小さくして、よく転がるように見せる）
        private const float RollRadius = 0.3f;
        private const int MaxKnockTurns = 3;

        private PenguinModel _model;
        private Rest _rest;
        private PenguinPose _pose = PenguinPose.Idle;
        private float _knockSeconds = KnockSecondsMin;
        private float _blinkTimer;

        /// <summary>床の上での速度（マス/秒）。歩きの揺れと、Facing が無いときの向きに使う</summary>
        public Vector3 Velocity { get; set; }

        /// <summary>向く方向。ゼロなら進む方向を向く</summary>
        public Vector3 Facing { get; set; }

        /// <summary>突進中（お腹ですべる）</summary>
        public bool Sliding { get; set; }

        /// <summary>ダウン中（お腹を下にして倒れ、ヒレをじたばた）</summary>
        public bool Down { get; set; }

        /// <summary>凍っている。姿勢をその場で止める（呼吸・まばたきも止めて、氷の中で固まって見せる）</summary>
        public bool Frozen { get; set; }

        /// <summary>技を出す前に構えている（体がぷくっと膨らんで震える）</summary>
        public bool WindingUp { get; set; }

        public bool IsDefeatFinished => _pose.Defeated && _pose.Defeat >= 1f;

        public void PlayAttack(SkillShape shape)
        {
            _pose.Attack = 0f;
            _pose.AttackShape = shape;
        }

        /// <summary>必殺技・合体技</summary>
        public void PlaySpecial() => _pose.Special = 0f;

        public void PlayHit() => _pose.Hit = 0f;

        public void PlayBlink() => _pose.Blink = 0f;

        /// <summary>
        /// 吹っ飛ばされた。velocity は飛ばされた初速（マス/秒）。Core と同じ減速で止まるまでの時間だけ、飛ぶ向きへ転がる。
        /// 飛ばしてきた側へすぐ向き直って後ろへ転がる（横へ転がると、広げたヒレが床にめり込むため）。
        /// 位置は呼び出し側が動かす（ここでは転がりと跳ねだけ）
        /// </summary>
        public void PlayKnockback(Vector3 velocity)
        {
            Bind();
            var local = FlatLocal(transform, velocity);
            var speed = local.magnitude;
            if (speed <= 0f)
            {
                return;
            }

            _knockSeconds = Mathf.Clamp(speed / Balance.KnockbackDeceleration, KnockSecondsMin, KnockSecondsMax);
            var distance = speed * speed / (2f * Balance.KnockbackDeceleration) / Mathf.Max(0.01f, transform.lossyScale.x);
            _pose.Knock = 0f;
            _pose.KnockDirection = local / speed;
            _pose.Yaw = YawOf(-local);
            _pose.KnockTurns = Mathf.Min(MaxKnockTurns, Mathf.Round(distance / (2f * Mathf.PI * RollRadius)));
        }

        /// <summary>倒された。回りながら飛んで消える。終わったら IsDefeatFinished</summary>
        public void PlayDefeat()
        {
            _pose.Defeated = true;
            _pose.Defeat = 0f;
        }

        /// <summary>今の向きをなめらかに回さず、すぐこの方向へ向ける（出てきたとき・試作の場で並べたとき）</summary>
        public void SnapFacing(Vector3 direction)
        {
            Bind();
            _pose.Yaw = SnapYaw(transform, direction, _pose.Yaw);
            Apply(_pose);
        }

        /// <summary>動きを全部止めて立った姿勢に戻す（向きは残す）</summary>
        public void ResetPose()
        {
            Bind();
            var yaw = _pose.Yaw;
            _pose = PenguinPose.Idle;
            _pose.Yaw = yaw;
            Sliding = false;
            Down = false;
            Frozen = false;
            WindingUp = false;
            Velocity = Vector3.zero;
            _blinkTimer = NextBlinkInterval(BlinkIntervalMin, BlinkIntervalMax);
            Apply(_pose);
        }

        private void Awake() => Bind();

        private void Bind()
        {
            if (_model != null)
            {
                return;
            }

            _model = GetComponent<PenguinModel>();
            _rest = new Rest(_model);
            _blinkTimer = NextBlinkInterval(BlinkIntervalMin, BlinkIntervalMax);
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>dt 秒進めて姿勢を置く。試作の場のスクショは、これで時刻を進めて撮る</summary>
        public void Tick(float dt)
        {
            Bind();
            if (Frozen)
            {
                return;
            }

            var local = FlatLocal(transform, Velocity);
            var speed = local.magnitude;
            var knocked = _pose.Knock < 1f;
            var moving = speed > MovingThreshold && !knocked && !Down;
            Turn(local, moving, dt);

            _pose.Walk = Mathf.MoveTowards(_pose.Walk, moving && !Sliding ? 1f : 0f, WalkBlendRate * dt);
            _pose.WalkPhase = Mathf.Repeat(_pose.WalkPhase + speed * CyclesPerUnit * dt, 1f);
            _pose.Slide = Mathf.MoveTowards(_pose.Slide, Sliding && !Down ? 1f : 0f, SlideBlendRate * dt);
            _pose.Down = Mathf.MoveTowards(_pose.Down, Down ? 1f : 0f, DownBlendRate * dt);
            _pose.WindUp = Mathf.MoveTowards(_pose.WindUp, WindingUp ? 1f : 0f, WindUpBlendRate * dt);
            _pose.Attack = Advance(_pose.Attack, dt, AttackSeconds);
            _pose.Special = Advance(_pose.Special, dt, SpecialSeconds);
            _pose.Hit = Advance(_pose.Hit, dt, HitSeconds);
            _pose.Knock = Advance(_pose.Knock, dt, _knockSeconds);
            _pose.Blink = Advance(_pose.Blink, dt, BlinkSeconds);
            if (_pose.Defeated)
            {
                _pose.Defeat = Advance(_pose.Defeat, dt, DefeatSeconds);
            }

            UpdateBlinkTimer(dt);
            _pose.Time += dt;
            Apply(_pose);
        }

        /// <summary>Facing があればそちらへ、無ければ進む向きへ回る。吹っ飛び中・ダウン中・撃破中は向きを変えない</summary>
        private void Turn(Vector3 localVelocity, bool moving, float dt)
        {
            if (_pose.Knock < 1f || Down || _pose.Defeated)
            {
                return;
            }

            var facing = FlatLocal(transform, Facing);
            var target = facing.sqrMagnitude > 0f ? facing : moving ? localVelocity : Vector3.zero;
            if (target.sqrMagnitude > 0f)
            {
                _pose.Yaw = Mathf.MoveTowardsAngle(_pose.Yaw, YawOf(target), TurnSpeed * dt);
            }
        }

        private void UpdateBlinkTimer(float dt)
        {
            _blinkTimer -= dt;
            if (_blinkTimer > 0f)
            {
                return;
            }

            // 全員が同時にまばたきしないよう、間隔を毎回ばらつかせる（見た目だけなので Core の乱数とは関係ない）
            _blinkTimer = NextBlinkInterval(BlinkIntervalMin, BlinkIntervalMax);
            PlayBlink();
        }

        /// <summary>姿勢を部品に置く。毎回休みの姿勢から組み直すので、前のフレームの値が残らない</summary>
        public void Apply(PenguinPose pose)
        {
            Bind();
            var parts = new PartPose(_rest);
            ApplyParts(pose, ref parts);
            parts.ApplyTo(_model);

            var figure = FigureOf(pose);
            _model.Figure.localPosition = figure.Position;
            _model.Figure.localRotation = figure.Rotation;
            _model.Figure.localScale = Vector3.one * figure.Scale;
            ApplyShadow(pose, figure);
            ApplyBlink(pose);
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
            public readonly Vector3 ShadowScale;
            public readonly Vector3[] EyeScales;
            public readonly float CenterHeight;
            public readonly float LieHeight;
            public readonly float TopHeight;

            public Rest(PenguinModel model)
            {
                Body = Offset.From(model.Body);
                Head = Offset.From(model.Head);
                FlipperLeft = Offset.From(model.FlipperLeft);
                FlipperRight = Offset.From(model.FlipperRight);
                FootLeft = Offset.From(model.FootLeft);
                FootRight = Offset.From(model.FootRight);
                ShadowScale = model.BlobShadow.transform.localScale;
                EyeScales = new Vector3[model.Eyes.Count];
                for (var i = 0; i < EyeScales.Length; i++)
                {
                    EyeScales[i] = model.Eyes[i].localScale;
                }

                CenterHeight = model.CenterHeight;
                LieHeight = model.LieHeight;
                TopHeight = model.TopHeight;
            }
        }

        /// <summary>部品ごとの姿勢。休みの姿勢に動きを足していき、最後にまとめて置く</summary>
        private struct PartPose
        {
            public Offset Body;
            public Offset Head;
            public Offset FlipperLeft;
            public Offset FlipperRight;
            public Offset FootLeft;
            public Offset FootRight;

            public PartPose(Rest rest)
            {
                Body = rest.Body;
                Head = rest.Head;
                FlipperLeft = rest.FlipperLeft;
                FlipperRight = rest.FlipperRight;
                FootLeft = rest.FootLeft;
                FootRight = rest.FootRight;
            }

            /// <summary>ヒレを外へ開く（左右で向きが逆なので、正の値で両方とも外へ）</summary>
            public void OpenFlippers(float degrees)
            {
                FlipperLeft.Euler.z -= degrees;
                FlipperRight.Euler.z += degrees;
            }

            /// <summary>ヒレを前後に振る。正で後ろへ、負で前へ</summary>
            public void SwingFlippers(float degrees)
            {
                FlipperLeft.Euler.x += degrees;
                FlipperRight.Euler.x += degrees;
            }

            /// <summary>胴をつぶす（正）・伸ばす（負）。つぶすと横に広がる</summary>
            public void Squash(float amount)
            {
                Body.Scale = Vector3.Scale(Body.Scale, new Vector3(1f + amount * 0.6f, 1f - amount, 1f + amount * 0.6f));
            }

            public void ApplyTo(PenguinModel model)
            {
                Body.ApplyTo(model.Body);
                Head.ApplyTo(model.Head);
                FlipperLeft.ApplyTo(model.FlipperLeft);
                FlipperRight.ApplyTo(model.FlipperRight);
                FootLeft.ApplyTo(model.FootLeft);
                FootRight.ApplyTo(model.FootRight);
            }
        }
    }
}
