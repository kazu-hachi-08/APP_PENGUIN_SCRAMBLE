using System;
using PenmonScramble.Common.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 遊び方。タイトルとステージ中のメニューから開く。
    /// 「操作」タブはキーボード・コントローラー・スマホを切り替えて見せる（押したキーが光るので、その場で試せる）。
    /// 「ルール」タブはページをめくる（HowToPlayScreen.Rules.cs）
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に Esc を見る
    [DefaultExecutionOrder(50)]
    public partial class HowToPlayScreen : MonoBehaviour
    {
        private const int ControlsTab = 0;
        private const int RulesTab = 1;

        // 操作の見せ方。_deviceButtons / _devicePanels の並びと同じ
        private const int KeyboardDevice = 0;
        private const int GamepadDevice = 1;
        private const int TouchDevice = 2;

        [SerializeField] private Button _backButton;
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private GameObject[] _tabPages;
        [SerializeField] private Button[] _deviceButtons;
        [SerializeField] private GameObject[] _devicePanels;
        [SerializeField] private Color _selectedColor = new Color(1f, 0.75f, 0.30f);
        [SerializeField] private Color _idleColor = new Color(0.30f, 0.38f, 0.55f);

        private int _tab;

        public event Action Closed;

        /// <summary>閉じたフレーム。同じ Esc でステージのメニューまで閉じないよう、StageMenu が見る</summary>
        public int ClosedFrame { get; private set; } = -1;

        private void Awake()
        {
            _backButton.onClick.AddListener(Close);
            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var tab = i;
                _tabButtons[i].onClick.AddListener(() => ShowTab(tab));
            }

            for (var i = 0; i < _deviceButtons.Length; i++)
            {
                var device = i;
                _deviceButtons[i].onClick.AddListener(() => ShowDevice(device));
            }

            AwakeRules();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            ShowTab(ControlsTab);
            ShowDevice(DefaultDevice());
            ShowPage(0);
        }

        public void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(false);
            ClosedFrame = Time.frameCount;
            Closed?.Invoke();
        }

        private void Update()
        {
            if (InputManager.HasInstance && InputManager.Instance.IsPauseDown)
            {
                Close();
                return;
            }

            if (_tab == RulesTab)
            {
                UpdatePageKeys();
            }
        }

        /// <summary>いま遊んでいる端末の操作を最初に見せる。スマホならタッチ、コントローラーがつながっていればコントローラー</summary>
        private static int DefaultDevice()
        {
            if (Application.isMobilePlatform)
            {
                return TouchDevice;
            }

            return Gamepad.current != null ? GamepadDevice : KeyboardDevice;
        }

        private void ShowTab(int tab)
        {
            _tab = tab;
            Select(_tabButtons, _tabPages, tab);
        }

        private void ShowDevice(int device)
        {
            Select(_deviceButtons, _devicePanels, device);
        }

        /// <summary>選んだボタンを明るい色にして、対になる中身だけを出す</summary>
        private void Select(Button[] buttons, GameObject[] contents, int index)
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                var selected = i == index;
                buttons[i].targetGraphic.color = selected ? _selectedColor : _idleColor;
                contents[i].SetActive(selected);
            }
        }
    }
}
