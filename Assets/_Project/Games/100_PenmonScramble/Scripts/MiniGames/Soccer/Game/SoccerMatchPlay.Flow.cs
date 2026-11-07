using System.Collections.Generic;
using PenmonScramble.Common.Profile;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 結果とメニュー: 試合が終わったら少し見せてから、コインと記録を書いて保存し、結果（勝ち負け・点・得失点差・コインの内訳・
    /// 「もう一度」「スタジアムを選ぶ」「ロビーへ」）。試合中のメニュー（本編の StageMenu の見た目）の「試合をやり直す」「ミニゲームへ」もここで受ける。
    /// みんなででは、勝ち負けと報酬は自分のチームから見て、各自の端末で記録に書く。点を一番入れた人を MVP として出す。
    /// 「もう一度」は出さず、「スタジアムを選ぶ」（ホストだけ）で全員が待ちの窓へ戻る
    /// </summary>
    public partial class SoccerMatchPlay
    {
        private const string WinMessage = "勝ち！";
        private const string LossMessage = "負け…";

        [SerializeField] private StageMenu _menu;
        [SerializeField] private MiniGameResultPanel _result;
        [Tooltip("試合が終わってから結果を出すまで（喜ぶ・悔しがるのを見せる）")]
        [SerializeField] private float _resultDelay = 1.6f;
        [SerializeField] private MusicClips _musicClips;

        private bool _record;
        private bool _shootoutOnly;
        private float _resultLeft = -1f;

        private bool IsMenuOpen => _menu != null && _menu.IsOpen;

        /// <summary>
        /// サッカーの曲。本編から来たときの曲を引き継がず、ここで差し替える（AudioManager はシーンをまたいで残る）。
        /// 同じ曲が流れている間に呼んでも頭からにはならない（AudioManager が見分ける）
        /// </summary>
        public void PlayMusic()
        {
            GameMusic.Play(MusicTrack.Soccer, _musicClips);
        }

        private void BindFlow()
        {
            _result.Chosen += OnResultChosen;
            // 「試合をやり直す」はひとりのときだけ出る
            _menu.RestartRequested += StartSolo;
            _menu.TitleRequested += MiniGameLaunch.ReturnToHub;
        }

        /// <summary>試合を始めるたび: メニューを開けるようにして、結果を隠す</summary>
        private void BeginFlow()
        {
            // 結果で止めた曲を「もう一度」・「試合をやり直す」で流し直す
            PlayMusic();
            _resultLeft = -1f;
            _result.SetOnline(_mode.IsOnline, _mode.IsHost);
            _result.Hide();
            _menu.Bind(solo: !_mode.IsOnline, canRestart: !_mode.IsOnline, isHost: _mode.IsHost);
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
            var won = e.Team == OwnTeam;
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
            // 本編・ゴルフと同じく、結果の音が曲に埋もれないよう止める
            GameMusic.Stop();
            GameSounds.Play(info.Outcome != SoccerOutcome.Loss ? GameSound.Clear : GameSound.Fail);
            _result.Show(SoccerResultText.Build(info));
            ShowMvp();
        }

        /// <summary>みんなでのみ: 点を一番入れた人（並んだら全員）を、結果の左に出す。誰も入れていなければ出さない</summary>
        private void ShowMvp()
        {
            if (!_mode.IsOnline)
            {
                return;
            }

            var seats = SoccerMvp.Find(_seatGoals);
            if (seats.Count == 0)
            {
                return;
            }

            var names = new List<string>();
            foreach (var seat in seats)
            {
                names.Add(SeatNames.Get(seat));
            }

            _hud.ShowMvp(string.Join("・", names), _seatGoals[seats[0]], MiniGameSeats.ColorOf(seats[0]));
        }

        /// <summary>
        /// 自分のチーム（ひとりでは赤）の結果からコインを決め、記録に書いて保存する（CLAUDE.md §3.5: SaveData は変えず、ミニゲームの表の行に書くだけ）。
        /// ベスト ＝ 得失点差（PK戦で決まった試合は 0）、special ＝ 勝った回数。初めて勝ったかは、書く前の勝った回数が 0 かで決める。
        /// 開発用の試しのピッチ・PK戦の確認では書かない
        /// </summary>
        private SoccerResultInfo RecordResult()
        {
            var team = OwnTeam;
            var goals = _world.Score(team);
            var conceded = _world.Score(SoccerRules.Opponent(team));
            var won = _match.Winner == team;
            var outcome = !won ? SoccerOutcome.Loss : _match.WonByShootout ? SoccerOutcome.ShootoutWin : SoccerOutcome.Win;
            var game = MiniGameTable.Get(MiniGameTable.SoccerId);
            var records = new MiniGameRecords(GameSave.Record.Data);
            var hadBest = records.TryGetBest(game.Id, _stadium.No, out var previousBest);
            var firstWin = (records.Find(game.Id, _stadium.No)?.special ?? 0) == 0;
            var reward = SoccerReward.Compute(goals, outcome, _record && firstWin);
            var shootout = _match.Shootout;
            var info = new SoccerResultInfo(team, goals, conceded, outcome, shootout, reward, _record, previousBest, hadBest);
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
                    StartSolo();
                    break;
                case MiniGameResultPanel.CoursesCard:
                    if (_mode.IsOnline)
                    {
                        RequestRoom();
                        break;
                    }

                    MiniGameLaunch.ReturnToHub();
                    break;
                case MiniGameResultPanel.LobbyCard:
                    MiniGameLaunch.ReturnToLobby();
                    break;
            }
        }
    }
}
