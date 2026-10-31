using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Tests
{
    /// <summary>3D のペンギンの動き。見た目の良し悪しはスクショで人が決めるので、ここでは壊れ方だけを見る</summary>
    public class PenguinMotionTests
    {
        private const float Step = 1f / 60f;
        // 部品が床にめり込んでよい深さ（足の裏の丸みの分）
        private const float FloorTolerance = 0.03f;

        private ToonMaterials _materials;
        private GameObject _parent;

        [SetUp]
        public void SetUp()
        {
            _materials = new ToonMaterials(Shader.Find("PenmonScramble/Toon"), Shader.Find("PenmonScramble/BlobShadow"));
            _parent = new GameObject("PenguinMotionTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_parent);
            _materials.DestroyAll();
        }

        private PenguinMotion Build(int speciesNo = 1)
        {
            var model = PenguinModelBuilder.Build(PenguinLooks.Get(speciesNo), _materials, _parent.transform);
            return model.gameObject.AddComponent<PenguinMotion>();
        }

        private static void Run(PenguinMotion motion, float seconds)
        {
            for (var t = 0f; t < seconds; t += Step)
            {
                motion.Tick(Step);
            }
        }

        [Test]
        public void AfterEveryMotion_IdlePoseReturnsToRest()
        {
            var motion = Build();
            motion.Apply(PenguinPose.Idle);
            var rest = Snapshot(motion);

            motion.Down = true;
            motion.PlayAttack(SkillShape.Nova);
            motion.PlaySpecial();
            motion.PlayHit();
            motion.PlayKnockback(Vector3.right * 10f);
            Run(motion, 0.3f);
            motion.Apply(PenguinPose.Idle);

            var after = Snapshot(motion);
            for (var i = 0; i < rest.Length; i++)
            {
                Assert.That(Vector3.Distance(rest[i], after[i]), Is.LessThan(1e-4f), $"部品 {i} に前の動きが残っている");
            }
        }

        [Test]
        public void AllMotions_StayAboveTheFloor([Values(SkillShape.Melee, SkillShape.Projectile, SkillShape.Dash, SkillShape.Spread, SkillShape.Nova, SkillShape.Zone, SkillShape.Drop)] SkillShape shape)
        {
            var motion = Build();
            motion.PlayAttack(shape);
            AssertAboveFloorWhile(motion, PenguinMotion.AttackSeconds, shape.ToString());
        }

        [Test]
        public void LyingDown_TouchesTheFloorWithoutSinking()
        {
            var motion = Build();
            var standingHead = motion.GetComponent<PenguinModel>().Head.position.y;
            motion.Down = true;
            AssertAboveFloorWhile(motion, 1.5f, "ダウン");

            Assert.That(LowestPoint(motion), Is.LessThan(0.1f), "倒れたのに浮いている");
            Assert.That(motion.GetComponent<PenguinModel>().Head.position.y, Is.LessThan(standingHead * 0.7f), "倒れていない");
        }

        [Test]
        public void Sliding_And_Special_StayAboveTheFloor()
        {
            var motion = Build();
            motion.Sliding = true;
            AssertAboveFloorWhile(motion, 0.6f, "すべる");
            motion.ResetPose();
            motion.PlaySpecial();
            AssertAboveFloorWhile(motion, PenguinMotion.SpecialSeconds, "必殺技");
        }

        [Test]
        public void Knockback_RollsAndLandsUpright()
        {
            var motion = Build();
            var figure = motion.GetComponent<PenguinModel>().Figure;
            motion.PlayKnockback(Vector3.right * 12f);
            var tilted = false;
            for (var t = 0f; t < 1.2f; t += Step)
            {
                motion.Tick(Step);
                tilted |= Vector3.Angle(figure.up, Vector3.up) > 60f;
            }

            Assert.That(tilted, Is.True, "強く飛ばされたのに転がっていない");
            Assert.That(Vector3.Angle(figure.up, Vector3.up), Is.LessThan(1f), "転がり終わっても立っていない");
            Assert.That(figure.localPosition.magnitude, Is.LessThan(1e-3f));
        }

        [Test]
        public void Defeat_FinishesAndVanishes()
        {
            var motion = Build();
            motion.PlayDefeat();
            Assert.That(motion.IsDefeatFinished, Is.False);
            Run(motion, PenguinMotion.DefeatSeconds + Step);

            Assert.That(motion.IsDefeatFinished, Is.True);
            Assert.That(motion.GetComponent<PenguinModel>().Figure.localScale.x, Is.LessThan(1e-3f));
        }

        [Test]
        public void TurnsSmoothly_TowardTheWayItWalks()
        {
            var motion = Build();
            var figure = motion.GetComponent<PenguinModel>().Figure;
            motion.SnapFacing(Vector3.forward);
            motion.Velocity = Vector3.right * 4f;
            motion.Tick(Step);
            Assert.That(Vector3.Angle(figure.forward, Vector3.right), Is.GreaterThan(45f), "一瞬で向きが変わった");

            Run(motion, 0.5f);
            Assert.That(Vector3.Angle(Flat(figure.forward), Vector3.right), Is.LessThan(1f));
        }

        [Test]
        public void Blink_ClosesTheEyesAndOpensAgain()
        {
            var motion = Build();
            var eye = motion.GetComponent<PenguinModel>().Eyes[0];
            motion.ResetPose();
            var open = eye.localScale.y;
            motion.PlayBlink();
            motion.Tick(0.05f);
            Assert.That(eye.localScale.y, Is.LessThan(open * 0.5f));

            motion.Tick(0.2f);
            Assert.That(eye.localScale.y, Is.EqualTo(open).Within(1e-5f));
        }

        [Test]
        public void Frozen_HoldsThePoseUntilThawed()
        {
            var motion = Build();
            motion.Velocity = Vector3.right * 3f;
            Run(motion, 0.3f);
            motion.Frozen = true;
            var frozen = Snapshot(motion);
            Run(motion, 0.5f);

            var after = Snapshot(motion);
            for (var i = 0; i < frozen.Length; i++)
            {
                Assert.That(Vector3.Distance(frozen[i], after[i]), Is.LessThan(1e-5f), $"部品 {i} が凍ったまま動いた");
            }

            motion.Frozen = false;
            Run(motion, 0.1f);
            Assert.That(Snapshot(motion), Is.Not.EqualTo(frozen), "溶けたら動き出す");
        }

        private static void AssertAboveFloorWhile(PenguinMotion motion, float seconds, string label)
        {
            for (var t = 0f; t < seconds; t += Step)
            {
                motion.Tick(Step);
                Assert.That(LowestPoint(motion), Is.GreaterThan(-FloorTolerance), $"{label}: {t:0.00}秒で床にめり込んだ");
            }
        }

        /// <summary>丸い影（床に置いた板）を除いた部品のいちばん低いところ</summary>
        private static float LowestPoint(PenguinMotion motion)
        {
            var shadow = motion.GetComponent<PenguinModel>().BlobShadow;
            var lowest = float.MaxValue;
            foreach (var renderer in motion.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer == shadow) continue;
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var matrix = renderer.transform.localToWorldMatrix;
                foreach (var vertex in mesh.vertices)
                {
                    lowest = Mathf.Min(lowest, matrix.MultiplyPoint3x4(vertex).y);
                }
            }

            return lowest;
        }

        private static Vector3[] Snapshot(PenguinMotion motion)
        {
            var transforms = motion.GetComponentsInChildren<Transform>();
            var values = new Vector3[transforms.Length * 3];
            for (var i = 0; i < transforms.Length; i++)
            {
                values[i * 3] = transforms[i].position;
                values[i * 3 + 1] = transforms[i].rotation * Vector3.forward;
                values[i * 3 + 2] = transforms[i].lossyScale;
            }

            return values;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
