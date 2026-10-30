using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトルのロゴの動き: 開くと上から落ちてきて弾み、そのあとはゆっくり上下に揺れる（§9）。
    /// 止まった絵だと「メニュー画面」に見えるので、ロゴだけでも動かしてゲームらしさを出す
    /// </summary>
    public class TitleLogoMotion : MonoBehaviour
    {
        [SerializeField] private float _dropHeight = 600f;
        [SerializeField] private float _dropSeconds = 0.6f;
        [SerializeField] private float _bobHeight = 12f;
        [SerializeField] private float _bobSeconds = 2.4f;

        private RectTransform _rect;
        private Vector2 _home;
        private float _elapsed;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
        }

        private void OnEnable()
        {
            // ステージから戻ってタイトルが開き直すたびに、もう一度落とす
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            var offset = _elapsed < _dropSeconds
                ? _dropHeight * (1f - BounceOut(_elapsed / _dropSeconds))
                : _bobHeight * Mathf.Sin((_elapsed - _dropSeconds) / _bobSeconds * Mathf.PI * 2f);
            _rect.anchoredPosition = _home + new Vector2(0f, offset);
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
