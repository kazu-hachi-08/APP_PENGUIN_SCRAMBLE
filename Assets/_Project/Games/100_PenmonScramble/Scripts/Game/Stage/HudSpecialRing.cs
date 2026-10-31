using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// HUD の顔のまわりの必殺技のゲージ（§4.3・§9.8）。5つに区切った輪が、敵1体分ずつたまる。
    /// 満タンになったら、スマホの必殺技のボタン（TouchSpecialButton）と同じく、後ろの光が脈打ち、外の輪が回り、「必殺OK」の札が弾む
    /// </summary>
    public class HudSpecialRing : MonoBehaviour
    {
        private const float HaloMinAlpha = 0.35f;
        private const float HaloMaxAlpha = 0.85f;

        [Tooltip("区切りのある輪。Filled（Radial360・上から時計回り）でゲージの分だけ出す")]
        [SerializeField] private Image _gauge;
        [SerializeField] private Image _halo;
        [SerializeField] private RectTransform _spin;
        [SerializeField] private RectTransform _tag;
        [SerializeField] private Color _color = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private Color _readyColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField] private float _pulsePerSecond = 2.5f;
        [SerializeField] private float _spinDegreesPerSecond = 90f;
        [SerializeField] private float _haloGrow = 0.15f;
        [SerializeField] private float _tagBounce = 0.12f;

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
            _gauge.color = ready ? _readyColor : _color;
            _halo.gameObject.SetActive(ready);
            _spin.gameObject.SetActive(ready);
            _tag.gameObject.SetActive(ready);
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            _readyTime += Time.unscaledDeltaTime;
            var wave = 0.5f + 0.5f * Mathf.Sin(_readyTime * _pulsePerSecond * Mathf.PI * 2f);
            _gauge.color = Color.Lerp(_readyColor, Color.white, wave * 0.5f);
            _halo.color = new Color(_readyColor.r, _readyColor.g, _readyColor.b, Mathf.Lerp(HaloMinAlpha, HaloMaxAlpha, wave));
            _halo.rectTransform.localScale = Vector3.one * (1f + _haloGrow * wave);
            _spin.localRotation = Quaternion.Euler(0f, 0f, -_readyTime * _spinDegreesPerSecond);
            _tag.localScale = Vector3.one * (1f + _tagBounce * wave);
        }
    }
}
