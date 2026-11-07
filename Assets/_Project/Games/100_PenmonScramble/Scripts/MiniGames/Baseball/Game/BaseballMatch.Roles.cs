using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Profile;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 受け持ち: 打つのは打席の子の持ち主、投げるのはその打席に投げる人（守る側の人が打席ごとに交代。BaseballTeamPlan.PitchingSeat）。
    /// 持ち主がこの端末の人なら手元の入力、ほかの端末の人なら届いた入力、いなければ CPU。
    /// HUD の左（自分の側）はこの端末の人のチーム
    /// </summary>
    public partial class BaseballMatch
    {
        private readonly int[] _platesFielded = new int[BaseballRules.TeamCount];
        private int _pitcherSeat = -1;

        /// <summary>今の打者の持ち主の席（CPU は −1）</summary>
        private int BatterSeat => _plan.OwnerOf(_game.BattingTeam, _game.CurrentBatter);

        /// <summary>この打席に投げる人の席（CPU は −1）</summary>
        private int PitcherSeat => _pitcherSeat;

        private bool LocalBats => BatterSeat >= 0 && BatterSeat == _localSeat;

        private bool LocalPitches => _pitcherSeat >= 0 && _pitcherSeat == _localSeat;

        private bool CpuBats => BatterSeat < 0;

        private bool CpuPitches => _pitcherSeat < 0;

        /// <summary>この端末の人のチームが打つ番か（HUD の向き）</summary>
        private bool LocalTeamBats => _game.BattingTeam == _localTeam;

        private void ResetRoles()
        {
            System.Array.Clear(_platesFielded, 0, _platesFielded.Length);
            _pitcherSeat = -1;
            _throwCount = 0;
        }

        /// <summary>
        /// 打席の始め（ひとり・ホスト）: 守る側の、この打席に投げる人を決める。投げる人の子でない子が投げる子になっていたら、その人のいちばん若い子へ替える
        /// </summary>
        private void PickPitchingSeat()
        {
            var fielding = _game.FieldingTeam;
            _pitcherSeat = _plan.PitchingSeat(fielding, _platesFielded[fielding]++);
            if (_pitcherSeat < 0)
            {
                _game.ChoosePitcher(BaseballAi.ChoosePitcher(_game));
                return;
            }

            if (!_plan.CanPitch(_pitcherSeat, fielding, _game.CurrentPitcher))
            {
                var slot = _plan.FirstPitchable(_pitcherSeat, fielding);
                if (slot >= 0)
                {
                    _game.ChoosePitcher(slot);
                }
            }
        }

        /// <summary>
        /// チームの呼び方: ひとりは「あなた」と「CPU」、協力は「みんな」と「CPU」、対戦は「赤」「青」（スコアボードと打順のカードの見出し）
        /// </summary>
        private string TeamName(int team)
        {
            if (!IsOnline)
            {
                return team == _localTeam ? "あなた" : "CPU";
            }

            if (_plan.Mode == MiniGameOnlineMode.Coop)
            {
                return _plan.IsCpuTeam(team) ? "CPU" : "みんな";
            }

            return $"{MiniGameTeamColors.TeamName(team)}チーム";
        }

        /// <summary>みんなでのとき、打順のカードに持ち主（P1〜P4・席の色）を出す。ひとりでは出さない</summary>
        private void ShowOwners()
        {
            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                for (var slot = 0; slot < _teams[team].Count; slot++)
                {
                    var owner = IsOnline ? _plan.OwnerOf(team, slot) : -1;
                    _hud.SetOwner(team, slot, owner >= 0 ? SeatNames.Get(owner) : string.Empty, MiniGameSeats.ColorOf(owner));
                }
            }
        }

        /// <summary>今の番の人の呼び方（HUD の「〇〇 が打つ番です」に使う。CPU は「CPU」）</summary>
        private static string SeatLabel(int seat) => seat >= 0 ? SeatNames.Get(seat) : "CPU";
    }
}
