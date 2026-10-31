using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Tests
{
    /// <summary>ステージを3Dで描く部分。座標の置き方・カメラの構え・床に寝かせる向き・ユニットの目印</summary>
    public class Stage3DTests
    {
        private const float Tolerance = 1e-4f;

        private GameObject _parent;
        private StageMaterials _materials;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("Stage3DTests");
            _materials = new StageMaterials(Shader.Find("PenmonScramble/Toon"), Shader.Find("PenmonScramble/BlobShadow"), Shader.Find("PenmonScramble/Fx"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_parent);
            _materials.DestroyAll();
        }

        [Test]
        public void CorePosition_IsPlacedOnFloor()
        {
            Assert.That(Vector3.Distance(new Vec2(3f, -2f).ToFloor(), new Vector3(3f, 0f, -2f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(new Vector2(1f, 4f).ToFloor(0.5f), new Vector3(1f, 0.5f, 4f)), Is.LessThan(Tolerance));
        }

        [TestCase(16f / 9f)]
        [TestCase(21f / 9f)]
        [TestCase(4f / 3f)]
        public void StageCamera_ShowsWholeAreaAndHeadroom(float aspect)
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(_parent.transform, false);
            camera.aspect = aspect;
            camera.fieldOfView = StageCamera.FieldOfView;
            var (position, rotation) = AreaCameraFit.PerspectivePose(aspect, Vector3.zero, new Vector2(Balance.AreaWidth, Balance.AreaHeight), StageCamera.Pitch, StageCamera.FieldOfView, StageCamera.Headroom, StageCamera.Margin);
            camera.transform.SetPositionAndRotation(position, rotation);

            for (var corner = 0; corner < 8; corner++)
            {
                var x = (corner & 1) == 0 ? -Balance.AreaWidth * 0.5f : Balance.AreaWidth * 0.5f;
                var z = (corner & 2) == 0 ? -Balance.AreaHeight * 0.5f : Balance.AreaHeight * 0.5f;
                var y = (corner & 4) == 0 ? 0f : StageCamera.Headroom;
                var viewport = camera.WorldToViewportPoint(new Vector3(x, y, z));
                Assert.That(viewport.z, Is.GreaterThan(0f), $"角 {corner} がカメラの後ろ");
                Assert.That(viewport.x, Is.InRange(0f, 1f), $"角 {corner} が左右にはみ出す");
                Assert.That(viewport.y, Is.InRange(StageCamera.Margin * 0.5f - Tolerance, 1f - StageCamera.Margin * 0.5f + Tolerance), $"角 {corner} が HUD の余白に入る");
            }
        }

        [Test]
        public void StageCamera_FarSideIsUpOnScreen()
        {
            // Core の y（奥）が画面の上になる。入力の向き（スティックの上 = Core の +y）と画面の向きがそろう
            var (position, rotation) = AreaCameraFit.PerspectivePose(16f / 9f, Vector3.zero, new Vector2(Balance.AreaWidth, Balance.AreaHeight), StageCamera.Pitch, StageCamera.FieldOfView, StageCamera.Headroom, StageCamera.Margin);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(_parent.transform, false);
            camera.aspect = 16f / 9f;
            camera.fieldOfView = StageCamera.FieldOfView;
            camera.transform.SetPositionAndRotation(position, rotation);

            var near = camera.WorldToViewportPoint(new Vec2(0f, -1f).ToFloor());
            var far = camera.WorldToViewportPoint(new Vec2(0f, 1f).ToFloor());
            var right = camera.WorldToViewportPoint(new Vec2(1f, 0f).ToFloor());
            Assert.That(far.y, Is.GreaterThan(near.y));
            Assert.That(right.x, Is.GreaterThan(0.5f));
        }

        [TestCase(1f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(-1f, 0.5f)]
        [TestCase(0.3f, -1f)]
        public void FloorDecal_LiesFlatAndPointsAlongDirection(float x, float y)
        {
            var direction = new Vector2(x, y);
            var rotation = FloorDecal.Facing(FloorDecal.YawOf(direction));

            Assert.That(Vector3.Distance(rotation * Vector3.right, direction.normalized.ToFloor()), Is.LessThan(Tolerance), "絵の右が向ける方向を向く");
            Assert.That(Vector3.Distance(rotation * Vector3.back, Vector3.up), Is.LessThan(Tolerance), "絵の表が上を向く");
        }

        [Test]
        public void StarMesh_FacesPointOutward()
        {
            var mesh = ToyMeshes.Star;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                // Unity は時計回りが表。表の向きは (b - a) × (c - a)
                var normal = Vector3.Cross(b - a, c - a);
                var center = (a + b + c) / 3f;
                Assert.That(Mathf.Sign(normal.z), Is.EqualTo(Mathf.Sign(center.z)), $"三角形 {i / 3} が内側を向いている");
            }
        }

        [Test]
        public void PlayerUnit_HasLabelAboveHeadAndReviveAreaWhileDown()
        {
            var unit = new UnitState(1, Team.Player, 0, 1, new Vec2(2f, 1f), Vec2.Right, 100, 100, Balance.PenguinRadius, UnitFlags.None);
            var view = UnitView.Create(_parent.transform, _materials, unit, Color.red, "P1");

            Assert.That(Vector3.Distance(view.transform.position, new Vector3(2f, 0f, 1f)), Is.LessThan(Tolerance));
            var label = view.GetComponentInChildren<TextMesh>();
            Assert.That(label.text, Is.EqualTo("P1"));
            var model = view.GetComponentInChildren<PenguinModel>();
            Assert.That(label.transform.position.y, Is.GreaterThan(model.TopHeight * model.transform.localScale.y));
            Assert.That(ReviveRing(view).enabled, Is.False);

            var down = new UnitState(1, Team.Player, 0, 1, new Vec2(2f, 1f), Vec2.Right, 0, 100, Balance.PenguinRadius, UnitFlags.Down, 0.5f);
            view.PushState(down, Balance.TickSeconds);
            view.Render(1f);
            Assert.That(ReviveRing(view).enabled, Is.True);
        }

        [Test]
        public void EnemyUnit_HasNoMarks()
        {
            var unit = new UnitState(2, Team.Enemy, 0, 3, Vec2.Zero, Vec2.Right, 50, 50, Balance.PenguinRadius, UnitFlags.None);
            var view = UnitView.Create(_parent.transform, _materials, unit, null, null);

            Assert.That(view.GetComponentInChildren<TextMesh>(), Is.Null);
            Assert.That(view.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
            Assert.That(view.GetComponentInChildren<Collider>(), Is.Null, "当たりは Core が持つので Collider は付けない");
        }

        private static SpriteRenderer ReviveRing(UnitView view)
        {
            foreach (var renderer in view.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == "ReviveRing")
                {
                    return renderer;
                }
            }

            Assert.Fail("復活の輪が無い");
            return null;
        }
    }
}
