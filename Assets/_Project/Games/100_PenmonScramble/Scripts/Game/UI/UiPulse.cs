using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>ゆっくり明滅して少しふくらむ。「タップしてスタート」のように、押せば進むことを知らせる文字に付ける</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UiPulse : MonoBehaviour
    {
        [SerializeField] private float _seconds = 1.4f;
        [SerializeField] private float _minAlpha = 0.35f;
        [SerializeField] private float _scaleAmount = 0.04f;

        private CanvasGroup _group;
        private float _elapsed;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            var wave = 0.5f + 0.5f * Mathf.Cos(_elapsed / _seconds * Mathf.PI * 2f);
            _group.alpha = Mathf.Lerp(_minAlpha, 1f, wave);
            transform.localScale = Vector3.one * (1f + _scaleAmount * wave);
        }
    }
}
