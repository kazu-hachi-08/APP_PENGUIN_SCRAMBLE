using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 結果の札1枚（「倒した数」「コイン」など）: 右からすべり込んで少し行き過ぎて戻り、出てから数字を 0 から数え上げる。
    /// 姿は「出てからの秒」だけで決める（Evaluate）。飛ばしたときに最後の姿へすぐ合わせられるようにするため
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ResultRow : MonoBehaviour
    {
        private const float SlideSeconds = 0.4f;
        private const float Overshoot = 1.6f;
        // すべり込んで止まりかけてから数え始める
        private const float CountDelay = 0.2f;
        private const float CountSeconds = 0.6f;
        // 数え上げの音は、数字が変わるたびに鳴らすと詰まりすぎるので間を空ける（秒）
        private const float CountSoundInterval = 0.05f;
        private const float BumpSeconds = 0.18f;
        private const float BumpScale = 0.12f;
        private const float PulseSeconds = 1.2f;
        private const float PulseScale = 0.04f;

        [SerializeField] private RectTransform _plate;
        [SerializeField] private Text _value;
        [SerializeField] private Vector2 _slideFrom = new Vector2(520f, 0f);
        [Tooltip("出たあと、ゆっくりふくらみ続ける（「解放！」のように目を引きたい札）")]
        [SerializeField] private bool _pulse;

        private CanvasGroup _group;
        private RectTransform _rect;
        private Vector2 _home;
        private int _target = -1;
        private string _format = "{0}";
        private int _shown = -1;
        private float _lastSoundAt = float.NegativeInfinity;
        private float _bump = BumpSeconds;
        private float _age = -1f;

        public RectTransform Plate => _plate;

        /// <summary>出さない札は有効にならず Awake が呼ばれないので、使う前に自分で結び付ける</summary>
        private void Bind()
        {
            if (_group != null)
            {
                return;
            }

            _group = GetComponent<CanvasGroup>();
            _rect = (RectTransform)transform;
            // 札は飾りなので、後ろの「タップで飛ばす」を邪魔しない
            _group.blocksRaycasts = false;
        }

        /// <param name="home">止まる場所（出す札だけを上から詰めて並べるので、出すたびに決める）</param>
        /// <param name="target">数え上げる数。負なら数えない（文字は呼んだ側が入れる）</param>
        public void Setup(bool shown, Vector2 home, int target, string format)
        {
            gameObject.SetActive(shown);
            Bind();
            _home = home;
            _target = target;
            _format = format;
            _shown = -1;
            _age = -1f;
            Evaluate(-1f, silent: true);
        }

        /// <param name="t">出てからの秒。負ならまだ出さない</param>
        /// <param name="silent">飛ばしたときは、数え上げの音を鳴らさない</param>
        public void Evaluate(float t, bool silent)
        {
            Bind();
            _age = t;
            if (t < 0f)
            {
                _group.alpha = 0f;
                _rect.anchoredPosition = _home + _slideFrom;
                return;
            }

            var slide = Mathf.Clamp01(t / SlideSeconds);
            _rect.anchoredPosition = _home + _slideFrom * (1f - BackOut(slide));
            // 動き始めのうちに見えるようにする（遠くで薄いまま止まって見えないように）
            _group.alpha = Mathf.Clamp01(slide * 3f);
            Count(t, silent);
        }

        /// <summary>コインが吸い込まれたときなどに、札をぽんとふくらませる</summary>
        public void Bump()
        {
            _bump = 0f;
        }

        private void Count(float t, bool silent)
        {
            if (_target < 0 || _value == null)
            {
                return;
            }

            var progress = Mathf.Clamp01((t - CountDelay) / CountSeconds);
            // 最初は速く、終わりはゆっくり数える（最後の数字が読みやすい）
            var eased = 1f - (1f - progress) * (1f - progress);
            var shown = Mathf.RoundToInt(_target * eased);
            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            _value.text = string.Format(_format, shown);
            if (!silent && shown > 0 && Time.unscaledTime - _lastSoundAt >= CountSoundInterval)
            {
                _lastSoundAt = Time.unscaledTime;
                GameSounds.Play(GameSound.ResultCount);
            }
        }

        private void Update()
        {
            _bump = Mathf.Min(BumpSeconds, _bump + Time.unscaledDeltaTime);
            var bump = Mathf.Sin(_bump / BumpSeconds * Mathf.PI) * BumpScale;
            var pulse = _pulse && _age >= SlideSeconds
                ? (0.5f - 0.5f * Mathf.Cos((_age - SlideSeconds) / PulseSeconds * Mathf.PI * 2f)) * PulseScale
                : 0f;
            _plate.localScale = Vector3.one * (1f + bump + pulse);
        }

        private static float BackOut(float t)
        {
            var u = t - 1f;
            return 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u;
        }
    }
}
