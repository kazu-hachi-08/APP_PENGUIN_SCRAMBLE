using PenmonScramble.Common.Input;
using PenmonScramble.Common.Online;
using PenmonScramble.Common.Profile;
using PenmonScramble.Common.Scene;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトル（ひとりで / 部屋を作る / コードで参加）からステージを始め、入力・HUD・結果をつなぐ。
    /// 計算はソロとホストが StageHost、ゲストは StageGuest で、どちらもスナップショットを StageView に渡して描く。
    /// ステージはホストがタイトルで選んだもの、連れていくのは各自の記録の編成。報酬は各自の記録に書く。
    /// BGM はタイトル・ステージ・ボスエリアで切り替え、結果で止めてジングルを鳴らす。
    /// 遊んでいる間はメニュー（StageMenu）を開ける。開いている間は自分の入力を「メニュー中」にする。
    /// スマホのボタンは遊んでいる間だけ出し、場に出ている子の技・待ち・ゲージを映す（TouchControlsView）
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に読む
    [DefaultExecutionOrder(100)]
    public partial class StageRunner : MonoBehaviour
    {
        [SerializeField] private StageView _view;
        [SerializeField] private StageHud _hud;
        [SerializeField] private OnlineSession _session;
        [SerializeField] private ModeSelectPanel _modeSelect;
        [SerializeField] private TitleMenu _titleMenu;
        [SerializeField] private StageMenu _menu;
        [SerializeField] private TouchControlsView _touch;

        private readonly LocalInputSampler _input = new LocalInputSampler();
        private StageLink _link;
        private StageHost _host;
        private StageGuest _guest;
        private int _localSeat;
        private PlayerState _lastPlayerState;
        private bool _leaving;
        private MusicTrack? _music;
        // 毎フレーム渡すので、メソッドから作る関数を使い回す
        private System.Func<int, Color> _seatColor;

        private StageSnapshot Latest => _host != null ? _host.Latest : _guest?.Latest;

        private void Start()
        {
            _seatColor = _view.SeatColor;
            _hud.RetryRequested += Retry;
            _hud.TitleRequested += ReturnToTitle;
            _menu.RestartRequested += Retry;
            _menu.TitleRequested += ReturnToTitle;
            // ゲストが1人抜けても、残りで遊び続ける
            _session.ContinueWhenMemberLeaves = true;
            _modeSelect.ShowMultiplayer(StartSolo, StartOnline, Balance.MaxPlayers);
            PlayMusic(MusicTrack.Title);
        }

        private void OnDestroy()
        {
            EndSlowMotion();
            _hud.RetryRequested -= Retry;
            _hud.TitleRequested -= ReturnToTitle;
            _menu.RestartRequested -= Retry;
            _menu.TitleRequested -= ReturnToTitle;
            _session.OnPeerDisconnected -= ReturnToTitle;
            _session.OnConnectionLost -= ReturnToTitle;
            _session.OnMemberLeft -= OnMemberLeft;
            _guest?.Dispose();
            _link?.Close();
        }

        private void StartSolo()
        {
            // 協力の名前は OnlineSession が配る。ひとりのときは自分の名前だけ
            SeatNames.UseSolo(UserProfile.Name);
            _menu.Bind(solo: true, canRestart: true);
            StartHost(1, null);
        }

        private void StartOnline(int seat, int playerCount)
        {
            // ホストは開始直後から送ってくるので、このコールバックの中ですぐに受け取れるようにする
            _link = new StageLink();
            _link.Open();
            // ゲストにとってはホストが抜けた、ホストにとっては自分の通信が止まった。どちらも続けられないのでタイトルへ
            _session.OnPeerDisconnected += ReturnToTitle;
            _session.OnConnectionLost += ReturnToTitle;

            // 「やり直し」は全員の挑戦を始め直すので、ホストだけが押せる
            _menu.Bind(solo: false, canRestart: seat == StageHost.LocalSeat);
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
            _input.InMenu = _menu.IsOpen;
            _input.Sample(input);
            _host?.Update(Time.deltaTime, _input);
            _guest?.Update(Time.deltaTime, _input);

            var latest = Latest;
            if (latest == null)
            {
                return;
            }

            UpdateHud(latest);
            _menu.SetAvailable(latest.Result == StageResult.Playing);
            // メニューを開いている間は、スマホのボタンを隠す（メニューの札と重ねず、押しても届かないことを見せる）
            _touch.SetShown(latest.Result == StageResult.Playing && !_menu.IsOpen);
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
                _touch.Show(_lastPlayerState, fallen: state == null);
            }

            _menu.SetStatus(snapshot, _lastPlayerState, fallen: state == null);

            _hud.SetProgress(snapshot);
            _hud.SetEdgeMarkers(snapshot, _localSeat, _seatColor);
            // 自分の3体が届くまでは中身が空なので出さない（ゲストは最初のスナップショットまで）
            // メニューを開いている間は隠す（メニューの帯と右の欄が、同じことをもっと大きく出す。協力は後ろが透けて重なるため）
            _hud.SetShown(_lastPlayerState != null && !_menu.IsOpen);
        }

        private void Retry()
        {
            _lastPlayerState = null;
            EndSlowMotion();
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
            EndSlowMotion();
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
