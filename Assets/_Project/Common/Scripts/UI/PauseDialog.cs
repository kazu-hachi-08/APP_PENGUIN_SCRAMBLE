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
        // 遊び方を持つゲームだけ置く（無ければ出さない）
        [SerializeField] private Button _helpButton;

        [Header("Audio Sliders")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _seSlider;
        [SerializeField] private Toggle _muteToggle;

        private Action _onResume;
        private Action _onRestart;
        private Action _onTitle;
        private Action _onHelp;

        private void Awake()
        {
            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
            if (_restartButton != null) _restartButton.onClick.AddListener(OnRestartClicked);
            if (_titleButton != null) _titleButton.onClick.AddListener(OnTitleClicked);
            if (_helpButton != null) _helpButton.onClick.AddListener(OnHelpClicked);

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
            WithAudio(audioMgr =>
            {
                if (_bgmSlider != null) _bgmSlider.value = audioMgr.BgmVolume;
                if (_seSlider != null) _seSlider.value = audioMgr.SeVolume;
                if (_muteToggle != null) _muteToggle.isOn = audioMgr.IsMuted;
            });
        }

        public void Initialize(Action onResume, Action onRestart, Action onTitle, Action onHelp = null)
        {
            _onResume = onResume;
            _onRestart = onRestart;
            _onTitle = onTitle;
            _onHelp = onHelp;
        }

        /// <summary>やり直しを押せない人（オンラインのゲストなど）には出さない</summary>
        public void SetRestartVisible(bool visible)
        {
            if (_restartButton != null) _restartButton.gameObject.SetActive(visible);
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

        private void OnHelpClicked() => HideAndInvoke(_onHelp);

        // 先に閉じてからコールバックを呼ぶ（コールバック側で次の画面を開いても、このダイアログが残らないようにする）
        private void HideAndInvoke(Action callback)
        {
            AudioManager.PlaySeIfAvailable(SeId.ButtonClick);
            Hide();
            callback?.Invoke();
        }

        private void OnBgmSliderChanged(float value) => WithAudio(a => a.SetBgmVolume(value));

        private void OnSeSliderChanged(float value) => WithAudio(a => a.SetSeVolume(value));

        private void OnMuteToggleChanged(bool isMute) => WithAudio(a => a.SetMute(isMute));

        private void OnDisable() => WithAudio(a => a.SaveAudioSettings());

        // AudioManager が無い（シーン単体で開いた・終了処理中など）ときは何もしない
        private static void WithAudio(Action<AudioManager> onReady)
        {
            if (AudioManager.HasInstance) onReady(AudioManager.Instance);
        }
    }
}
