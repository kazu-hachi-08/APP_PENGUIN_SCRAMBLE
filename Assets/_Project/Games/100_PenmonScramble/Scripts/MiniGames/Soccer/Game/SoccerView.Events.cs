using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 起きたことへの見た目の反応: 蹴る動き（パスは小さく）・柵が弾む・バンパーが膨らむ・網が揺れる・キーパーが取る / 投げる、
    /// ゴールで点を取った側が喜び、取られた側（子とキーパー）が悔しがる。キックオフで元に戻す
    /// </summary>
    public partial class SoccerView
    {
        // 網・柵を揺らす当たりの速さの下限（壁沿いに転がるだけでは揺らさない）
        private const float BoardPopMinSpeed = 4f;

        // パスの蹴る動きの大きさ（シュートより小さく振る）
        private const float PassKickPower = 0.25f;

        public void OnEvent(SoccerEvent e, SoccerWorld world)
        {
            switch (e.Kind)
            {
                case SoccerEventKind.Kick:
                    PlayerAt(e.Player)?.Kick(e.Strength);
                    break;
                case SoccerEventKind.Pass:
                    PlayerAt(e.Player)?.Kick(PassKickPower);
                    break;
                case SoccerEventKind.WallHit:
                    if (e.Team < 0 && e.Strength >= BoardPopMinSpeed)
                    {
                        PopBoardNear(e.Position);
                    }

                    break;
                case SoccerEventKind.NetHit:
                    ShakeNet(e.Team, BallPoint(e.Position, world), e.Strength);
                    break;
                case SoccerEventKind.Goal:
                    ShakeNet(SoccerRules.Opponent(e.Team), BallPoint(e.Position, world), world.Ball.Velocity.Length);
                    React(e.Team);
                    break;
                case SoccerEventKind.Kickoff:
                    React(-1);
                    break;
                case SoccerEventKind.KeeperCatch:
                    KeeperAt(e.Team)?.PlayCatch();
                    break;
                case SoccerEventKind.KeeperThrow:
                    KeeperAt(e.Team)?.PlayThrow();
                    break;
                case SoccerEventKind.BumperHit:
                    PopBoardNear(e.Position);
                    PopPostNear(e.Position);
                    break;
            }
        }

        /// <summary>試合が終わったとき: 勝った側の子は喜び、負けた側の子とキーパーは悔しがる</summary>
        public void ShowWinner(int team) => React(team);

        /// <summary>scorer の子は喜び、相手の子とキーパーは悔しがる。−1 なら全員元に戻す</summary>
        private void React(int scorer)
        {
            foreach (var player in _players)
            {
                player.React(scorer >= 0 && player.Team == scorer, scorer >= 0 && player.Team != scorer);
            }

            foreach (var keeper in _keepers)
            {
                keeper.SetSlump(scorer >= 0 && keeper.Team != scorer);
            }
        }

        private Vector3 BallPoint(PenmonScramble.Core.Vec2 position, SoccerWorld world)
        {
            return SoccerSpace.ToWorld(position, world.Ball.Height + SoccerBalance.BallRadius);
        }

        private SoccerPlayerView PlayerAt(int index) => index >= 0 && index < _players.Count ? _players[index] : null;

        private SoccerKeeperView KeeperAt(int team) => team >= 0 && team < _keepers.Count ? _keepers[team] : null;
    }
}
