using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// フィールドの子の AI（人が動かしていない子をすべて動かす）。毎歩、チームごとに
    /// 「ボールを取りに行く子（持っている子）」を1体だけ決め、ほかの子には持ち場を配る（全員がボールに群がらないように）。
    /// - 持っている子: 相手のゴールへ運び、コースが空いていればシュート、囲まれたら・前の味方が空いていればパス（SoccerAi.Carry.cs）
    /// - 取りに行く子: 転がるボールは追いつける所へ、相手が持っていれば寄ってスライディング（SoccerAi.Chase.cs）
    /// 持っている子のほかは、少し前に見たボール（SoccerAiSight。弱いほど遅れる）で動く
    /// - ほかの子: 攻めなら前と後ろに開き、守りならゴールとボールの間を埋める（SoccerAi.Roles.cs）
    /// 出すのは人と同じ SoccerInput だけ（AI だけの近道はしない）。乱数は種から作る System.Random（オンラインではホストだけが計算する）
    /// </summary>
    public sealed partial class SoccerAi
    {
        private readonly Random _random;
        private readonly SoccerAiBrain[] _brains;
        private readonly TeamPlan[] _plans = new TeamPlan[SoccerRules.TeamCount];
        private readonly SoccerAiLevel[] _levels = new SoccerAiLevel[SoccerRules.TeamCount];
        private readonly SoccerAiSight _sight = new SoccerAiSight();
        private readonly SoccerBallSight[] _seen = new SoccerBallSight[SoccerRules.TeamCount];

        // 必殺シュートを使うチーム（人のいるチームでは、AI の味方が人のゲージを使わないように使わない）
        private readonly bool[] _usesSpecial = new bool[SoccerRules.TeamCount];

        public SoccerAi(SoccerWorld world, int seed)
        {
            _random = new Random(seed);
            _brains = new SoccerAiBrain[world.Players.Count];
            for (var i = 0; i < _brains.Length; i++)
            {
                _brains[i] = new SoccerAiBrain();
            }

            for (var team = 0; team < _plans.Length; team++)
            {
                _plans[team] = new TeamPlan(world.Players.Count);
                _levels[team] = SoccerBalance.AiLevel(SoccerBalance.TeammateAiStars);
            }
        }

        public void SetLevel(int team, SoccerAiLevel level) => _levels[team] = level;

        /// <summary>そのチームの AI が、ゲージが満タンのときに必殺シュートを使うか（CPU のチームだけ使う）</summary>
        public void SetUsesSpecial(int team, bool uses) => _usesSpecial[team] = uses;

        /// <summary>
        /// 試合の段に合わせて考える: 試合中は Think、PK戦は ThinkShootout。それ以外（キックオフの合図・ゴールの間など）は何も押さない
        /// </summary>
        public void ThinkMatch(SoccerMatch match, SoccerInput[] inputs, Func<int, bool> isHuman)
        {
            switch (match.Phase)
            {
                case SoccerMatchPhase.Playing:
                    Think(match.World, inputs, isHuman);
                    break;
                case SoccerMatchPhase.Shootout:
                    ThinkShootout(match.Shootout, inputs, isHuman);
                    break;
                default:
                    for (var i = 0; i < inputs.Length; i++)
                    {
                        if (!isHuman(i))
                        {
                            inputs[i] = SoccerInput.None;
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// 人が動かしていない子（isHuman が false）の、この歩の入力を inputs に書く。人の子の入力には触らない
        /// </summary>
        public void Think(SoccerWorld world, SoccerInput[] inputs, Func<int, bool> isHuman)
        {
            _sight.Record(world.Ball);
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                _seen[team] = _sight.Seen(_levels[team].ReactionSeconds, world.Pitch);
                PlanTeam(world, team, isHuman);
            }

            foreach (var player in world.Players)
            {
                if (!isHuman(player.Index))
                {
                    inputs[player.Index] = ThinkPlayer(world, player);
                }
            }
        }

        private SoccerInput ThinkPlayer(SoccerWorld world, SoccerPlayer player)
        {
            var brain = _brains[player.Index];
            var level = _levels[player.Team];
            brain.Tick(SoccerRules.StepSeconds);
            var decide = brain.ThinkTimer <= 0f;
            if (decide)
            {
                brain.ThinkTimer = level.DecisionSeconds * (1f + ((float)_random.NextDouble() * 2f - 1f) * SoccerBalance.AiDecisionJitter);
            }

            if (world.IsGoalScored || !player.CanAct)
            {
                brain.CancelShot();
                return SoccerInput.None;
            }

            if (world.Ball.Holder == player.Index)
            {
                return Carry(world, player, brain, level, decide);
            }

            brain.CancelShot();
            var plan = _plans[player.Team];
            if (plan.Chaser == player.Index || Engaged(player))
            {
                return Chase(world, player, brain, level, decide);
            }

            return new SoccerInput(Approach(player.Position, plan.Targets[player.Index]), false);
        }

        /// <summary>持ち場の子でも、相手が持っているボールがすぐそばにあれば奪いに行く（目の前を素通りさせない）</summary>
        private bool Engaged(SoccerPlayer player)
        {
            var ball = _seen[player.Team];
            if (!ball.IsHeldByPlayer || _plans[player.Team].Attacking)
            {
                return false;
            }

            return Vec2.Distance(ball.Position, player.Position) <= SoccerBalance.AiEngageDistance;
        }

        /// <summary>target へ歩く（近づいたらゆるめ、着いたら止まる）</summary>
        private static Vec2 Approach(Vec2 from, Vec2 target)
        {
            var to = target - from;
            var distance = to.Length;
            if (distance <= SoccerBalance.AiArriveDistance)
            {
                return Vec2.Zero;
            }

            return to / distance * Math.Min(1f, distance / SoccerBalance.AiSlowDistance);
        }

        /// <summary>−1〜1 の山の形の乱数（真ん中ほど出やすい。狙いのぶれに使う）</summary>
        private float Triangle() => (float)(_random.NextDouble() + _random.NextDouble() - 1.0);

        private bool Roll(float chance) => _random.NextDouble() < chance;

        /// <summary>チームの攻める向きで見た X（相手のゴールが +）</summary>
        private static float Forward(int team, Vec2 position) => position.X * SoccerPitchDef.AttackDirection(team);

        /// <summary>チームの攻める向きで見た座標（相手のゴールが +X）を、ピッチの座標へ戻す</summary>
        private static Vec2 FromLocal(int team, float x, float y) => new Vec2(x * SoccerPitchDef.AttackDirection(team), y);

        /// <summary>a から b への線のそばに、team の相手（キーパーは除く）がいなければ true</summary>
        private static bool LaneClear(SoccerWorld world, int team, Vec2 a, Vec2 b, float clearance)
        {
            foreach (var other in world.Players)
            {
                if (other.Team == team || other.IsDown)
                {
                    continue;
                }

                var closest = MiniGames.Core.MiniGameGeometry.ClosestOnSegment(a, b, other.Position);
                if (Vec2.Distance(closest, other.Position) < clearance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>position に一番近い、team の相手の子までの距離（いなければとても大きい数）</summary>
        private static float NearestOpponentDistance(SoccerWorld world, int team, Vec2 position, out SoccerPlayer nearest)
        {
            nearest = null;
            var best = float.MaxValue;
            foreach (var other in world.Players)
            {
                if (other.Team == team || other.IsDown)
                {
                    continue;
                }

                var distance = Vec2.Distance(other.Position, position);
                if (distance < best)
                {
                    best = distance;
                    nearest = other;
                }
            }

            return best;
        }
    }

    /// <summary>AI の子1体の覚え（考え直すまでの時間・ためているシュート・出すと決めたスライディング）</summary>
    internal sealed class SoccerAiBrain
    {
        public float ThinkTimer;

        /// <summary>シュートをためている（残りの秒と、離すときの向き）</summary>
        public bool Charging;
        public float ChargeLeft;
        public Vec2 Aim;

        /// <summary>届いたらスライディングを出すと決めている（考え直すたびに決め直す）</summary>
        public bool SlideArmed;

        public void Tick(float dt)
        {
            ThinkTimer -= dt;
            ChargeLeft -= dt;
        }

        public void CancelShot() => Charging = false;
    }
}
