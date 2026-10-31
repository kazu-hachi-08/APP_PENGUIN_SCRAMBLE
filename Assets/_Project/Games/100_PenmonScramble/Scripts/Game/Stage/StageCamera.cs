using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 斜め上から見下ろすカメラを、今のエリアへ動かす。土台（これ）は床の上を縦横に動き（縦は画面より高い闘技場だけ）、
    /// 腕（_arm）がエリア（16×9マス）がちょうど画面に収まる位置・角度にカメラを構える。
    /// 引く（zoom）ときは、腕を土台からまっすぐ遠ざける。向きを変えずに距離だけ掛けると、床に映る範囲がそのまま zoom 倍になる。
    /// 揺れ（CameraShake）は腕の先のカメラが自分の位置をずらして出すので、ここは土台と腕だけを持つ
    /// </summary>
    public class StageCamera : MonoBehaviour
    {
        /// <summary>構え（透視・見下ろし 55°・画角 30°）。試作の場（PreviewCameraPresets）も同じ値を使う</summary>
        public const float Pitch = 55f;
        public const float FieldOfView = 30f;

        /// <summary>奥の端に立ったペンギンの頭まで入れる高さ（マス）</summary>
        public const float Headroom = 1.6f;

        /// <summary>上下の HUD の分だけ画面の端を空ける（割合）</summary>
        public const float Margin = 0.1f;

        [SerializeField] private float _smoothSeconds = 0.25f;
        // 闘技場に入ったときに全体を見せる・戻るときは、ゆっくり動かして場所の変わり目を目で追えるようにする
        [SerializeField] private float _slowSmoothSeconds = 0.7f;
        [SerializeField] private float _zoomSmoothSeconds = 0.5f;
        [SerializeField] private Transform _arm;
        [SerializeField] private Camera _camera;

        private Vector2 _target;
        private Vector2 _velocity;
        private float _fittedAspect;
        private Vector3 _fittedArm;
        private float _targetZoom = 1f;
        private float _zoom = 1f;
        private float _zoomVelocity;
        private bool _slow;

        /// <summary>引き 1 で画面に収まる床の広さ（マス）</summary>
        public static Vector2 View => new Vector2(Balance.AreaWidth, Balance.AreaHeight);

        /// <param name="target">床の上の、画面の真ん中に映す点</param>
        /// <param name="zoom">引き。1 でエリア1枚分が収まる</param>
        /// <param name="snap">挑戦の始めは、前の挑戦の位置から流れてこないよう、すぐに合わせる</param>
        /// <param name="slow">闘技場の全体を見せるときなど、大きく動くのをゆっくり見せる</param>
        public void Follow(Vector2 target, float zoom, bool snap, bool slow)
        {
            _target = target;
            _targetZoom = zoom;
            _slow = slow;
            if (snap)
            {
                _velocity = Vector2.zero;
                _zoomVelocity = 0f;
                _zoom = zoom;
                SetFloorPosition(target);
            }
        }

        private void Start() => FitArea();

        private void LateUpdate()
        {
            // 画面の縦横比が変わったら（ブラウザの窓の大きさを変えた・スマホを回した）構え直す
            if (!Mathf.Approximately(_camera.aspect, _fittedAspect))
            {
                FitArea();
            }

            var current = new Vector2(transform.position.x, transform.position.z);
            var smooth = _slow ? _slowSmoothSeconds : _smoothSeconds;
            SetFloorPosition(Vector2.SmoothDamp(current, _target, ref _velocity, smooth));
            _zoom = Mathf.SmoothDamp(_zoom, _targetZoom, ref _zoomVelocity, _slow ? _slowSmoothSeconds : _zoomSmoothSeconds);
            ApplyZoom();
        }

        private void FitArea()
        {
            _fittedAspect = _camera.aspect;
            _camera.orthographic = false;
            _camera.fieldOfView = FieldOfView;
            var (position, rotation) = AreaCameraFit.PerspectivePose(_camera.aspect, Vector3.zero, View, Pitch, FieldOfView, Headroom, Margin);
            _fittedArm = position;
            _arm.localRotation = rotation;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            _arm.localPosition = _fittedArm * _zoom;
        }

        private void SetFloorPosition(Vector2 point)
        {
            var position = transform.position;
            position.x = point.x;
            position.z = point.y;
            transform.position = position;
        }
    }
}
