using System;
using PenmonScramble.Common.Input;
using PenmonScramble.Golf.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 打つ操作。タッチ・キーボード・コントローラーで同じ手ごたえにする:
    /// 向き＝スティック / ←→（押し続けると速く回る）/ 画面の横のドラッグ。
    /// 打つ＝J / ×（南）/ 右の大ボタンでゲージを動かし始め、もう一度で止めて振り下ろす（クラブが当たった瞬間に打つ）。
    /// 打つ子の切り替え＝K / ○（東）/ 子のカードを押す。特殊＝L / △（北）/ 特殊のボタン（風は 右 → 左 → OFF の順に替わる）。
    /// 雷の特殊ショットは、転がっている間に向き（±★の角度まで）を決めて、打つボタンでもう一度押すと加速する。
    /// 見回す＝右スティック / 画面の2本指（マウスの右ドラッグ）
    /// </summary>
    public class GolfShotControl : MonoBehaviour
    {
        private const int ShootAction = 0;
        private const int SwitchAction = 1;
        private const int SpecialAction = 2;

        // スティックの倒しがこれより小さければ回さない
        private const float TurnDeadZone = 0.1f;
        private const float LookDeadZone = 0.2f;

        [SerializeField] private GolfDragPad _pad;

        // 押し始めはゆっくり回して細かく合わせ、押し続けると速く回す（度/秒）
        [SerializeField] private float _turnSpeedSlow = 40f;
        [SerializeField] private float _turnSpeedFast = 150f;
        [SerializeField] private float _turnAccelSeconds = 0.8f;
        // 画面の高さ分ドラッグしたときに回る角度
        [SerializeField] private float _dragTurnDegrees = 120f;
        // 雷の加速の向きを回す速さ（度/秒）。転がっている間に合わせるので、狙うときより速め
        [SerializeField] private float _boostTurnSpeed = 140f;

        // 見回し: 右スティックを倒しきったときの速さ（度/秒）と、画面の高さ分ドラッグしたときの角度
        [SerializeField] private float _lookStickSpeed = 120f;
        [SerializeField] private float _lookDragDegrees = 200f;

        private readonly GolfPowerGauge _gauge = new GolfPowerGauge();
        private GolfRound _round;
        private GolfShooter _shooter;
        private float _turnHeld;
        private float _boostTurn;

        /// <summary>打つ子を替えた（HUD のカードを弾ませる・音）</summary>
        public event Action MemberChanged;

        /// <summary>特殊の ON / OFF・風の向きを替えた</summary>
        public event Action SpecialChanged;

        public GolfPowerGauge Gauge => _gauge;

        /// <summary>振り下ろしている（ゲージを止めてから、クラブが球に当たるまで）</summary>
        public bool Striking => _shooter != null && _shooter.Striking;

        /// <summary>次の1打を特殊ショットにする（いま選んでいる子がまだ使っていないときだけ ON にできる）</summary>
        public bool SpecialArmed { get; private set; }

        /// <summary>風の曲げる向き（右 +1 / 左 −1）</summary>
        public int Curve { get; private set; } = 1;

        private GolfHoleWorld World => _round.World;

        /// <summary>いまの番の人の3体</summary>
        public GolfTeam CurrentTeam => _round.Teams[World.CurrentPlayer];

        /// <summary>ON にしているときの特殊ショット（ガイド線を曲げる・HUD に出す）。OFF なら None</summary>
        public GolfSpecial ArmedSpecial => SpecialArmed ? GolfSpecial.For(CurrentTeam.Current, Curve) : GolfSpecial.None;

        /// <summary>雷の加速を使える（打った球が転がっている間）</summary>
        public bool BoostAvailable => _round != null && World.CanBoost(World.CurrentPlayer);

        /// <summary>加速する向き（ラジアン。いまの進む向き ＋ 合わせた分）</summary>
        public float BoostAngle
        {
            get
            {
                var velocity = World.Balls[World.CurrentPlayer].Velocity;
                return Mathf.Atan2(velocity.Y, velocity.X) + _boostTurn;
            }
        }

        public void Bind(GolfRound round, GolfShooter shooter)
        {
            _round = round;
            _shooter = shooter;
            ResetForTurn();
        }

        /// <summary>番が替わった・ホールが始まった: ゲージと特殊を戻す（前の人の ON を次の人に持ち越さない）</summary>
        public void ResetForTurn()
        {
            _gauge.Reset();
            _turnHeld = 0f;
            _boostTurn = 0f;
            SpecialArmed = false;
            Curve = 1;
        }

        /// <summary>カードを押して打つ子を選ぶ。狙っている間だけ（ゲージを動かし始めたら替えられない）</summary>
        public void SelectMember(int index)
        {
            if (CanSwitch() && CurrentTeam.Select(index))
            {
                OnMemberChanged();
            }
        }

        /// <summary>毎フレーム、ホールを進める前に呼ぶ</summary>
        public void Tick(float deltaTime)
        {
            if (_round == null || !InputManager.HasInstance)
            {
                return;
            }

            var input = InputManager.Instance;
            var drag = _pad != null ? _pad.TakeAim() : 0f;
            if (World.Phase == GolfHolePhase.Rolling)
            {
                _gauge.Reset();
                TickBoost(input, drag, deltaTime);
                return;
            }

            if (World.Phase != GolfHolePhase.Aiming || Striking)
            {
                return;
            }

            if (input.IsActionDown(SwitchAction) && CanSwitch() && CurrentTeam.SelectNext())
            {
                OnMemberChanged();
            }

            if (input.IsActionDown(SpecialAction) && CanSwitch())
            {
                CycleSpecial();
            }

            // ドラッグの量はゲージを動かしている間も読み捨てる（打った後に溜まった分で向きが飛ばないように）
            if (!_gauge.Running)
            {
                Turn(input.MoveVector.x, deltaTime);
                TurnByDrag(drag);
            }

            if (input.IsActionDown(ShootAction))
            {
                Press();
            }

            _gauge.Tick(deltaTime);
        }

        /// <summary>カメラの見回しの量（度）。右スティックと2本指のドラッグを合わせる</summary>
        public Vector2 TakeLook(float deltaTime)
        {
            var look = _pad != null ? _pad.TakeLook() * _lookDragDegrees : Vector2.zero;
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                var stick = gamepad.rightStick.ReadValue();
                if (stick.magnitude >= LookDeadZone)
                {
                    look += stick * (_lookStickSpeed * deltaTime);
                }
            }

            return look;
        }

        /// <summary>ふつうの子は OFF ↔ ON、風の子は OFF → 右 → 左 → OFF。使った子は ON にできない</summary>
        public void CycleSpecial()
        {
            if (!CanSwitch() || !CurrentTeam.CanUseSpecial(CurrentTeam.Selected))
            {
                return;
            }

            var wind = GolfSpecial.KindOf(CurrentTeam.Current.Element) == GolfSpecialKind.WindCurve;
            if (!SpecialArmed)
            {
                SpecialArmed = true;
                Curve = 1;
            }
            else if (wind && Curve > 0)
            {
                Curve = -1;
            }
            else
            {
                SpecialArmed = false;
            }

            SpecialChanged?.Invoke();
        }

        private void OnMemberChanged()
        {
            // 替えた子の特殊ショットは別物なので、ON は持ち越さない
            SpecialArmed = false;
            MemberChanged?.Invoke();
        }

        private bool CanSwitch() => _round != null && World.Phase == GolfHolePhase.Aiming && !_gauge.Running && !Striking;

        /// <summary>1回目でゲージを動かし始め、2回目で止めて振り下ろす。打つのはクラブが当たった瞬間</summary>
        private void Press()
        {
            if (!_gauge.Running)
            {
                _gauge.Start();
                return;
            }

            var power = _gauge.Stop();
            var player = World.CurrentPlayer;
            var shot = new GolfShot(CurrentTeam.Selected, World.AimAngle, power, SpecialArmed, Curve);
            _shooter.Strike(power, () =>
            {
                _round.Shoot(player, shot);
                _gauge.Reset();
                SpecialArmed = false;
            });
        }

        /// <summary>雷: 転がっている間に向きを合わせ（いまの進む向きから ±★の角度まで）、打つボタンで加速する</summary>
        private void TickBoost(InputManager input, float drag, float deltaTime)
        {
            if (!BoostAvailable)
            {
                _boostTurn = 0f;
                return;
            }

            var max = World.BoostMaxTurn(World.CurrentPlayer);
            var axis = Mathf.Abs(input.MoveVector.x) >= TurnDeadZone ? input.MoveVector.x : 0f;
            var turn = -axis * _boostTurnSpeed * Mathf.Deg2Rad * deltaTime - drag * _dragTurnDegrees * Mathf.Deg2Rad;
            _boostTurn = Mathf.Clamp(_boostTurn + turn, -max, max);
            if (input.IsActionDown(ShootAction))
            {
                _round.Boost(World.CurrentPlayer, _boostTurn);
                _boostTurn = 0f;
            }
        }

        /// <summary>右へ倒すと右回り（上から見て時計回り。角度は減る）</summary>
        private void Turn(float axis, float deltaTime)
        {
            if (Mathf.Abs(axis) < TurnDeadZone)
            {
                _turnHeld = 0f;
                return;
            }

            _turnHeld += deltaTime;
            var speed = Mathf.Lerp(_turnSpeedSlow, _turnSpeedFast, _turnHeld / _turnAccelSeconds) * Mathf.Abs(axis);
            World.SetAim(World.AimAngle - Mathf.Sign(axis) * speed * Mathf.Deg2Rad * deltaTime);
        }

        private void TurnByDrag(float screens)
        {
            if (screens != 0f)
            {
                World.SetAim(World.AimAngle - screens * _dragTurnDegrees * Mathf.Deg2Rad);
            }
        }
    }
}
