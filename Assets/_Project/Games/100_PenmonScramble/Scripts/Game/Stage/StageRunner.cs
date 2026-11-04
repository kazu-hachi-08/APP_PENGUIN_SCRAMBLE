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
    /// 「ひとりで遊ぶ」はロビーのカードで選んだストーリー、「みんなで遊ぶ」はホストが待ちの窓で選んだボスラッシュ（RushLobby）。
    /// 待ちの窓でミニゲームが選ばれていたら、ステージは始めずにミニゲームのシーンへ移る（RoomModeSelector）。
    /// 連れていくのは各自の記録の編成。報酬は各自の記録に書く。
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
        [SerializeField] private RushLobby _rushLobby;
        [SerializeField] private RoomModeSelector _roomModes;
        [SerializeField] private StageMenu _menu;
        [SerializeField] private TouchControlsView _touch;
        [SerializeField] private MusicClips _musicClips;

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
            _hud.ResultNextRequested += OnResultNext;
            _hud.TitleRequested += ReturnToTitle;
            _menu.RestartRequested += Retry;
            _menu.TitleRequested += ReturnToTitle;
            // ゲストが1人抜けても、残りで遊び続ける
            _session.ContinueWhenMemberLeaves = true;
            _modeSelect.ShowMultiplayer(StartSolo, StartOnline, Balance.MaxPlayers);
            PlayMusic(MusicTrack.Title);
            StartDevStageIfRequested();
        }

        /// <summary>
        /// エディタの Tools/PenmonScramble/Dev から開いたときは、タイトルを飛ばして開発用のステージをひとりで始める
        /// （「ひとりで遊ぶ」を押したのと同じ流れ）
        /// </summary>
        private void StartDevStageIfRequested()
        {
            if (!DevStageRequest.TryTake(out var stageNo, out var difficulty) || Stages.Get(stageNo) == null)
            {
                return;
            }

            _modeSelect.gameObject.SetActive(false);
            SeatNames.UseSolo(UserProfile.Name);
            _menu.Bind(solo: true, canRestart: true);
            StartHost(1, null, Stages.Get(stageNo), difficulty);
        }

        private void OnDestroy()
        {
            EndSlowMotion();
            _hud.ResultNextRequested -= OnResultNext;
            _hud.TitleRequested -= ReturnToTitle;
            _menu.RestartRequested -= Retry;
            _menu.TitleRequested -= ReturnToTitle;
            _session.OnPeerDisconnected -= ReturnToTitle;
            _session.OnConnectionLost -= ReturnToTitle;
            _session.OnMemberLeft -= OnMemberLeft;
            _session.OnReturnToRoom -= ReturnToTitle;
            _guest?.Dispose();
            _link?.Close();
        }

        private void StartSolo()
        {
            // 協力の名前は OnlineSession が配る。ひとりのときは自分の名前だけ
            SeatNames.UseSolo(UserProfile.Name);
            _menu.Bind(solo: true, canRestart: true);
            StartHost(1, null, SelectedStage, Difficulty.Normal);
        }

        private void StartOnline(int seat, int playerCount)
        {
            if (_roomModes.StartElsewhere(seat, playerCount))
            {
                // 移るまでの暗転の間は、空のステージではなく待ちの窓を見せておく
                _modeSelect.gameObject.SetActive(true);
                return;
            }

            // ホストは開始直後から送ってくるので、このコールバックの中ですぐに受け取れるようにする
            _link = new StageLink();
            _link.Open();
            // ゲストにとってはホストが抜けた、ホストにとっては自分の通信が止まった。どちらも続けられないのでタイトルへ
            _session.OnPeerDisconnected += ReturnToTitle;
            _session.OnConnectionLost += ReturnToTitle;

            // 「やり直し」は全員の挑戦を始め直すので、ホストだけが押せる。
            // ホストがひとりで始めたときは、ひとりで遊ぶのと同じくメニューで止める（待っている仲間がいないので）
            _menu.Bind(solo: playerCount == 1, canRestart: seat == StageHost.LocalSeat);
            if (seat == StageHost.LocalSeat)
            {
                _session.OnMemberLeft += OnMemberLeft;
                _link.InputReceived += OnRemoteInput;
                _link.PartyReceived += OnRemoteParty;
                var choice = _rushLobby.Choice;
                StartHost(playerCount, _link, RushTable.Get(choice.StageNo) ?? RushTable.Get(RushTable.FirstNo), choice.Difficulty);
                return;
            }

            SetLocalSeat(seat);
            // ホストが結果で「ステージを選ぶ」を押したら、部屋をつないだままシーンを読み直して待ちの窓へ（OnlineSession が部屋を引き継ぐ）
            _session.OnReturnToRoom += ReturnToTitle;
            _guest = new StageGuest(_view, _link);
            _link.SendParty(StageCodec.EncodeParty(GameSave.Record.PartyPenguins()));
        }

        /// <summary>ロビーのカードで選んだストーリーのステージ（ひとりで遊ぶ）</summary>
        private StageDef SelectedStage => StageTable.Get(_titleMenu.SelectedStageNo) ?? StageTable.Stage1;

        /// <summary>最初の挑戦は、ゲストの編成がそろってから StageHost が始める</summary>
        private void StartHost(int playerCount, StageLink link, StageDef stage, Difficulty difficulty)
        {
            SetLocalSeat(StageHost.LocalSeat);
            _host = new StageHost(_view, link, playerCount, stage, GameSave.Record.PartyStats(), difficulty);
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
                // 闘技場の連戦（ボスラッシュ）は、どの戦いもボスの曲
                PlayMusic(latest.IsBossArea || latest.ArenaRound > 0 ? MusicTrack.Boss : MusicTrack.Stage);
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
            GameMusic.Play(track, _musicClips);
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

        /// <summary>
        /// 協力のホストが結果で「ステージを選ぶ」: 全員で部屋の待ちの窓へ戻る。
        /// 画面はタイトルへ戻るのと同じくシーンを読み直して作り直し、部屋だけを OnlineSession が引き継ぐ
        /// </summary>
        private void ReturnToRoom()
        {
            if (_leaving)
            {
                return;
            }

            _session.ReturnToRoom();
            ReturnToTitle();
        }

        /// <summary>シーンを読み直すと OnlineSession が部屋から抜ける（ReturnToRoom の後なら、部屋を引き継ぐ）</summary>
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
