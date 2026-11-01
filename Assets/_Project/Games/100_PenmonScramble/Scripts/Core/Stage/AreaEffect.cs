using System.Collections.Generic;

namespace PenmonScramble.Core
{
    public enum AreaEffectKind : byte
    {
        /// <summary>しばらく残り、中の敵に少しずつダメージ（設置）</summary>
        Zone,

        /// <summary>少し待ってから1回だけ当たる（落下）。待っている間は落ちる場所を見せる</summary>
        Drop,

        /// <summary>一番近い敵を追いかけながら、決まった間隔で当たる（合体技「雷雲」）</summary>
        Cloud,

        /// <summary>しばらく残り、近くの相手を中心へ引き寄せ、中にいる間は決まった間隔で当たる（巨大ペンモン・オオフルマカモメの竜巻）</summary>
        Vortex,

        /// <summary>まっすぐ床を走り、触れた相手を1回ずつ飛ばす（本気のサメの2本目の背びれ）</summary>
        Fin,

        /// <summary>設置と同じにしばらく残って中の相手を削る。見た目だけ溶岩の色（サメの食いつきのしぶき）</summary>
        Lava,
    }

    /// <summary>地面に置かれた範囲（設置・落下の技、必殺技）</summary>
    public sealed class AreaEffect
    {
        internal AreaEffect(int id, Unit owner, SkillDef skill, AreaEffectKind kind, Vec2 position, float delay)
            : this(id, owner, skill, kind, position, delay, skill.AreaRadius)
        {
        }

        /// <param name="radius">当たる半径。技の AreaRadius と違うとき（背びれは体の半径から決める）</param>
        internal AreaEffect(int id, Unit owner, SkillDef skill, AreaEffectKind kind, Vec2 position, float delay, float radius)
        {
            Radius = radius;
            Id = id;
            OwnerId = owner.Id;
            Team = owner.Team;
            Element = owner.Element;
            Skill = skill;
            Kind = kind;
            Position = position;
            // 撃った子が倒れても範囲は残るので、ここで決めておく
            Damage = StageWorld.CalcDamage(owner, skill);
            Remaining = kind == AreaEffectKind.Drop ? delay : skill.Duration;
        }

        public int Id { get; }
        public int OwnerId { get; }
        public Team Team { get; }
        public Element Element { get; }
        public SkillDef Skill { get; }
        public AreaEffectKind Kind { get; }
        /// <summary>雷雲・背びれと、ChaseSpeed のある竜巻が動く</summary>
        public Vec2 Position { get; internal set; }
        public float Radius { get; }
        public int Damage { get; }

        /// <summary>設置・雷雲: 消えるまで / 落下: 落ちるまで（秒）</summary>
        internal float Remaining { get; set; }

        /// <summary>設置・雷雲: 次にダメージを与えるまで（秒）。0 から始めて、置いた瞬間にも当てる</summary>
        internal float IntervalTimer { get; set; }

        internal bool IsDead { get; set; }

        /// <summary>背びれ: 走る向きと速さ（マス/秒）</summary>
        internal Vec2 Velocity { get; set; }

        /// <summary>背びれ: 1回の走りで、同じ相手に何度も当たらないようにする（背びれのときだけ作る）</summary>
        internal HashSet<int> HitUnitIds => _hitUnitIds ??= new HashSet<int>();
        private HashSet<int> _hitUnitIds;
    }
}
