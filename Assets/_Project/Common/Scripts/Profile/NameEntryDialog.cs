using System;
using PenguinScramble.Common.Audio;
using PenguinScramble.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinScramble.Common.Profile
{
    /// <summary>
    /// 初回起動時のユーザー名入力ダイアログ。
    /// 名前は変更不可なので閉じるボタンは置かず、決定するまで背面の操作をブロックする。
    /// </summary>
    public class NameEntryDialog : MonoBehaviour
    {
        private const string WebPromptMessage = "ユーザー名を入力してください（6文字まで）";

        [SerializeField] private InputField _inputField;
        [SerializeField] private Button _decideButton;
        // ブラウザ版だけ入力欄の上に被せ、押されたらブラウザの入力ダイアログを出す
        [SerializeField] private Button _webInputCover;

        private Action _onDecided;

        private void Awake()
        {
            _inputField.characterLimit = UserProfile.MaxNameLength;
            _inputField.onValueChanged.AddListener(_ => RefreshDecideButton());
            _decideButton.onClick.AddListener(OnDecideClicked);
            _webInputCover.onClick.AddListener(OnWebInputClicked);
            _webInputCover.gameObject.SetActive(WebNamePrompt.IsAvailable);
        }

        public void Show(Action onDecided)
        {
            _onDecided = onDecided;
            _inputField.text = string.Empty;
            RefreshDecideButton();
            gameObject.SetActive(true);
        }

        private void RefreshDecideButton()
        {
            _decideButton.interactable = UserProfile.IsValid(_inputField.text);
        }

        private void OnWebInputClicked()
        {
            string result = WebNamePrompt.Prompt(WebPromptMessage, _inputField.text);
            if (result == null) return;

            // prompt には文字数制限を掛けられないため、ここで6文字に切り詰める
            string name = UserProfile.Sanitize(result);
            if (name.Length > UserProfile.MaxNameLength)
            {
                name = name.Substring(0, UserProfile.MaxNameLength);
            }
            _inputField.text = name;
        }

        private void OnDecideClicked()
        {
            AudioManager.PlaySeIfAvailable(SeId.ButtonClick);
            string name = UserProfile.Sanitize(_inputField.text);
            if (!UserProfile.IsValid(name)) return;

            if (!UIManager.HasInstance)
            {
                Decide(name);
                return;
            }

            // 変更不可なので、打ち間違いのまま確定しないよう1回だけ確認する。キャンセル時はこのダイアログに戻る
            UIManager.Instance.ShowConfirmDialog(
                title: "この名前でいい？",
                message: $"「{name}」\nあとから変更できません",
                onConfirm: () => Decide(name),
                confirmText: "決定",
                cancelText: "やり直す");
        }

        private void Decide(string name)
        {
            if (!UserProfile.TrySave(name)) return;

            gameObject.SetActive(false);
            _onDecided?.Invoke();
        }

    }
}
