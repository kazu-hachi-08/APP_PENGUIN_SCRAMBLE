using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 縦のパワーゲージ。中身は下を軸に伸び縮みし、弱いと緑・中くらいで黄色・強いと赤。いまの強さの所に白い線。
    /// 動かしている間は枠が脈打つ（「もう一度押すと打つ」と分かるように）
    /// </summary>
    public class GolfGaugeView : MonoBehaviour
    {
        private static readonly Color Weak = new Color(0.40f, 0.88f, 0.45f);
        private static readonly Color Middle = new Color(1f, 0.85f, 0.25f);
        private static readonly Color Strong = new Color(1f, 0.36f, 0.28f);

        [SerializeField] private RectTransform _fill;
        [SerializeField] private Image _fillImage;
        [SerializeField] private RectTransform _needle;
        [SerializeField] private RectTransform _frame;
        [SerializeField] private float _pulseRate = 3f;
        [SerializeField] private float _pulseScale = 0.06f;

        private float _time;

        public void Show(bool visible, float power, bool running)
        {
            gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var t = Mathf.Clamp01(power);
            _fill.localScale = new Vector3(1f, t, 1f);
            _fillImage.color = t < 0.5f ? Color.Lerp(Weak, Middle, t * 2f) : Color.Lerp(Middle, Strong, (t - 0.5f) * 2f);
            _needle.anchorMin = _needle.anchorMax = new Vector2(0.5f, t);
            _needle.gameObject.SetActive(running || t > 0f);

            _time = running ? _time + Time.deltaTime : 0f;
            var pulse = running ? Mathf.Abs(Mathf.Sin(_time * _pulseRate * Mathf.PI)) * _pulseScale : 0f;
            _frame.localScale = Vector3.one * (1f + pulse);
        }
    }
}
