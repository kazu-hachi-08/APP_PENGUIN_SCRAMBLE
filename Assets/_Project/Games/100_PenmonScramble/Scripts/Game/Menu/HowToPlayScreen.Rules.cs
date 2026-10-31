using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 遊び方の「ルール」タブ: 1ページに1つのことを、短い文と顔の絵で見せる（HowToPlayPages）。
    /// ◀ ▶ のボタンのほか、キーボードの ← → / A D とコントローラーの方向キーでもめくれる
    /// </summary>
    public partial class HowToPlayScreen
    {
        [SerializeField] private Text _ruleHeading;
        [SerializeField] private Text _ruleBody;
        [SerializeField] private Image _ruleFace;
        [SerializeField] private Text _pageText;
        [SerializeField] private Button _prevPageButton;
        [SerializeField] private Button _nextPageButton;

        private int _page;

        private void AwakeRules()
        {
            _prevPageButton.onClick.AddListener(() => ShowPage(_page - 1));
            _nextPageButton.onClick.AddListener(() => ShowPage(_page + 1));
        }

        private void ShowPage(int page)
        {
            var pages = HowToPlayPages.All;
            _page = Mathf.Clamp(page, 0, pages.Count - 1);
            var current = pages[_page];
            _ruleHeading.text = current.Heading;
            _ruleBody.text = current.Body;
            CreatureArt.ShowFace(_ruleFace, current.SpeciesNo);
            _pageText.text = $"{_page + 1} / {pages.Count}";
            _prevPageButton.interactable = _page > 0;
            _nextPageButton.interactable = _page < pages.Count - 1;
        }

        private void UpdatePageKeys()
        {
            var step = PageStep(Keyboard.current, Gamepad.current);
            if (step != 0)
            {
                ShowPage(_page + step);
            }
        }

        private static int PageStep(Keyboard keyboard, Gamepad gamepad)
        {
            var prev = (keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.dpad.left.wasPressedThisFrame);
            var next = (keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.dpad.right.wasPressedThisFrame);
            return (next ? 1 : 0) - (prev ? 1 : 0);
        }
    }
}
