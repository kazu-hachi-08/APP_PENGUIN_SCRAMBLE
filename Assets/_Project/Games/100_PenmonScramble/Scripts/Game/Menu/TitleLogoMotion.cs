using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトルのロゴの動き: 開くと上から落ちてきて弾み、そのあとはゆっくり上下に揺れる。
    /// 止まった絵だと「メニュー画面」に見えるので、ロゴだけでも動かしてゲームらしさを出す。
    /// 「タップしてスタート」の画面では大きくまん中に、ロビーでは小さく上に置く（SetPose。移るときはなめらかに動く）
    /// </summary>
    public class TitleLogoMotion : MonoBehaviour
    {
        [SerializeField] private float _dropHeight = 600f;
        [SerializeField] private float _dropSeconds = 0.6f;
        [SerializeField] private float _bobHeight = 12f;
        [SerializeField] private float _bobSeconds = 2.4f;
        [SerializeField] private float _posePerSecond = 7f;

        private RectTransform _rect;
        private Vector2 _home;
        private Vector2 _targetHome;
        private float _scale = 1f;
        private float _targetScale = 1f;
        private float _elapsed;
        private bool _posed;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            // 開く前に SetPose で場所が決まっていれば、そちらを使う
            if (!_posed)
            {
                _home = _targetHome = _rect.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            // ステージから戻ってタイトルが開き直すたびに、もう一度落とす
            _elapsed = 0f;
        }

        /// <summary>ロゴを置く場所と大きさ。instant なら動かさずにすぐそこへ置く</summary>
        public void SetPose(Vector2 home, float scale, bool instant)
        {
            _posed = true;
            _targetHome = home;
            _targetScale = scale;
            if (instant)
            {
                _home = home;
                _scale = scale;
            }
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            var follow = 1f - Mathf.Exp(-_posePerSecond * dt);
            _home = Vector2.Lerp(_home, _targetHome, follow);
            _scale = Mathf.Lerp(_scale, _targetScale, follow);

            var offset = _elapsed < _dropSeconds
                ? _dropHeight * (1f - BounceOut(_elapsed / _dropSeconds))
                : _bobHeight * _scale * Mathf.Sin((_elapsed - _dropSeconds) / _bobSeconds * Mathf.PI * 2f);
            _rect.anchoredPosition = _home + new Vector2(0f, offset);
            _rect.localScale = Vector3.one * _scale;
        }

        /// <summary>着地して2回小さく弾む（よくある Bounce の式）</summary>
        private static float BounceOut(float t)
        {
            const float n = 7.5625f;
            const float d = 2.75f;
            if (t < 1f / d)
            {
                return n * t * t;
            }

            if (t < 2f / d)
            {
                t -= 1.5f / d;
                return n * t * t + 0.75f;
            }

            if (t < 2.5f / d)
            {
                t -= 2.25f / d;
                return n * t * t + 0.9375f;
            }

            t -= 2.625f / d;
            return n * t * t + 0.984375f;
        }
    }
}
