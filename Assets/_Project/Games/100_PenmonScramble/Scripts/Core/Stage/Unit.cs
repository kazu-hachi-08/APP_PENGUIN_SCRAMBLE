using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 場にいるペンギン（味方・敵の両方）。位置などは StageWorld だけが書き換える。
    /// プレイヤーは3体を連れていて、場に出ている1体（Active）の値がこのユニットの値になる。敵は1体だけ
    /// </summary>
    public sealed class Unit
    {
        /// <summary>敵など、プレイヤーの席に属さないユニットの Seat</summary>
        public const int NoSeat = -1;

        private readonly PartyMember[] _members;

        internal Unit(int id, Team team, IReadOnlyList<UnitStats> party, Vec2 position, int seat)
        {
            Id = id;
            Team = team;
            Seat = seat;
            _members = new PartyMember[party.Count];
            for (var i = 0; i < party.Count; i++)
            {
                _members[i] = new PartyMember(party[i]);
            }

            Position = position;
            Facing = team == Team.Player ? Vec2.Right : -Vec2.Right;
        }

        public int Id { get; }
        public Team Team { get; }

        /// <summary>操作しているプレイヤーの席（ホスト=0）。オンラインで誰のペンギンかを見分けるため</summary>
        public int Seat { get; }

        public IReadOnlyList<PartyMember> Members => _members;
        public int ActiveIndex { get; private set; }
        public PartyMember Active => _members[ActiveIndex];

        public int SpeciesNo => Active.Stats.SpeciesNo;
        public Element Element => Active.Stats.Element;
        public float Radius => Active.Stats.Radius;
        public float MoveSpeed => Active.Stats.MoveSpeed;
        public int MaxHp => Active.MaxHp;
        public float Attack => Active.Stats.Attack;
        public SkillDef[] Skills => Active.Stats.Skills;
        internal float CooldownScale => Active.Stats.CooldownScale;
        internal float KnockbackScale => Active.Stats.KnockbackScale;
        public bool IsBoss => Active.Stats.IsBoss;

        public int Hp
        {
            get => Active.Hp;
            internal set => Active.Hp = value;
        }

        public Vec2 Position { get; internal set; }

        /// <summary>向き（長さ1）。技はこの方向へ出す</summary>
        public Vec2 Facing { get; internal set; }

        /// <summary>吹っ飛びの速さ。減速しながら0に戻る</summary>
        public Vec2 KnockbackVelocity { get; internal set; }

        /// <summary>必殺技のゲージ（0〜1）。プレイヤーごとに1本で、交代しても引き継ぐ（§4.3）</summary>
        public float SpecialGauge { get; internal set; }

        /// <summary>次に交代できるまでの秒数</summary>
        public float SwitchCooldown { get; internal set; }

        public float FrozenSeconds { get; internal set; }
        public float InvincibleSeconds { get; internal set; }

        /// <summary>ダウン中に、近くに仲間がいた時間の合計（秒）。離れてもためた分は残る（§4.5）</summary>
        public float ReviveSeconds { get; internal set; }

        /// <summary>足が遅くなっている残りの秒数（合体技「吹雪」）</summary>
        public float SlowSeconds { get; internal set; }
        public bool IsSlowed => SlowSeconds > 0f;

        // 合体技（§4.6）: この敵に最後に技を当てたプレイヤーと、その属性。窓が開いている間に別の人が当てると起きる
        internal int ComboAttackerId { get; set; }
        internal Element ComboElement { get; set; }
        internal float ComboWindowSeconds { get; set; }
        internal float ComboCooldownSeconds { get; set; }

        /// <summary>今の吹っ飛びで、もうぶつかった相手。1回飛ぶ間に同じ相手へ何度も当たらないようにする（§4.4）</summary>
        internal HashSet<int> ChainHitIds { get; } = new HashSet<int>();

        internal DashState Dash { get; set; }

        /// <summary>大ボスの攻撃の進み具合。大ボスでなければ null</summary>
        internal BigBossBrain Brain { get; set; }

        /// <summary>大ボスがもぐっている（ヒョウアザラシ）。攻撃が効かず、ほかのユニットとも押し合わない</summary>
        public bool IsSubmerged { get; internal set; }
        internal float[] Cooldowns => Active.Cooldowns;
        internal PlayerInput Input { get; set; }

        // 交代は押した瞬間だけ。押しっぱなしで2秒ごとに替わり続けないようにする
        private bool _wasSwitchHeld;

        /// <summary>場に出ている子が元気か。倒れても控えがいれば、その更新の終わりに次の子が出る</summary>
        public bool IsAlive => Hp > 0;
        public bool HasAliveMember => FindNextAlive() >= 0 || IsAlive;

        /// <summary>3体とも倒れて、その場で仲間の助けを待っている（§4.5）。敵は倒れたら場から消えるので、プレイヤーだけ</summary>
        public bool IsDown => Team == Team.Player && !HasAliveMember;

        /// <summary>強く飛ばされている間は、歩くことも技を出すこともできない</summary>
        public bool IsStunned => KnockbackVelocity.Length > Balance.StunSpeed;
        public bool IsDashing => Dash != null;
        public bool IsFrozen => FrozenSeconds > 0f;
        public bool IsInvincible => InvincibleSeconds > 0f || IsSubmerged;
        public bool IsSpecialReady => SpecialGauge >= 1f;

        public float GetCooldown(int slot) => Cooldowns[slot];

        /// <summary>1→2→3→1 の順で、倒れた子を飛ばして次の子を出す。いなければ false</summary>
        internal bool SwitchToNextAlive()
        {
            var next = FindNextAlive();
            if (next < 0)
            {
                return false;
            }

            ActiveIndex = next;
            Dash = null;
            return true;
        }

        /// <summary>ダウンから起き上がる。先頭の子を、最大 HP の hpRatio で出す（§4.5）</summary>
        internal void ReviveFirstMember(float hpRatio)
        {
            ActiveIndex = 0;
            var first = _members[0];
            first.Hp = Math.Max(1, (int)MathF.Round(first.MaxHp * hpRatio));
            Dash = null;
        }

        private int FindNextAlive()
        {
            for (var i = 1; i < _members.Length; i++)
            {
                var index = (ActiveIndex + i) % _members.Length;
                if (_members[index].IsAlive)
                {
                    return index;
                }
            }

            return -1;
        }

        internal bool ConsumeSwitchPress()
        {
            var pressed = Input.Switch && !_wasSwitchHeld;
            _wasSwitchHeld = Input.Switch;
            return pressed;
        }
    }
}
