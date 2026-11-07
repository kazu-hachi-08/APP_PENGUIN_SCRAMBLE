using System;
using System.Collections.Generic;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 1打の入力（打つ子・向き・強さ・特殊）。オンラインのゲストが送るのもこの形
    /// </summary>
    public readonly struct GolfShot
    {
        public readonly int Member;
        public readonly float Angle;
        public readonly float Power;
        public readonly bool Special;

        /// <summary>風の特殊ショットの曲げる向き（右 +1 / 左 −1）。ほかでは使わない</summary>
        public readonly int Curve;

        public GolfShot(int member, float angle, float power, bool special = false, int curve = 1)
        {
            Member = member;
            Angle = angle;
            Power = power;
            Special = special;
            Curve = curve;
        }
    }

    /// <summary>
    /// 1コース（3ホール）を続けて回る。ホールごとの打数を残し（スコアカード）、2ホール目からは前までの打数が少ない順に打つ。
    /// 特殊ショットは各子 1コースに1回（使ったかは GolfTeam が持つ）。ホールの進行は GolfHoleWorld で、起きたことは Happened でまとめて渡す
    /// </summary>
    public sealed partial class GolfRound
    {
        // まだ回っていないホールの打数
        private const int NotPlayed = 0;

        private readonly int[,] _strokes;
        private readonly bool[] _recorded;
        private readonly List<bool[]> _specialsAtHoleStart = new List<bool[]>();
        private readonly bool[] _removed;
        private readonly List<int> _absent = new List<int>();

        public GolfRound(GolfCourseDef course, IReadOnlyList<GolfTeam> teams) : this(course, teams, false)
        {
        }

        /// <param name="mirror">オンラインのゲストの写し（GolfRound.Mirror.cs）。球の計算はせず、届いたスナップショットを写す</param>
        private GolfRound(GolfCourseDef course, IReadOnlyList<GolfTeam> teams, bool mirror)
        {
            if (course == null || course.Holes.Count == 0)
            {
                throw new ArgumentException("ホールの無いコース", nameof(course));
            }

            Course = course;
            Teams = teams;
            _strokes = new int[course.Holes.Count, teams.Count];
            _recorded = new bool[course.Holes.Count];
            _removed = new bool[teams.Count];
            IsMirror = mirror;
            StartHole(0);
        }

        /// <summary>ホールで起きたこと（いまのホールの GolfHoleWorld.Happened をそのまま渡す）</summary>
        public event Action<GolfEvent> Happened;

        /// <summary>ホールが始まった（やり直しも含む）。見た目を組み直す合図</summary>
        public event Action<int> HoleStarted;

        /// <summary>ホールの全員が終わって、打数をスコアカードに書いた</summary>
        public event Action<int> HoleCompleted;

        public GolfCourseDef Course { get; }
        public IReadOnlyList<GolfTeam> Teams { get; }
        public int PlayerCount => Teams.Count;
        public int HoleCount => Course.Holes.Count;
        public int HoleIndex { get; private set; }
        public GolfHoleWorld World { get; private set; }
        public GolfHoleDef Hole => World.Hole;
        public bool IsLastHole => HoleIndex >= HoleCount - 1;

        /// <summary>全部のホールを回り終えた</summary>
        public bool IsFinished => Array.TrueForAll(_recorded, r => r);

        /// <summary>そのホールの打数。まだ回っていなければ 0（いま回っているホールは途中の打数）</summary>
        public int Strokes(int hole, int player)
        {
            if (hole == HoleIndex && !_recorded[hole])
            {
                return World.Strokes(player);
            }

            return _recorded[hole] ? _strokes[hole, player] : NotPlayed;
        }

        public bool IsRecorded(int hole) => _recorded[hole];

        /// <summary>書き終えたホールの打数の合計</summary>
        public int Total(int player)
        {
            if (player < 0 || player >= PlayerCount)
            {
                return 0;
            }

            var total = 0;
            for (var hole = 0; hole < HoleCount; hole++)
            {
                total += _recorded[hole] ? _strokes[hole, player] : 0;
            }

            return total;
        }

        /// <summary>書き終えたホールのパーの合計（途中のパーとの差を出すため）</summary>
        public int ParThroughRecorded()
        {
            var par = 0;
            for (var hole = 0; hole < HoleCount; hole++)
            {
                par += _recorded[hole] ? Course.Holes[hole].Par : 0;
            }

            return par;
        }

        public int HoleInOnes(int player)
        {
            var count = 0;
            for (var hole = 0; hole < HoleCount; hole++)
            {
                count += _recorded[hole] && _strokes[hole, player] == 1 ? 1 : 0;
            }

            return count;
        }

        /// <summary>
        /// 打つ。打つ子を選び直し、特殊を頼まれていて、その子がまだ使っていなければ特殊ショットにする（使ったことにする）。
        /// 番でない人・狙っている間でないときは何もせず false
        /// </summary>
        public bool Shoot(int player, GolfShot shot)
        {
            if (IsMirror || World.Phase != GolfHolePhase.Aiming || player != World.CurrentPlayer)
            {
                return false;
            }

            var team = Teams[player];
            team.Select(shot.Member);
            var special = GolfSpecial.None;
            if (shot.Special && team.CanUseSpecial(team.Selected))
            {
                special = GolfSpecial.For(team.Current, shot.Curve);
            }

            if (!World.Shoot(player, shot.Angle, shot.Power, special))
            {
                return false;
            }

            if (!special.IsNone)
            {
                team.MarkSpecialUsed(team.Selected);
            }

            return true;
        }

        public bool Boost(int player, float turn) => !IsMirror && World.Boost(player, turn);

        public void Tick(float seconds)
        {
            if (!IsMirror)
            {
                World.Tick(seconds);
            }
        }

        /// <summary>持ち時間は減らさずに、動く物の時刻だけ進める（全体を見せている間など。写しは TickMirror で進む）</summary>
        public void Idle(float seconds)
        {
            if (!IsMirror)
            {
                World.Idle(seconds);
            }
        }

        /// <summary>書いたスコアカードの打数（いま回っているホールも、書き終えていなければ 0）</summary>
        internal int CardStrokes(int hole, int player) => _strokes[hole, player];

        // ---- 抜けた人（オンライン） ----

        /// <summary>途中で抜けた（オンラインの切断）。スコアカードには残すが、順位と打順からは外す</summary>
        public bool IsRemoved(int player) => player >= 0 && player < PlayerCount && _removed[player];

        /// <summary>抜けた人の球を消して打順から外す（このホールも、この後のホールも）</summary>
        public void RemovePlayer(int player)
        {
            if (IsMirror || player < 0 || player >= PlayerCount || _removed[player])
            {
                return;
            }

            _removed[player] = true;
            _absent.Add(player);
            World.RemovePlayer(player);
        }

        /// <summary>
        /// 順位（1 から。打数の合計が少ないほど上、同じ打数は同じ順位）。抜けた人は 0。
        /// ひとりで回ったときも 1（順位のボーナスを付けるかは呼ぶ側が人数で決める）
        /// </summary>
        public int Rank(int player)
        {
            if (IsRemoved(player) || player < 0 || player >= PlayerCount)
            {
                return 0;
            }

            var rank = 1;
            var total = Total(player);
            for (var other = 0; other < PlayerCount; other++)
            {
                if (other != player && !_removed[other] && Total(other) < total)
                {
                    rank++;
                }
            }

            return rank;
        }

        /// <summary>いまのホールを書き終えていれば、次のホールへ。最後のホールの後・まだ途中なら false</summary>
        public bool NextHole()
        {
            if (IsMirror || !_recorded[HoleIndex] || IsLastHole)
            {
                return false;
            }

            StartHole(HoleIndex + 1);
            return true;
        }

        /// <summary>いまのホールをはじめから（そのホールで使った特殊ショットも戻す）。書き終えたホールはやり直さない</summary>
        public void RestartHole()
        {
            if (IsMirror || _recorded[HoleIndex])
            {
                return;
            }

            for (var i = 0; i < Teams.Count; i++)
            {
                Teams[i].RestoreSpecials(_specialsAtHoleStart[i]);
            }

            StartHole(HoleIndex);
        }

        /// <summary>コースをはじめから（スコアカードも特殊ショットも空に戻す）</summary>
        public void Restart()
        {
            if (IsMirror)
            {
                return;
            }

            Array.Clear(_strokes, 0, _strokes.Length);
            Array.Clear(_recorded, 0, _recorded.Length);
            foreach (var team in Teams)
            {
                team.ResetSpecials();
            }

            StartHole(0);
        }

        /// <summary>2ホール目からの打つ順（前のホールまでの打数が少ない順。同じなら席の順）</summary>
        public int[] TurnOrderFor(int hole)
        {
            if (hole == 0)
            {
                return GolfTurnOrder.SeatOrder(PlayerCount);
            }

            var totals = new int[PlayerCount];
            for (var player = 0; player < PlayerCount; player++)
            {
                for (var h = 0; h < hole; h++)
                {
                    totals[player] += _strokes[h, player];
                }
            }

            return GolfTurnOrder.ByTotals(totals);
        }

        private void StartHole(int hole)
        {
            if (World != null)
            {
                World.Happened -= OnHappened;
            }

            HoleIndex = hole;
            _specialsAtHoleStart.Clear();
            foreach (var team in Teams)
            {
                _specialsAtHoleStart.Add(team.SaveSpecials());
            }

            World = new GolfHoleWorld(Course.Holes[hole], TurnOrderFor(hole), _absent);
            World.Happened += OnHappened;
            HoleStarted?.Invoke(hole);
            // 作った時点で最初の人の番が始まっているので、そのことも渡す（写しはホストから届く出来事で知る）
            if (IsMirror)
            {
                return;
            }

            Happened?.Invoke(new GolfEvent(GolfEventKind.TurnStart, World.CurrentPlayer, World.Balls[World.CurrentPlayer].Position));
        }

        private void OnHappened(GolfEvent e)
        {
            if (e.Kind == GolfEventKind.HoleFinished)
            {
                RecordHole();
            }

            Happened?.Invoke(e);
            if (e.Kind == GolfEventKind.HoleFinished)
            {
                HoleCompleted?.Invoke(HoleIndex);
            }
        }

        private void RecordHole()
        {
            for (var player = 0; player < PlayerCount; player++)
            {
                _strokes[HoleIndex, player] = World.Strokes(player);
            }

            _recorded[HoleIndex] = true;
        }
    }
}
