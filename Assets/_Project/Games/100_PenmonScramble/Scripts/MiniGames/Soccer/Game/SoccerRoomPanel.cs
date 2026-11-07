using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 「みんなで遊ぶ」の待ちの窓に出す、サッカーの選択（スタジアム・対戦 / 協力・席ごとの赤・青）。
    /// 板の動きはチームで戦うミニゲームで共通（MiniGameTeamRoomPanel）。ここはサッカーの名前と中身の一言だけ
    /// </summary>
    public class SoccerRoomPanel : MiniGameTeamRoomPanel
    {
        private readonly SoccerCourseGuide _guide = new SoccerCourseGuide();

        public override string Id => MiniGameTable.SoccerId;

        public override string Label => "サッカー";

        protected override string LinkName => "scramble.soccer";

        protected override IMiniGameCourseGuide Guide => _guide;

        protected override string OnlineLayout(int courseNo, MiniGameOnlineMode mode, int humans) => _guide.OnlineLayout(courseNo, mode, humans);

        protected override bool IsBuilt(int courseNo) => SoccerStadiumTable.Get(courseNo) != null;
    }
}
