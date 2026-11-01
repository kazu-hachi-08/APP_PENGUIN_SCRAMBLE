using System;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中のメニューの「やり直す」「タイトルへ」の確認。メニューと同じ斜めの札の見た目の小さな窓で、
    /// 札は「もどる」と「（やること）」の2枚。最初は「もどる」を選んでおく（うっかり決定を押しても何も起きないように）。
    /// 左右で選び、決定で押す。Esc / OPTIONS は StageMenu が「もどる」として渡す
    /// </summary>
    public class StageMenuConfirm : MonoBehaviour
    {
        private const int CancelIndex = 0;
        private const int OkIndex = 1;

        [SerializeField] private Text _heading;
        [SerializeField] private Text _message;
        [Tooltip("左から「もどる」「（やること）」")]
        [SerializeField] private StageMenuCard[] _cards;
        [SerializeField] private float _cardStagger = 0.05f;

        private Action _onOk;
        private Action _onCancel;
        private int _selected;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            foreach (var card in _cards)
            {
                card.Hovered += OnHovered;
                card.Pressed += OnPressed;
            }
        }

        public void Show(string heading, string message, string okLabel, Action onOk, Action onCancel)
        {
            _heading.text = heading;
            _message.text = message;
            _cards[OkIndex].SetLabel(okLabel);
            _onOk = onOk;
            _onCancel = onCancel;
            gameObject.SetActive(true);
            Select(CancelIndex);
            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].SnapSelection();
                _cards[i].PlayIn(i * _cardStagger);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Move(Vector2Int step)
        {
            if (step.x != 0)
            {
                Select(Mathf.Clamp(_selected + step.x, 0, _cards.Length - 1));
                GameSounds.Play(GameSound.Click);
            }
        }

        public void Submit() => Press(_selected);

        public void Cancel() => Press(CancelIndex);

        private void OnHovered(StageMenuCard card) => Select(Array.IndexOf(_cards, card));

        private void OnPressed(StageMenuCard card) => Press(Array.IndexOf(_cards, card));

        private void Select(int index)
        {
            if (index < 0)
            {
                return;
            }

            _selected = index;
            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].SetSelected(i == index);
            }
        }

        /// <summary>先に閉じてから呼ぶ（呼んだ先でメニューごと閉じても、この窓が残らないように）</summary>
        private void Press(int index)
        {
            if (!IsOpen || index < 0)
            {
                return;
            }

            GameSounds.Play(GameSound.Click);
            Hide();
            var callback = index == OkIndex ? _onOk : _onCancel;
            callback?.Invoke();
        }
    }
}
