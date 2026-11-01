using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>試験場の1回分の結果（数字はプレイヤー1人あたりの平均）</summary>
    public readonly struct BalanceRunResult
    {
        public BalanceRunResult(bool cleared, bool timedOut, float seconds, float damageRatio, float fainted, float downs, float bossSeconds, float bossHits, int roundsWon, int defeated)
        {
            Cleared = cleared;
            TimedOut = timedOut;
            Seconds = seconds;
            DamageRatio = damageRatio;
            Fainted = fainted;
            Downs = downs;
            BossSeconds = bossSeconds;
            BossHits = bossHits;
            RoundsWon = roundsWon;
            Defeated = defeated;
        }

        public bool Cleared { get; }

        /// <summary>時間切れで止めた（決着がつかないほど長い＝失敗に数える）</summary>
        public bool TimedOut { get; }

        /// <summary>始まってから決着までの秒数（ゲームの中の時間）</summary>
        public float Seconds { get; }

        /// <summary>受けたダメージの合計 ÷ 3体の最大 HP の合計（1 で3体分を丸ごと削られた）</summary>
        public float DamageRatio { get; }

        /// <summary>倒れた子の数（起き上がってまた倒れたら、また数える）</summary>
        public float Fainted { get; }

        /// <summary>3体とも倒れてダウンした回数</summary>
        public float Downs { get; }

        /// <summary>ボス（ステージのボス・巨大ペンモン・大ボス）が場にいた秒数の合計</summary>
        public float BossSeconds { get; }

        /// <summary>ボスの攻撃に当たった回数</summary>
        public float BossHits { get; }

        /// <summary>勝った闘技場の戦いの数（ボスラッシュの失敗の報酬に使う）</summary>
        public int RoundsWon { get; }

        /// <summary>倒した敵の数（失敗の報酬に使う）</summary>
        public int Defeated { get; }
    }

    /// <summary>
    /// 試験場の1回: ステージを最初から最後まで、ボットに戦わせて測る。描画も通信も無いので、実際の何十倍も速く回る
    /// </summary>
    public sealed class BalanceRun
    {
        /// <summary>これより長くかかったら止めて失敗に数える（ゲームの中の秒数）。ふつうは長くても 10 分ほどで決着がつく</summary>
        public const float TimeLimitSeconds = 20f * 60f;

        private readonly StageWorld _world;
        private readonly Unit[] _players;
        private readonly StageBot[] _bots;
        private readonly bool[] _wasDown;
        private readonly bool[][] _wasAlive;
        private readonly Dictionary<int, Unit> _bosses = new Dictionary<int, Unit>();
        private readonly int _totalMaxHp;
        private long _damage;
        private int _fainted;
        private int _downs;
        private int _bossHits;
        private int _bossTicks;

        public BalanceRun(StageDef stage, Difficulty difficulty, int playerCount, BotSkill skill, BalanceParty party, int seed)
        {
            var random = new Random(seed);
            var seats = new int[playerCount];
            var parties = new UnitStats[playerCount][];
            for (var i = 0; i < playerCount; i++)
            {
                seats[i] = i;
                parties[i] = party.Create(random);
                foreach (var member in parties[i])
                {
                    _totalMaxHp += member.MaxHp;
                }
            }

            _players = TrialStage.Create(stage, random.Next(), seats, parties, out _world, difficulty);
            _bots = new StageBot[playerCount];
            _wasDown = new bool[playerCount];
            _wasAlive = new bool[playerCount][];
            for (var i = 0; i < playerCount; i++)
            {
                _bots[i] = new StageBot(skill, random.Next());
                _wasAlive[i] = new[] { true, true, true };
            }

            _world.Hit += OnHit;
        }

        public BalanceRunResult Run()
        {
            var limit = (int)(TimeLimitSeconds / Balance.TickSeconds);
            while (_world.Result == StageResult.Playing && _world.TickCount < limit)
            {
                Step();
            }

            var count = _players.Length;
            return new BalanceRunResult(
                _world.Result == StageResult.Cleared,
                _world.Result == StageResult.Playing,
                _world.TickCount * Balance.TickSeconds,
                (float)_damage / Math.Max(1, _totalMaxHp),
                (float)_fainted / count,
                (float)_downs / count,
                _bossTicks * Balance.TickSeconds,
                (float)_bossHits / count,
                _world.ArenaRoundsWon,
                _world.EnemiesDefeated);
        }

        private void Step()
        {
            var time = _world.TickCount * Balance.TickSeconds;
            for (var i = 0; i < _players.Length; i++)
            {
                _world.SetInput(_players[i].Id, _bots[i].Next(_world, _players[i], time));
            }

            _world.Tick();
            CountFalls();
            CountBosses();
        }

        private void CountFalls()
        {
            for (var i = 0; i < _players.Length; i++)
            {
                var player = _players[i];
                for (var m = 0; m < player.Members.Count; m++)
                {
                    var alive = player.Members[m].IsAlive;
                    if (_wasAlive[i][m] && !alive)
                    {
                        _fainted++;
                    }

                    _wasAlive[i][m] = alive;
                }

                if (player.IsDown && !_wasDown[i])
                {
                    _downs++;
                }

                _wasDown[i] = player.IsDown;
            }
        }

        private void CountBosses()
        {
            var any = false;
            foreach (var unit in _world.Units)
            {
                if (unit.Team == Team.Enemy && unit.IsBoss && unit.IsAlive)
                {
                    _bosses[unit.Id] = unit;
                    any = true;
                }
            }

            if (any)
            {
                _bossTicks++;
            }
        }

        private void OnHit(HitEvent hit)
        {
            var target = _world.FindUnit(hit.TargetId);
            if (target == null || target.Team != Team.Player)
            {
                return;
            }

            _damage += hit.Damage;
            if (_bosses.ContainsKey(hit.AttackerId))
            {
                _bossHits++;
            }
        }
    }
}
