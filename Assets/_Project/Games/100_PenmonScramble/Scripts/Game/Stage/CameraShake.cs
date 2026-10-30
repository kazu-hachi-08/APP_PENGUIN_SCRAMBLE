using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 画面の揺れ（§9）。カメラの位置を元の位置からずらすだけ。揺れが重なったら強い方を取る
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        private Vector3 _basePosition;
        private float _amplitude;
        private float _seconds;
        private float _remaining;

        private void Awake()
        {
            _basePosition = transform.localPosition;
        }

        public void Shake(float amplitude, float seconds)
        {
            // 残っている揺れより弱いなら、上書きしない（大きな揺れが小さな揺れで消えないように）
            if (amplitude < _amplitude * Remaining01)
            {
                return;
            }

            _amplitude = amplitude;
            _seconds = Mathf.Max(0.01f, seconds);
            _remaining = _seconds;
        }

        private float Remaining01 => _seconds > 0f ? Mathf.Clamp01(_remaining / _seconds) : 0f;

        private void LateUpdate()
        {
            if (_remaining <= 0f)
            {
                transform.localPosition = _basePosition;
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            var offset = Random.insideUnitCircle * (_amplitude * Remaining01);
            transform.localPosition = _basePosition + new Vector3(offset.x, offset.y, 0f);
        }
    }
}
