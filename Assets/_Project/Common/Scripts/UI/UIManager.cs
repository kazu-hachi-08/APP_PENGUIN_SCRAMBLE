using System;
using PenmonScramble.Common.Core;
using UnityEngine;

namespace PenmonScramble.Common.UI
{
    /// <summary>
    /// 共通UI（ダイアログ、ポーズ、リザルト等）の表示と制御を統括するマネージャー
    /// </summary>
    public class UIManager : SingletonMonoBehaviour<UIManager>
    {
        [Header("UI Dialog References")]
        [SerializeField] private CommonDialog _commonDialog;
        [SerializeField] private PauseDialog _pauseDialog;
        [SerializeField] private ResultDialog _resultDialog;

        /// <summary>
        /// 汎用確認ダイアログの表示
        /// </summary>
        public void ShowConfirmDialog(
            string title,
            string message,
            Action onConfirm = null,
            Action onCancel = null,
            string confirmText = "OK",
            string cancelText = "キャンセル")
        {
            if (_commonDialog != null)
            {
                _commonDialog.Show(title, message, onConfirm, onCancel, confirmText, cancelText, showCancelButton: true);
            }
            else
            {
                WarnMissingDialog("CommonDialog");
            }
        }

        /// <summary>
        /// 一時停止ダイアログの表示
        /// </summary>
        public void ShowPauseDialog(Action onResume, Action onRestart, Action onTitle)
        {
            if (_pauseDialog != null)
            {
                _pauseDialog.Initialize(onResume, onRestart, onTitle);
                _pauseDialog.Show();
            }
            else
            {
                WarnMissingDialog("PauseDialog");
            }
        }

        /// <summary>
        /// 一時停止ダイアログを閉じる
        /// </summary>
        public void HidePauseDialog()
        {
            if (_pauseDialog != null)
            {
                _pauseDialog.Hide();
            }
        }

        /// <summary>
        /// リザルト画面の表示
        /// </summary>
        public void ShowResultDialog(
            string title,
            string scoreInfo,
            string detailInfo = "",
            Action onRetry = null,
            Action onTitle = null)
        {
            if (_resultDialog != null)
            {
                _resultDialog.Show(title, scoreInfo, detailInfo, onRetry, onTitle);
            }
            else
            {
                WarnMissingDialog("ResultDialog");
            }
        }

        // 参照未設定の警告文を1か所にまとめる（出る文字列はダイアログごとに別）
        private static void WarnMissingDialog(string dialogName)
        {
            Debug.LogWarning($"[UIManager] {dialogName}参照が設定されていません。");
        }
    }
}
