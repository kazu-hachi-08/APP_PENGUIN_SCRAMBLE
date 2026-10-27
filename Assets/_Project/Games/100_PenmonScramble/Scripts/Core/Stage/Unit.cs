namespace PenmonScramble.Core
{
    /// <summary>
    /// 場にいるペンギン（味方・敵の両方）。位置などは StageWorld だけが書き換える
    /// </summary>
    public sealed class Unit
    {
        /// <summary>敵など、プレイヤーの席に属さないユニットの Seat</summary>
        public const int NoSeat = -1;

        internal Unit(int id, Team team, UnitStats stats, Vec2 position, int seat)
        {
            Id = id;
            Team = team;
            Seat = seat;
            Radius = stats.Radius;
            MoveSpeed = stats.MoveSpeed;
            MaxHp = stats.MaxHp;
            Hp = stats.MaxHp;
            Attack = stats.Attack;
            Skills = stats.Skills;
            Cooldowns = new float[stats.Skills.Length];
            Position = position;
            Facing = team == Team.Player ? Vec2.Right : -Vec2.Right;
        }

        public int Id { get; }
        public Team Team { get; }

        /// <summary>操作しているプレイヤーの席（ホスト=0）。オンラインで誰のペンギンかを見分けるため</summary>
        public int Seat { get; }
        public float Radius { get; }
        public float MoveSpeed { get; }
        public int MaxHp { get; }
        public float Attack { get; }
        public SkillDef[] Skills { get; }

        public int Hp { get; internal set; }
        public Vec2 Position { get; internal set; }

        /// <summary>向き（長さ1）。技はこの方向へ出す</summary>
        public Vec2 Facing { get; internal set; }

        /// <summary>吹っ飛びの速さ。減速しながら0に戻る</summary>
        public Vec2 KnockbackVelocity { get; internal set; }

        internal float[] Cooldowns { get; }
        internal PlayerInput Input { get; set; }

        public bool IsAlive => Hp > 0;

        /// <summary>強く飛ばされている間は、歩くことも技を出すこともできない</summary>
        public bool IsStunned => KnockbackVelocity.Length > Balance.StunSpeed;

        public float GetCooldown(int slot) => Cooldowns[slot];
    }
}
