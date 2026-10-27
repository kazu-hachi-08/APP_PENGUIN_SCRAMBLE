using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>合体技が起きる条件・起きない条件（§4.6）</summary>
    public class ComboTests
    {
        private static readonly PlayerInput UseSkill1 = new PlayerInput(Vec2.Zero, true, false);

        private StageWorld _world;
        private Unit _enemy;
        private readonly List<ComboEvent> _combos = new List<ComboEvent>();

        [SetUp]
        public void SetUp()
        {
            _world = Create();
            _combos.Clear();
            _world.Combo += _combos.Add;
            // 吹っ飛ばない的。合体技の大ばくはつなどで遠くへ飛んで、次の技が届かなくならないように
            _enemy = AddDummyEnemy(_world, Vec2.Zero, maxHp: 10000);
        }

        /// <summary>的をはさんで左右に立つプレイヤー。技は吹っ飛ばさず、待ち時間の短い近接</summary>
        private Unit AddPlayer(Element element, float x, int seat)
        {
            var melee = Melee();
            melee.Cooldown = 0.1f;
            var stats = Stats(melee);
            stats.Element = element;
            return _world.AddUnit(Team.Player, stats, new Vec2(x, 0f), seat);
        }

        [Test]
        public void TwoPlayersWithinWindow_TriggersCombo()
        {
            var a = AddPlayer(Element.Ice, -1f, 0);
            var b = AddPlayer(Element.Water, 1f, 1);

            Press(_world, a, UseSkill1);
            Press(_world, b, UseSkill1);

            Assert.That(_combos.Count, Is.EqualTo(1));
            Assert.That(_combos[0].Kind, Is.EqualTo(ComboKind.Freeze));
            Assert.That(_enemy.IsFrozen, Is.True, "氷＋水は凍結");
        }

        [Test]
        public void SamePlayerTwice_DoesNotTriggerCombo()
        {
            var a = AddPlayer(Element.Ice, -1f, 0);
            AddPlayer(Element.Water, 5f, 1);

            Press(_world, a, UseSkill1);
            TickFor(_world, Seconds(0.3f));
            Press(_world, a, UseSkill1);

            Assert.That(_enemy.Hp, Is.EqualTo(_enemy.MaxHp - 20), "2回とも当たっている");
            Assert.That(_combos, Is.Empty);
        }

        [Test]
        public void AfterWindow_DoesNotTriggerCombo()
        {
            var a = AddPlayer(Element.Ice, -1f, 0);
            var b = AddPlayer(Element.Water, 1f, 1);

            Press(_world, a, UseSkill1);
            TickFor(_world, Seconds(Balance.ComboWindowSeconds + 0.1f));
            Press(_world, b, UseSkill1);

            Assert.That(_combos, Is.Empty);
        }

        [Test]
        public void SameEnemy_WaitsCooldownBeforeNextCombo()
        {
            var a = AddPlayer(Element.Ice, -1f, 0);
            var b = AddPlayer(Element.Water, 1f, 1);
            Press(_world, a, UseSkill1);
            Press(_world, b, UseSkill1);

            // 合体技のヒットストップの間は待ち時間も進まないので、技の待ち時間より長めに待つ
            TickFor(_world, Seconds(0.3f));
            Press(_world, a, UseSkill1);
            Press(_world, b, UseSkill1);
            Assert.That(_combos.Count, Is.EqualTo(1), "2秒の間は起きない");

            TickFor(_world, Seconds(Balance.ComboCooldownSeconds));
            Press(_world, a, UseSkill1);
            Press(_world, b, UseSkill1);
            Assert.That(_combos.Count, Is.EqualTo(2), "2秒たてばまた起きる");
        }

        [Test]
        public void ComboDamage_DoesNotTriggerAnotherCombo()
        {
            var a = AddPlayer(Element.Wind, -1f, 0);
            var b = AddPlayer(Element.Wind, 1f, 1);
            // a の技が的と一緒に当たる、別の敵。そこへ b の合体技（大ばくはつ）が当たっても、次の合体技は起きない
            var other = AddDummyEnemy(_world, new Vec2(-0.3f, 1f), maxHp: 10000);

            Press(_world, a, UseSkill1);
            Press(_world, b, UseSkill1);
            TickFor(_world, Seconds(0.5f));

            Assert.That(_combos.Count, Is.EqualTo(1));
            Assert.That(other.Hp, Is.LessThan(other.MaxHp), "大ばくはつは周りにも当たる");
        }

        [Test]
        public void ThunderCloud_ChasesEnemy()
        {
            var a = AddPlayer(Element.Wind, -1f, 0);
            var b = AddPlayer(Element.Thunder, 1f, 1);
            Press(_world, a, UseSkill1);
            Press(_world, b, UseSkill1);
            var far = AddDummyEnemy(_world, new Vec2(0f, 6f), maxHp: 10000);
            // 合体技を受けた敵がいなくなれば、雷雲は残りの敵を追う
            _world.RemoveUnit(_enemy.Id);

            TickFor(_world, Seconds(1f));

            Assert.That(_combos[0].Kind, Is.EqualTo(ComboKind.ThunderCloud));
            var cloud = _world.AreaEffects[0];
            Assert.That(cloud.Kind, Is.EqualTo(AreaEffectKind.Cloud));
            Assert.That(cloud.Position.Y, Is.GreaterThan(1f), "上の敵へ寄っていく");
            Assert.That(cloud.Position.Y, Is.LessThan(far.Position.Y));
        }

        [TestCase(Element.Ice, Element.Water, ComboKind.Freeze)]
        [TestCase(Element.Water, Element.Ice, ComboKind.Freeze)]
        [TestCase(Element.Ice, Element.Wind, ComboKind.Blizzard)]
        [TestCase(Element.Thunder, Element.Ice, ComboKind.IceBurst)]
        [TestCase(Element.Wind, Element.Water, ComboKind.Whirlpool)]
        [TestCase(Element.Water, Element.Thunder, ComboKind.Shock)]
        [TestCase(Element.Thunder, Element.Wind, ComboKind.ThunderCloud)]
        [TestCase(Element.Thunder, Element.Thunder, ComboKind.Explosion)]
        public void KindOf_DependsOnElementPair(Element a, Element b, ComboKind expected)
        {
            Assert.That(ComboMoves.KindOf(a, b), Is.EqualTo(expected));
        }
    }
}
