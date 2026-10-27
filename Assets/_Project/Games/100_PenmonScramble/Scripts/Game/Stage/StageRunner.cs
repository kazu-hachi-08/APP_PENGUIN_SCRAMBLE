using PenmonScramble.Common.Input;
using PenmonScramble.Common.Online;
using PenmonScramble.Common.Scene;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトル（ひとりで / 部屋を作る / コードで参加）からステージを始め、入力・HUD・結果をつなぐ。
    /// 計算はソロとホストが StageHost、ゲストは StageGuest で、どちらもスナップショットを StageView に渡して描く
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に読む
    [DefaultExecutionOrder(100)]
    public class StageRunner : MonoBehaviour
    {
        // 結果が出た直後の連打で、すぐやり直しにならないようにする
        private const float RetryInputDelaySeconds = 1f;
        private const int RetryAction = 0;

        [SerializeField] private StageView _view;
        [SerializeField] private StageHud _hud;
        [SerializeField] private OnlineSession _session;
        [SerializeField] private ModeSelectPanel _modeSelect;

        private readonly LocalInputSampler _input = new LocalInputSampler();
        private StageLink _link;
        private StageHost _host;
        private StageGuest _guest;
        private int _localSeat;
        private PlayerState _lastPlayerState;
        private float _resultShownAt = -1f;
        private bool _leaving;

        private StageSnapshot Latest => _host != null ? _host.Latest : _guest?.Latest;

        private void Start()
        {
            _hud.RetryRequested += Retry;
            _hud.TitleRequested += ReturnToTitle;
            // ゲストが1人抜けても、残りで遊び続ける（§8）
            _session.ContinueWhenMemberLeaves = true;
            _modeSelect.ShowMultiplayer(StartSolo, StartOnline, Balance.MaxPlayers);
        }

        private void OnDestroy()
        {
            _hud.RetryRequested -= Retry;
            _hud.TitleRequested -= ReturnToTitle;
            _session.OnPeerDisconnected -= ReturnToTitle;
            _session.OnConnectionLost -= ReturnToTitle;
            _session.OnMemberLeft -= OnMemberLeft;
            _guest?.Dispose();
            _link?.Close();
        }

        private void StartSolo()
        {
            StartHost(1, null);
        }

        private void StartOnline(int seat, int playerCount)
        {
            // ホストは開始直後から送ってくるので、このコールバックの中ですぐに受け取れるようにする
            _link = new StageLink();
            _link.Open();
            // ゲストにとってはホストが抜けた、ホストにとっては自分の通信が止まった。どちらも続けられないのでタイトルへ（§8）
            _session.OnPeerDisconnected += ReturnToTitle;
            _session.OnConnectionLost += ReturnToTitle;

            if (seat == StageHost.LocalSeat)
            {
                _session.OnMemberLeft += OnMemberLeft;
                _link.InputReceived += OnRemoteInput;
                StartHost(playerCount, _link);
                return;
            }

            SetLocalSeat(seat);
            _guest = new StageGuest(_view, _link);
        }

        private void StartHost(int playerCount, StageLink link)
        {
            SetLocalSeat(StageHost.LocalSeat);
            _host = new StageHost(_view, link, playerCount);
            _host.StartRound();
        }

        private void SetLocalSeat(int seat)
        {
            _localSeat = seat;
            _hud.SetPlayer(StageView.SeatLabel(seat), _view.SeatColor(seat));
        }

        private void OnRemoteInput(ulong clientId, byte[] data)
        {
            if (_session.TryGetSeat(clientId, out var seat) && StageCodec.TryDecodeInput(data, out var input))
            {
                _host.ReceiveInput(seat, input);
            }
        }

        private void OnMemberLeft(int seat)
        {
            _host.RemovePlayer(seat);
        }

        private void Update()
        {
            // タイトルの間は何もしない
            if (_host == null && _guest == null)
            {
                return;
            }

            var input = InputManager.Instance;
            _input.Sample(input);
            _host?.Update(Time.deltaTime, _input);
            _guest?.Update(Time.deltaTime, _input);

            var latest = Latest;
            if (latest == null)
            {
                return;
            }

            UpdateHud(latest);
            UpdateResult(latest, input);
        }

        private void UpdateHud(StageSnapshot snapshot)
        {
            // 3体とも倒れた後はスナップショットにいないので、最後に見た3体を HP 0 で出す
            var state = snapshot.FindPlayerState(_localSeat);
            if (state != null)
            {
                _lastPlayerState = state;
            }

            if (_lastPlayerState != null)
            {
                _hud.SetParty(_lastPlayerState, fallen: state == null);
            }

            _hud.SetEnemiesLeft(snapshot.CountAlive(Team.Enemy));
        }

        private void UpdateResult(StageSnapshot snapshot, InputManager input)
        {
            if (snapshot.Result == StageResult.Playing)
            {
                // ゲストは、ホストが「もう一度」を押したら新しい挑戦のスナップショットが届いてここに来る
                if (_resultShownAt >= 0f)
                {
                    _resultShownAt = -1f;
                    _hud.HideResult();
                }

                return;
            }

            if (_resultShownAt < 0f)
            {
                _resultShownAt = Time.time;
                _hud.ShowResult(snapshot.Result == StageResult.Cleared, canRetry: _host != null);
                return;
            }

            if (Time.time - _resultShownAt >= RetryInputDelaySeconds && input.IsActionDown(RetryAction))
            {
                Retry();
            }
        }

        private void Retry()
        {
            _lastPlayerState = null;
            _host?.StartRound();
        }

        /// <summary>シーンを読み直すと OnlineSession が部屋から抜ける</summary>
        private void ReturnToTitle()
        {
            if (_leaving)
            {
                return;
            }

            _leaving = true;
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadTitleScene();
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Title);
            }
        }
    }
}
