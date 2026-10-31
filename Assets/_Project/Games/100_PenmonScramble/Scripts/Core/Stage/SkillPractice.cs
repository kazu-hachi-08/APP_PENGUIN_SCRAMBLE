using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ペンモン画面の技の実演。動かない的を3体置き、選んだ子が1つの技（または必殺技）を「構える → 出す → 収まるのを待つ → 元の位置へ戻る」でくり返す。
    /// 本番と同じ StageWorld で動かすので、届く距離・飛ばし方・ダメージの数字が本番どおりに見える
    /// </summary>
    public sealed class SkillPractice
    {
        /// <summary>
        /// 実演の場の中心。ステージ（原点から右へ並ぶ）とタイトルのジオラマ（原点のまわり）から遠く離し、
        /// 同じ3Dの空間に置いても互いのカメラに映らないようにする
        /// </summary>
        public static readonly Vec2 Center = new Vec2(0f, -1000f);

        /// <summary>画面に映す範囲（場の中心からのずれと広さ）。場はエリア1枚分あり、飛ばされた的が場の端で止まる所は映りにくい</summary>
        public static readonly Vec2 ViewOffset = new Vec2(-0.4f, 0f);
        public static readonly Vec2 ViewSize = new Vec2(10f, 4.2f);

        /// <summary>技1・技2の次の番号で、必殺技を表す</summary>
        public const int SpecialSlot = 2;

        private const int Seed = 1;
        // 的は倒れない（毎回 HP を戻す）。1回で削り切られないよう十分に大きくする
        private const int DummyHp = 999999;
        // 的の種類（★1 の水・風・雷）。氷の子の技が的の色に紛れないよう、色の違う子を並べる
        private static readonly int[] DummySpecies = { 6, 11, 16 };
        // 前の的の位置（場の中心から）と、その後ろの2体のずれ。扇・貫通・周囲の違いが分かるよう、斜め後ろに置く
        private const float FrontDummyX = 1.5f;
        private static readonly Vec2[] DummyOffsets = { Vec2.Zero, new Vec2(1.1f, 1f), new Vec2(1.1f, -1f) };
        private const float DummyWalkSpeed = 2.5f;
        private const float HomeTolerance = 0.08f;

        // 待ち時間がこれ以下の技は「押しっぱなしで連打」を見せる
        private const float RapidCooldown = 0.5f;
        private const float BurstSeconds = 1.6f;
        private const float RestSeconds = 0.7f;
        // 弾・範囲・飛ばされた的が収まるのを待つ上限（収まらない動きがあっても止まらないように）
        private const float SettleTimeoutSeconds = 4f;

        // 構える位置（前の的までの距離）。技の届く距離から決める
        private const float MeleeReachRatio = 0.7f;
        private const float NovaReachRatio = 0.6f;
        private const float DashRunUpRatio = 0.5f;
        private const float DashRunUpMax = 3f;
        private const float ShotReachRatio = 0.7f;
        private const float ShotGapMax = 5f;
        // 設置は的3体のまん中あたりに置く（向きは前の的なので、前の的より奥に届く分だけ引く）
        private const float ZoneDepth = 0.7f;
        private const float DropGap = 3.5f;

        private enum Phase
        {
            Rest,
            Act,
            Settle,
            Return,
        }

        private readonly int _slot;
        private readonly bool _isSpecial;
        private readonly SkillDef _skill;
        private readonly Vec2 _playerHome;
        private readonly List<(Unit unit, Vec2 home)> _dummies = new List<(Unit, Vec2)>();
        private Phase _phase = Phase.Rest;
        private float _timer = RestSeconds;

        /// <param name="slot">0 が技1、SpecialSlot が必殺技</param>
        public SkillPractice(UnitStats penguin, int slot)
        {
            _slot = slot;
            _isSpecial = slot == SpecialSlot;
            _skill = _isSpecial ? SpecialMoves.For(penguin.Element) : penguin.Skills[slot];
            var (min, max) = FieldBounds(_isSpecial);
            World = new StageWorld(min, max, Seed);

            var front = Center + new Vec2(FrontDummyX, 0f);
            _playerHome = front - new Vec2(Gap(_skill), 0f);
            Player = World.AddUnit(Team.Player, penguin, _playerHome, 0);
            for (var i = 0; i < DummyOffsets.Length; i++)
            {
                var home = front + DummyOffsets[i];
                _dummies.Add((World.AddUnit(Team.Enemy, DummyStats(DummySpecies[i % DummySpecies.Length]), home), home));
            }
        }

        public StageWorld World { get; }
        public Unit Player { get; }

        /// <summary>
        /// 技はエリア1枚分の場で見せる（飛ばされた的が場の端で止まる所は映りにくい）。
        /// 必殺技は「画面の端から」「画面のどこかに」効くので、場を映す範囲と同じにして、効き目が全部小窓に入るようにする
        /// </summary>
        private static (Vec2 min, Vec2 max) FieldBounds(bool isSpecial)
        {
            var center = isSpecial ? Center + ViewOffset : Center;
            var half = isSpecial ? ViewSize * 0.5f : new Vec2(Balance.AreaWidth * 0.5f, Balance.AreaHeight * 0.5f);
            return (center - half, center + half);
        }

        /// <summary>前の的まで（中心どうし）の距離</summary>
        private static float Gap(SkillDef skill)
        {
            switch (skill.Shape)
            {
                case SkillShape.Melee:
                    return skill.Range * MeleeReachRatio + Balance.PenguinRadius;
                case SkillShape.Nova:
                    return skill.Range * NovaReachRatio + Balance.PenguinRadius;
                case SkillShape.Dash:
                    return MathF.Min(skill.Range * DashRunUpRatio, DashRunUpMax) + Balance.PenguinRadius;
                case SkillShape.Projectile:
                case SkillShape.Spread:
                    return MathF.Min(skill.Range * ShotReachRatio, ShotGapMax);
                case SkillShape.Zone:
                    return MathF.Max(skill.Range - ZoneDepth, Balance.PenguinRadius * 2f);
                default:
                    return DropGap;
            }
        }

        /// <summary>技を持たない敵は立っているだけ（StageWorld.UpdateEnemy）。倒れないよう HP は大きく</summary>
        private static UnitStats DummyStats(int speciesNo)
        {
            var stats = SpeciesTable.EnemyStats(SpeciesTable.Get(speciesNo), 1);
            stats.Skills = Array.Empty<SkillDef>();
            stats.MaxHp = DummyHp;
            return stats;
        }

        // 必殺技は待ち時間が 0 だが、1回で満タンのゲージを使い切るので連打にはしない
        private bool IsRapid => !_isSpecial && _skill.Cooldown <= RapidCooldown;

        public void Tick()
        {
            const float dt = Balance.TickSeconds;
            World.SetInput(Player.Id, NextInput(dt));
            World.Tick();
            foreach (var (dummy, _) in _dummies)
            {
                dummy.Hp = dummy.MaxHp;
            }
        }

        private PlayerInput NextInput(float dt)
        {
            switch (_phase)
            {
                case Phase.Rest:
                    return Rest(dt);
                case Phase.Act:
                    return Act(dt);
                case Phase.Settle:
                    return Settle(dt);
                default:
                    return Return(dt);
            }
        }

        private PlayerInput Rest(float dt)
        {
            _timer -= dt;
            if (_timer <= 0f && IsReady())
            {
                if (_isSpecial)
                {
                    // 実演ではゲージをためる手間を省き、毎回満タンにしてから出す
                    Player.SpecialGauge = 1f;
                }

                _phase = Phase.Act;
                // 連打できない技は1回だけ押す（押しっぱなしだと、待ち時間が明けた瞬間にまた出てしまう）
                _timer = IsRapid ? BurstSeconds : 0f;
                return Press();
            }

            return default;
        }

        private PlayerInput Act(float dt)
        {
            _timer -= dt;
            if (_timer > 0f)
            {
                return Press();
            }

            _phase = Phase.Settle;
            _timer = SettleTimeoutSeconds;
            return default;
        }

        private bool IsReady() => _isSpecial || Player.GetCooldown(_slot) <= 0f;

        private PlayerInput Press() => new PlayerInput(Vec2.Zero, _slot == 0, _slot == 1, _isSpecial);

        private PlayerInput Settle(float dt)
        {
            _timer -= dt;
            if (_timer <= 0f || IsSettled())
            {
                _phase = Phase.Return;
            }

            return default;
        }

        /// <summary>必殺技のカットイン・突進・弾・範囲が終わり、凍った的が溶けて、飛ばされた的が止まった</summary>
        private bool IsSettled()
        {
            if (World.IsSpecialPending || Player.IsDashing || World.Projectiles.Count > 0 || World.AreaEffects.Count > 0)
            {
                return false;
            }

            foreach (var unit in World.Units)
            {
                if (!unit.KnockbackVelocity.IsNearlyZero || unit.IsFrozen)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>的は歩いて元の位置へ、子はスティックを倒して元の位置へ戻る。全員そろったら一息おいて次を出す</summary>
        private PlayerInput Return(float dt)
        {
            var allHome = true;
            foreach (var (dummy, home) in _dummies)
            {
                allHome &= WalkDummyHome(dummy, home, dt);
            }

            var toHome = _playerHome - Player.Position;
            if (toHome.Length > HomeTolerance)
            {
                // 近づいたらスティックを戻していき、行き過ぎないようにする
                var stick = toHome / MathF.Max(Player.MoveSpeed * dt, toHome.Length);
                return new PlayerInput(stick.ClampLength(1f), false, false);
            }

            Player.Facing = Vec2.Right;
            if (allHome)
            {
                _phase = Phase.Rest;
                _timer = RestSeconds;
            }

            return default;
        }

        private bool WalkDummyHome(Unit dummy, Vec2 home, float dt)
        {
            var toHome = home - dummy.Position;
            var distance = toHome.Length;
            if (distance <= HomeTolerance)
            {
                dummy.Facing = (Player.Position - dummy.Position).Normalized;
                return true;
            }

            var step = MathF.Min(DummyWalkSpeed * dt, distance);
            dummy.Facing = toHome / distance;
            dummy.Position += dummy.Facing * step;
            return false;
        }
    }
}
