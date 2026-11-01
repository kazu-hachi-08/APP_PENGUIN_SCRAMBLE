using PenmonScramble.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中のメニューの「設定」で、札の右にすべり出る音量の板（BGM・効果音）。
    /// 値はタイトルの設定と同じ AudioManager のものなので、どちらで変えても同じになる。閉じたときに保存する。
    /// キーボード・コントローラーでは上下で行を選び、左右で動かす（マウス・指はつまみを直接動かす）
    /// </summary>
    public class StageMenuVolume : MonoBehaviour
    {
        // 効果音のつまみを動かしたときの確かめの音は、この間隔より詰めて鳴らさない
        private const float SePreviewInterval = 0.12f;

        [SerializeField] private Slider[] _sliders;
        [Tooltip("キーボード・コントローラーで選んでいる行の後ろの光。_sliders と同じ並び")]
        [SerializeField] private Image[] _rowHighlights;
        [SerializeField] private float _keyStep = 0.1f;

        private const int BgmRow = 0;
        private const int SeRow = 1;

        private bool _syncing;
        private float _lastPreviewAt = float.NegativeInfinity;
        private int _row;
        private bool _keyFocus;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _sliders[BgmRow].onValueChanged.AddListener(OnBgmChanged);
            _sliders[SeRow].onValueChanged.AddListener(OnSeChanged);
        }

        /// <param name="keyFocus">キーボード・コントローラーで開いた。最初の行を光らせて、上下左右を受ける</param>
        public void Open(bool keyFocus)
        {
            gameObject.SetActive(true);
            // 開いたときに値を合わせるだけで音が鳴らないようにする
            _syncing = true;
            _sliders[BgmRow].value = AudioManager.HasInstance ? AudioManager.Instance.BgmVolume : 1f;
            _sliders[SeRow].value = AudioManager.HasInstance ? AudioManager.Instance.SeVolume : 1f;
            _syncing = false;
            _row = BgmRow;
            SetKeyFocus(keyFocus);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SaveAudioSettings();
            }

            gameObject.SetActive(false);
        }

        /// <summary>上下で行を選び、左右でつまみを動かす</summary>
        public void Move(Vector2Int step)
        {
            if (step.y != 0)
            {
                _row = Mathf.Clamp(_row - step.y, 0, _sliders.Length - 1);
                SetKeyFocus(true);
                return;
            }

            if (step.x != 0)
            {
                var slider = _sliders[_row];
                slider.value = Mathf.Clamp01(slider.value + step.x * _keyStep);
                SetKeyFocus(true);
            }
        }

        private void SetKeyFocus(bool keyFocus)
        {
            _keyFocus = keyFocus;
            for (var i = 0; i < _rowHighlights.Length; i++)
            {
                _rowHighlights[i].enabled = _keyFocus && i == _row;
            }
        }

        private void OnBgmChanged(float value)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SetBgmVolume(value);
            }
        }

        private void OnSeChanged(float value)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SetSeVolume(value);
            }

            if (_syncing || Time.unscaledTime - _lastPreviewAt < SePreviewInterval)
            {
                return;
            }

            // 大きさを確かめられるよう、動かすと音を鳴らす（タイトルの設定と同じ）
            _lastPreviewAt = Time.unscaledTime;
            GameSounds.Play(GameSound.Click);
        }
    }
}
