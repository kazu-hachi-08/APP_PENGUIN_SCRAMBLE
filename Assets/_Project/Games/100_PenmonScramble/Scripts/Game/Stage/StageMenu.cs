using System;
using PenmonScramble.Common.Input;
using PenmonScramble.Common.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中のメニュー（右上のボタン・Esc / P・PS5 コントローラーの OPTIONS）: つづける・遊び方・やり直し・タイトルへ・音量（§3.2）。
    /// ソロは全体を止める（Time.timeScale）。協力は止めずに重ねるだけで、開いている間は自分のペンギンが動かない
    /// （ホストがすべて計算するので、1人の都合で全員を止めないため。§8）
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後、StageRunner が自分の入力を読む前に開け閉めする
    [DefaultExecutionOrder(50)]
    public class StageMenu : MonoBehaviour
    {
        [SerializeField] private Button _openButton;
        [SerializeField] private PauseDialog _dialog;
        [SerializeField] private CommonDialog _confirm;
        [SerializeField] private HowToPlayScreen _howToPlay;

        private bool _solo;
        private bool _canRestart;
        private bool _available;

        public event Action RestartRequested;
        public event Action TitleRequested;

        /// <summary>メニュー・遊び方・確認のどれかを開いている間は true（自分の入力を止める）</summary>
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            _openButton.onClick.AddListener(Open);
            _dialog.Initialize(Close, ConfirmRestart, ConfirmTitle, OpenHowToPlay);
            _howToPlay.Closed += OnHowToPlayClosed;
            // タイトルの間は出さない（ステージが始まったら StageRunner が SetAvailable で出す）
            _openButton.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _howToPlay.Closed -= OnHowToPlayClosed;
            // 止めたまま画面を離れても、次の画面が止まらないように
            Time.timeScale = 1f;
        }

        /// <param name="solo">ソロは開いている間、全体を止める</param>
        /// <param name="canRestart">「やり直し」を出すのはソロとホストだけ（§8「もう一度はホストだけ」とそろえる）</param>
        public void Bind(bool solo, bool canRestart)
        {
            _solo = solo;
            _canRestart = canRestart;
            _dialog.SetRestartVisible(canRestart);
        }

        /// <summary>遊んでいる間だけ開ける。結果が出たら閉じる（結果の画面と重ねない）</summary>
        public void SetAvailable(bool available)
        {
            if (_available == available)
            {
                return;
            }

            _available = available;
            _openButton.gameObject.SetActive(available);
            if (!available)
            {
                Close();
            }
        }

        private void Update()
        {
            // 遊び方は自分で Esc を受けて閉じる。同じフレームでメニューまで閉じないよう、開いている間とその直後は見ない
            if (!_available || !InputManager.HasInstance || !InputManager.Instance.IsPauseDown
                || _howToPlay.gameObject.activeSelf || _howToPlay.ClosedFrame == Time.frameCount)
            {
                return;
            }

            if (_confirm.gameObject.activeSelf)
            {
                _confirm.Hide();
                ShowDialog();
            }
            else if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void Open()
        {
            IsOpen = true;
            if (_solo)
            {
                Time.timeScale = 0f;
            }

            ShowDialog();
        }

        private void ShowDialog()
        {
            ClearSelection();
            _dialog.Show();
        }

        private void Close()
        {
            // 先に下ろす（遊び方を閉じたときに、メニューを出し直さないように）
            IsOpen = false;
            _dialog.Hide();
            _confirm.Hide();
            _howToPlay.Close();
            Time.timeScale = 1f;
            ClearSelection();
        }

        /// <summary>
        /// 押したボタンが選ばれたままだと、Space（交代）や J（技1）が「決定」として届いて、
        /// 右上のボタンやメニューのボタンをもう一度押したことになってしまう
        /// </summary>
        private static void ClearSelection()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void OpenHowToPlay()
        {
            _howToPlay.Open();
        }

        private void OnHowToPlayClosed()
        {
            // Close から閉じたときは、メニューに戻さない
            if (IsOpen)
            {
                ShowDialog();
            }
        }

        private void ConfirmRestart()
        {
            var message = _solo ? "はじめからやり直します。\nいまの進み具合は残りません" : "全員ではじめからやり直します";
            _confirm.Show("やり直し", message, OnRestartConfirmed, ShowDialog, "やり直す", "もどる");
        }

        private void ConfirmTitle()
        {
            _confirm.Show("タイトルへ", TitleMessage(), OnTitleConfirmed, ShowDialog, "タイトルへ", "もどる");
        }

        /// <summary>協力では、抜けると何が起きるかを伝える（§8: ゲストは部屋から抜け、ホストが抜けると全員が戻る）</summary>
        private string TitleMessage()
        {
            if (_solo)
            {
                return "ステージをやめてタイトルに戻ります。\nいまの進み具合は残りません";
            }

            return _canRestart
                ? "部屋を閉じてタイトルに戻ります。\nほかの人も全員タイトルに戻ります"
                : "部屋から抜けてタイトルに戻ります";
        }

        private void OnRestartConfirmed()
        {
            Close();
            RestartRequested?.Invoke();
        }

        private void OnTitleConfirmed()
        {
            Close();
            TitleRequested?.Invoke();
        }
    }
}
