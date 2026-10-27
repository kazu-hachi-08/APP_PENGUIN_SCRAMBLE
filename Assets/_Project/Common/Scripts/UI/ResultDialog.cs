using System;
using PenmonScramble.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Common.UI
{
    /// <summary>
    /// 共通リザルト画面
    /// </summary>
    public class ResultDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _resultTitleText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _detailText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        private Action _onRetry;
        private Action _onTitle;

        private void Awake()
        {
            if (_retryButton != null) _retryButton.onClick.AddListener(OnRetryClicked);
            if (_titleButton != null) _titleButton.onClick.AddListener(OnTitleClicked);
        }

        public void Show(
            string title,
            string scoreInfo,
            string detailInfo = "",
            Action onRetry = null,
            Action onTitle = null)
        {
            if (_resultTitleText != null) _resultTitleText.text = title;
            if (_scoreText != null) _scoreText.text = scoreInfo;
            if (_detailText != null) _detailText.text = detailInfo;

            _onRetry = onRetry;
            _onTitle = onTitle;
            // 続きが無い場面（負けて終わりなど）では押しても何も起きないボタンを見せない
            if (_retryButton != null) _retryButton.gameObject.SetActive(onRetry != null);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnRetryClicked()
        {
            AudioManager.PlaySeIfAvailable(SeId.ButtonClick);
            Hide();
            _onRetry?.Invoke();
        }

        private void OnTitleClicked()
        {
            AudioManager.PlaySeIfAvailable(SeId.ButtonClick);
            Hide();
            _onTitle?.Invoke();
        }
    }
}
