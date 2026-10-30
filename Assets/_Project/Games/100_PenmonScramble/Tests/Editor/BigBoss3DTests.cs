using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>
    /// 3D の大ボス（PLAN_3D フェーズ5）: 3体とも組み立てられる・赤い範囲から攻撃の構えを読み取れる・どの動きでも床にめり込まない・
    /// もぐると床の下に隠れて飛び出す。見た目の良し悪しはスクショで人が決めるので、ここでは壊れ方だけを見る
    /// </summary>
    public class BigBoss3DTests
    {
        private const float Step = 1f / 60f;
        // 部品が床にめり込んでよい深さ（丸みの分）
        private const float FloorTolerance = 0.05f;

        private static readonly int[] Bosses = { BigBossTable.SkuaNo, BigBossTable.LeopardSealNo, BigBossTable.OrcaNo };

        private GameObject _parent;
        private StageMaterials _materials;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("BigBoss3DTests");
            _materials = new StageMaterials(Shader.Find("PenmonScramble/Toon"), Shader.Find("PenmonScramble/BlobShadow"), Shader.Find("PenmonScramble/Fx"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_parent);
            _materials.DestroyAll();
        }

        private BigBossMotion Build(int no)
        {
            var model = BigBossModelBuilder.Build(no, _materials.Toon, _parent.transform);
            return model.gameObject.AddComponent<BigBossMotion>();
        }

        private static void Run(BigBossMotion motion, float seconds)
        {
            for (var t = 0f; t < seconds; t += Step)
            {
                motion.Tick(Step);
            }
        }

        [Test]
        public void EveryBigBoss_IsBuiltWithItsOwnJointsAndNoColliders([ValueSource(nameof(Bosses))] int no)
        {
            var model = Build(no).GetComponent<BigBossModel>();

            Assert.That(model.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(model.GetComponentInChildren<PenguinModel>(), Is.Null);
            Assert.That(model[BigBossPart.Body], Is.Not.Null);
            Assert.That(model[BigBossPart.LimbLeft], Is.Not.Null);
            Assert.That(model[BigBossPart.Tail], Is.Not.Null);
            Assert.That(model.Eyes.Count, Is.EqualTo(2));
            if (no == BigBossTable.SkuaNo)
            {
                Assert.That(model[BigBossPart.TipRight], Is.Not.Null, "翼の先がしなる");
                Assert.That(model[BigBossPart.FootLeft], Is.Not.Null);
            }
            else
            {
                Assert.That(model[BigBossPart.Jaw], Is.Not.Null, "口を開けてかみつく");
                Assert.That(model[BigBossPart.Hips], Is.Not.Null, "くねる・尾を振る");
            }
        }

        [Test]
        public void TopHeight_MatchesTheTallestPart([ValueSource(nameof(Bosses))] int no)
        {
            var model = Build(no).GetComponent<BigBossModel>();
            Assert.That(Bounds(model).max.y, Is.EqualTo(model.TopHeight).Within(0.2f), "頭の上の文字と氷の塊の高さが合う");
        }

        [Test]
        public void Skua_FliesAboveItsShadow()
        {
            var motion = Build(BigBossTable.SkuaNo);
            motion.Tick(Step);
            Assert.That(Bounds(motion.GetComponent<BigBossModel>()).min.y, Is.GreaterThan(0.3f), "足の先も床から離れている");
        }

        [Test]
        public void AllMotions_StayAboveTheFloor([ValueSource(nameof(Bosses))] int no, [Values(BossAttackKind.Charge, BossAttackKind.Rain, BossAttackKind.Wave)] BossAttackKind kind)
        {
            var motion = Build(no);
            motion.Velocity = Vector3.forward * 2f;
            motion.Telegraph = kind;
            AssertAboveFloorWhile(motion, 1f, $"{kind} の構え");

            motion.Telegraph = null;
            motion.PlayStrike(kind);
            motion.Sliding = kind == BossAttackKind.Charge;
            AssertAboveFloorWhile(motion, BigBossMotion.StrikeSeconds, $"{kind} が出た");

            motion.Sliding = false;
            motion.PlayHit();
            AssertAboveFloorWhile(motion, BigBossMotion.HitSeconds, "被弾");
        }

        [Test]
        public void AfterEveryMotion_IdlePoseReturnsToRest([ValueSource(nameof(Bosses))] int no)
        {
            var motion = Build(no);
            motion.Apply(BigBossPose.Idle);
            var rest = Snapshot(motion);

            motion.Velocity = Vector3.right * 3f;
            motion.Telegraph = BossAttackKind.Wave;
            motion.Sliding = true;
            motion.Submerged = true;
            motion.PlayHit();
            Run(motion, 0.5f);
            motion.Submerged = false;
            motion.PlayStrike(BossAttackKind.Rain);
            Run(motion, 0.3f);
            motion.Apply(BigBossPose.Idle);

            var after = Snapshot(motion);
            for (var i = 0; i < rest.Length; i++)
            {
                Assert.That(Vector3.Distance(rest[i], after[i]), Is.LessThan(1e-4f), $"部品 {i} に前の動きが残っている");
            }
        }

        [Test]
        public void Submerged_SinksBelowTheFloor_ThenLeapsOutAndLands()
        {
            var motion = Build(BigBossTable.LeopardSealNo);
            var model = motion.GetComponent<BigBossModel>();
            motion.Submerged = true;
            Run(motion, BigBossMotion.SubmergeSeconds + Step);

            Assert.That(motion.IsUnderground, Is.True);
            Assert.That(Bounds(model).max.y, Is.LessThan(0f), "床の下に隠れきる");

            motion.Submerged = false;
            var highest = float.MinValue;
            for (var t = 0f; t < BigBossMotion.EmergeSeconds; t += Step)
            {
                motion.Tick(Step);
                highest = Mathf.Max(highest, Bounds(model).min.y);
            }

            Assert.That(motion.IsUnderground, Is.False);
            Assert.That(highest, Is.GreaterThan(0.2f), "床から跳び上がる");
            Assert.That(Bounds(model).min.y, Is.EqualTo(0f).Within(FloorTolerance), "着地して床に戻る");
        }

        [Test]
        public void Defeat_SpinsAwayAndFinishes([ValueSource(nameof(Bosses))] int no)
        {
            var motion = Build(no);
            motion.PlayDefeat();
            Run(motion, BigBossMotion.DefeatSeconds + Step);

            Assert.That(motion.IsDefeatFinished, Is.True);
            Assert.That(motion.GetComponent<BigBossModel>().Figure.localScale.x, Is.LessThan(0.01f));
        }

        [Test]
        public void Frozen_HoldsThePose()
        {
            var motion = Build(BigBossTable.SkuaNo);
            Run(motion, 0.2f);
            var before = Snapshot(motion);

            motion.Frozen = true;
            Run(motion, 0.5f);

            var after = Snapshot(motion);
            for (var i = 0; i < before.Length; i++)
            {
                Assert.That(after[i], Is.EqualTo(before[i]), "凍ったら羽ばたきも止まる");
            }
        }

        /// <summary>Core の本物の予告をスナップショットにして（通信と同じく符号化して）読み取る</summary>
        [TestCase(BigBossTable.SkuaNo, 0, BossAttackKind.Charge)]
        [TestCase(BigBossTable.SkuaNo, 1, BossAttackKind.Rain)]
        [TestCase(BigBossTable.LeopardSealNo, 0, BossAttackKind.Charge)]
        [TestCase(BigBossTable.LeopardSealNo, 1, BossAttackKind.Ambush)]
        [TestCase(BigBossTable.OrcaNo, 0, BossAttackKind.Wave)]
        [TestCase(BigBossTable.OrcaNo, 1, BossAttackKind.Charge)]
        [TestCase(BigBossTable.OrcaNo, 2, BossAttackKind.Charge)]
        public void Telegraph_IsReadFromTheWarnings(int no, int attackIndex, BossAttackKind expected)
        {
            var world = Create();
            var boss = world.AddUnit(Team.Enemy, BigBossTable.Stats(BigBossTable.Get(no), playerCount: 1, world: 1), new Vec2(4f, 1f));
            boss.Brain.NextAttackIndex = attackIndex;
            world.AddUnit(Team.Player, Stats(Melee()), new Vec2(-4f, 3f), seat: 0);
            var recorder = new StageRecorder(world, round: 1);
            Assert.That(BigBossTelegraph.Read(recorder.Capture().Warnings, boss.Position.ToVector2(), boss.IsSubmerged), Is.Null, "予告が出る前は構えない");

            for (var i = 0; i < Seconds(Balance.BigBossFirstAttackDelay) + 2 && world.Warnings.Count == 0; i++)
            {
                world.Tick();
            }

            var snapshot = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(recorder.Capture()));
            snapshot.TryGetBoss(out var state);
            Assert.That(BigBossTelegraph.Read(snapshot.Warnings, state.Position.ToVector2(), state.Has(UnitFlags.Submerged)), Is.EqualTo(expected));
        }

        [Test]
        public void UnitView_BuildsTheBigBoss_FollowsItsTelegraph_AndHidesWhileUnderground()
        {
            var state = BossState(UnitFlags.Boss);
            var view = UnitView.Create(_parent.transform, _materials, state, null, null);
            Assert.That(view.IsBigBoss, Is.True);
            Assert.That(view.GetComponentInChildren<PenguinModel>(true), Is.Null);
            var model = view.GetComponentInChildren<BigBossModel>(true);
            var motion = view.GetComponentInChildren<BigBossMotion>(true);

            var circle = new List<WarningState> { new WarningState(9, new Vec2(3f, 2f), new Vec2(3f, 2f), 1.6f, 0f) };
            view.PushState(BossState(UnitFlags.Boss | UnitFlags.Submerged), Balance.TickSeconds);
            view.PushWarnings(circle);
            Assert.That(motion.Telegraph, Is.EqualTo(BossAttackKind.Ambush));
            Assert.That(motion.Submerged, Is.True);

            Run(motion, BigBossMotion.SubmergeSeconds + Step);
            view.TickStatus(Step);
            Assert.That(model.Figure.gameObject.activeSelf, Is.False, "沈みきったら体を出さない");
            Assert.That(FindRenderer(view, "UnderShadow").enabled, Is.True, "床に暗い影");

            view.PushState(BossState(UnitFlags.Boss, new Vec2(3f, 2f)), Balance.TickSeconds);
            view.PushWarnings(new List<WarningState>());
            view.TickStatus(Step);
            Assert.That(motion.Telegraph, Is.Null);
            Assert.That(model.Figure.gameObject.activeSelf, Is.True, "飛び出す");
            Assert.That(FindRenderer(view, "UnderShadow").enabled, Is.False);
        }

        private static UnitState BossState(UnitFlags flags, Vec2 position = default)
        {
            var def = BigBossTable.Get(BigBossTable.LeopardSealNo);
            return new UnitState(1, Team.Enemy, 0, def.No, position, Vec2.Right, 100, 100, def.Radius, flags);
        }

        private static Renderer FindRenderer(UnitView view, string objName)
        {
            foreach (var renderer in view.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name == objName)
                {
                    return renderer;
                }
            }

            Assert.Fail($"{objName} が無い");
            return null;
        }

        private static void AssertAboveFloorWhile(BigBossMotion motion, float seconds, string what)
        {
            var model = motion.GetComponent<BigBossModel>();
            for (var t = 0f; t < seconds; t += Step)
            {
                motion.Tick(Step);
                Assert.That(Bounds(model).min.y, Is.GreaterThan(-FloorTolerance), $"{what}: {t:0.00}秒で床にめり込んだ（{LowestPart(model)}）");
            }
        }

        private static string LowestPart(BigBossModel model)
        {
            var lowest = "";
            var y = float.MaxValue;
            foreach (var renderer in model.Figure.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.bounds.min.y < y)
                {
                    y = renderer.bounds.min.y;
                    lowest = renderer.name;
                }
            }

            return lowest;
        }

        /// <summary>体の部品（足元の丸い影は除く）を全部囲む箱</summary>
        private static Bounds Bounds(BigBossModel model)
        {
            var bounds = new Bounds(model.Figure.position, Vector3.zero);
            var first = true;
            foreach (var renderer in model.Figure.GetComponentsInChildren<MeshRenderer>())
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private static Vector3[] Snapshot(BigBossMotion motion)
        {
            var nodes = motion.GetComponent<BigBossModel>().Figure.GetComponentsInChildren<Transform>();
            var result = new Vector3[nodes.Length * 2];
            for (var i = 0; i < nodes.Length; i++)
            {
                result[i * 2] = nodes[i].position;
                result[i * 2 + 1] = nodes[i].lossyScale;
            }

            return result;
        }
    }
}
