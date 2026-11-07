using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Profile;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 結果とメニュー: 試合が終わったら「ゲームセット」を少し見せてから、コインと記録を書いて保存し、結果（勝ち負け・点・得失点差・コインの内訳・
    /// 「もう一度」「球場を選ぶ」「ロビーへ」）。試合中のメニュー（本編の StageMenu の見た目）の「試合をやり直す」「ミニゲームへ」もここで受ける。
    /// 「もう一度」は先攻と後攻を入れ替えて始める（ひとりでは試合ごとに交互）。「試合をやり直す」は同じ先攻・後攻のまま。
    /// みんなでは各自が自分のチームの結果で記録を書き、結果の左に MVP（打点の一番多い人）を出す。「もう一度」は無く、
    /// 「球場を選ぶ」（ホストだけ）で全員が待ちの窓へ戻る
    /// </summary>
    public partial class BaseballMatch
    {
        private static readonly Color WinColor = new Color(1f, 0.82f, 0.2f);
        private static readonly Color LoseColor = new Color(0.62f, 0.82f, 1f);
        private static readonly Color GameSetBandColor = new Color(0.1f, 0.1f, 0.14f, 0.85f);

        [Header("結果とメニュー")]
        [SerializeField] private StageMenu _menu;
        [SerializeField] private MiniGameResultPanel _result;
        [Tooltip("ゲームセットから結果を出すまで（喜ぶ・悔しがるのを見せる）")]
        [SerializeField] private float _resultDelay = 2.2f;

        private bool _record;
        private float _resultLeft = -1f;
        private int[] _seatRbi = System.Array.Empty<int>();

        /// <summary>オンラインのホストが結果で「球場を選ぶ」を押した（全員で待ちの窓へ戻る。BaseballOnlinePlay が受ける）</summary>
        public event System.Action RoomRequested;

        private bool IsMenuOpen => _menu != null && _menu.IsOpen;

        private void BindResult()
        {
            _result.Chosen += OnResultChosen;
            // 「試合をやり直す」はひとりのときだけ出る
            _menu.RestartRequested += StartGame;
            _menu.TitleRequested += MiniGameLaunch.ReturnToHub;
        }

        /// <summary>試合を始めるたび: メニューを開けるようにして、結果を隠す</summary>
        private void BeginResultFlow()
        {
            _resultLeft = -1f;
            _seatRbi = new int[_plan.SeatCount];
            _result.SetOnline(IsOnline, !IsGuest);
            _result.Hide();
            _hud.SetResult(false);
            _hud.HideMvp();
            _menu.Bind(solo: !IsOnline, canRestart: !IsOnline, isHost: IsHost);
            _menu.SetAvailable(true);
            _menu.SetCaption(Caption());
        }

        private string Caption()
        {
            if (IsPractice)
            {
                return "試しの打席";
            }

            return _stadium != null ? $"球場 {_stadium.No}  {_stadium.Name}" : "試しの試合";
        }

        /// <summary>ゲームセット: 勝ち負けと点を帯つきで出し、少し見せてから結果を出す</summary>
        private void EnterOver()
        {
            _stage = BaseballStage.Over;
            _menu.SetAvailable(false);
            RefreshSpecialToggle();
            NotifyChanged();
            var mine = _game.Score(_localTeam);
            var theirs = _game.Score(1 - _localTeam);
            var outcome = BaseballReward.OutcomeOf(mine, theirs);
            var verdict = outcome == BaseballOutcome.Draw ? "引き分け" : outcome == BaseballOutcome.Win ? "勝ち！" : "負け…";
            var color = outcome == BaseballOutcome.Draw ? Color.white : outcome == BaseballOutcome.Win ? WinColor : LoseColor;
            var reason = _game.EndedByMercy ? "\n<size=40>点差で早く終わりました</size>" : string.Empty;
            _hud.ShowBanner($"ゲームセット\n<size=90>{verdict}</size>{reason}", color, _resultDelay, GameSetBandColor);
            _hud.ShowScore(_game);
            BaseballSoundCues.PlayGameOver(outcome == BaseballOutcome.Win);
            _resultLeft = _resultDelay;
        }

        private void TickResult(float dt)
        {
            if (_resultLeft < 0f)
            {
                return;
            }

            _resultLeft -= dt;
            if (_resultLeft <= 0f)
            {
                _resultLeft = -1f;
                ShowResult();
            }
        }

        private void ShowResult()
        {
            var info = RecordResult();
            _hud.SetResult(true);
            GameSounds.Play(info.Outcome == BaseballOutcome.Loss ? GameSound.Fail : GameSound.Clear);
            _result.Show(BaseballResultText.Build(info));
            ShowMvp();
        }

        /// <summary>打点を数える（ひとり・ホスト。人が打った打席だけ。押し出しの四球も）</summary>
        private void CountRbi(int seat, int runs)
        {
            if (seat >= 0 && seat < _seatRbi.Length)
            {
                _seatRbi[seat] += runs;
            }
        }

        /// <summary>みんなでのみ: 打点の一番多い人（並んだら全員）を、結果の左に出す。誰も打点が無ければ出さない</summary>
        private void ShowMvp()
        {
            if (!IsOnline)
            {
                return;
            }

            var seats = MiniGameMvp.Find(_seatRbi);
            if (seats.Count == 0)
            {
                return;
            }

            var names = new List<string>();
            foreach (var seat in seats)
            {
                names.Add(SeatNames.Get(seat));
            }

            _hud.ShowMvp(string.Join("・", names), _seatRbi[seats[0]], MiniGameSeats.ColorOf(seats[0]));
        }

        /// <summary>
        /// 自分のチームの結果からコインを決め、記録に書いて保存する（CLAUDE.md §3.5: SaveData は変えず、ミニゲームの表の行に書くだけ）。
        /// ベスト ＝ 得失点差、special ＝ 勝った回数（引き分けは数えない）。初めて勝ったかは、書く前の勝った回数が 0 かで決める。
        /// 開発用の試しの試合では書かない
        /// </summary>
        private BaseballResultInfo RecordResult()
        {
            var runs = _game.Score(_localTeam);
            var conceded = _game.Score(1 - _localTeam);
            var outcome = BaseballReward.OutcomeOf(runs, conceded);
            var game = MiniGameTable.Get(MiniGameTable.BaseballId);
            var courseNo = _stadium != null ? _stadium.No : 0;
            var records = new MiniGameRecords(GameSave.Record.Data);
            var hadBest = records.TryGetBest(game.Id, courseNo, out var previousBest);
            var firstWin = (records.Find(game.Id, courseNo)?.special ?? 0) == 0;
            var reward = BaseballReward.Compute(runs, outcome, _record && firstWin);
            var info = new BaseballResultInfo(runs, conceded, outcome, reward, _record, previousBest, hadBest);
            if (!_record)
            {
                return info;
            }

            var won = outcome == BaseballOutcome.Win;
            var played = MiniGameReward.Apply(GameSave.Record.Data, game, courseNo, runs - conceded, won ? 1 : 0, reward.Total);
            GameSave.Save();
            return info.WithRecord(played.FirstPlay, played.NewBest && hadBest);
        }

        private void OnResultChosen(int card)
        {
            switch (card)
            {
                case MiniGameResultPanel.RetryCard:
                    PlayAgain();
                    break;
                case MiniGameResultPanel.CoursesCard:
                    if (IsOnline)
                    {
                        RoomRequested?.Invoke();
                        break;
                    }

                    MiniGameLaunch.ReturnToHub();
                    break;
                case MiniGameResultPanel.LobbyCard:
                    MiniGameLaunch.ReturnToLobby();
                    break;
            }
        }

        /// <summary>もう一度: 試合なら先攻と後攻を入れ替えて始め直す</summary>
        private void PlayAgain()
        {
            if (IsOnline)
            {
                return;
            }

            _mode = _mode switch
            {
                BaseballPlayMode.MatchAway => BaseballPlayMode.MatchHome,
                BaseballPlayMode.MatchHome => BaseballPlayMode.MatchAway,
                _ => _mode,
            };
            StartGame();
        }
    }
}
