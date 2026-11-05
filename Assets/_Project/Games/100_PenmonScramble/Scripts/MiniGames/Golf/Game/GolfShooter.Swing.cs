using System;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 打つ子の動きの流れ: 構える（ゲージに合わせて振りかぶる）→ 振り下ろす（当たった瞬間に打つ）→ 振り抜く → 球を見送る。
    /// カップインで喜ぶ（カップの横にぽんと出て跳ねる）・池で頭を抱える（その場で）
    /// </summary>
    public sealed partial class GolfShooter
    {
        // 振りの角度（度）: ゲージ 100% で振りかぶる大きさ・振り抜く大きさ（強いほど大きく振り抜く）
        private const float BackswingMax = 115f;
        private const float FollowMin = 60f;
        private const float FollowMax = 125f;
        // 構えの間に振りかぶりを追う速さ（度/秒）。ゲージより速く、カクッとしない程度
        private const float BackswingFollowSpeed = 720f;

        // 振り下ろし（いちばん大きく振りかぶったときの秒。小さいほど短い）・振り抜き・振り抜いたまま止まる秒
        private const float DownswingSeconds = 0.14f;
        private const float DownswingMinSeconds = 0.06f;
        private const float FollowSeconds = 0.2f;
        private const float FollowHoldSeconds = 0.5f;
        // 見送る間はクラブを少し下ろして、球の方へ体を向ける
        private const float WatchSwing = -35f;
        private const float WatchSettleSpeed = 160f;

        private State _state = State.Hidden;
        private float _swing;
        private float _stateTime;
        private float _strikeFrom;
        private float _strikeSeconds;
        private float _followTo;
        private float _strikePower;
        private Action _onImpact;
        private Vector3 _watchTarget;

        /// <summary>振り下ろしている（まだ当たっていない）。この間は狙いもゲージも動かさない</summary>
        public bool Striking => _state == State.Downswing;

        /// <summary>
        /// 球の横で構える。backswing はいまのゲージの強さ（0〜1。動かしていなければ 0）。毎フレーム呼んで、狙う向きに合わせて回り込む
        /// </summary>
        public void Address(GolfMember member, Vector3 ball, Vector3 aim, GolfSurface surface, float backswing)
        {
            var actor = Use(member);
            if (_state != State.Address)
            {
                BeginAddress(actor);
            }

            MoveTo(StandPoint(actor, ball, aim, surface));
            actor.Motion.Facing = FacingFor(aim);
            _swing = Mathf.MoveTowards(_swing, backswing * BackswingMax, BackswingFollowSpeed * Time.deltaTime);
        }

        private void BeginAddress(Actor actor)
        {
            var motion = actor.Motion;
            motion.Cheering = false;
            motion.Despairing = false;
            motion.Holding = true;
            actor.Grip.gameObject.SetActive(true);
            _state = State.Address;
        }

        /// <summary>振り下ろす。クラブが球に当たった瞬間に onImpact を呼ぶ（そこで Core に打たせる）</summary>
        public void Strike(float power, Action onImpact)
        {
            if (_state != State.Address)
            {
                onImpact?.Invoke();
                return;
            }

            _onImpact = onImpact;
            _strikePower = power;
            _strikeFrom = _swing;
            _strikeSeconds = Mathf.Max(DownswingMinSeconds, DownswingSeconds * Mathf.Clamp01(_swing / BackswingMax));
            Enter(State.Downswing);
        }

        /// <summary>
        /// Core で打った（持ち時間切れの自動の1打もここへ来る）。振り下ろしていなければ、その場で振り抜く
        /// </summary>
        public void OnShot(float power)
        {
            if (_state == State.Downswing || _state == State.Follow || _current == null)
            {
                return;
            }

            _strikePower = power;
            BeginFollow();
        }

        /// <summary>見送る球の位置（毎フレーム。止まるまで顔を向ける）</summary>
        public void Watch(Vector3 ball)
        {
            _watchTarget = ball;
        }

        /// <summary>カップインで喜ぶ: カップの横（見ている側）にぽんと出て、クラブを置いて跳ねる</summary>
        public void Cheer(Vector3 at, Vector3 faceToward)
        {
            if (_current == null)
            {
                return;
            }

            var motion = _current.Motion;
            motion.Holding = false;
            motion.Despairing = false;
            _current.Grip.gameObject.SetActive(false);
            MoveTo(at);
            StartPop();
            motion.SnapFacing(faceToward - at);
            motion.Facing = Vector3.zero;
            motion.Cheering = true;
            Enter(State.React);
        }

        /// <summary>池・場外に落ちて頭を抱える（その場で。クラブは置く）</summary>
        public void Despair()
        {
            if (_current == null)
            {
                return;
            }

            var motion = _current.Motion;
            motion.Holding = false;
            motion.Cheering = false;
            _current.Grip.gameObject.SetActive(false);
            motion.Despairing = true;
            Enter(State.React);
        }

        private void Enter(State state)
        {
            _state = state;
            _stateTime = 0f;
        }

        private void BeginFollow()
        {
            _strikeFrom = _swing;
            _followTo = -Mathf.Lerp(FollowMin, FollowMax, _strikePower);
            Enter(State.Follow);
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            TickPop(dt);
            if (_current == null)
            {
                return;
            }

            _stateTime += dt;
            switch (_state)
            {
                case State.Downswing:
                    TickDownswing();
                    break;
                case State.Follow:
                    TickFollow();
                    break;
                case State.Watch:
                    TickWatch(dt);
                    break;
            }

            _current.Motion.HoldSwing = _swing;
            _current.PoseClub(_swing);
        }

        /// <summary>だんだん速く振り下ろす（当たる瞬間がいちばん速い）</summary>
        private void TickDownswing()
        {
            var t = Mathf.Clamp01(_stateTime / _strikeSeconds);
            _swing = Mathf.Lerp(_strikeFrom, 0f, t * t);
            if (t < 1f)
            {
                return;
            }

            BeginFollow();
            var impact = _onImpact;
            _onImpact = null;
            impact?.Invoke();
        }

        /// <summary>当たった勢いのまま振り抜き（だんだん遅く）、しばらく止めてから見送りへ</summary>
        private void TickFollow()
        {
            var t = Mathf.Clamp01(_stateTime / FollowSeconds);
            _swing = Mathf.Lerp(_strikeFrom, _followTo, 1f - (1f - t) * (1f - t));
            if (_stateTime >= FollowSeconds + FollowHoldSeconds)
            {
                _watchTarget = _current.Root.position + _current.Root.forward;
                Enter(State.Watch);
            }
        }

        private void TickWatch(float dt)
        {
            _swing = Mathf.MoveTowards(_swing, WatchSwing, WatchSettleSpeed * dt);
            var toBall = _watchTarget - _current.Root.position;
            toBall.y = 0f;
            if (toBall.sqrMagnitude > GolfBalance.BallRadius * GolfBalance.BallRadius)
            {
                _current.Motion.Facing = toBall;
            }
        }

        private enum State
        {
            Hidden,
            Address,
            Downswing,
            Follow,
            Watch,
            React,
        }
    }
}
