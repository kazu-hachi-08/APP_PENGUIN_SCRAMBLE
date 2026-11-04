using System;
using PenmonScramble.Common.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中のメニュー（右上の ‖・Esc / P・PS5 コントローラーの OPTIONS）。左に斜めの札（つづける・やり直す・遊び方・設定・タイトルへ）、
    /// 上の帯にステージ名とエリアのつぶ、右に編成の3体。札は StageMenuCard、右の欄は StageMenuParty、確認は StageMenuConfirm、音量は StageMenuVolume。
    /// ソロは全体を止める（Time.timeScale）。協力は止めずに重ねるだけで、開いている間は自分のペンギンが動かない
    /// （ホストがすべて計算するので、1人の都合で全員を止めないため。）
    /// 札を選ぶ・押すのは StageMenu.Cards.cs、上の帯と右の欄は StageMenu.Status.cs
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後、StageRunner が自分の入力を読む前に開け閉めする
    [DefaultExecutionOrder(50)]
    public partial class StageMenu : MonoBehaviour
    {
        [SerializeField] private Button _openButton;
        [Tooltip("メニュー全体（後ろ・帯・札・右の欄）。閉じている間は隠し、閉じる動きの間は薄くしていく")]
        [SerializeField] private CanvasGroup _root;
        [Tooltip("後ろの幕より手前の中身。後ろを撮る間と、遊び方を開いている間は隠す")]
        [SerializeField] private GameObject _content;
        [SerializeField] private StageMenuBackdrop _backdrop;
        [SerializeField] private StageMenuConfirm _confirm;
        [SerializeField] private StageMenuVolume _volume;
        [Tooltip("無ければ「遊び方」の札を出さない（遊び方のページがまだ無いミニゲーム）")]
        [SerializeField] private HowToPlayScreen _howToPlay;
        [Tooltip("札が抜けるのが早く終わっても、後ろの幕はこの秒をかけて消す")]
        [SerializeField] private float _minCloseSeconds = 0.15f;

        private readonly MenuNavigator _navigator = new MenuNavigator();
        private bool _solo;
        private bool _canRestart;
        private bool _isHost;
        private bool _available;
        // 中身を出したか（ソロは後ろを撮り終わるまで出さない）
        private bool _shown;
        // 閉じる動きの残り。0 より大きい間は閉じている途中
        private float _closing;
        private float _closeSeconds;

        public event Action RestartRequested;
        public event Action TitleRequested;

        /// <summary>メニュー・遊び方・確認のどれかを開いている間（閉じる動きの間も）は true（自分の入力を止める）</summary>
        public bool IsOpen { get; private set; }

        private bool IsClosing => _closing > 0f;

        private void Awake()
        {
            _openButton.onClick.AddListener(Open);
            if (_howToPlay != null)
            {
                _howToPlay.Closed += OnHowToPlayClosed;
            }

            AwakeCards();
            // タイトルの間は出さない（ステージが始まったら StageRunner が SetAvailable で出す）
            _openButton.gameObject.SetActive(false);
            _root.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_howToPlay != null)
            {
                _howToPlay.Closed -= OnHowToPlayClosed;
            }

            // 止めたまま画面を離れても、次の画面が止まらないように
            Time.timeScale = 1f;
        }

        /// <param name="solo">ソロは開いている間、全体を止める</param>
        /// <param name="canRestart">「やり直す」を出すのはソロとホストだけ</param>
        /// <param name="isHost">みんなでのホストか（抜けるときの確認の言葉を変える）。省略すると canRestart と同じ（本編のステージはホストだけがやり直せる）</param>
        public void Bind(bool solo, bool canRestart, bool? isHost = null)
        {
            _solo = solo;
            _canRestart = canRestart;
            _isHost = isHost ?? canRestart;
            BindCards();
            BindStatus();
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
                CloseNow();
            }
        }

        private void Update()
        {
            _navigator.Update();
            if (IsClosing)
            {
                UpdateClosing();
                return;
            }

            // 遊び方は自分で Esc を受けて閉じる。同じフレームでメニューまで閉じないよう、開いている間とその直後は見ない
            if (!_available || !InputManager.HasInstance || IsHowToPlayBusy())
            {
                return;
            }

            var pause = InputManager.Instance.IsPauseDown;
            if (!IsOpen)
            {
                if (pause)
                {
                    Open();
                }

                return;
            }

            if (_shown)
            {
                UpdateFocus(pause);
            }
        }

        private void LateUpdate()
        {
            if (_shown)
            {
                ClearSelection();
            }
        }

        private void Open()
        {
            if (IsOpen || !_available)
            {
                return;
            }

            IsOpen = true;
            _shown = false;
            // メニューの帯の下に透けて見えないよう、開いている間は隠す
            _openButton.gameObject.SetActive(false);
            if (_solo)
            {
                Time.timeScale = 0f;
            }

            _root.gameObject.SetActive(true);
            _root.alpha = 1f;
            _content.SetActive(false);
            // ソロは止まった画面を撮ってから中身を出す（メニューごと撮らないように）
            _backdrop.Show(_solo, ShowContent);
        }

        private void ShowContent()
        {
            _shown = true;
            _navigator.Reset();
            _content.SetActive(true);
            ShowStatusNow();
            PlayCardsIn();
            ClearSelection();
        }

        /// <summary>札が左へ抜けていく。ソロは抜け終わってから動き出す</summary>
        private void BeginClose()
        {
            _confirm.Hide();
            _volume.Close();
            _closeSeconds = Mathf.Max(PlayCardsOut(), _minCloseSeconds);
            _closing = _closeSeconds;
        }

        private void UpdateClosing()
        {
            _closing -= Time.unscaledDeltaTime;
            // 札が抜けるのに合わせて、帯・右の欄・後ろの幕も薄くしていく（ソロはぼけた絵が止まった画面へ戻っていく）
            _root.alpha = Mathf.Clamp01(_closing / _closeSeconds);
            if (_closing <= 0f)
            {
                CloseNow();
            }
        }

        /// <summary>動きを待たずにすぐ閉じる（やり直し・タイトルへ・結果が出たとき）</summary>
        private void CloseNow()
        {
            // 先に下ろす（遊び方を閉じたときに、メニューを出し直さないように）
            IsOpen = false;
            _shown = false;
            _closing = 0f;
            _confirm.Hide();
            _volume.Close();
            if (_howToPlay != null)
            {
                _howToPlay.Close();
            }

            _backdrop.Hide();
            _root.gameObject.SetActive(false);
            _openButton.gameObject.SetActive(_available);
            Time.timeScale = 1f;
            ClearSelection();
        }

        /// <summary>
        /// 押したボタン・つまみが選ばれたままだと、Space（交代）や Enter が「決定」として届いて、もう一度押したことになり、
        /// 方向キーでつまみが勝手に動く。札とつまみの選びはこのメニューが自分で持つので、UI の選択は使わない
        /// </summary>
        private static void ClearSelection()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private bool IsHowToPlayBusy() => _howToPlay != null && (_howToPlay.gameObject.activeSelf || _howToPlay.ClosedFrame == Time.frameCount);

        private void OpenHowToPlay()
        {
            if (_howToPlay == null)
            {
                return;
            }

            // 遊び方は画面いっぱいに出るので、後ろの幕だけ残して中身を隠す
            _content.SetActive(false);
            _shown = false;
            _howToPlay.Open();
        }

        private void OnHowToPlayClosed()
        {
            // CloseNow から閉じたときは、メニューに戻さない
            if (IsOpen && !IsClosing)
            {
                ShowContent();
            }
        }
    }
}
