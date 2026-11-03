using PenmonScramble.Common.Input;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 打つ操作（仮）: ←→ / スティックで向きを回し、J / ×（南）でゲージを動かし始め、もう一度で止めて打つ。
    /// 試しのホールでは、どの席の番も同じ手元の操作で打つ。タッチ・ドラッグ・本番のゲージはフェーズ 3
    /// </summary>
    public class GolfShotControl : MonoBehaviour
    {
        private const int ShootAction = 0;

        // スティックの倒しがこれより小さければ回さない
        private const float TurnDeadZone = 0.1f;

        // 押し始めはゆっくり回して細かく合わせ、押し続けると速く回す
        [SerializeField] private float _turnSpeedSlow = 40f;
        [SerializeField] private float _turnSpeedFast = 150f;
        [SerializeField] private float _turnAccelSeconds = 0.8f;

        // ゲージが 0 → 1 → 0 と1往復する秒数
        [SerializeField] private float _gaugeCycleSeconds = 1.6f;

        private GolfHoleWorld _world;
        private float _turnHeld;
        private float _gaugeTime;

        /// <summary>ゲージを動かしている（もう一度押すと打つ）</summary>
        public bool Charging { get; private set; }

        /// <summary>いまのゲージの強さ（0〜1）</summary>
        public float Power { get; private set; }

        public void Bind(GolfHoleWorld world)
        {
            _world = world;
            ResetGauge();
        }

        /// <summary>毎フレーム、ホールを進める前に呼ぶ（打った1打がそのフレームから転がり始めるように）</summary>
        public void Tick(float deltaTime)
        {
            if (_world == null || _world.Phase != GolfHolePhase.Aiming || !InputManager.HasInstance)
            {
                ResetGauge();
                return;
            }

            var input = InputManager.Instance;
            Turn(input.MoveVector.x, deltaTime);
            if (input.IsActionDown(ShootAction))
            {
                if (Charging)
                {
                    _world.Shoot(_world.CurrentPlayer, _world.AimAngle, Power);
                    ResetGauge();
                    return;
                }

                Charging = true;
            }

            if (Charging)
            {
                _gaugeTime += deltaTime;
                Power = Mathf.PingPong(_gaugeTime * 2f / _gaugeCycleSeconds, 1f);
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
            _world.SetAim(_world.AimAngle - Mathf.Sign(axis) * speed * Mathf.Deg2Rad * deltaTime);
        }

        private void ResetGauge()
        {
            Charging = false;
            Power = 0f;
            _gaugeTime = 0f;
            _turnHeld = 0f;
        }
    }
}
