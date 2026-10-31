using System;
using PenmonScramble.Common.Audio;
using PenmonScramble.Common.Profile;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトルの設定（§2）: なまえ・BGM・効果音。
    /// 音量はステージ中のメニューと同じ AudioManager の値なので、どちらで変えても同じになる。閉じたときに保存する
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        // 効果音の大きさを、つまみを動かしながら耳で確かめられるように鳴らす。動かしている間ずっと鳴らないよう間を空ける
        private const float SePreviewInterval = 0.15f;

        [SerializeField] private Text _nameText;
        [SerializeField] private Button _changeNameButton;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _seSlider;
        [SerializeField] private Button _closeButton;
        [SerializeField] private NameEntryDialog _nameEntry;

        private float _lastPreviewAt = float.NegativeInfinity;
        private bool _syncing;

        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _changeNameButton.onClick.AddListener(() => _nameEntry.Show(RefreshName, RefreshName));
            _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            _seSlider.onValueChanged.AddListener(OnSeChanged);
            _closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            // 開いたときに値を合わせるだけで音が鳴らないようにする
            _syncing = true;
            _bgmSlider.value = AudioManager.Instance.BgmVolume;
            _seSlider.value = AudioManager.Instance.SeVolume;
            _syncing = false;
            RefreshName();
        }

        public void Close()
        {
            AudioManager.Instance.SaveAudioSettings();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void RefreshName()
        {
            _nameText.text = UserProfile.Name;
        }

        private void OnBgmChanged(float value)
        {
            AudioManager.Instance.SetBgmVolume(value);
        }

        private void OnSeChanged(float value)
        {
            AudioManager.Instance.SetSeVolume(value);
            if (_syncing || Time.unscaledTime - _lastPreviewAt < SePreviewInterval)
            {
                return;
            }

            _lastPreviewAt = Time.unscaledTime;
            GameSounds.Play(GameSound.Click);
        }
    }
}
