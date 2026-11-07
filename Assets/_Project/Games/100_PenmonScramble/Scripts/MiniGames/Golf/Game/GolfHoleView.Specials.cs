using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 特殊ショットの見た目: 氷は球の通った後に凍った道（水色の平たい板）を残し、番が替わると溶けて消える。
    /// 水切りで跳ねた所・雷で加速した所には輪を広げる（池の水しぶきと同じ動き）
    /// </summary>
    public partial class GolfHoleView
    {
        // 凍った道の板を置く間と大きさ（幅は球の直径の約1.7倍）
        private const float IceSpacing = 0.22f;
        private const float IceWidth = GolfBalance.BallRadius * 3.4f;
        private const float IceLift = 0.03f;
        private const float IceMeltSeconds = 0.6f;

        private static readonly Color IceColor = new Color(0.80f, 0.95f, 1f);
        private static readonly Color BoostColor = new Color(1f, 0.92f, 0.3f);
        private static readonly Color SkipColor = new Color(0.95f, 0.98f, 1f);

        private readonly List<Transform> _ice = new List<Transform>();
        private Vec2 _lastIce;
        private bool _hasIce;
        private float _melt;

        /// <summary>毎フレーム、打った球に氷が効いている間だけ通り道に板を置く</summary>
        public void TrackIce(GolfBall ball)
        {
            if (ball.IceLeft <= 0f || ball.IsAirborne || !ball.IsInPlay)
            {
                _hasIce = false;
                return;
            }

            if (_hasIce && Vec2.Distance(_lastIce, ball.Position) < IceSpacing)
            {
                return;
            }

            var along = _hasIce ? GolfSpace.ToWorld(ball.Position - _lastIce) : GolfSpace.ToWorld(ball.Velocity);
            var rotation = along.sqrMagnitude > 0f ? Quaternion.LookRotation(along) : Quaternion.identity;
            var part = AddPart("Ice", ToyMeshes.Puck, _course, GroundPoint(ball.Position) + Vector3.up * IceLift, rotation,
                new Vector3(IceWidth, 0.02f, IceWidth), Materials.Get(IceColor, 0.8f));
            _ice.Add(part);
            _lastIce = ball.Position;
            _hasIce = true;
        }

        /// <summary>番が替わったら、凍った道を溶かして消す</summary>
        public void MeltIce()
        {
            _hasIce = false;
            if (_ice.Count > 0)
            {
                _melt = IceMeltSeconds;
            }
        }

        public void SkipAt(Vec2 point, bool onWater)
        {
            if (onWater)
            {
                SplashAt(point);
                return;
            }

            BurstAt(point, SkipColor);
        }

        public void BoostAt(Vec2 point) => BurstAt(point, BoostColor);

        private void BurstAt(Vec2 point, Color color)
        {
            var ring = AddPart("Burst", ToyMeshes.Torus, _course, GroundPoint(point) + Vector3.up * AreaLift * 4f, Quaternion.identity,
                Vector3.one * SplashStartSize, Materials.Get(color, 0.6f));
            _splashes.Add(new Splash(ring));
        }

        private void TickSpecials(float dt)
        {
            if (_melt <= 0f)
            {
                return;
            }

            _melt -= dt;
            var scale = Mathf.Clamp01(_melt / IceMeltSeconds);
            foreach (var part in _ice)
            {
                part.localScale = new Vector3(IceWidth * scale, 0.02f, IceWidth * scale);
            }

            if (_melt <= 0f)
            {
                ClearSpecials();
            }
        }

        private void ClearSpecials()
        {
            foreach (var part in _ice)
            {
                if (part != null)
                {
                    Destroy(part.gameObject);
                }
            }

            _ice.Clear();
            _hasIce = false;
            _melt = 0f;
        }
    }
}
