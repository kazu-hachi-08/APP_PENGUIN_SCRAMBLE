using System;
using PenmonScramble.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Common.UI
{
    /// <summary>
    /// 一時停止（ポーズ）メニューダイアログ
    /// </summary>
    public class PauseDialog : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _titleButton;

        [Header("Audio Sliders")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _seSlider;
        [SerializeField] private Toggle _muteToggle;

        private Action _onResume;
        private Action _onRestart;
        private Action _onTitle;

        private void Awake()
        {
            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
            if (_restartButton != null) _restartButton.onClick.AddListener(OnRestartClicked);
            if (_titleButton != null) _titleButton.onClick.AddListener(OnTitleClicked);

            if (_bgmSlider != null)
            {
                _bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
            }

            if (_seSlider != null)
            {
                _seSlider.onValueChanged.AddListener(OnSeSliderChanged);
            }

            if (_muteToggle != null)
            {
                _muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);
            }
        }

        private void OnEnable()
        {
            // AudioManagerの現在値をUIに同期
            if (AudioManager.HasInstance)
            {
                var audioMgr = AudioManager.Instance;
                if (_bgmSlider != null) _bgmSlider.value = audioMgr.BgmVolume;
                if (_seSlider != null) _seSlider.value = audioMgr.SeVolume;
                if (_muteToggle != null) _muteToggle.isOn = audioMgr.IsMuted;
            }
        }

        public void Initialize(Action onResume, Action onRestart, Action onTitle)
        {
            _onResume = onResume;
            _onRestart = onRestart;
            _onTitle = onTitle;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnResumeClicked() => HideAndInvoke(_onResume);

        private void OnRestartClicked() => HideAndInvoke(_onRestart);

        private void OnTitleClicked() => HideAndInvoke(_onTitle);

        // 先に閉じてからコールバックを呼ぶ（コールバック側で次の画面を開いても、このダイアログが残らないようにする）
        private void HideAndInvoke(Action callback)
        {
            AudioManager.PlaySeIfAvailable(SeId.ButtonClick);
            Hide();
            callback?.Invoke();
        }

        private void OnBgmSliderChanged(float value)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SetBgmVolume(value);
            }
        }

        private void OnSeSliderChanged(float value)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SetSeVolume(value);
            }
        }

        private void OnMuteToggleChanged(bool isMute)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SetMute(isMute);
            }
        }

        private void OnDisable()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SaveAudioSettings();
            }
        }
    }
}
