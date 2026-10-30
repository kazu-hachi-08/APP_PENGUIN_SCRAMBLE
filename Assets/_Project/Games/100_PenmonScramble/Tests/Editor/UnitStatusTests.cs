using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Tests
{
    /// <summary>
    /// ユニットの状態の見た目（PLAN_3D フェーズ4）: 敵は暗く・被弾で白く・凍ると水色と氷・ダウンは灰色・無敵は点滅・突進の残像・交代の煙。
    /// 見た目の良し悪しはスクショで人が決めるので、ここでは状態が見た目に出ているかだけを見る
    /// </summary>
    public class UnitStatusTests
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private const int Species = 1;

        private GameObject _parent;
        private StageMaterials _materials;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("UnitStatusTests");
            _materials = new StageMaterials(Shader.Find("PenmonScramble/Toon"), Shader.Find("PenmonScramble/BlobShadow"), Shader.Find("PenmonScramble/Fx"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_parent);
            _materials.DestroyAll();
        }

        [Test]
        public void Enemy_IsTheSameShapeInDarkerColors()
        {
            var player = Create(Team.Player, UnitFlags.None);
            var enemy = Create(Team.Enemy, UnitFlags.None);

            Assert.That(Brightness(PartColor(enemy, "Belly")), Is.LessThan(Brightness(PartColor(player, "Belly")) * 0.7f));
            Assert.That(Model(enemy).GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(Model(player).GetComponentsInChildren<MeshRenderer>().Length));
        }

        [Test]
        public void Hit_FlashesWhiteForAMoment()
        {
            var view = Create(Team.Player, UnitFlags.None);
            var back = PartColor(view, "Torso");

            view.Flash();
            view.TickStatus(0.02f);
            Assert.That(Brightness(PartColor(view, "Torso")), Is.GreaterThan(0.8f), "黒い背中も白く光る");

            view.TickStatus(0.2f);
            Assert.That(PartColor(view, "Torso"), Is.EqualTo(back));
        }

        [Test]
        public void Frozen_TurnsIcyInsideAnIceBlock_AndShattersWhenThawed()
        {
            var view = Create(Team.Enemy, UnitFlags.None);
            Assert.That(view.GetComponentInChildren<IceBlock>(), Is.Null, "凍らない子の分まで氷を作らない");

            Push(view, Team.Enemy, UnitFlags.Frozen);
            var ice = view.GetComponentInChildren<IceBlock>();
            Assert.That(ice, Is.Not.Null);
            Assert.That(ice.IsShown, Is.True);
            Assert.That(view.GetComponentInChildren<PenguinMotion>().Frozen, Is.True);
            var belly = PartColor(view, "Belly");
            Assert.That(belly.b, Is.GreaterThan(belly.r), "水色になる");
            var model = Model(view);
            Assert.That(ice.transform.localScale.y, Is.GreaterThan(model.TopHeight * model.transform.localScale.y * 0.5f), "体を包む大きさ");

            Push(view, Team.Enemy, UnitFlags.None);
            Assert.That(ice.IsShown, Is.False);
            Assert.That(_parent.GetComponentInChildren<IceShatter>(), Is.Not.Null, "溶けたら砕ける");
            Assert.That(view.GetComponentInChildren<PenguinMotion>().Frozen, Is.False);
        }

        [Test]
        public void Down_TurnsGray()
        {
            var view = Create(Team.Player, UnitFlags.None);
            var feet = PartColor(view, "Foot");

            Push(view, Team.Player, UnitFlags.Down);
            var gray = PartColor(view, "Foot");
            Assert.That(Saturation(gray), Is.LessThan(Saturation(feet) * 0.6f), "オレンジの足も灰色っぽくなる");
        }

        [Test]
        public void Invincible_Blinks()
        {
            var view = Create(Team.Player, UnitFlags.Invincible);
            var figure = Model(view).Figure.gameObject;
            var shown = false;
            var hidden = false;
            for (var i = 0; i < 20; i++)
            {
                view.TickStatus(0.01f);
                shown |= figure.activeSelf;
                hidden |= !figure.activeSelf;
            }

            Assert.That(shown && hidden, Is.True, "見えたり消えたりする");

            Push(view, Team.Player, UnitFlags.None);
            Assert.That(figure.activeSelf, Is.True, "無敵が切れたら見えたまま");
        }

        [Test]
        public void Dashing_LeavesAfterimagesThatFadeAway()
        {
            var view = Create(Team.Player, UnitFlags.None);
            Assert.That(view.GetComponent<AfterimageTrail>(), Is.Null, "突進しない子の分まで作らない");

            Push(view, Team.Player, UnitFlags.Dashing);
            var trail = view.GetComponent<AfterimageTrail>();
            Assert.That(trail, Is.Not.Null);
            trail.Tick(0.05f);
            trail.Tick(0.05f);
            Assert.That(trail.VisibleCount, Is.EqualTo(1), "止まったままなら重ねて置かない");

            for (var i = 1; i <= 4; i++)
            {
                view.PushState(State(Team.Player, UnitFlags.Dashing, position: new Vec2(i * 0.4f, 0f)), Balance.TickSeconds);
                view.Render(1f);
                trail.Tick(0.05f);
            }

            Assert.That(trail.VisibleCount, Is.GreaterThan(1), "動いた分だけ間をあけて置く");
            Assert.That(_parent.transform.Find("Afterimage"), Is.Not.Null, "写しはユニットの外に置く（ユニットと一緒に動かない）");

            Push(view, Team.Player, UnitFlags.None);
            trail.Tick(0.5f);
            Assert.That(trail.VisibleCount, Is.EqualTo(0));
        }

        [Test]
        public void Switching_PuffsSmokeAndPopsTheNewPenguinIn()
        {
            var view = Create(Team.Player, UnitFlags.None);
            var fullScale = PenguinModelBuilder.ScaleForRadius(Balance.PenguinRadius);

            view.PushState(State(Team.Player, UnitFlags.None, species: 2), Balance.TickSeconds);
            Assert.That(view.SpeciesNo, Is.EqualTo(2));
            Assert.That(_parent.GetComponentInChildren<SmokePuff>(), Is.Not.Null);
            Assert.That(Model(view).transform.localScale.x, Is.LessThan(fullScale * 0.9f), "小さく出てくる");

            view.TickStatus(0.5f);
            Assert.That(Model(view).transform.localScale.x, Is.EqualTo(fullScale).Within(1e-5f));
        }

        [Test]
        public void Pop_StartsSmallOvershootsAndSettles()
        {
            Assert.That(ToyEasing.Pop(0f, 0.5f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(ToyEasing.Pop(1f, 0.5f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(ToyEasing.Pop(2f, 0.5f), Is.EqualTo(1f).Within(1e-5f));
            var max = 0f;
            for (var t = 0f; t < 1f; t += 0.01f)
            {
                max = Mathf.Max(max, ToyEasing.Pop(t, 0.5f));
            }

            Assert.That(max, Is.GreaterThan(1.05f), "一度はみ出す");
        }

        [Test]
        public void IceChunkMesh_FacesPointOutward()
        {
            var mesh = ToyMeshes.IceChunk;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var center = Vector3.up * 0.5f;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a);
                Assert.That(Vector3.Dot(normal, (a + b + c) / 3f - center), Is.GreaterThan(0f), $"三角形 {i / 3} が内側を向いている");
            }

            Assert.That(mesh.bounds.min.y, Is.EqualTo(0f).Within(1e-5f), "底が原点");
            Assert.That(mesh.bounds.max.y, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Tint_DarkensThenMovesTowardTheTarget()
        {
            var color = new Color(1f, 0.5f, 0f, 1f);
            Assert.That(PenguinTint.None.Apply(color), Is.EqualTo(color));
            Assert.That(PenguinTint.Dark(0.5f).Apply(color), Is.EqualTo(new Color(0.5f, 0.25f, 0f, 1f)));
            Assert.That(PenguinTint.Dark(0.5f).Toward(Color.white, 1f).Apply(color), Is.EqualTo(Color.white));
        }

        private UnitView Create(Team team, UnitFlags flags)
        {
            Color? seat = team == Team.Player ? Color.red : (Color?)null;
            var view = UnitView.Create(_parent.transform, _materials, State(team, flags), seat, team == Team.Player ? "P1" : null);
            view.TickStatus(0f);
            return view;
        }

        private static void Push(UnitView view, Team team, UnitFlags flags)
        {
            view.PushState(State(team, flags), Balance.TickSeconds);
            view.TickStatus(0f);
        }

        private static UnitState State(Team team, UnitFlags flags, int species = Species, Vec2 position = default)
        {
            return new UnitState(1, team, 0, species, position, Vec2.Right, 100, 100, Balance.PenguinRadius, flags);
        }

        private static PenguinModel Model(UnitView view) => view.GetComponentInChildren<PenguinModel>(true);

        private static Color PartColor(UnitView view, string partName)
        {
            foreach (var renderer in view.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name == partName)
                {
                    return renderer.sharedMaterial.GetColor(BaseColorId);
                }
            }

            Assert.Fail($"部品 {partName} が無い");
            return default;
        }

        private static float Brightness(Color color) => color.maxColorComponent;

        private static float Saturation(Color color)
        {
            Color.RGBToHSV(color, out _, out var s, out _);
            return s;
        }
    }
}
