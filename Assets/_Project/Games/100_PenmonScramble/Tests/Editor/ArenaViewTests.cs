using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Tests
{
    /// <summary>闘技場の見せ方: カメラの追い方と引き・画面の端の印の置き場所・柵の並べ方</summary>
    public class ArenaViewTests
    {
        private const float Tolerance = 1e-3f;
        private const float Padding = 1.5f;
        private const float MaxZoom = 1.3f;

        private static readonly Vector2 View = new Vector2(Balance.AreaWidth, Balance.AreaHeight);

        private static AreaShape Arena()
        {
            var half = new Vec2(Balance.ArenaWidth, Balance.ArenaHeight) * 0.5f;
            return AreaShape.Ellipse(-half, half);
        }

        private static (Vector2 center, float zoom) Follow(AreaShape bounds, params Vector2[] players)
        {
            return CameraFraming.FollowPlayers(new List<Vector2>(players), bounds.Min.ToVector2(), bounds.Max.ToVector2(), View, Padding, MaxZoom);
        }

        [Test]
        public void Arena_OnePlayerInTheMiddle_IsFollowedWithoutZoom()
        {
            var (center, zoom) = Follow(Arena(), new Vector2(2f, -1f));

            Assert.That(zoom, Is.EqualTo(1f));
            Assert.That(Vector2.Distance(center, new Vector2(2f, -1f)), Is.LessThan(Tolerance));
        }

        [TestCase(-15.5f, 0f)]
        [TestCase(15.5f, 0f)]
        [TestCase(0f, 8.5f)]
        [TestCase(0f, -8.5f)]
        [TestCase(-12f, 5f)]
        public void Arena_CameraStopsBeforeShowingOutsideTheArena(float x, float y)
        {
            var arena = Arena();
            var (center, zoom) = Follow(arena, new Vector2(x, y));
            var half = View * 0.5f * zoom;

            Assert.That(center.x - half.x, Is.GreaterThanOrEqualTo(arena.Min.X - Tolerance));
            Assert.That(center.x + half.x, Is.LessThanOrEqualTo(arena.Max.X + Tolerance));
            Assert.That(center.y - half.y, Is.GreaterThanOrEqualTo(arena.Min.Y - Tolerance));
            Assert.That(center.y + half.y, Is.LessThanOrEqualTo(arena.Max.Y + Tolerance));
        }

        [Test]
        public void Arena_PlayersFarApart_ZoomOutOnlyUpToTheLimit()
        {
            var (_, near) = Follow(Arena(), new Vector2(-4f, 0f), new Vector2(4f, 0f));
            var (_, apart) = Follow(Arena(), new Vector2(-7f, 0f), new Vector2(7f, 0f));
            var (_, far) = Follow(Arena(), new Vector2(-14f, 0f), new Vector2(14f, 0f));

            Assert.That(near, Is.EqualTo(1f));
            Assert.That(apart, Is.GreaterThan(1f).And.LessThan(MaxZoom));
            Assert.That(far, Is.EqualTo(MaxZoom));
        }

        [Test]
        public void Arena_TwoPlayers_CenterIsBetweenThem()
        {
            var (center, _) = Follow(Arena(), new Vector2(-3f, 1f), new Vector2(5f, -1f));

            Assert.That(Vector2.Distance(center, new Vector2(1f, 0f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void NormalArea_IsExactlyOneScreen_SoCameraStaysInTheMiddle()
        {
            var area = StageLayout.Area(null, 0);
            var (center, zoom) = CameraFraming.FollowPlayers(new List<Vector2> { new Vector2(-7f, 4f) }, area.Min.ToVector2(), area.Max.ToVector2(), View, Padding, 1f);

            Assert.That(zoom, Is.EqualTo(1f));
            Assert.That(Vector2.Distance(center, area.Center.ToVector2()), Is.LessThan(Tolerance));
        }

        [Test]
        public void NoPlayers_LooksAtTheMiddleOfTheBounds()
        {
            var arena = Arena();
            var (center, zoom) = Follow(arena);

            Assert.That(zoom, Is.EqualTo(1f));
            Assert.That(Vector2.Distance(center, arena.Center.ToVector2()), Is.LessThan(Tolerance));
        }

        [Test]
        public void ArenaIntro_ZoomShowsTheWholeArena()
        {
            var arena = Arena();

            Assert.That(CameraFraming.ZoomToShow(new Vector2(arena.Width, arena.Height), View), Is.EqualTo(2f).Within(Tolerance));
            Assert.That(CameraFraming.ZoomToShow(View * 0.5f, View), Is.EqualTo(1f));
        }

        /// <summary>腕を土台から zoom 倍に遠ざけると、床に映る範囲がそのまま zoom 倍になる（StageCamera の引き方）</summary>
        [TestCase(16f / 9f)]
        [TestCase(21f / 9f)]
        public void ZoomedArm_ShowsTheWholeArena(float aspect)
        {
            var parent = new GameObject("ArenaViewTests");
            try
            {
                var camera = parent.AddComponent<Camera>();
                camera.aspect = aspect;
                camera.fieldOfView = StageCamera.FieldOfView;
                var (position, rotation) = AreaCameraFit.PerspectivePose(aspect, Vector3.zero, View, StageCamera.Pitch, StageCamera.FieldOfView, StageCamera.Headroom, StageCamera.Margin);
                var zoom = CameraFraming.ZoomToShow(new Vector2(Balance.ArenaWidth, Balance.ArenaHeight), View);
                camera.transform.SetPositionAndRotation(position * zoom, rotation);

                foreach (var corner in new[] { new Vector3(-16f, 0f, -9f), new Vector3(16f, 0f, -9f), new Vector3(-16f, 0f, 9f), new Vector3(16f, 0f, 9f) })
                {
                    var viewport = camera.WorldToViewportPoint(corner);
                    Assert.That(viewport.x, Is.InRange(0f, 1f), $"{corner}");
                    Assert.That(viewport.y, Is.InRange(0f, 1f), $"{corner}");
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void EdgeMarker_OnlyForPointsOutsideTheScreen()
        {
            var screen = new Vector2(1920f, 1080f);

            Assert.That(EdgeMarkerPlacement.IsOnScreen(new Vector3(960f, 540f, 10f), screen, 24f), Is.True);
            Assert.That(EdgeMarkerPlacement.IsOnScreen(new Vector3(10f, 540f, 10f), screen, 24f), Is.False, "端で切れかけている");
            Assert.That(EdgeMarkerPlacement.IsOnScreen(new Vector3(2400f, 540f, 10f), screen, 24f), Is.False);
            Assert.That(EdgeMarkerPlacement.IsOnScreen(new Vector3(960f, 540f, -10f), screen, 24f), Is.False, "カメラの後ろ");
        }

        [Test]
        public void EdgeMarker_SitsOnTheEdgeAndPointsAtTheTarget()
        {
            var box = Rect.MinMaxRect(-800f, -400f, 800f, 300f);

            var (right, rightAngle) = EdgeMarkerPlacement.PlaceOnEdge(box, new Vector2(3000f, -50f));
            Assert.That(right.x, Is.EqualTo(800f).Within(Tolerance));
            Assert.That(rightAngle, Is.EqualTo(0f).Within(1f));

            var (top, topAngle) = EdgeMarkerPlacement.PlaceOnEdge(box, new Vector2(-50f, 5000f));
            Assert.That(top.y, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(topAngle, Is.EqualTo(90f).Within(1f));

            var (corner, cornerAngle) = EdgeMarkerPlacement.PlaceOnEdge(box, new Vector2(-5000f, -5000f));
            var onLeft = Mathf.Abs(corner.x - box.xMin) < Tolerance && corner.y >= box.yMin - Tolerance;
            var onBottom = Mathf.Abs(corner.y - box.yMin) < Tolerance && corner.x >= box.xMin - Tolerance;
            Assert.That(onLeft || onBottom, Is.True, $"{corner}");
            Assert.That(cornerAngle, Is.LessThan(-90f));

            var (inside, _) = EdgeMarkerPlacement.PlaceOnEdge(box, new Vector2(100f, 0f));
            Assert.That(Vector2.Distance(inside, new Vector2(100f, 0f)), Is.LessThan(Tolerance), "枠の中ならそのまま");
        }

        [Test]
        public void EdgeMarker_BehindTheCamera_PointsTheOtherWay()
        {
            var screen = new Vector2(1920f, 1080f);

            var facing = EdgeMarkerPlacement.FacingPoint(new Vector3(1900f, 540f, -5f), screen);

            Assert.That(facing.x, Is.LessThan(screen.x * 0.5f));
        }

        [Test]
        public void Fence_PostsLieOnTheEllipse_EvenlySpaced_StartingAtTheEntrance()
        {
            var arena = Arena();
            var posts = ArenaFence.PostPoints(arena, ArenaFence.PostSpacing);
            var a = arena.Width * 0.5f;
            var b = arena.Height * 0.5f;

            Assert.That(Vector2.Distance(posts[0], new Vector2(arena.Min.X, arena.Center.Y)), Is.LessThan(Tolerance), "左端（入口）から");
            for (var i = 0; i < posts.Count; i++)
            {
                var p = posts[i];
                Assert.That((p.x / a) * (p.x / a) + (p.y / b) * (p.y / b), Is.EqualTo(1f).Within(0.01f));
                var gap = Vector2.Distance(p, posts[(i + 1) % posts.Count]);
                Assert.That(gap, Is.EqualTo(ArenaFence.PostSpacing).Within(0.1f));
            }
        }

        [Test]
        public void Fence_DoorwayPosts_AreFewAndSymmetric()
        {
            var arena = StageLayout.Area(StageTable.Get(3), StageTable.Get(3).Areas.Count - 1);
            var doorway = StageLayout.Doorway(arena);
            var posts = ArenaFence.PostPoints(arena, ArenaFence.PostSpacing);

            var door = posts.FindAll(p => ArenaFence.IsInDoorway(p, doorway));

            Assert.That(door.Count, Is.InRange(1, 5));
            Assert.That(door.Exists(p => Mathf.Abs(p.y - arena.Center.Y) < Tolerance), Is.True, "入口の真ん中の柱");
            foreach (var p in door)
            {
                Assert.That(door.Exists(q => Mathf.Abs(q.x - p.x) < 0.01f && Mathf.Abs((q.y - arena.Center.Y) + (p.y - arena.Center.Y)) < 0.01f), Is.True, "上下で対称");
            }

            // 入口の柱を除くと、通り道（縦 3 マス）より広く開く（反対の右端の柱は数えない）
            var nearestOutside = float.MaxValue;
            foreach (var p in posts)
            {
                if (!door.Contains(p) && p.x < arena.Center.X)
                {
                    nearestOutside = Mathf.Min(nearestOutside, Mathf.Abs(p.y - arena.Center.Y));
                }
            }

            Assert.That(nearestOutside, Is.GreaterThan(Balance.ArenaDoorHalfWidth));
        }
    }
}
