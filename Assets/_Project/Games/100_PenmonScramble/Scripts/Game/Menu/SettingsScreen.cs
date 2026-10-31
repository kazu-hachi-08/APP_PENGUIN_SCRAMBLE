using System;
using PenmonScramble.Common.Audio;
using PenmonScramble.Common.Profile;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトルの設定（§3.3）: なまえ・BGM・効果音・記録の初期化。
    /// 音量はステージ中のメニューと同じ AudioManager の値なので、どちらで変えても同じになる。閉じたときに保存する。
    /// 初期化は確認の窓の赤いボタンを2回押して確定する（最初の3体を引き直したい人向け。間違えて消さないように）
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

        [Header("初期化")]
        [SerializeField] private Button _resetButton;
        [SerializeField] private GameObject _resetConfirm;
        [SerializeField] private Button _resetConfirmButton;
        [SerializeField] private Text _resetConfirmText;
        [SerializeField] private Button _resetCancelButton;
        [SerializeField] private string _resetLabel = "初期化する";
        [SerializeField] private string _resetArmedLabel = "もう一度押す";

        private float _lastPreviewAt = float.NegativeInfinity;
        private bool _syncing;
        private bool _resetArmed;

        public event Action Closed;

        /// <summary>記録を初期化した（設定は閉じた後）。ロビーが無料ガチャを開く</summary>
        public event Action RecordReset;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _changeNameButton.onClick.AddListener(() => _nameEntry.Show(RefreshName, RefreshName));
            _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            _seSlider.onValueChanged.AddListener(OnSeChanged);
            _closeButton.onClick.AddListener(Close);
            _resetButton.onClick.AddListener(OpenResetConfirm);
            _resetConfirmButton.onClick.AddListener(OnResetPressed);
            _resetCancelButton.onClick.AddListener(CloseResetConfirm);
            _resetConfirm.SetActive(false);
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
            CloseResetConfirm();
            AudioManager.Instance.SaveAudioSettings();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>Esc / OPTIONS: 確認の窓が開いていればそれだけを閉じる</summary>
        public void Back()
        {
            if (_resetConfirm.activeSelf)
            {
                CloseResetConfirm();
                return;
            }

            Close();
        }

        private void OpenResetConfirm()
        {
            SetResetArmed(false);
            _resetConfirm.SetActive(true);
        }

        private void CloseResetConfirm()
        {
            _resetConfirm.SetActive(false);
        }

        private void OnResetPressed()
        {
            if (!_resetArmed)
            {
                SetResetArmed(true);
                return;
            }

            GameSave.Reset();
            Close();
            RecordReset?.Invoke();
        }

        private void SetResetArmed(bool armed)
        {
            _resetArmed = armed;
            _resetConfirmText.text = armed ? _resetArmedLabel : _resetLabel;
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
