using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 開くたびに、画面の外の方からすべり込んで少し行き過ぎて戻る。
    /// ロビーの部品（上の札・左の列・右下のバトル）を時間差で入れて、ぱっと切り替わるより「ゲームの画面」らしく見せる
    /// </summary>
    public class UiSlideIn : MonoBehaviour
    {
        [SerializeField] private Vector2 _from = new Vector2(-400f, 0f);
        [SerializeField] private float _delay;
        [SerializeField] private float _seconds = 0.45f;
        // 行き過ぎる量（BackOut の強さ）
        [SerializeField] private float _overshoot = 1.6f;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _home;
        private float _elapsed;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
            _group = GetComponent<CanvasGroup>();
            if (_group == null)
            {
                _group = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            Apply(0f);
        }

        private void Update()
        {
            if (_elapsed > _delay + _seconds)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            Apply(Mathf.Clamp01((_elapsed - _delay) / _seconds));
        }

        private void Apply(float t)
        {
            _rect.anchoredPosition = _home + _from * (1f - BackOut(t));
            // 動き始めのうちに見えるようにする（遠くで薄いまま止まって見えないように）
            _group.alpha = Mathf.Clamp01(t * 3f);
        }

        private float BackOut(float t)
        {
            var u = t - 1f;
            return 1f + (_overshoot + 1f) * u * u * u + _overshoot * u * u;
        }
    }
}
