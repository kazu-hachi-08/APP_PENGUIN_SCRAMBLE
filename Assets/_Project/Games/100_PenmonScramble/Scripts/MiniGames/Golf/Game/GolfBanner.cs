using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>画面のまん中の知らせ（「ホール 1  パー 3」「ナイスイン！」など）。出た瞬間に大きく出て縮み、しばらくして消える</summary>
    public class GolfBanner : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _popSeconds = 0.18f;
        [SerializeField] private float _popScale = 1.5f;
        [SerializeField] private float _fadeSeconds = 0.3f;

        private float _time;
        private float _hold;

        public void Show(string message, Color color, float seconds)
        {
            _text.text = message;
            _text.color = color;
            _time = 0f;
            _hold = seconds;
            gameObject.SetActive(true);
            Apply();
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            _time += Time.deltaTime;
            Apply();
            if (_time >= _hold + _fadeSeconds)
            {
                Hide();
            }
        }

        private void Apply()
        {
            var pop = Mathf.Clamp01(_time / _popSeconds);
            var scale = Mathf.Lerp(_popScale, 1f, 1f - (1f - pop) * (1f - pop));
            transform.localScale = Vector3.one * scale;
            _group.alpha = 1f - Mathf.Clamp01((_time - _hold) / _fadeSeconds);
        }
    }
}
