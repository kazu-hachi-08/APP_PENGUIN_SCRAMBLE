using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ホールの間と結果とメニュー: 全員がホールを終えたら少し見せてからスコアカード →「つぎのホールへ」。
    /// 最後のホールの後は、コインと記録を書いて保存し、結果（スコアカード ＋ 内訳 ＋「もう一度」「コースを選ぶ」「ロビーへ」）。
    /// ステージ中のメニュー（本編の StageMenu の見た目）の「ホールをやり直す」「ミニゲームへ」もここで受ける。
    /// オンラインは、次のホールへ進めるのも結果で「コースを選ぶ」（全員で待ちの窓へ）もホストだけ。ゲストはホストを待つ。
    /// メニューは止めず、「ホールをやり直す」は出さない（ほかの人が打っている途中なので）
    /// </summary>
    public partial class GolfRoundPlay
    {
        // 全員が終わってからスコアカードを出すまで（最後の知らせ・喜ぶのを見せる）
        private const float HoleOverSeconds = 1.4f;

        [SerializeField] private GolfScorecard _scorecard;
        [SerializeField] private GolfResultPanel _result;
        [SerializeField] private StageMenu _menu;
        [SerializeField] private MusicClips _musicClips;

        private bool _record;
        private float _holeOver;

        private bool IsMenuOpen => _menu != null && _menu.IsOpen;

        /// <summary>ゴルフの曲。本編から来たときの曲を引き継がず、ここで差し替える（AudioManager はシーンをまたいで残る）</summary>
        public void PlayMusic()
        {
            GameMusic.Play(MusicTrack.Golf, _musicClips);
        }

        private void BindFlow()
        {
            _result.Chosen += OnResultChosen;
            _menu.RestartRequested += () => _round.RestartHole();
            // オンラインでも、ミニゲーム画面へ戻ると部屋から抜ける（ホストが抜けると全員の試合が終わる）
            _menu.TitleRequested += MiniGameLaunch.ReturnToHub;
            _menu.Bind(solo: !_mode.IsOnline, canRestart: !_mode.IsOnline, isHost: _mode.Role == GolfRole.Host);
            _result.SetOnline(_mode.IsOnline, _mode.Role == GolfRole.Host);
            HidePanels();
        }

        private void HidePanels()
        {
            _scorecard.Hide();
            _result.Hide();
            _menu.SetAvailable(true);
        }

        private void UpdateMenuCaption()
        {
            var caption = _round.HoleCount > 1
                ? $"{_round.Course.Name}  ホール {_round.HoleIndex + 1} / {_round.HoleCount}"
                : _round.Course.Name;
            _menu.SetCaption(caption);
        }

        private void OnHoleCompleted(int hole)
        {
            _phase = Phase.HoleOver;
            _holeOver = HoleOverSeconds;
            _menu.SetAvailable(false);
            _view.ShowGuide(null, 0f);
        }

        /// <summary>喜ぶのを見せ終わるまで待ってから、スコアカードか結果へ</summary>
        private void TickHoleOver(float dt)
        {
            _hold -= dt;
            _holeOver -= dt;
            if (_hold > 0f || _holeOver > 0f)
            {
                return;
            }

            _hud.SetPlaying(false);
            if (_round.IsLastHole)
            {
                ShowResult();
                return;
            }

            _phase = Phase.Scorecard;
            var title = $"ホール {_round.HoleIndex + 1} おわり";
            if (_mode.IsGuest)
            {
                // 次のホールはホストが始める（届いたら BuildHole がスコアカードを閉じる）
                _scorecard.Show(_round, title, null, null, docked: false, note: "ホストが次のホールへ進めるのを待っています");
                return;
            }

            _scorecard.Show(_round, title, $"ホール {_round.HoleIndex + 2} へ", () => _round.NextHole());
        }

        private void ShowResult()
        {
            _phase = Phase.Result;
            var info = RecordResult();
            // 本編と同じく、結果の音が曲に埋もれないよう止める（「もう一度」で頭から流し直す）
            GameMusic.Stop();
            GameSounds.Play(GameSound.Clear);
            _scorecard.Show(_round, "ラウンド終了！", null, null);
            _result.Show(info);
        }

        /// <summary>
        /// 自分（ひとりなら席 0、オンラインは自分の席）の結果からコインを決め、記録に書いて保存する
        /// （CLAUDE.md §3.5: 項目は足すだけ。ここでは今ある表に書くだけ）。報酬は各自の端末で書く。
        /// オンラインは順位のボーナスも付ける。開発用の試しのホールでは書かない
        /// </summary>
        private GolfResultInfo RecordResult()
        {
            var seat = _mode.RecordSeat;
            var rank = _mode.IsOnline && _round.PlayerCount > 1 ? _round.Rank(seat) : 0;
            var strokes = _round.Total(seat);
            var par = _round.Course.TotalPar;
            var holeInOnes = _round.HoleInOnes(seat);
            var game = MiniGameTable.Get(MiniGameTable.GolfId);
            var records = new MiniGameRecords(GameSave.Record.Data);
            var hadBest = records.TryGetBest(game.Id, _round.Course.No, out var previousBest);
            var firstPlay = records.Plays(game.Id, _round.Course.No) == 0;
            var reward = GolfReward.Compute(strokes, par, holeInOnes, _record && firstPlay, rank);
            if (!_record)
            {
                return new GolfResultInfo(strokes, par, reward, false, false, 0, false);
            }

            var played = MiniGameReward.Apply(GameSave.Record.Data, game, _round.Course.No, strokes, holeInOnes, reward.Total);
            GameSave.Save();
            return new GolfResultInfo(strokes, par, reward, played.NewBest && hadBest, played.FirstPlay, hadBest ? previousBest : 0, true);
        }

        private void OnResultChosen(int card)
        {
            switch (card)
            {
                case GolfResultPanel.RetryCard:
                    // 同じ顔ぶれ・同じコースで、スコアカードを空にしてはじめから
                    _round.Restart();
                    PlayMusic();
                    break;
                case GolfResultPanel.CoursesCard:
                    if (_mode.IsOnline)
                    {
                        RequestRoom();
                        break;
                    }

                    MiniGameLaunch.ReturnToHub();
                    break;
                case GolfResultPanel.LobbyCard:
                    MiniGameLaunch.ReturnToLobby();
                    break;
            }
        }
    }
}
