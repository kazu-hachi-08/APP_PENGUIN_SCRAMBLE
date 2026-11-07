using PenmonScramble.Baseball.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 「みんなで遊ぶ」の待ちの窓に出す、野球の選択（球場・対戦 / 協力・席ごとの赤・青。赤が先攻）。
    /// 板の動きはチームで戦うミニゲームで共通（MiniGameTeamRoomPanel。サッカーと同じ）。ここは野球の名前と中身の一言だけ
    /// </summary>
    public class BaseballRoomPanel : MiniGameTeamRoomPanel
    {
        private readonly BaseballCourseGuide _guide = new BaseballCourseGuide();

        public override string Id => MiniGameTable.BaseballId;

        public override string Label => "野球";

        // メッセージの名前は配った版どうしで合わせるので変えない
        protected override string LinkName => "scramble.baseball";

        protected override IMiniGameCourseGuide Guide => _guide;

        protected override string OnlineLayout(int courseNo, MiniGameOnlineMode mode, int humans) => _guide.OnlineLayout(courseNo, mode, humans);

        protected override bool IsBuilt(int courseNo) => BaseballStadiumTable.Get(courseNo) != null;
    }
}
