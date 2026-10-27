using System;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Common.UI
{
    /// <summary>
    /// 汎用OK/Cancel確認ダイアログ
    /// </summary>
    public class CommonDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Text _confirmButtonText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Text _cancelButtonText;

        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        public void Show(
            string title,
            string message,
            Action onConfirm = null,
            Action onCancel = null,
            string confirmText = "OK",
            string cancelText = "キャンセル",
            bool showCancelButton = true)
        {
            if (_titleText != null) _titleText.text = title;
            if (_messageText != null) _messageText.text = message;

            if (_confirmButtonText != null) _confirmButtonText.text = confirmText;
            if (_cancelButtonText != null) _cancelButtonText.text = cancelText;

            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(showCancelButton);
            }

            _onConfirm = onConfirm;
            _onCancel = onCancel;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnConfirmClicked()
        {
            Hide();
            _onConfirm?.Invoke();
        }

        private void OnCancelClicked()
        {
            Hide();
            _onCancel?.Invoke();
        }
    }
}
