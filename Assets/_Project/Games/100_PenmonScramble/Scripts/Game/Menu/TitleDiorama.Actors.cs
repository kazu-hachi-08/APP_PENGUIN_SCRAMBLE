using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ジオラマの役者: まん中で待ち構える味方（Hero）と、寄ってきては吹っ飛ばされる野生のペンギン（Wild）。
    /// 姿勢は PenguinMotion がつけるので、ここは床の上の位置と「いつ何をするか」だけを決める
    /// </summary>
    public sealed partial class TitleDiorama
    {
        // 大きさはステージと同じ（当たりの円に胴の太さをそろえる）
        private static readonly float ModelScale = PenguinModelBuilder.ScaleForRadius(Balance.PenguinRadius);

        private const int StartingWilds = 4;
        private const float StartingSpawnDistance = 4.5f;
        private const int MaxWilds = 8;
        private const float SpawnDistance = 7.5f;
        // 出てくる向き（度。0 で右、90 で奥）。手前はボタンの帯に隠れるので出さない
        private const float SpawnAngleMin = -25f;
        private const float SpawnAngleMax = 205f;
        private const float SpawnIntervalMin = 0.45f;
        private const float SpawnIntervalMax = 0.9f;

        private const float WildWalkSpeed = 2.2f;
        // 味方のまわりのこの距離で立ち止まって、技を受ける・殴りかかる
        private const float ReachDistance = 1.15f;
        private const float WildAttackIntervalMin = 0.8f;
        private const float WildAttackIntervalMax = 1.6f;
        // 技を振ってから当たるまで（ヒレを振り切るあたり）
        private const float AttackImpactSeconds = PenguinMotion.AttackSeconds * 0.4f;
        private const float HeroAttackCooldown = 0.55f;

        private const float KnockSpeedMin = 9f;
        private const float KnockSpeedMax = 13f;
        // 飛ぶ向きを少しばらつかせる（度）。みんな同じ向きへ飛ぶと作り物っぽい
        private const float KnockSpread = 25f;
        // これより遠くへ飛んだら、画面の外なので消す
        private const float VanishDistance = 16f;
        private const int DefeatCoins = 3;
        private const float DefeatSpread = 1f;

        // 必殺技は着地（PenguinMotion の跳ぶ動きの終わり）で、まわりをまとめて飛ばす
        private const float SpecialImpactSeconds = PenguinMotion.SpecialSeconds * 0.8f;
        private const float SpecialIntervalMin = 4.5f;
        private const float SpecialIntervalMax = 7.5f;
        private const float ShockwaveRadius = 2.8f;
        private const float ShockwaveBoost = 1.2f;

        // 出てきたときは小さく出てから弾む（交代と同じ見せ方）
        private const float PopSeconds = 0.3f;
        private const float PopStart = 0.5f;
        private const float FlashSeconds = 0.1f;
        private const float FlashAmount = 0.85f;

        private PenguinModel BuildPenguin(int speciesNo, Vector2 position)
        {
            var model = PenguinModelBuilder.Build(PenguinLooks.Get(speciesNo), _materials.Toon, _world);
            model.transform.position = position.ToFloor();
            model.transform.localScale = Vector3.one * ModelScale;
            model.SetRealShadows(false);
            model.gameObject.AddComponent<PenguinMotion>();
            return model;
        }

        private static Vector3 Toward(Vector2 from, Vector2 to) => (to - from).ToFloor();

        /// <summary>まん中で待ち構える味方。届く所まで来た野生のペンギンを技で飛ばす</summary>
        private sealed class Hero
        {
            private readonly TitleDiorama _owner;
            private readonly PenguinMotion _motion;
            private readonly SkillShape[] _shapes;
            private Wild _victim;
            private float _impactTimer = -1f;
            private float _specialTimer = -1f;
            private float _cooldown;
            private int _nextShape;

            public Hero(TitleDiorama owner, int speciesNo, Vector2 position)
            {
                _owner = owner;
                Position = position;
                var model = owner.BuildPenguin(speciesNo, position);
                _motion = model.GetComponent<PenguinMotion>();
                _motion.SnapFacing(Vector3.back);
                var species = SpeciesTable.Get(speciesNo);
                _shapes = new[] { StandingShape(species.Skill1.Shape), StandingShape(species.Skill2.Shape) };
            }

            public Vector2 Position { get; }

            /// <summary>突進はその場ですべっても前に出ないので、近接の振りで見せる</summary>
            private static SkillShape StandingShape(SkillShape shape) => shape == SkillShape.Dash ? SkillShape.Melee : shape;

            public void PlaySpecial()
            {
                if (_specialTimer >= 0f)
                {
                    return;
                }

                _motion.PlaySpecial();
                _specialTimer = SpecialImpactSeconds;
            }

            public void PlayHit() => _motion.PlayHit();

            public void Tick(float dt)
            {
                TickSpecial(dt);
                TickImpact(dt);
                _cooldown -= dt;
                if (_cooldown <= 0f && _victim == null && _specialTimer < 0f)
                {
                    // 振り終えたらこちらへ向き直る（奥から来る相手へ向いたままだと、背中しか見えない）
                    _motion.Facing = Vector3.back;
                    StartAttack(_owner.FindReached(this));
                }
            }

            private void TickSpecial(float dt)
            {
                if (_specialTimer < 0f)
                {
                    return;
                }

                _specialTimer -= dt;
                if (_specialTimer <= 0f)
                {
                    _specialTimer = -1f;
                    _owner.Shockwave(Position);
                }
            }

            private void TickImpact(float dt)
            {
                if (_impactTimer < 0f)
                {
                    return;
                }

                _impactTimer -= dt;
                if (_impactTimer > 0f)
                {
                    return;
                }

                _impactTimer = -1f;
                if (_victim.CanBeHit)
                {
                    _victim.Knock(_victim.Position - Position, Random.Range(KnockSpeedMin, KnockSpeedMax));
                }

                _victim = null;
            }

            private void StartAttack(Wild wild)
            {
                if (wild == null)
                {
                    return;
                }

                _victim = wild;
                wild.Brace();
                _motion.Facing = Toward(Position, wild.Position);
                _motion.PlayAttack(_shapes[_nextShape]);
                _nextShape = (_nextShape + 1) % _shapes.Length;
                _impactTimer = AttackImpactSeconds;
                _cooldown = HeroAttackCooldown;
            }
        }

        /// <summary>野生のペンギン。出てくる → いちばん近い味方へ歩く → 殴りかかる → 飛ばされて転がる → コインと星になって消える</summary>
        private sealed class Wild
        {
            private enum State
            {
                Walking,
                Waiting,
                // 味方が技を振り始めた（ほかの味方に取られないように）
                Braced,
                Knocked,
                Defeated,
            }

            private readonly TitleDiorama _owner;
            private readonly PenguinModel _model;
            private readonly PenguinMotion _motion;
            private readonly PenguinTint _tint = PenguinTint.Dark(UnitView.EnemyDarkness);
            private State _state;
            private Vector2 _velocity;
            private float _popAge;
            private float _flashTimer;
            private float _attackTimer;

            public Wild(TitleDiorama owner, int speciesNo, Vector2 position)
            {
                _owner = owner;
                Position = position;
                _model = owner.BuildPenguin(speciesNo, position);
                _model.SetTint(_tint);
                _motion = _model.GetComponent<PenguinMotion>();
                Target = owner.NearestHero(position);
                _motion.SnapFacing(Toward(position, Target.Position));
                _attackTimer = AttackInterval();
                SmokePuff.Spawn(owner._world, owner._materials, position, Balance.PenguinRadius, _model.TopHeight * ModelScale);
                TickPop(0f);
            }

            public Vector2 Position { get; private set; }
            public Hero Target { get; private set; }
            public bool IsWaiting => _state == State.Waiting;
            public bool CanBeHit => _state <= State.Braced;

            public void Brace() => _state = State.Braced;

            /// <summary>direction の向きへ飛ばす（少しばらつかせる）。飛んでいる間は Core と同じ減速で止まる</summary>
            public void Knock(Vector2 direction, float speed)
            {
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Random.insideUnitCircle;
                }

                var angle = Random.Range(-KnockSpread, KnockSpread) * Mathf.Deg2Rad;
                var d = direction.normalized;
                d = new Vector2(d.x * Mathf.Cos(angle) - d.y * Mathf.Sin(angle), d.x * Mathf.Sin(angle) + d.y * Mathf.Cos(angle));
                _velocity = d * speed;
                _state = State.Knocked;
                _flashTimer = FlashSeconds;
                _motion.Velocity = Vector3.zero;
                _motion.Facing = Vector3.zero;
                _motion.PlayKnockback(_velocity.ToFloor());
            }

            /// <summary>1フレーム進める。消えたら false</summary>
            public bool Tick(float dt)
            {
                TickPop(dt);
                TickFlash(dt);
                switch (_state)
                {
                    case State.Walking:
                        Walk(dt);
                        break;
                    case State.Waiting:
                        TickAttack(dt);
                        break;
                    case State.Knocked:
                        Fly(dt);
                        break;
                    case State.Defeated when _motion.IsDefeatFinished:
                        Destroy(_model.gameObject);
                        return false;
                }

                if (Position.sqrMagnitude > VanishDistance * VanishDistance)
                {
                    Destroy(_model.gameObject);
                    return false;
                }

                _model.transform.position = Position.ToFloor();
                return true;
            }

            private void Walk(float dt)
            {
                Target = _owner.NearestHero(Position);
                var offset = Target.Position - Position;
                var distance = offset.magnitude;
                if (distance <= ReachDistance)
                {
                    _state = State.Waiting;
                    _motion.Velocity = Vector3.zero;
                    _motion.Facing = offset.ToFloor();
                    return;
                }

                var step = offset / distance * WildWalkSpeed;
                Position += step * Mathf.Min(dt, (distance - ReachDistance) / WildWalkSpeed);
                _motion.Velocity = step.ToFloor();
            }

            /// <summary>待っている間も黙って立たず、味方に殴りかかる（味方はのけぞる）</summary>
            private void TickAttack(float dt)
            {
                _attackTimer -= dt;
                if (_attackTimer > 0f)
                {
                    return;
                }

                _attackTimer = AttackInterval();
                _motion.PlayAttack(SkillShape.Melee);
                Target.PlayHit();
            }

            private void Fly(float dt)
            {
                var speed = _velocity.magnitude;
                var next = Mathf.Max(0f, speed - Balance.KnockbackDeceleration * dt);
                Position += _velocity * dt;
                _velocity = speed > 0f ? _velocity * (next / speed) : Vector2.zero;
                if (next > 0f)
                {
                    return;
                }

                _state = State.Defeated;
                _motion.PlayDefeat();
                DefeatSparkle.Spawn(_owner._world, _owner._materials, Position, DefeatCoins, DefeatSpread);
            }

            private void TickPop(float dt)
            {
                if (_popAge >= PopSeconds)
                {
                    return;
                }

                _popAge += dt;
                _model.transform.localScale = Vector3.one * (ModelScale * ToyEasing.Pop(_popAge / PopSeconds, PopStart));
            }

            private void TickFlash(float dt)
            {
                if (_flashTimer <= 0f)
                {
                    return;
                }

                _flashTimer -= dt;
                _model.SetTint(_flashTimer > 0f ? _tint.Toward(Color.white, FlashAmount) : _tint);
            }

            private static float AttackInterval() => Random.Range(WildAttackIntervalMin, WildAttackIntervalMax);
        }
    }
}
