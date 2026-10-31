using PenmonScramble.Core;
using UnityEngine;
using static PenmonScramble.Game.ToyMotionMath;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 3D の大ボスの関節を動かす（移動・攻撃の予告の構え・攻撃が出た瞬間・突進・もぐる／飛び出す・被弾・撃破）。
    /// 状態は外（UnitView）から受け取り、関節と体全体（Figure）だけを動かす。毎フレームの姿勢は BigBossPose だけから決まる（Apply）。
    /// 大ボスごとの動き方は BigBossMotion.Skua.cs / .Seal.cs / .Orca.cs、体全体の動きは BigBossMotion.Figure.cs
    /// </summary>
    [RequireComponent(typeof(BigBossModel))]
    public sealed partial class BigBossMotion : MonoBehaviour, IUnitMotion
    {
        // 体が大きく重いので、ペンギン（720）よりゆっくり向きを変える
        private const float TurnSpeed = 300f;
        private const float MoveBlendRate = 4f;
        private const float TelegraphBlendRate = 6f;
        private const float DashBlendRate = 8f;
        private const float MovingThreshold = 0.05f;

        public const float StrikeSeconds = 0.6f;
        public const float EmergeSeconds = 0.75f;
        public const float SubmergeSeconds = 0.4f;
        public const float HitSeconds = 0.35f;
        // 大ボスの最期は、ペンギン（0.6秒）より長く見せる
        public const float DefeatSeconds = 0.9f;
        private const float BlinkSeconds = 0.18f;
        private const float BlinkIntervalMin = 2.5f;
        private const float BlinkIntervalMax = 5f;

        private BigBossModel _model;
        private Offset[] _rest;
        private Offset[] _work;
        private Vector3[] _eyeScales;
        private Vector3 _shadowScale;
        private BigBossPose _pose = BigBossPose.Idle;
        private BossAttackKind? _telegraph;
        private bool _submerged;
        private float _blinkTimer;

        public Vector3 Velocity { get; set; }

        public Vector3 Facing { get; set; }

        /// <summary>突進中</summary>
        public bool Sliding { get; set; }

        /// <summary>大ボスはダウンしない（受け取るだけ）</summary>
        public bool Down { get; set; }

        public bool Frozen { get; set; }

        public bool IsDefeatFinished => _pose.Defeated && _pose.Defeat >= 1f;

        /// <summary>攻撃の予告の構え。null で構えを解く（攻撃が出たら PlayStrike）</summary>
        public BossAttackKind? Telegraph
        {
            get => _telegraph;
            set
            {
                _telegraph = value;
                if (value.HasValue)
                {
                    _pose.TelegraphKind = value.Value;
                }
            }
        }

        /// <summary>もぐっている（飛び出し）。もぐると床の下へ沈み、出てくると飛び出す</summary>
        public bool Submerged
        {
            get => _submerged;
            set
            {
                if (_submerged && !value)
                {
                    PlayEmerge();
                }

                _submerged = value;
            }
        }

        /// <summary>床の下に隠れきっている（体を出さず、床の暗い影だけ見せる）</summary>
        public bool IsUnderground => _submerged && _pose.Submerge >= 1f;

        public BigBossPose Pose => _pose;

        /// <summary>攻撃が出た瞬間。構えから一気に動く（羽を打ち下ろす・波を起こす・かみつく）</summary>
        public void PlayStrike(BossAttackKind kind)
        {
            _pose.Strike = 0f;
            _pose.StrikeKind = kind;
            _pose.Telegraph = 0f;
        }

        public void PlayEmerge()
        {
            _pose.Emerge = 0f;
            _pose.Submerge = 0f;
        }

        /// <summary>大ボスは技を持たない（攻撃は PlayStrike）。来たときは、かみつく・突っ込む出だしで見せる</summary>
        public void PlayAttack(SkillShape shape) => PlayStrike(BossAttackKind.Charge);

        public void PlaySpecial() => PlayStrike(BossAttackKind.Charge);

        public void PlayHit() => _pose.Hit = 0f;

        /// <summary>大ボスは飛ばされる強さが 1/10 で転がるほど飛ばないので、のけぞるだけにする</summary>
        public void PlayKnockback(Vector3 velocity) => PlayHit();

        public void PlayBlink() => _pose.Blink = 0f;

        public void PlayDefeat()
        {
            _pose.Defeated = true;
            _pose.Defeat = 0f;
            _telegraph = null;
            _submerged = false;
            _pose.Submerge = 0f;
        }

        public void SnapFacing(Vector3 direction)
        {
            Bind();
            _pose.Yaw = SnapYaw(transform, direction, _pose.Yaw);
            Apply(_pose);
        }

        /// <summary>動きを全部止めて休みの姿勢に戻す（向きは残す）</summary>
        public void ResetPose()
        {
            Bind();
            var yaw = _pose.Yaw;
            _pose = BigBossPose.Idle;
            _pose.Yaw = yaw;
            _telegraph = null;
            _submerged = false;
            Sliding = false;
            Frozen = false;
            Velocity = Vector3.zero;
            Apply(_pose);
        }

        private void Awake() => Bind();

        private void Bind()
        {
            if (_model != null)
            {
                return;
            }

            _model = GetComponent<BigBossModel>();
            _rest = new Offset[BigBossModel.JointCount];
            for (var i = 0; i < _rest.Length; i++)
            {
                var joint = _model[(BigBossPart)i];
                _rest[i] = joint != null ? Offset.From(joint) : default;
            }

            _work = new Offset[_rest.Length];
            _eyeScales = new Vector3[_model.Eyes.Count];
            for (var i = 0; i < _eyeScales.Length; i++)
            {
                _eyeScales[i] = _model.Eyes[i].localScale;
            }

            _shadowScale = _model.BlobShadow.transform.localScale;
            _blinkTimer = NextBlinkInterval(BlinkIntervalMin, BlinkIntervalMax);
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>dt 秒進めて姿勢を置く</summary>
        public void Tick(float dt)
        {
            Bind();
            if (Frozen)
            {
                return;
            }

            var local = FlatLocal(transform, Velocity);
            var speed = local.magnitude;
            var moving = speed > MovingThreshold;
            Turn(local, moving, dt);

            _pose.Move = Mathf.MoveTowards(_pose.Move, moving ? 1f : 0f, MoveBlendRate * dt);
            _pose.Phase = Mathf.Repeat(_pose.Phase + CycleRate(speed) * dt, 1f);
            _pose.Telegraph = Mathf.MoveTowards(_pose.Telegraph, _telegraph.HasValue ? 1f : 0f, TelegraphBlendRate * dt);
            _pose.Dash = Mathf.MoveTowards(_pose.Dash, Sliding ? 1f : 0f, DashBlendRate * dt);
            _pose.Submerge = _submerged ? Mathf.Min(1f, _pose.Submerge + dt / SubmergeSeconds) : 0f;
            _pose.Strike = Advance(_pose.Strike, dt, StrikeSeconds);
            _pose.Emerge = Advance(_pose.Emerge, dt, EmergeSeconds);
            _pose.Hit = Advance(_pose.Hit, dt, HitSeconds);
            _pose.Blink = Advance(_pose.Blink, dt, BlinkSeconds);
            if (_pose.Defeated)
            {
                _pose.Defeat = Advance(_pose.Defeat, dt, DefeatSeconds);
            }

            UpdateBlinkTimer(dt);
            _pose.Time += dt;
            Apply(_pose);
        }

        /// <summary>Facing があればそちらへ、無ければ進む向きへ回る。もぐっている間・倒れた後は向きを変えない</summary>
        private void Turn(Vector3 localVelocity, bool moving, float dt)
        {
            if (_submerged || _pose.Defeated)
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

        /// <summary>羽ばたき・這う・泳ぐ動きの速さ（周/秒）。動く・構える・突進するほど速くなる</summary>
        private float CycleRate(float speed)
        {
            var style = Style;
            return style.CycleBase + speed * style.CyclePerUnit + _pose.Telegraph * style.CycleTelegraph + _pose.Dash * style.CycleDash;
        }

        private void UpdateBlinkTimer(float dt)
        {
            _blinkTimer -= dt;
            if (_blinkTimer > 0f)
            {
                return;
            }

            _blinkTimer = NextBlinkInterval(BlinkIntervalMin, BlinkIntervalMax);
            PlayBlink();
        }

        /// <summary>攻撃が出た瞬間の動きのうち、kind の攻撃のものだけ Bell を返す（別の攻撃の出た瞬間では 0）</summary>
        private static float StrikeBell(BigBossPose pose, BossAttackKind kind, float start, float peak, float end) =>
            pose.StrikeKind == kind ? Bell(pose.Strike, start, peak, end) : 0f;

        /// <summary>羽ばたき・這う・泳ぐ周期の角度（ラジアン）。Phase は周単位なので 2π を掛ける</summary>
        private static float CyclePhase(BigBossPose pose) => pose.Phase * Mathf.PI * 2f;

        /// <summary>姿勢を関節に置く。毎回休みの姿勢から組み直すので、前のフレームの値が残らない</summary>
        public void Apply(BigBossPose pose)
        {
            Bind();
            System.Array.Copy(_rest, _work, _rest.Length);
            var figure = FigureOffset.Rest(_model.CenterHeight);
            switch (BigBossModel.SpeciesOf(_model.BossNo))
            {
                case BigBossSpecies.Skua:
                    PoseSkua(pose, ref figure);
                    break;
                case BigBossSpecies.LeopardSeal:
                    PoseSeal(pose, ref figure);
                    break;
                default:
                    PoseOrca(pose, ref figure);
                    break;
            }

            for (var i = 0; i < _work.Length; i++)
            {
                var joint = _model[(BigBossPart)i];
                if (joint != null)
                {
                    _work[i].ApplyTo(joint);
                }
            }

            ApplyFigure(pose, figure);
            ApplyBlink(pose);
        }

        private void ApplyBlink(BigBossPose pose)
        {
            var open = 1f - 0.9f * Bell(pose.Blink, 0f, 0.5f, 1f);
            for (var i = 0; i < _eyeScales.Length; i++)
            {
                _model.Eyes[i].localScale = Vector3.Scale(_eyeScales[i], new Vector3(1f, open, 1f));
            }
        }

        private ref Offset Joint(BigBossPart part) => ref _work[(int)part];

        /// <summary>左右の関節（side は左が -1、右が 1）</summary>
        private ref Offset Side(BigBossPart left, BigBossPart right, float side) => ref _work[(int)(side < 0f ? left : right)];

        /// <summary>大ボスごとの動きの速さ（周/秒）: いつも・1マス/秒ごと・構えている間に足す分・突進中に足す分</summary>
        private readonly struct MotionStyle
        {
            public readonly float CycleBase;
            public readonly float CyclePerUnit;
            public readonly float CycleTelegraph;
            public readonly float CycleDash;

            public MotionStyle(float cycleBase, float cyclePerUnit, float cycleTelegraph, float cycleDash)
            {
                CycleBase = cycleBase;
                CyclePerUnit = cyclePerUnit;
                CycleTelegraph = cycleTelegraph;
                CycleDash = cycleDash;
            }
        }

        private MotionStyle Style
        {
            get
            {
                switch (BigBossModel.SpeciesOf(_model.BossNo))
                {
                    case BigBossSpecies.Skua: return SkuaStyle;
                    case BigBossSpecies.LeopardSeal: return SealStyle;
                    default: return OrcaStyle;
                }
            }
        }
    }
}
