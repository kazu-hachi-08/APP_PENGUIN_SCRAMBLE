using System;
using System.Collections.Generic;
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
    /// 打つ子の切り替え＝K / ○（東）/ 子のカードを押す。見回す＝右スティック / 画面の2本指（マウスの右ドラッグ）
    /// </summary>
    public class GolfShotControl : MonoBehaviour
    {
        private const int ShootAction = 0;
        private const int SwitchAction = 1;

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

        // 見回し: 右スティックを倒しきったときの速さ（度/秒）と、画面の高さ分ドラッグしたときの角度
        [SerializeField] private float _lookStickSpeed = 120f;
        [SerializeField] private float _lookDragDegrees = 200f;

        private readonly GolfPowerGauge _gauge = new GolfPowerGauge();
        private GolfHoleWorld _world;
        private IReadOnlyList<GolfTeam> _teams;
        private GolfShooter _shooter;
        private float _turnHeld;

        /// <summary>打つ子を替えた（HUD のカードを弾ませる・音）</summary>
        public event Action MemberChanged;

        public GolfPowerGauge Gauge => _gauge;

        /// <summary>振り下ろしている（ゲージを止めてから、クラブが球に当たるまで）</summary>
        public bool Striking => _shooter != null && _shooter.Striking;

        /// <summary>いまの番の人の3体</summary>
        public GolfTeam CurrentTeam => _teams[_world.CurrentPlayer];

        public void Bind(GolfHoleWorld world, IReadOnlyList<GolfTeam> teams, GolfShooter shooter)
        {
            _world = world;
            _teams = teams;
            _shooter = shooter;
            _gauge.Reset();
            _turnHeld = 0f;
        }

        /// <summary>カードを押して打つ子を選ぶ。狙っている間だけ（ゲージを動かし始めたら替えられない）</summary>
        public void SelectMember(int index)
        {
            if (CanSwitch() && CurrentTeam.Select(index))
            {
                MemberChanged?.Invoke();
            }
        }

        /// <summary>毎フレーム、ホールを進める前に呼ぶ</summary>
        public void Tick(float deltaTime)
        {
            if (_world == null || _world.Phase != GolfHolePhase.Aiming)
            {
                _gauge.Reset();
                return;
            }

            if (Striking || !InputManager.HasInstance)
            {
                return;
            }

            var input = InputManager.Instance;
            if (input.IsActionDown(SwitchAction) && CanSwitch() && CurrentTeam.SelectNext())
            {
                MemberChanged?.Invoke();
            }

            // ドラッグの量はゲージを動かしている間も読み捨てる（打った後に溜まった分で向きが飛ばないように）
            var drag = _pad != null ? _pad.TakeAim() : 0f;
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

        private bool CanSwitch() => _world != null && _world.Phase == GolfHolePhase.Aiming && !_gauge.Running && !Striking;

        /// <summary>1回目でゲージを動かし始め、2回目で止めて振り下ろす。打つのはクラブが当たった瞬間</summary>
        private void Press()
        {
            if (!_gauge.Running)
            {
                _gauge.Start();
                return;
            }

            var power = _gauge.Stop();
            var player = _world.CurrentPlayer;
            var aim = _world.AimAngle;
            _shooter.Strike(power, () =>
            {
                _world.Shoot(player, aim, power);
                _gauge.Reset();
            });
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
            _world.SetAim(_world.AimAngle - Mathf.Sign(axis) * speed * Mathf.Deg2Rad * deltaTime);
        }

        private void TurnByDrag(float screens)
        {
            if (screens != 0f)
            {
                _world.SetAim(_world.AimAngle - screens * _dragTurnDegrees * Mathf.Deg2Rad);
            }
        }
    }
}
