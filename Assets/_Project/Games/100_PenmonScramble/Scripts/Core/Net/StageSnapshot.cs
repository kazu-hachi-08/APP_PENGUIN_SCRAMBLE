using System.Collections.Generic;

namespace PenmonScramble.Core
{
    public enum StageResult : byte
    {
        Playing,
        Cleared,
        Failed,
    }

    /// <summary>描くのに要るユニット1体分の値</summary>
    public readonly struct UnitState
    {
        public readonly int Id;
        public readonly Team Team;
        public readonly int Seat;
        public readonly Vec2 Position;
        public readonly Vec2 Facing;
        public readonly int Hp;
        public readonly int MaxHp;
        public readonly float Radius;

        public UnitState(int id, Team team, int seat, Vec2 position, Vec2 facing, int hp, int maxHp, float radius)
        {
            Id = id;
            Team = team;
            Seat = seat;
            Position = position;
            Facing = facing;
            Hp = hp;
            MaxHp = maxHp;
            Radius = radius;
        }
    }

    public readonly struct ProjectileState
    {
        public readonly int Id;
        public readonly Vec2 Position;
        public readonly float Radius;

        public ProjectileState(int id, Vec2 position, float radius)
        {
            Id = id;
            Position = position;
            Radius = radius;
        }
    }

    /// <summary>
    /// ある更新の時点の、描くのに要る全部（位置・HP・前回からの間に起きたこと）。
    /// ホストは自分の画面にもこれを描き、ゲストへも同じものを送る。ソロ・ホスト・ゲストで描き方を分けないため（§2・§8）
    /// </summary>
    public sealed class StageSnapshot
    {
        /// <summary>何回目の挑戦か。「もう一度」で増える。ゲストはこれが変わったら画面を作り直す</summary>
        public int Round { get; set; }
        public int Tick { get; set; }
        public StageResult Result { get; set; }
        public Vec2 AreaMin { get; set; }
        public Vec2 AreaMax { get; set; }

        public List<UnitState> Units { get; } = new List<UnitState>();
        public List<ProjectileState> Projectiles { get; } = new List<ProjectileState>();
        public List<HitEvent> Hits { get; } = new List<HitEvent>();
        public List<DefeatedEvent> Defeats { get; } = new List<DefeatedEvent>();
        public List<SkillUsedEvent> SkillsUsed { get; } = new List<SkillUsedEvent>();

        public bool TryGetPlayer(int seat, out UnitState state)
        {
            foreach (var unit in Units)
            {
                if (unit.Team == Team.Player && unit.Seat == seat)
                {
                    state = unit;
                    return true;
                }
            }

            state = default;
            return false;
        }

        public int CountAlive(Team team)
        {
            var count = 0;
            foreach (var unit in Units)
            {
                if (unit.Team == team)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 送らなかった1つ前のスナップショットの出来事を、前に足す。
        /// 位置は最新だけ送れば足りるが、出来事（ダメージ数字など）は取りこぼすと見えなくなるため
        /// </summary>
        public void PrependEvents(StageSnapshot older)
        {
            Hits.InsertRange(0, older.Hits);
            Defeats.InsertRange(0, older.Defeats);
            SkillsUsed.InsertRange(0, older.SkillsUsed);
        }
    }
}
