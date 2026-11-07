using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 結果とメニュー: 試合が終わったら「ゲームセット」を少し見せてから、コインと記録を書いて保存し、結果（勝ち負け・点・得失点差・コインの内訳・
    /// 「もう一度」「球場を選ぶ」「ロビーへ」）。試合中のメニュー（本編の StageMenu の見た目）の「試合をやり直す」「ミニゲームへ」もここで受ける。
    /// 「もう一度」は先攻と後攻を入れ替えて始める（ひとりでは試合ごとに交互）。「試合をやり直す」は同じ先攻・後攻のまま
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

        private bool IsMenuOpen => _menu != null && _menu.IsOpen;

        private void BindResult()
        {
            _result.Chosen += OnResultChosen;
            _menu.RestartRequested += StartGame;
            _menu.TitleRequested += MiniGameLaunch.ReturnToHub;
        }

        /// <summary>試合を始めるたび: メニューを開けるようにして、結果を隠す</summary>
        private void BeginResultFlow()
        {
            _resultLeft = -1f;
            _result.SetOnline(false, true);
            _result.Hide();
            _hud.SetResult(false);
            _menu.Bind(solo: true, canRestart: true);
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
            _stage = Stage.Over;
            _menu.SetAvailable(false);
            RefreshSpecialToggle();
            var mine = _game.Score(_humanTeam);
            var theirs = _game.Score(1 - _humanTeam);
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
        }

        /// <summary>
        /// 自分のチームの結果からコインを決め、記録に書いて保存する（CLAUDE.md §3.5: SaveData は変えず、ミニゲームの表の行に書くだけ）。
        /// ベスト ＝ 得失点差、special ＝ 勝った回数（引き分けは数えない）。初めて勝ったかは、書く前の勝った回数が 0 かで決める。
        /// 開発用の試しの試合では書かない
        /// </summary>
        private BaseballResultInfo RecordResult()
        {
            var runs = _game.Score(_humanTeam);
            var conceded = _game.Score(1 - _humanTeam);
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
