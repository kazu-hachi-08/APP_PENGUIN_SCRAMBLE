using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ゴルフのカメラ。ホールの始めはホール全体を見せ、狙う間は球の後ろ斜め上から狙う向きを、打った後は球を追う。
    /// 止まったら次の人の球の後ろへ寄る（どれも同じなめらかな追い方で移るので、切り替えでパッと飛ばない）。
    /// 狙う間・追う間は、右スティック / 画面の2本指で回り込んで見回せる（番が替わると戻る）
    /// </summary>
    public class GolfCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("ホール全体")]
        [SerializeField] private float _overviewPitch = 58f;
        // 画面の端ぎりぎりにならないよう、少し余白をとる
        [SerializeField] private float _overviewMargin = 1.1f;

        [Header("狙う")]
        [SerializeField] private float _aimBack = 3.4f;
        [SerializeField] private float _aimHeight = 2.3f;
        [SerializeField] private float _aimLookAhead = 2.6f;
        [SerializeField] private float _aimSmooth = 0.35f;

        [Header("追う")]
        [SerializeField] private float _followBack = 4.6f;
        [SerializeField] private float _followHeight = 3.6f;
        [SerializeField] private float _followSmooth = 0.3f;

        [Header("見回す")]
        [SerializeField] private float _orbitPitchMin = -18f;
        [SerializeField] private float _orbitPitchMax = 35f;
        [SerializeField] private float _overviewSmooth = 0.8f;

        private Mode _mode;
        private Vector3 _overviewPosition;
        private Vector3 _overviewLook;
        private Vector3 _target;
        private Vector3 _forward = Vector3.forward;
        private Vector3 _position;
        private Vector3 _look;
        private Vector3 _positionVelocity;
        private Vector3 _lookVelocity;
        private float _orbitYaw;
        private float _orbitPitch;

        /// <summary>ホールの始め: 全体が収まる位置へすぐ置く</summary>
        public void ShowHole(IReadOnlyList<Vec2> ground)
        {
            ComputeOverview(ground);
            _mode = Mode.Overview;
            _position = _overviewPosition;
            _look = _overviewLook;
            _positionVelocity = Vector3.zero;
            _lookVelocity = Vector3.zero;
            ResetOrbit();
            Apply();
        }

        /// <summary>狙う: 球の後ろ斜め上から、狙う向き（床の上の向き）を見る</summary>
        public void Aim(Vector3 ball, Vector3 aimDirection)
        {
            _mode = Mode.Aim;
            _target = ball;
            if (aimDirection.sqrMagnitude > 0f)
            {
                _forward = aimDirection.normalized;
            }
        }

        /// <summary>追う: 打った向きのまま、少し引いて高くから球を見る</summary>
        public void Follow(Vector3 ball)
        {
            _mode = Mode.Follow;
            _target = ball;
        }

        /// <summary>見回す（度）。左右は回り込み、上下は見下ろす角度</summary>
        public void Orbit(Vector2 degrees)
        {
            _orbitYaw = Mathf.Repeat(_orbitYaw + degrees.x + 180f, 360f) - 180f;
            _orbitPitch = Mathf.Clamp(_orbitPitch + degrees.y, _orbitPitchMin, _orbitPitchMax);
        }

        public void ResetOrbit()
        {
            _orbitYaw = 0f;
            _orbitPitch = 0f;
        }

        private void LateUpdate()
        {
            var (position, look, smooth) = Desired();
            _position = Vector3.SmoothDamp(_position, position, ref _positionVelocity, smooth);
            _look = Vector3.SmoothDamp(_look, look, ref _lookVelocity, smooth);
            Apply();
        }

        private (Vector3 position, Vector3 look, float smooth) Desired()
        {
            switch (_mode)
            {
                case Mode.Aim:
                    return Around(_target, _aimBack, _aimHeight, _target + _forward * _aimLookAhead, _aimSmooth);
                case Mode.Follow:
                    return Around(_target, _followBack, _followHeight, _target, _followSmooth);
                default:
                    return (_overviewPosition, _overviewLook, _overviewSmooth);
            }
        }

        /// <summary>中心 center から見て、狙う向きの後ろ back・高さ height の所。見回しの分だけ center のまわりを回す</summary>
        private (Vector3, Vector3, float) Around(Vector3 center, float back, float height, Vector3 look, float smooth)
        {
            var offset = -_forward * back + Vector3.up * height;
            var side = Vector3.Cross(Vector3.up, _forward);
            var orbit = Quaternion.AngleAxis(_orbitYaw, Vector3.up) * Quaternion.AngleAxis(_orbitPitch, side);
            var turnedLook = center + Quaternion.AngleAxis(_orbitYaw, Vector3.up) * (look - center);
            return (center + orbit * offset, turnedLook, smooth);
        }

        private void Apply()
        {
            var direction = _look - _position;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            _camera.transform.SetPositionAndRotation(_position, Quaternion.LookRotation(direction));
        }

        /// <summary>真上から少し手前に倒した向きで、地面の全体が画面に収まる距離</summary>
        private void ComputeOverview(IReadOnlyList<Vec2> ground)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var point in ground)
            {
                min = Vector2.Min(min, new Vector2(point.X, point.Y));
                max = Vector2.Max(max, new Vector2(point.X, point.Y));
            }

            var size = max - min;
            var center = GolfSpace.ToWorld(new Vec2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f));
            var rotation = Quaternion.Euler(_overviewPitch, 0f, 0f);
            // 奥行きは斜めに見る分だけ縮んで見える
            var depth = size.y * Mathf.Sin(_overviewPitch * Mathf.Deg2Rad);
            var halfHeight = Mathf.Max(depth, size.x / _camera.aspect) * 0.5f * _overviewMargin;
            var distance = halfHeight / Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _overviewLook = center;
            _overviewPosition = center - rotation * Vector3.forward * distance;
        }

        private enum Mode
        {
            Overview,
            Aim,
            Follow,
        }
    }
}
