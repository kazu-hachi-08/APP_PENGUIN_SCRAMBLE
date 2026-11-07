using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// 必殺シュート: チームで1本のゲージ（持つ・奪う・パスが通るとたまる）が満タンのとき、持っている子が押すと、
    /// カットイン（全体が止まる）の後に、その子の属性の軌道で蹴る。★で強さが変わる。
    /// 飛んでいる間はフィールドの子は誰も持てない（止められるのはキーパーだけで、止める割合も下がる）
    /// </summary>
    public sealed partial class SoccerWorld
    {
        private readonly float[] _gauge = new float[SoccerRules.TeamCount];
        private float _cutInSeconds;
        private int _cutInPlayer = SoccerBall.NoPlayer;
        private Vec2 _cutInStick;

        // 直前にパスした子（受けた味方のゲージを足す）。誰かが持ったら消す
        private int _lastPasser = SoccerBall.NoPlayer;

        /// <summary>飛んでいる必殺シュート。飛んでいなければ null</summary>
        public SoccerSpecialShot Special { get; private set; }

        /// <summary>カットインで全体が止まっている（試合の時間も止める）</summary>
        public bool IsCutIn => _cutInSeconds > 0f;

        /// <summary>チームのゲージ（0〜1）</summary>
        public float Gauge(int team) => _gauge[team];

        public bool IsSpecialReady(int team) => _gauge[team] >= 1f;

        /// <summary>子の属性と★（必殺シュートの種類と強さ）。作った後、試合の前に決める</summary>
        public void SetKind(int index, Element element, int stars)
        {
            _players[index].Element = element;
            _players[index].Stars = Math.Max(1, stars);
        }

        /// <summary>ゲージを決まった量にする（開発用・テスト用）</summary>
        public void SetGauge(int team, float value) => _gauge[team] = Math.Clamp(value, 0f, 1f);

        private void AddGauge(int team, float amount) => _gauge[team] = Math.Min(1f, _gauge[team] + amount);

        /// <summary>持った子のチームのゲージを足す（味方のパスを受けたなら多めに）</summary>
        private void GaugeOnPossess(SoccerPlayer picker)
        {
            var passed = _lastPasser != SoccerBall.NoPlayer && _lastPasser != picker.Index && _players[_lastPasser].Team == picker.Team;
            AddGauge(picker.Team, SoccerBalance.GaugePossess + (passed ? SoccerBalance.GaugePassReceived : 0f));
            _lastPasser = SoccerBall.NoPlayer;
        }

        /// <summary>カットインを数え、終わった歩で蹴る。止まっている間は true（ほかは何も進めない）</summary>
        private bool StepCutIn(List<SoccerEvent> events)
        {
            if (!IsCutIn)
            {
                return false;
            }

            _cutInSeconds -= Dt;
            if (_cutInSeconds <= 0f)
            {
                _cutInSeconds = 0f;
                ReleaseSpecial(events);
            }

            return true;
        }

        /// <summary>押した瞬間: ゲージを使い切ってカットインを始める（蹴るのはカットインの後）</summary>
        private void StartSpecial(SoccerPlayer player, Vec2 stick, List<SoccerEvent> events)
        {
            player.ResetCharge();
            _gauge[player.Team] = 0f;
            _cutInSeconds = SoccerBalance.SpecialCutInSeconds;
            _cutInPlayer = player.Index;
            _cutInStick = stick;
            events.Add(new SoccerEvent(SoccerEventKind.SpecialCutIn, player.Index, player.Team, player.Position, player.Stars));
        }

        private void ReleaseSpecial(List<SoccerEvent> events)
        {
            var index = _cutInPlayer;
            _cutInPlayer = SoccerBall.NoPlayer;
            if (index == SoccerBall.NoPlayer || Ball.Holder != index)
            {
                return;
            }

            var player = _players[index];
            var goal = Pitch.GoalOf(SoccerRules.Opponent(player.Team));
            var target = SpecialTarget(player, _cutInStick, goal);
            var toTarget = target - player.Position;
            var direction = toTarget.IsNearlyZero ? player.Facing : toTarget.Normalized;
            var shot = new SoccerSpecialShot(index, player.Team, player.Element, player.Stars);
            var speed = Launch(shot, player, direction, target, out var launchDirection, out var verticalSpeed);
            Release(player, launchDirection, speed, verticalSpeed);
            Special = shot;
            events.Add(new SoccerEvent(SoccerEventKind.Kick, index, player.Team, Ball.Position, 1f, verticalSpeed > 0f));
            events.Add(new SoccerEvent(SoccerEventKind.SpecialKick, index, player.Team, Ball.Position, player.Stars));
        }

        /// <summary>
        /// 狙う所（ゴールの線の上）: スティックをゴールの方へ倒していればその向きの先（口の内側に収める）、
        /// そうでなければキーパーのいない側の柱の内側
        /// </summary>
        private Vec2 SpecialTarget(SoccerPlayer player, Vec2 stick, SoccerGoal goal)
        {
            var inner = goal.HalfWidth - SoccerBalance.SpecialPostInset;
            var from = player.Position;
            var toLine = goal.LineX - from.X;
            if (!stick.IsNearlyZero && stick.X * toLine > 0f)
            {
                var y = from.Y + stick.Y / stick.X * toLine;
                return new Vec2(goal.LineX, Math.Clamp(y, -inner, inner));
            }

            var keeperY = _keepers.Length > goal.Team ? _keepers[goal.Team].Position.Y : 0f;
            var side = keeperY > 0f ? -1f : keeperY < 0f ? 1f : (from.Y >= 0f ? 1f : -1f);
            return new Vec2(goal.LineX, side * inner);
        }

        /// <summary>属性ごとの蹴り出し。返すのは速さ（向きと上向きの速さは out）</summary>
        private float Launch(SoccerSpecialShot shot, SoccerPlayer player, Vec2 direction, Vec2 target, out Vec2 launchDirection, out float verticalSpeed)
        {
            launchDirection = direction;
            verticalSpeed = 0f;
            var distance = Vec2.Distance(player.Position, target);
            switch (shot.Element)
            {
                case Element.Water:
                    shot.Flight.HopsLeft = SoccerBalance.WaterHops(shot.Stars);
                    verticalSpeed = WaterLaunchSpeed(distance, shot.Flight.HopsLeft, shot.Gravity);
                    shot.Flight.HopSpeed = verticalSpeed * SoccerBalance.WaterHopRatio;
                    return SoccerBalance.WaterShotSpeed;
                case Element.Wind:
                    launchDirection = WindLaunch(shot, player.Position, direction, target, distance);
                    return SoccerBalance.WindShotSpeed;
                case Element.Thunder:
                    return ThunderLaunch(shot, player, direction);
                default:
                    return SoccerBalance.IceShotSpeed;
            }
        }

        /// <summary>
        /// 水: 跳ねる回数ぶんの山を、山ごとに WaterHopRatio 倍の長さにして、最後の山で的に着くように、はじめの跳ね上がる速さを決める
        /// （高さは WaterMinPeak〜WaterMaxPeak に収める。収まらなければ的の手前か先で跳ね終わる）
        /// </summary>
        private static float WaterLaunchSpeed(float distance, int hops, float gravity)
        {
            var r = SoccerBalance.WaterHopRatio;
            var totalSeconds = distance / SoccerBalance.WaterShotSpeed;
            var firstSeconds = totalSeconds * (1f - r) / (1f - MathF.Pow(r, hops + 1));
            var peak = gravity * firstSeconds * firstSeconds / 8f;
            peak = Math.Clamp(peak, SoccerBalance.WaterMinPeak, SoccerBalance.WaterMaxPeak);
            return MathF.Sqrt(2f * gravity * peak);
        }

        /// <summary>
        /// 風: 的へのまっすぐな向きから、口の外側へ角度 θ ずらして蹴り、一定の速さで向きを変えて的に着く円の弧にする
        /// （弧の向きの変わり方 ＝ 2·速さ·sinθ / 距離）。どちらへずらすかは、的がゴールの真ん中から外れている側。
        /// 弧のふくらみが壁にかかるなら反対の側へ曲げ、それでもかかるなら θ を小さくする（壁に当たって終わらないように）
        /// </summary>
        private Vec2 WindLaunch(SoccerSpecialShot shot, Vec2 from, Vec2 direction, Vec2 target, float distance)
        {
            var theta = SoccerBalance.WindCurveDegrees(shot.Stars) * MathF.PI / 180f;
            var outward = Math.Abs(target.Y) > 0.01f ? Math.Sign(target.Y) : (from.Y >= 0f ? 1f : -1f);
            // 左回り（+θ）で外側へ向くか: 進む向きの左手側 (−d.Y, d.X) が外側 (0, outward) と同じ向きなら +
            var side = direction.X * outward >= 0f ? 1f : -1f;
            for (var i = 0; i < WindFitTries && !ArcFits(from, target, side * theta); i++)
            {
                if (i == 0 && ArcFits(from, target, -side * theta))
                {
                    side = -side;
                    break;
                }

                theta *= 0.5f;
            }

            shot.TurnRate = distance > 0.01f ? -side * 2f * SoccerBalance.WindShotSpeed * MathF.Sin(theta) / distance : 0f;
            return direction.Rotated(side * theta);
        }

        // 弧が壁にかからないよう θ を半分にしていく回数と、壁から空ける余白
        private const int WindFitTries = 4;
        private const float WindWallMargin = 1f;

        // ゴールの線へ着く向きが、線に垂直な向きからこの cos より寝ていたら弧を選び直す（60°）
        private const float WindMinArriveCos = 0.5f;

        /// <summary>from から target への弧（蹴り出しの向きのずれ signedTheta）が、ゴールの線へ立った角度で着き、一番ふくらむ所が壁から余白より内側か</summary>
        private bool ArcFits(Vec2 from, Vec2 target, float signedTheta)
        {
            var chord = target - from;
            var length = chord.Length;
            if (length < 0.01f)
            {
                return true;
            }

            // ゴールの線へ寝た角度で着く弧は、口に届く前に口の横の壁に当たる
            var arrive = (chord / length).Rotated(-signedTheta);
            if (Math.Abs(arrive.X) < WindMinArriveCos)
            {
                return false;
            }

            // 弦の真ん中から、弦の左手側へ（弦の長さ / 2）·tan(θ/2) ふくらむ
            var normal = new Vec2(-chord.Y, chord.X) / length;
            var apex = from + chord * 0.5f + normal * (length * 0.5f * MathF.Tan(signedTheta * 0.5f));
            return Math.Abs(apex.Y) <= Pitch.HalfWidth - WindWallMargin && Math.Abs(apex.X) <= Pitch.HalfLength - WindWallMargin;
        }

        /// <summary>雷: しばらく消えて速く進み、キーパーの線の手前で現れてから超高速で飛ぶ</summary>
        private float ThunderLaunch(SoccerSpecialShot shot, SoccerPlayer player, Vec2 direction)
        {
            var keeperLine = _keepers.Length > 0
                ? _keepers[SoccerRules.Opponent(player.Team)].LineX
                : Pitch.GoalOf(SoccerRules.Opponent(player.Team)).LineX;
            var along = Math.Abs(direction.X) > 0.01f ? (keeperLine - player.Position.X) / direction.X : float.MaxValue;
            var blink = Math.Min(SoccerBalance.ThunderBlinkDistance(shot.Stars), Math.Max(0f, along - SoccerBalance.ThunderAppearGap));
            shot.Flight.HiddenSeconds = SoccerBalance.ThunderHiddenSeconds;
            shot.Direction = direction;
            shot.AppearSpeed = SoccerBalance.ThunderSpeed(shot.Stars);
            return blink / SoccerBalance.ThunderHiddenSeconds;
        }

        /// <summary>飛んでいる必殺シュートの1歩（摩擦なし。属性ごとに曲がる・跳ねる・消える）</summary>
        private void StepSpecialBall(List<SoccerEvent> events)
        {
            var shot = Special;
            shot.Seconds += Dt;
            var flight = shot.Flight;
            flight.Velocity = Ball.Velocity;
            flight.Height = Ball.Height;
            flight.VerticalSpeed = Ball.VerticalSpeed;
            flight.Advance(shot, Dt);
            shot.Flight = flight;
            Ball.Velocity = flight.Velocity;
            Ball.Height = flight.Height;
            Ball.VerticalSpeed = flight.VerticalSpeed;
            if (flight.Landed)
            {
                events.Add(new SoccerEvent(SoccerEventKind.Land, SoccerBall.NoPlayer, -1, Ball.Position, flight.LandSpeed));
            }

            MoveBall(events);
            if (flight.HopsDone || shot.Seconds >= SoccerBalance.SpecialMaxSeconds)
            {
                EndSpecial();
            }
        }

        /// <summary>
        /// 飛んでいる必殺シュートが、このキーパーの線をいつ・どこ・どの高さで通るか（飛び方の計算を先へ進めて読む。壁は読まない）。
        /// ゴールの口を外れる・バーより高い・線まで届かないなら false
        /// </summary>
        private bool PredictSpecial(SoccerKeeper keeper, out SoccerShotPrediction prediction)
        {
            prediction = default;
            var shot = Special;
            var goal = Pitch.GoalOf(keeper.Team);
            var position = Ball.Position;
            if ((position.X - keeper.LineX) * goal.Outward >= 0f)
            {
                return false;
            }

            var flight = shot.Flight;
            flight.Velocity = Ball.Velocity;
            flight.Height = Ball.Height;
            flight.VerticalSpeed = Ball.VerticalSpeed;
            for (var t = shot.Seconds; t < SoccerBalance.SpecialMaxSeconds; t += Dt)
            {
                flight.Advance(shot, Dt);
                var next = position + flight.Velocity * Dt;
                if ((next.X - keeper.LineX) * goal.Outward >= 0f)
                {
                    var along = (keeper.LineX - position.X) / (next.X - position.X);
                    var y = position.Y + (next.Y - position.Y) * along;
                    if (Math.Abs(y) >= goal.HalfWidth || !goal.PassesUnderBar(flight.Height))
                    {
                        return false;
                    }

                    prediction = new SoccerShotPrediction(t - shot.Seconds + Dt * along, y, flight.Height);
                    return true;
                }

                position = next;
            }

            return false;
        }

        /// <summary>氷: この歩でボールが通った線のそばにいる相手を凍らせる（ボールは止まらず貫く）</summary>
        private void FreezeAlong(Vec2 from, List<SoccerEvent> events)
        {
            if (Special == null || Special.Element != Element.Ice)
            {
                return;
            }

            var seconds = SoccerBalance.IceFreezeSeconds(Special.Stars);
            foreach (var player in _players)
            {
                if (player.Team == Special.Team || player.IsFrozen)
                {
                    continue;
                }

                var closest = MiniGames.Core.MiniGameGeometry.ClosestOnSegment(from, Ball.Position, player.Position);
                if (Vec2.Distance(closest, player.Position) > SoccerBalance.IceFreezeReach)
                {
                    continue;
                }

                player.ResetCharge();
                player.Enter(SoccerPlayerState.Frozen);
                player.FrozenSeconds = seconds;
                player.DashSeconds = 0f;
                player.Velocity = Vec2.Zero;
                events.Add(new SoccerEvent(SoccerEventKind.Freeze, player.Index, player.Team, player.Position, seconds));
            }
        }

        /// <summary>ふつうのボールに戻す（壁・網・キーパー・ゴール・時間切れ）</summary>
        private void EndSpecial()
        {
            if (Special == null)
            {
                return;
            }

            Special = null;
            _trajectoryChanged = true;
        }

        /// <summary>キックオフで並び直すとき: 止まっている・飛んでいる必殺シュートを消す（ゲージは残す）</summary>
        private void ResetSpecial()
        {
            Special = null;
            _cutInSeconds = 0f;
            _cutInPlayer = SoccerBall.NoPlayer;
            _lastPasser = SoccerBall.NoPlayer;
        }
    }
}
