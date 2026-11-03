using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 壁: 角の丸い柱を寝かせた、おもちゃの柵（本体と、上の色の帯）。種類で色を変える:
    /// ふつう・低い壁＝ワールドの柵の色（本編の闘技場の柵と同じ）、バンパー＝黄色に赤い帯（当たると弾む）、柔らかい壁＝水色のスポンジ
    /// </summary>
    public partial class GolfHoleView
    {
        private const float WallThickness = 0.16f;
        private const float WallGloss = 0.35f;
        private const float CapHeight = 0.07f;
        private const float CapWiden = 1.3f;

        // バンパーが当たって弾む: 長さ・膨らむ割合。当たった所からこの距離までの壁を弾ませる
        private const float PopSeconds = 0.22f;
        private const float PopScale = 0.35f;
        private const float PopReach = GolfBalance.BallRadius * 2.5f;

        private static readonly Color BumperColor = new Color(1f, 0.82f, 0.22f);
        private static readonly Color SoftWallColor = new Color(0.60f, 0.82f, 0.98f);

        private readonly List<WallPart> _walls = new List<WallPart>();
        private readonly List<WallPart> _wallPops = new List<WallPart>();

        private void BuildWalls(GolfHoleDef hole, StageTheme theme)
        {
            _walls.Clear();
            foreach (var wall in hole.Walls)
            {
                _walls.Add(BuildWall(wall, theme));
            }
        }

        private WallPart BuildWall(GolfWall wall, StageTheme theme)
        {
            var along = GolfSpace.ToWorld(wall.B - wall.A);
            var root = new GameObject("Wall").transform;
            root.SetParent(_course, false);
            root.localPosition = GolfSpace.ToWorld((wall.A + wall.B) * 0.5f);
            root.localRotation = Quaternion.LookRotation(along);

            var kind = KindOf(wall);
            var body = kind == WallKind.Bumper ? BumperColor : kind == WallKind.Soft ? SoftWallColor : theme.FencePost;
            // 両端は丸いので、壁の厚みだけ長くして角を埋める
            var length = along.magnitude + WallThickness;
            AddPart("Body", ToyMeshes.Puck, root, Vector3.up * (wall.Height * 0.5f), Quaternion.identity,
                new Vector3(WallThickness, wall.Height, length), Materials.Get(body, WallGloss));
            if (kind != WallKind.Soft)
            {
                var cap = kind == WallKind.Bumper ? theme.FenceCap : theme.FenceRail;
                AddPart("Cap", ToyMeshes.Puck, root, Vector3.up * wall.Height, Quaternion.identity,
                    new Vector3(WallThickness * CapWiden, CapHeight, length + WallThickness * (CapWiden - 1f)), Materials.Get(cap, WallGloss));
            }

            return new WallPart(wall, root, kind == WallKind.Bumper);
        }

        private static WallKind KindOf(GolfWall wall)
        {
            if (wall.Bounce > 1f)
            {
                return WallKind.Bumper;
            }

            return wall.Bounce < GolfBalance.WallBounce ? WallKind.Soft : WallKind.Normal;
        }

        /// <summary>球が当たった所のバンパーを弾ませる（ふつうの壁は動かさない）</summary>
        public void PopWallNear(Vec2 point)
        {
            foreach (var part in _walls)
            {
                if (!part.Bumper)
                {
                    continue;
                }

                var closest = GolfGeometry.ClosestOnSegment(part.Wall.A, part.Wall.B, point);
                if (Vec2.Distance(closest, point) <= PopReach)
                {
                    part.PopLeft = PopSeconds;
                    if (!_wallPops.Contains(part))
                    {
                        _wallPops.Add(part);
                    }
                }
            }
        }

        private void TickWallPops(float dt)
        {
            for (var i = _wallPops.Count - 1; i >= 0; i--)
            {
                var part = _wallPops[i];
                part.PopLeft -= dt;
                var t = 1f - Mathf.Clamp01(part.PopLeft / PopSeconds);
                var swell = Mathf.Sin(t * Mathf.PI) * PopScale;
                part.Root.localScale = new Vector3(1f + swell, 1f + swell * 0.5f, 1f);
                if (part.PopLeft <= 0f)
                {
                    part.Root.localScale = Vector3.one;
                    _wallPops.RemoveAt(i);
                }
            }
        }

        private enum WallKind
        {
            Normal,
            Soft,
            Bumper,
        }

        private sealed class WallPart
        {
            public readonly GolfWall Wall;
            public readonly Transform Root;
            public readonly bool Bumper;
            public float PopLeft;

            public WallPart(GolfWall wall, Transform root, bool bumper)
            {
                Wall = wall;
                Root = root;
                Bumper = bumper;
            }
        }
    }
}
