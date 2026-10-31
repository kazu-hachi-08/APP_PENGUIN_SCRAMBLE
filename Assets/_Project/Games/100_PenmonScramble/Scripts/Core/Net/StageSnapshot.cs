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

        /// <summary>3体とも倒れて、仲間の助けを待っている</summary>
        Down = 8,

        /// <summary>ステージのボス（大きく描き、HUD に HP を出す）</summary>
        Boss = 16,

        /// <summary>大ボスがもぐっている（攻撃が効かない。影だけ描く）</summary>
        Submerged = 32,

        /// <summary>協力プレイで、このプレイヤーがメニューを開いている（頭の上に「メニュー中」）</summary>
        InMenu = 64,

        /// <summary>雑魚・ステージのボスが技を出す前に構えている（体が膨らんで光る）</summary>
        WindingUp = 128,
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

    /// <summary>大ボスの攻撃の前の赤い範囲（BossWarning）</summary>
    public readonly struct WarningState
    {
        public readonly int Id;
        public readonly Vec2 From;
        public readonly Vec2 To;
        public readonly float HalfWidth;

        /// <summary>攻撃が来るまでの進み具合（0〜1）</summary>
        public readonly float Progress;

        public WarningState(int id, Vec2 from, Vec2 to, float halfWidth, float progress)
        {
            Id = id;
            From = from;
            To = to;
            HalfWidth = halfWidth;
            Progress = progress;
        }
    }

    /// <summary>
    /// 構えている敵の、地面のうすい予告（大ボスの赤い範囲 WarningState とは別）。
    /// 形ごとに描き方が違うので、技の形と大きさをそのまま送る
    /// </summary>
    public readonly struct TelegraphState
    {
        public readonly int UnitId;
        public readonly SkillShape Shape;
        public readonly Vec2 Origin;
        public readonly Vec2 Facing;

        /// <summary>
        /// 近接: 扇の半径 / 周囲: 円の半径 / 弾・拡散・突進: 中心から届く先までの長さ / 設置: 置く場所までの距離
        /// </summary>
        public readonly float Reach;

        /// <summary>近接: 扇の半分の角度 / 拡散: 弾が広がる扇の半分の角度（度）。ほかは 0</summary>
        public readonly float HalfAngleDegrees;

        /// <summary>弾・拡散・突進: 帯の半分の幅 / 設置: 置く円の半径（マス）。ほかは 0</summary>
        public readonly float HalfWidth;

        /// <summary>構えの長さ（秒）。毎秒15回しか届かないので、描く側はこれで間を進める</summary>
        public readonly float Seconds;

        /// <summary>構え始めてからの割合（0〜1。1 で技が出る）</summary>
        public readonly float Progress;

        public TelegraphState(int unitId, SkillShape shape, Vec2 origin, Vec2 facing, float reach, float halfAngleDegrees, float halfWidth, float seconds, float progress)
        {
            UnitId = unitId;
            Shape = shape;
            Origin = origin;
            Facing = facing;
            Reach = reach;
            HalfAngleDegrees = halfAngleDegrees;
            HalfWidth = halfWidth;
            Seconds = seconds;
            Progress = progress;
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

    /// <summary>プレイヤー1人分の、場に出ている子以外の値（3体・ゲージ・交代の待ち・技の待ち）</summary>
    public sealed class PlayerState
    {
        public int Seat { get; set; }
        public int ActiveIndex { get; set; }
        public float SpecialGauge { get; set; }
        public float SwitchCooldown { get; set; }
        public List<MemberState> Members { get; } = new List<MemberState>();

        /// <summary>場に出ている子の技の待ちの残り（技の番号ごとに 0〜1。1 は出したばかり、0 で出せる）。スマホのボタンに出す</summary>
        public List<float> SkillCooldowns { get; } = new List<float>();

        /// <summary>交代したら出る子（1→2→3→1 の順で、倒れた子は飛ばす）。いなければ -1</summary>
        public int NextAliveIndex()
        {
            return Unit.NextAliveIndex(ActiveIndex, Members.Count, index => Members[index].Hp > 0);
        }
    }

    /// <summary>
    /// ある更新の時点の、描くのに要る全部（位置・HP・前回からの間に起きたこと）。
    /// ホストは自分の画面にもこれを描き、ゲストへも同じものを送る。ソロ・ホスト・ゲストで描き方を分けないため
    /// </summary>
    public sealed class StageSnapshot
    {
        /// <summary>何回目の挑戦か。「もう一度」で増える。ゲストはこれが変わったら画面を作り直す</summary>
        public int Round { get; set; }

        /// <summary>遊んでいるステージの番号。ゲストはクリアしたときにこの番号を自分の記録に書く</summary>
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

        /// <summary>ステージの最後（ボスのいる）エリアにいる。テスト用の1エリアだけの場は違う</summary>
        public bool IsBossArea => AreaCount > 1 && AreaIndex == AreaCount - 1;

        public List<UnitState> Units { get; } = new List<UnitState>();
        public List<ProjectileState> Projectiles { get; } = new List<ProjectileState>();
        public List<EffectState> Effects { get; } = new List<EffectState>();
        public List<WarningState> Warnings { get; } = new List<WarningState>();
        public List<TelegraphState> Telegraphs { get; } = new List<TelegraphState>();
        public List<PlayerState> Players { get; } = new List<PlayerState>();
        public List<HitEvent> Hits { get; } = new List<HitEvent>();
        public List<DefeatedEvent> Defeats { get; } = new List<DefeatedEvent>();
        public List<SkillUsedEvent> SkillsUsed { get; } = new List<SkillUsedEvent>();
        public List<SpecialUsedEvent> SpecialsUsed { get; } = new List<SpecialUsedEvent>();
        public List<BurstEvent> Bursts { get; } = new List<BurstEvent>();
        public List<ComboEvent> Combos { get; } = new List<ComboEvent>();

        public bool TryGetPlayer(int seat, out UnitState state)
        {
            return TryFindUnit(unit => unit.Team == Team.Player && unit.Seat == seat, out state);
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
            return TryFindUnit(unit => unit.Has(UnitFlags.Boss), out state);
        }

        public int CountAlive(Team team)
        {
            return CountUnits(unit => unit.Team == team);
        }

        /// <summary>最初に match に合うユニットを返す。無ければ false（state は default）</summary>
        private bool TryFindUnit(Func<UnitState, bool> match, out UnitState state)
        {
            foreach (var unit in Units)
            {
                if (match(unit))
                {
                    state = unit;
                    return true;
                }
            }

            state = default;
            return false;
        }

        private int CountUnits(Func<UnitState, bool> match)
        {
            var count = 0;
            foreach (var unit in Units)
            {
                if (match(unit))
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
