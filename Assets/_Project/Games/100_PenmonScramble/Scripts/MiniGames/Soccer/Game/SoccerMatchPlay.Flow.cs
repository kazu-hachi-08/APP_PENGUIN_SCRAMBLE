using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 結果とメニュー: 試合が終わったら少し見せてから、コインと記録を書いて保存し、結果（勝ち負け・点・得失点差・コインの内訳・
    /// 「もう一度」「スタジアムを選ぶ」「ロビーへ」）。試合中のメニュー（本編の StageMenu の見た目）の「試合をやり直す」「ミニゲームへ」もここで受ける
    /// </summary>
    public partial class SoccerMatchPlay
    {
        private const string WinMessage = "勝ち！";
        private const string LossMessage = "負け…";

        [SerializeField] private StageMenu _menu;
        [SerializeField] private MiniGameResultPanel _result;
        [Tooltip("試合が終わってから結果を出すまで（喜ぶ・悔しがるのを見せる）")]
        [SerializeField] private float _resultDelay = 1.6f;

        private bool _record;
        private bool _shootoutOnly;
        private float _resultLeft = -1f;

        private bool IsMenuOpen => _menu != null && _menu.IsOpen;

        private void BindFlow()
        {
            _result.Chosen += OnResultChosen;
            _menu.RestartRequested += StartMatch;
            _menu.TitleRequested += MiniGameLaunch.ReturnToHub;
        }

        /// <summary>試合を始めるたび: メニューを開けるようにして、結果を隠す</summary>
        private void BeginFlow()
        {
            _resultLeft = -1f;
            _result.SetOnline(false, false);
            _result.Hide();
            _menu.Bind(solo: true, canRestart: true);
            _menu.SetAvailable(true);
            _menu.SetCaption(Caption());
        }

        private string Caption()
        {
            if (_shootoutOnly)
            {
                return "PK戦の確認";
            }

            return _record ? $"スタジアム {_stadium.No}  {_stadium.Name}" : "試しのピッチ";
        }

        private void OnMatchEnd(SoccerEvent e)
        {
            _menu.SetAvailable(false);
            _resultLeft = _resultDelay;
            var won = e.Team == SoccerRules.Red;
            _hud.ShowBanner(won ? WinMessage : LossMessage, _resultDelay);
            _view.ShowWinner(e.Team);
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
            _hud.SetResult();
            GameSounds.Play(info.Outcome != SoccerOutcome.Loss ? GameSound.Clear : GameSound.Fail);
            _result.Show(SoccerResultText.Build(info));
        }

        /// <summary>
        /// 自分（赤）のチームの結果からコインを決め、記録に書いて保存する（CLAUDE.md §3.5: SaveData は変えず、ミニゲームの表の行に書くだけ）。
        /// ベスト ＝ 得失点差（PK戦で決まった試合は 0）、special ＝ 勝った回数。初めて勝ったかは、書く前の勝った回数が 0 かで決める。
        /// 開発用の試しのピッチ・PK戦の確認では書かない
        /// </summary>
        private SoccerResultInfo RecordResult()
        {
            var goals = _world.Score(SoccerRules.Red);
            var conceded = _world.Score(SoccerRules.Blue);
            var won = _match.Winner == SoccerRules.Red;
            var outcome = !won ? SoccerOutcome.Loss : _match.WonByShootout ? SoccerOutcome.ShootoutWin : SoccerOutcome.Win;
            var game = MiniGameTable.Get(MiniGameTable.SoccerId);
            var records = new MiniGameRecords(GameSave.Record.Data);
            var hadBest = records.TryGetBest(game.Id, _stadium.No, out var previousBest);
            var firstWin = (records.Find(game.Id, _stadium.No)?.special ?? 0) == 0;
            var reward = SoccerReward.Compute(goals, outcome, _record && firstWin);
            var shootout = _match.Shootout;
            var info = new SoccerResultInfo(goals, conceded, outcome, shootout, reward, _record, previousBest, hadBest);
            if (!_record)
            {
                return info;
            }

            var played = MiniGameReward.Apply(GameSave.Record.Data, game, _stadium.No, goals - conceded, won ? 1 : 0, reward.Total);
            GameSave.Save();
            return info.WithRecord(played.FirstPlay, played.NewBest && hadBest);
        }

        private void OnResultChosen(int card)
        {
            switch (card)
            {
                case MiniGameResultPanel.RetryCard:
                    StartMatch();
                    break;
                case MiniGameResultPanel.CoursesCard:
                    MiniGameLaunch.ReturnToHub();
                    break;
                case MiniGameResultPanel.LobbyCard:
                    MiniGameLaunch.ReturnToLobby();
                    break;
            }
        }
    }
}
