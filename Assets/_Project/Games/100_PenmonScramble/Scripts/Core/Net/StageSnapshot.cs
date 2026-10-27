using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    public enum StageResult : byte
    {
        Playing,
        Cleared,
        Failed,
    }

    /// <summary>見た目を変える状態。描く側が、技の定義を引かずに分かるようにする</summary>
    [Flags]
    public enum UnitFlags : byte
    {
        None = 0,
        Dashing = 1,
        Frozen = 2,
        Invincible = 4,

        /// <summary>3体とも倒れて、仲間の助けを待っている（§4.5）</summary>
        Down = 8,

        /// <summary>ステージのボス（大きく描き、HUD に HP を出す）</summary>
        Boss = 16,
    }

    /// <summary>描くのに要るユニット1体分の値（場に出ている子の値）</summary>
    public readonly struct UnitState
    {
        public readonly int Id;
        public readonly Team Team;
        public readonly int Seat;
        public readonly int SpeciesNo;
        public readonly Vec2 Position;
        public readonly Vec2 Facing;
        public readonly int Hp;
        public readonly int MaxHp;
        public readonly float Radius;
        public readonly UnitFlags Flags;

        /// <summary>ダウン中の復活の進み具合（0〜1）</summary>
        public readonly float Revive;

        public UnitState(int id, Team team, int seat, int speciesNo, Vec2 position, Vec2 facing, int hp, int maxHp, float radius, UnitFlags flags, float revive = 0f)
        {
            Id = id;
            Team = team;
            Seat = seat;
            SpeciesNo = speciesNo;
            Position = position;
            Facing = facing;
            Hp = hp;
            MaxHp = maxHp;
            Radius = radius;
            Flags = flags;
            Revive = revive;
        }

        public bool Has(UnitFlags flag) => (Flags & flag) != 0;
    }

    public readonly struct ProjectileState
    {
        public readonly int Id;
        public readonly Team Team;
        public readonly Element Element;
        public readonly Vec2 Position;
        public readonly float Radius;

        public ProjectileState(int id, Team team, Element element, Vec2 position, float radius)
        {
            Id = id;
            Team = team;
            Element = element;
            Position = position;
            Radius = radius;
        }
    }

    /// <summary>地面に置かれた範囲（設置・落下の予告）</summary>
    public readonly struct EffectState
    {
        public readonly int Id;
        public readonly AreaEffectKind Kind;
        public readonly Element Element;
        public readonly Vec2 Position;
        public readonly float Radius;

        public EffectState(int id, AreaEffectKind kind, Element element, Vec2 position, float radius)
        {
            Id = id;
            Kind = kind;
            Element = element;
            Position = position;
            Radius = radius;
        }
    }

    /// <summary>連れている1体分（HUD の控えの表示に使う）</summary>
    public readonly struct MemberState
    {
        public readonly int SpeciesNo;
        public readonly int Hp;
        public readonly int MaxHp;

        public MemberState(int speciesNo, int hp, int maxHp)
        {
            SpeciesNo = speciesNo;
            Hp = hp;
            MaxHp = maxHp;
        }
    }

    /// <summary>プレイヤー1人分の、場に出ている子以外の値（3体・ゲージ・交代の待ち）</summary>
    public sealed class PlayerState
    {
        public int Seat { get; set; }
        public int ActiveIndex { get; set; }
        public float SpecialGauge { get; set; }
        public float SwitchCooldown { get; set; }
        public List<MemberState> Members { get; } = new List<MemberState>();
    }

    /// <summary>
    /// ある更新の時点の、描くのに要る全部（位置・HP・前回からの間に起きたこと）。
    /// ホストは自分の画面にもこれを描き、ゲストへも同じものを送る。ソロ・ホスト・ゲストで描き方を分けないため（§2・§8）
    /// </summary>
    public sealed class StageSnapshot
    {
        /// <summary>何回目の挑戦か。「もう一度」で増える。ゲストはこれが変わったら画面を作り直す</summary>
        public int Round { get; set; }

        /// <summary>遊んでいるステージの番号。ゲストはクリアしたときにこの番号を自分の記録に書く（§8）</summary>
        public int StageNo { get; set; }
        public int Tick { get; set; }
        public StageResult Result { get; set; }
        /// <summary>いま動ける範囲。出入口が開いている間は次のエリアまで広がる</summary>
        public Vec2 AreaMin { get; set; }
        public Vec2 AreaMax { get; set; }
        public int AreaIndex { get; set; }
        public int AreaCount { get; set; } = 1;
        public bool IsGateOpen { get; set; }
        public int EnemiesDefeated { get; set; }

        public List<UnitState> Units { get; } = new List<UnitState>();
        public List<ProjectileState> Projectiles { get; } = new List<ProjectileState>();
        public List<EffectState> Effects { get; } = new List<EffectState>();
        public List<PlayerState> Players { get; } = new List<PlayerState>();
        public List<HitEvent> Hits { get; } = new List<HitEvent>();
        public List<DefeatedEvent> Defeats { get; } = new List<DefeatedEvent>();
        public List<SkillUsedEvent> SkillsUsed { get; } = new List<SkillUsedEvent>();
        public List<SpecialUsedEvent> SpecialsUsed { get; } = new List<SpecialUsedEvent>();
        public List<BurstEvent> Bursts { get; } = new List<BurstEvent>();
        public List<ComboEvent> Combos { get; } = new List<ComboEvent>();

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

        /// <summary>倒れて場から消えた人は見つからない</summary>
        public PlayerState FindPlayerState(int seat)
        {
            foreach (var player in Players)
            {
                if (player.Seat == seat)
                {
                    return player;
                }
            }

            return null;
        }

        public bool TryGetBoss(out UnitState state)
        {
            foreach (var unit in Units)
            {
                if (unit.Has(UnitFlags.Boss))
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
            SpecialsUsed.InsertRange(0, older.SpecialsUsed);
            Bursts.InsertRange(0, older.Bursts);
            Combos.InsertRange(0, older.Combos);
        }
    }
}
