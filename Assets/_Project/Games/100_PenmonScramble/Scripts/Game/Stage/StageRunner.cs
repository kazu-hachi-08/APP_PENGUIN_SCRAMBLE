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
    /// 計算はソロとホストが StageHost、ゲストは StageGuest で、どちらもスナップショットを StageView に渡して描く。
    /// ステージはホストがタイトルで選んだもの、連れていくのは各自の記録の編成（§8）。報酬は各自の記録に書く。
    /// BGM はタイトル・ステージ・ボスエリアで切り替え、結果で止めてジングルを鳴らす
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
        [SerializeField] private TitleMenu _titleMenu;

        private readonly LocalInputSampler _input = new LocalInputSampler();
        private StageLink _link;
        private StageHost _host;
        private StageGuest _guest;
        private int _localSeat;
        private PlayerState _lastPlayerState;
        private float _resultShownAt = -1f;
        private bool _leaving;
        private MusicTrack? _music;

        private StageSnapshot Latest => _host != null ? _host.Latest : _guest?.Latest;

        private void Start()
        {
            _hud.RetryRequested += Retry;
            _hud.TitleRequested += ReturnToTitle;
            // ゲストが1人抜けても、残りで遊び続ける（§8）
            _session.ContinueWhenMemberLeaves = true;
            _modeSelect.ShowMultiplayer(StartSolo, StartOnline, Balance.MaxPlayers);
            PlayMusic(MusicTrack.Title);
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
                _link.PartyReceived += OnRemoteParty;
                StartHost(playerCount, _link);
                return;
            }

            SetLocalSeat(seat);
            _guest = new StageGuest(_view, _link);
            _link.SendParty(StageCodec.EncodeParty(GameSave.Record.PartyPenguins()));
        }

        /// <summary>最初の挑戦は、ゲストの編成がそろってから StageHost が始める</summary>
        private void StartHost(int playerCount, StageLink link)
        {
            SetLocalSeat(StageHost.LocalSeat);
            var stage = StageTable.Get(_titleMenu.SelectedStageNo) ?? StageTable.Stage1;
            _host = new StageHost(_view, link, playerCount, stage, GameSave.Record.PartyStats());
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

        private void OnRemoteParty(ulong clientId, byte[] data)
        {
            if (_session.TryGetSeat(clientId, out var seat) && StageCodec.TryDecodeParty(data, out var party))
            {
                _host.ReceiveParty(seat, party);
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
            if (latest.Result == StageResult.Playing)
            {
                PlayMusic(latest.IsBossArea ? MusicTrack.Boss : MusicTrack.Stage);
            }
        }

        /// <summary>曲が変わるときだけ流す（毎フレーム呼ぶので、同じ曲を登録し直さないように）</summary>
        private void PlayMusic(MusicTrack track)
        {
            if (_music == track)
            {
                return;
            }

            _music = track;
            GameMusic.Play(track);
        }

        private void UpdateHud(StageSnapshot snapshot)
        {
            // 抜けた後などでスナップショットにいなければ、最後に見た3体を HP 0 で出す
            var state = snapshot.FindPlayerState(_localSeat);
            if (state != null)
            {
                _lastPlayerState = state;
            }

            if (_lastPlayerState != null)
            {
                _hud.SetParty(_lastPlayerState, fallen: state == null);
            }

            _hud.SetProgress(snapshot);
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
                var cleared = snapshot.Result == StageResult.Cleared;
                var coins = RecordResult(snapshot, cleared);
                _music = null;
                GameMusic.Stop();
                GameSounds.Play(cleared ? GameSound.Clear : GameSound.Fail);
                _hud.ShowResult(cleared, canRetry: _host != null, snapshot.EnemiesDefeated, coins);
                return;
            }

            if (Time.time - _resultShownAt >= RetryInputDelaySeconds && input.IsActionDown(RetryAction))
            {
                Retry();
            }
        }

        /// <summary>結果を自分の記録に書いて保存する。協力ではホストのクリアが全員の記録に付く（§6.3）</summary>
        private static int RecordResult(StageSnapshot snapshot, bool cleared)
        {
            var coins = GameSave.Record.RecordStage(snapshot.StageNo, cleared, snapshot.EnemiesDefeated);
            GameSave.Save();
            return coins;
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
