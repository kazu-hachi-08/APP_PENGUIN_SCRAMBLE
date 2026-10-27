using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// カメラを今のエリアへ動かす（§6.1）。横にだけ動く。
    /// 揺れ（CameraShake）は子のカメラが自分の位置をずらして出すので、ここは土台の位置だけを持つ
    /// </summary>
    public class StageCamera : MonoBehaviour
    {
        [SerializeField] private float _smoothSeconds = 0.25f;

        private float _targetX;
        private float _velocity;

        /// <param name="snap">挑戦の始めは、前の挑戦の位置から流れてこないよう、すぐに合わせる</param>
        public void Follow(float x, bool snap)
        {
            _targetX = x;
            if (snap)
            {
                _velocity = 0f;
                SetX(x);
            }
        }

        private void LateUpdate()
        {
            SetX(Mathf.SmoothDamp(transform.position.x, _targetX, ref _velocity, _smoothSeconds));
        }

        private void SetX(float x)
        {
            var position = transform.position;
            position.x = x;
            transform.position = position;
        }
    }
}
