using System;
using PenmonScramble.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Common.Profile
{
    /// <summary>
    /// ユーザー名の入力ダイアログ。初回起動（やめるボタン無し）と、設定からの変更（いまの名前を入れて開く）の両方で使う。
    /// 開いている間は背面の操作をブロックする
    /// </summary>
    public class NameEntryDialog : MonoBehaviour
    {
        private const string WebPromptMessage = "ユーザー名を入力してください（6文字まで）";

        [SerializeField] private InputField _inputField;
        [SerializeField] private Button _decideButton;
        // ブラウザ版だけ入力欄の上に被せ、押されたらブラウザの入力ダイアログを出す
        [SerializeField] private Button _webInputCover;
        [Tooltip("変更のときだけ出す。初回は名前を決めるまで閉じられない。未設定なら出さない")]
        [SerializeField] private Button _cancelButton;
        [Tooltip("初回と変更で見出しを変える。未設定なら変えない")]
        [SerializeField] private Text _headingText;
        [SerializeField] private string _firstHeading = "なまえを決めよう";
        [SerializeField] private string _changeHeading = "なまえの変更";

        private Action _onDecided;
        private Action _onCancelled;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _inputField.characterLimit = UserProfile.MaxNameLength;
            _inputField.onValueChanged.AddListener(_ => RefreshDecideButton());
            _decideButton.onClick.AddListener(OnDecideClicked);
            _webInputCover.onClick.AddListener(OnWebInputClicked);
            _webInputCover.gameObject.SetActive(WebNamePrompt.IsAvailable);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(Cancel);
        }

        /// <param name="onDecided">名前を保存して閉じた</param>
        /// <param name="onCancelled">渡したときだけ「やめる」を出す（変更のとき）。null なら決めるまで閉じられない（初回）</param>
        public void Show(Action onDecided, Action onCancelled = null)
        {
            _onDecided = onDecided;
            _onCancelled = onCancelled;
            // 変更のときは、いまの名前から直せるようにする
            _inputField.text = UserProfile.Name;
            if (_cancelButton != null) _cancelButton.gameObject.SetActive(onCancelled != null);
            if (_headingText != null) _headingText.text = UserProfile.HasName ? _changeHeading : _firstHeading;
            RefreshDecideButton();
            gameObject.SetActive(true);
        }

        /// <summary>やめられるときだけ閉じる（Esc などから呼ぶ）</summary>
        public void Cancel()
        {
            if (_onCancelled == null) return;

            AudioManager.PlaySeIfAvailable(SeId.ButtonClick);
            gameObject.SetActive(false);
            _onCancelled.Invoke();
        }

        private void RefreshDecideButton()
        {
            _decideButton.interactable = UserProfile.IsValid(_inputField.text);
        }

        private void OnWebInputClicked()
        {
            string result = WebNamePrompt.Prompt(WebPromptMessage, _inputField.text);
            if (result == null) return;

            // prompt には文字数制限を掛けられないため、ここで文字数上限（UserProfile.MaxNameLength）に切り詰める
            string name = UserProfile.Sanitize(result);
            if (name.Length > UserProfile.MaxNameLength)
            {
                name = name.Substring(0, UserProfile.MaxNameLength);
            }
            _inputField.text = name;
        }

        /// <summary>あとから変えられるので、確認は挟まずにそのまま決める</summary>
        private void OnDecideClicked()
        {
            AudioManager.PlaySeIfAvailable(SeId.ButtonClick);
            if (!UserProfile.TrySave(_inputField.text)) return;

            gameObject.SetActive(false);
            _onDecided?.Invoke();
        }
    }
}
