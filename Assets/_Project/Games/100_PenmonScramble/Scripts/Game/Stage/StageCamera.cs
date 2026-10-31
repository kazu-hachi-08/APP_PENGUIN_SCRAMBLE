using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 斜め上から見下ろすカメラを、今のエリアへ動かす。土台（これ）は床の上を縦横に動き（縦は画面より高い闘技場だけ）、
    /// 腕（_arm）がエリア（16×9マス）がちょうど画面に収まる位置・角度にカメラを構える。
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
        [SerializeField] private Transform _arm;
        [SerializeField] private Camera _camera;

        private Vector2 _target;
        private Vector2 _velocity;
        private float _fittedAspect;

        /// <param name="target">床の上の、画面の真ん中に映す点</param>
        /// <param name="snap">挑戦の始めは、前の挑戦の位置から流れてこないよう、すぐに合わせる</param>
        public void Follow(Vector2 target, bool snap)
        {
            _target = target;
            if (snap)
            {
                _velocity = Vector2.zero;
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
            SetFloorPosition(Vector2.SmoothDamp(current, _target, ref _velocity, _smoothSeconds));
        }

        private void FitArea()
        {
            _fittedAspect = _camera.aspect;
            _camera.orthographic = false;
            _camera.fieldOfView = FieldOfView;
            var size = new Vector2(Balance.AreaWidth, Balance.AreaHeight);
            var (position, rotation) = AreaCameraFit.PerspectivePose(_camera.aspect, Vector3.zero, size, Pitch, FieldOfView, Headroom, Margin);
            _arm.localPosition = position;
            _arm.localRotation = rotation;
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
