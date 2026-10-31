using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// スマホの必殺技のボタン（§3「ゲージ満タンで光る」）: 縁のリングがゲージの分だけたまり、たまるまでは面をくすませる。
    /// 満タンになったら、後ろの光が脈打ち、外の輪が回り、文字が弾む
    /// </summary>
    public class TouchSpecialButton : MonoBehaviour
    {
        // たまるまでは面を灰色へ寄せる（押しても出ないと分かるように）
        private const float IdleGray = 0.6f;
        private const float BaseDarken = 0.45f;
        private const float HaloMinAlpha = 0.45f;
        private const float HaloMaxAlpha = 0.9f;
        private static readonly Color Gray = new Color(0.40f, 0.42f, 0.48f);

        [SerializeField] private Image _face;
        [SerializeField] private Image _base;
        [Tooltip("縁のリング。Filled（Radial360）でゲージの分だけ出す")]
        [SerializeField] private Image _gauge;
        [SerializeField] private Image _halo;
        [SerializeField] private RectTransform _spin;
        [SerializeField] private RectTransform _label;
        [SerializeField] private Color _readyColor = new Color(1f, 0.78f, 0.1f);
        [SerializeField] private float _pulsePerSecond = 2.5f;
        [SerializeField] private float _spinDegreesPerSecond = 90f;
        [SerializeField] private float _haloGrow = 0.18f;
        [SerializeField] private float _labelBounce = 0.08f;

        private bool _ready;
        private float _readyTime;

        private void Awake()
        {
            ApplyLook(false);
        }

        public void SetGauge(float gauge)
        {
            _gauge.fillAmount = Mathf.Clamp01(gauge);
            var ready = gauge >= 1f;
            if (ready != _ready)
            {
                ApplyLook(ready);
            }
        }

        private void ApplyLook(bool ready)
        {
            _ready = ready;
            _readyTime = 0f;
            var face = ready ? _readyColor : Color.Lerp(_readyColor, Gray, IdleGray);
            _face.color = face;
            _base.color = Color.Lerp(face, Color.black, BaseDarken);
            _halo.gameObject.SetActive(ready);
            _spin.gameObject.SetActive(ready);
        }

        private void Update()
        {
            if (!_ready)
            {
                _label.localScale = Vector3.one;
                return;
            }

            _readyTime += Time.unscaledDeltaTime;
            var wave = 0.5f + 0.5f * Mathf.Sin(_readyTime * _pulsePerSecond * Mathf.PI * 2f);
            _halo.color = new Color(_readyColor.r, _readyColor.g, _readyColor.b, Mathf.Lerp(HaloMinAlpha, HaloMaxAlpha, wave));
            _halo.rectTransform.localScale = Vector3.one * (1f + _haloGrow * wave);
            _spin.localRotation = Quaternion.Euler(0f, 0f, -_readyTime * _spinDegreesPerSecond);
            _label.localScale = Vector3.one * (1f + _labelBounce * wave);
        }
    }
}
