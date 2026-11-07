using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>子1体の写し（見た目と、ゲストの手元のボタンの中身に要る分だけ）</summary>
    public struct SoccerPlayerSnap
    {
        public Vec2 Position;
        public Vec2 Velocity;
        public Vec2 Facing;
        public SoccerPlayerState State;
        public float StateSeconds;
        public bool IsCharging;
        public float ChargeSeconds;
        public float SlideCooldown;
        public float DashCooldown;
        public float FrozenSeconds;
    }

    public struct SoccerKeeperSnap
    {
        public Vec2 Position;
        public float Speed;
        public SoccerKeeperState State;
        public float StateSeconds;
        public float DiveSide;
    }

    public struct SoccerBallSnap
    {
        public Vec2 Position;
        public Vec2 Velocity;
        public float Height;
        public float VerticalSpeed;
        public int Holder;
        public int KeeperTeam;
        public int LastToucher;
    }

    /// <summary>飛んでいる必殺シュートの写し（ボールの尾の色と、雷の消えている間）</summary>
    public struct SoccerSpecialSnap
    {
        public int Shooter;
        public int Team;
        public Element Element;
        public int Stars;
        public bool Hidden;
    }

    /// <summary>
    /// PK戦の写し。蹴る子の狙い（AimSide・AimHigh）は送らない（守る人に見せないため。守る人の選びはホストにだけ届き、蹴った瞬間に決まる）
    /// </summary>
    public sealed class SoccerShootoutSnap
    {
        public SoccerShootoutPhase Phase;
        public float PhaseSeconds;
        public int KickingTeam;
        public int Kicker;
        public float Charge01;
        public bool IsCharging;
        public int KeeperSide;
        public SoccerPenaltyResult LastResult;
        public int Winner;
        public readonly List<SoccerPenaltyResult>[] Results = { new List<SoccerPenaltyResult>(), new List<SoccerPenaltyResult>() };
    }

    /// <summary>
    /// ホストが毎秒15回ゲストへ送る、試合のその時の全部（Docs/MINIGAME_SOCCER_PLAN.md「オンライン」）。
    /// 位置・ボール・点・時間・ゲージ・PK戦に、前に送ってから起きたこと（音・知らせ・子の反応）を載せる。
    /// ゲストは計算せず、これを写しの試合（SoccerMatch.ApplySnapshot）に写して描く
    /// </summary>
    public sealed class SoccerSnapshot
    {
        public int Stadium;
        public SoccerOnlineMode Mode;
        public int PerTeam;

        /// <summary>席ごとのチーム（抜けた席は −1）・いま動かしている子（いなければ −1）・入れた点</summary>
        public int[] SeatTeam;
        public int[] SeatControlled;
        public int[] SeatGoals;

        /// <summary>子ごとのペンモンの種類（ゲストが見た目と必殺シュートの種類を作る）</summary>
        public int[] Species;

        public SoccerMatchPhase Phase;
        public float PhaseSeconds;
        public float TimeLeft;
        public int Winner;
        public bool WonByShootout;

        public readonly int[] Score = new int[SoccerRules.TeamCount];
        public readonly float[] Gauge = new float[SoccerRules.TeamCount];
        public float CutInSeconds;
        public bool GoalScored;

        /// <summary>ピッチの時刻（SoccerWorld.Time。仕掛けの様子は時刻だけで決まるので、ゲストはこれで同じ様子を描く）</summary>
        public float WorldTime;

        public SoccerBallSnap Ball;

        /// <summary>飛んでいる必殺シュート。飛んでいなければ null</summary>
        public SoccerSpecialSnap? Special;

        public SoccerPlayerSnap[] Players;
        public SoccerKeeperSnap[] Keepers;

        /// <summary>PK戦。始まるまで null</summary>
        public SoccerShootoutSnap Shootout;

        public readonly List<SoccerEvent> Events = new List<SoccerEvent>();

        public int SeatCount => SeatTeam.Length;

        /// <summary>ホストのみ: 今の試合を写す。events は前に送ってから起きたこと（写した後に空にするのは呼ぶ側）</summary>
        public static SoccerSnapshot Capture(int stadium, SoccerMatch match, SoccerTeamPlan plan, SoccerHumans humans, IReadOnlyList<int> seatGoals, IReadOnlyList<SoccerEvent> events)
        {
            var world = match.World;
            var snapshot = new SoccerSnapshot
            {
                Stadium = stadium,
                Mode = plan.Mode,
                PerTeam = plan.PerTeam,
                Species = new List<int>(plan.Species).ToArray(),
            };
            CaptureSeats(snapshot, match, plan, humans, seatGoals);
            match.CaptureTo(snapshot);
            world.CaptureTo(snapshot);
            snapshot.Shootout = match.Shootout?.Capture();
            if (events != null)
            {
                snapshot.Events.AddRange(events);
            }

            return snapshot;
        }

        private static void CaptureSeats(SoccerSnapshot snapshot, SoccerMatch match, SoccerTeamPlan plan, SoccerHumans humans, IReadOnlyList<int> seatGoals)
        {
            var seats = plan.SeatCount;
            snapshot.SeatTeam = new int[seats];
            snapshot.SeatControlled = new int[seats];
            snapshot.SeatGoals = new int[seats];
            for (var seat = 0; seat < seats; seat++)
            {
                var here = humans.Has(seat);
                snapshot.SeatTeam[seat] = here ? plan.TeamOf(seat) : -1;
                snapshot.SeatControlled[seat] = here ? humans.DisplayedBy(match, seat) : SoccerBall.NoPlayer;
                snapshot.SeatGoals[seat] = seatGoals != null && seat < seatGoals.Count ? seatGoals[seat] : 0;
            }
        }
    }
}
