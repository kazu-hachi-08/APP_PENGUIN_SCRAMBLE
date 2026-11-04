using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>起きたことと、それが起きたホール（次のホールへ移った直後の出来事を、前のホールのものと取り違えないように）</summary>
    public readonly struct GolfNetEvent
    {
        public readonly int Hole;
        public readonly GolfEvent Event;

        public GolfNetEvent(int hole, GolfEvent e)
        {
            Hole = hole;
            Event = e;
        }
    }

    /// <summary>球1つの、そのときの様子</summary>
    public struct GolfBallState
    {
        public Vec2 Position;
        public Vec2 Velocity;
        public float Height;
        public bool InCup;
        public bool Active;
        public GolfSpecialKind Special;
        public float IceLeft;
        public bool BoostReady;
        public int BoostStars;
    }

    /// <summary>1人の打つ子の3体と、いま選んでいる子・特殊ショットを使ったか</summary>
    public struct GolfTeamState
    {
        public int[] Species;
        public int Selected;
        public bool[] SpecialUsed;
    }

    /// <summary>
    /// ホストが全員へ配る1コースの様子。ゲストはこれを写すだけで、球の計算はしない
    /// （端末によって小数の計算がずれても、全員が同じ結果を見るように）。
    /// Events は前に送ってから起きたことで、状態はそれが全部起きた後のもの
    /// </summary>
    public sealed class GolfSnapshot
    {
        public int CourseNo;
        public int HoleIndex;
        public GolfHolePhase Phase;
        public int CurrentPlayer;
        public int[] TurnOrder;
        public float ShotTimeLeft;

        /// <summary>番の人の手元（向き・ゲージ・打つ子・特殊）</summary>
        public GolfAim Aim;

        public GolfBallState[] Balls;

        /// <summary>いまのホールの打数</summary>
        public int[] Strokes;

        public bool[] Done;
        public GolfTeamState[] Teams;

        /// <summary>スコアカード [ホール, 席]（書き終えたホールだけ意味がある）</summary>
        public int[,] Card;

        public bool[] Recorded;

        /// <summary>途中で抜けた席</summary>
        public bool[] Removed;

        public List<GolfNetEvent> Events = new List<GolfNetEvent>();

        public int PlayerCount => Balls.Length;

        /// <summary>ホストのラウンドの、いまの様子を写す</summary>
        /// <param name="aim">番の人の手元</param>
        /// <param name="events">前に送ってから起きたこと（写すだけで消さない）</param>
        public static GolfSnapshot Capture(GolfRound round, GolfAim aim, IEnumerable<GolfNetEvent> events)
        {
            var world = round.World;
            var players = round.PlayerCount;
            var snapshot = new GolfSnapshot
            {
                CourseNo = round.Course.No,
                HoleIndex = round.HoleIndex,
                Phase = world.Phase,
                CurrentPlayer = world.CurrentPlayer,
                TurnOrder = new int[players],
                ShotTimeLeft = world.ShotTimeLeft,
                Aim = new GolfAim(round.Teams[world.CurrentPlayer].Selected, world.AimAngle, aim.Power, aim.GaugeRunning, aim.Striking, aim.Special, aim.Curve),
                Balls = new GolfBallState[players],
                Strokes = new int[players],
                Done = new bool[players],
                Teams = new GolfTeamState[players],
                Card = new int[round.HoleCount, players],
                Recorded = new bool[round.HoleCount],
                Removed = new bool[players],
            };

            for (var player = 0; player < players; player++)
            {
                snapshot.TurnOrder[player] = world.TurnOrder[player];
                snapshot.Balls[player] = BallState(world.Balls[player]);
                snapshot.Strokes[player] = world.Strokes(player);
                snapshot.Done[player] = world.IsDone(player);
                snapshot.Teams[player] = TeamState(round.Teams[player]);
                snapshot.Removed[player] = round.IsRemoved(player);
            }

            for (var hole = 0; hole < round.HoleCount; hole++)
            {
                snapshot.Recorded[hole] = round.IsRecorded(hole);
                for (var player = 0; player < players; player++)
                {
                    snapshot.Card[hole, player] = round.CardStrokes(hole, player);
                }
            }

            snapshot.Events.AddRange(events);
            return snapshot;
        }

        private static GolfBallState BallState(GolfBall ball)
        {
            return new GolfBallState
            {
                Position = ball.Position,
                Velocity = ball.Velocity,
                Height = ball.Height,
                InCup = ball.InCup,
                Active = ball.Active,
                Special = ball.Special,
                IceLeft = ball.IceLeft,
                BoostReady = ball.BoostReady,
                BoostStars = ball.BoostStars,
            };
        }

        private static GolfTeamState TeamState(GolfTeam team)
        {
            var state = new GolfTeamState
            {
                Species = new int[team.Members.Count],
                Selected = team.Selected,
                SpecialUsed = new bool[team.Members.Count],
            };
            for (var i = 0; i < team.Members.Count; i++)
            {
                state.Species[i] = team.Members[i].SpeciesNo;
                state.SpecialUsed[i] = team.IsSpecialUsed(i);
            }

            return state;
        }
    }
}
