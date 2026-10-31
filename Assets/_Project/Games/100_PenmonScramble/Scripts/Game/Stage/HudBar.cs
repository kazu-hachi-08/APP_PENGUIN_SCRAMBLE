using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// HUD の HP のバー（自分・ボス）: 減った分は明るい色の残りとして少し残ってから追いつき、減った瞬間にバーが揺れる。
    /// 中身は斜めの板を2つの切り抜き（RectMask2D）に入れて、切り抜きの右端を割合に合わせて縮める（斜めの形がつぶれないように）
    /// </summary>
    public class HudBar : MonoBehaviour
    {
        // これより小さい変化は「減った」とみなさない（割合の丸めで揺れないように）
        private const float DropEpsilon = 0.0001f;

        [SerializeField] private RectTransform _fillClip;
        [SerializeField] private RectTransform _trailClip;
        [SerializeField] private Image _fill;
        [SerializeField] private RectTransform _shakeTarget;
        [SerializeField] private float _trailDelay = 0.45f;
        [SerializeField] private float _trailPerSecond = 0.9f;
        [SerializeField] private float _shakeSeconds = 0.25f;
        [SerializeField] private float _shakeDistance = 7f;
        [SerializeField] private float _shakePerSecond = 30f;

        [Header("割合で色を変える（自分の HP）")]
        [SerializeField] private bool _colorByRatio;
        [SerializeField] private Color _color = new Color(0.35f, 0.9f, 0.45f);
        [SerializeField] private Color _midColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private Color _lowColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] private float _midRatio = 0.5f;
        [SerializeField] private float _lowRatio = 0.3f;
        [SerializeField] private float _blinkPerSecond = 2.5f;

        private float _ratio = 1f;
        private float _trailRatio = 1f;
        private float _trailWait;
        private float _shakeTimer;
        private float _elapsed;
        private Vector2 _home;
        private bool _homeKnown;

        private void Awake()
        {
            EnsureHome();
        }

        /// <summary>HUD は隠した状態から始まるので、Awake より先に Snap が呼ばれることがある。動かす前に元の位置を覚えておく</summary>
        private void EnsureHome()
        {
            if (!_homeKnown)
            {
                _home = _shakeTarget.anchoredPosition;
                _homeKnown = true;
            }
        }

        /// <summary>減ったら残りを遅れて追いつかせて揺らす。増えたら（回復・やり直し）残りもすぐ合わせる</summary>
        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (ratio < _ratio - DropEpsilon)
            {
                _trailWait = _trailDelay;
                _shakeTimer = _shakeSeconds;
            }

            _ratio = ratio;
            _trailRatio = Mathf.Max(_trailRatio, ratio);
            Apply();
        }

        /// <summary>別の子・別のボスに替わったときは、残りも揺れも出さずにそのまま合わせる</summary>
        public void Snap(float ratio)
        {
            _ratio = _trailRatio = Mathf.Clamp01(ratio);
            _trailWait = 0f;
            _shakeTimer = 0f;
            EnsureHome();
            _shakeTarget.anchoredPosition = _home;
            Apply();
        }

        private void Apply()
        {
            _fillClip.anchorMax = new Vector2(_ratio, 1f);
            _trailClip.anchorMax = new Vector2(_trailRatio, 1f);
        }

        private void Update()
        {
            // ヒットストップで時間が止まっても HUD は動かす
            var dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            UpdateTrail(dt);
            UpdateShake(dt);
            if (_colorByRatio)
            {
                _fill.color = RatioColor();
            }
        }

        private void UpdateTrail(float dt)
        {
            if (_trailWait > 0f)
            {
                _trailWait -= dt;
                return;
            }

            if (_trailRatio > _ratio)
            {
                _trailRatio = Mathf.Max(_ratio, _trailRatio - _trailPerSecond * dt);
                Apply();
            }
        }

        private void UpdateShake(float dt)
        {
            if (_shakeTimer <= 0f)
            {
                return;
            }

            EnsureHome();
            _shakeTimer -= dt;
            var strength = Mathf.Clamp01(_shakeTimer / _shakeSeconds);
            var offset = Mathf.Sin(_elapsed * _shakePerSecond * Mathf.PI * 2f) * _shakeDistance * strength;
            _shakeTarget.anchoredPosition = _home + new Vector2(offset, 0f);
        }

        /// <summary>多い間は緑、半分を切ったら黄色、残り少ないと赤く点滅する（危ないと気づけるように）</summary>
        private Color RatioColor()
        {
            if (_ratio > _midRatio)
            {
                return _color;
            }

            if (_ratio > _lowRatio)
            {
                return _midColor;
            }

            if (_ratio <= 0f)
            {
                return _lowColor;
            }

            var blink = 0.5f + 0.5f * Mathf.Sin(_elapsed * _blinkPerSecond * Mathf.PI * 2f);
            return Color.Lerp(_lowColor, Color.white, blink * 0.5f);
        }
    }
}
