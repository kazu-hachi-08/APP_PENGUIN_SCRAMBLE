using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 塁の上の走者（おもちゃのコマのように塁の横に立つ）。打席の結果が決まってから、Core の走者の動き（BaseballRunnerMove）どおりに
    /// 塁を回って走る（走っている間に何かを決めることは無い）。ホームへ還った子は喜んで消え、ゲッツーでアウトになった子は悔しがって消える。
    /// 体は「打順・種類・CPU か」ごとに一度作ったら取っておく（攻守が替わるたびに作り直さない）
    /// </summary>
    public sealed class BaseballRunners
    {
        private const float RunSpeed = 3.2f;
        private const float WalkSpeed = 1.4f;
        // ホームランで4つ回っても、次の打者が出てくるまでに走り終える長さ
        private const float MaxRunSeconds = 1.8f;
        // 塁の真上ではなく、ダイヤモンドの外側へこれだけずらして立つ（塁と重ならないように）
        private const float OffBase = 0.35f;
        // アウトになった子は、次の塁までのこの割合だけ走って止まる
        private const float OutShare = 0.6f;
        // 還った・アウトになった子が消えるまでの秒
        private const float LeaveSeconds = 0.8f;

        private readonly Dictionary<(int Slot, int Species, bool Wild), BaseballPenguin> _bodies = new Dictionary<(int, int, bool), BaseballPenguin>();
        private readonly List<Runner> _runners = new List<Runner>();
        private readonly BaseballFieldDef _field;
        private readonly ToonMaterials _materials;
        private readonly Transform _parent;
        private IReadOnlyList<int> _species = new int[0];
        private bool _wild;

        public BaseballRunners(BaseballFieldDef field, ToonMaterials materials, Transform parent)
        {
            _field = field;
            _materials = materials;
            _parent = parent;
        }

        /// <summary>打つ側のチームを替える（攻守交代・試合の始め）。塁の上の子は消す</summary>
        public void Use(IReadOnlyList<int> species, bool wild)
        {
            Clear();
            _species = species;
            _wild = wild;
        }

        public void Clear()
        {
            foreach (var runner in _runners)
            {
                runner.Body.SetActive(false);
            }

            _runners.Clear();
        }

        /// <summary>打席の結果の走者の動きを始める（打者は打席の子のいた所から走り出す。四球は歩く）</summary>
        public void Apply(IReadOnlyList<BaseballRunnerMove> moves, Vector3 batterFrom, bool walk)
        {
            FinishRuns();
            foreach (var move in moves)
            {
                var runner = move.From == 0 ? Enter(move.Runner, batterFrom) : Find(move.Runner);
                if (runner == null)
                {
                    continue;
                }

                runner.Path.Clear();
                AddPath(runner, move);
                runner.Scored = move.Scored;
                runner.Out = move.Out;
                runner.Speed = Mathf.Max(walk ? WalkSpeed : RunSpeed, PathLength(runner) / MaxRunSeconds);
            }
        }

        public void Tick(float deltaTime)
        {
            for (var i = _runners.Count - 1; i >= 0; i--)
            {
                var runner = _runners[i];
                runner.Body.Tick(deltaTime);
                if (runner.Path.Count > 0)
                {
                    Run(runner, deltaTime);
                }
                else if (runner.Leaving >= 0f)
                {
                    runner.Leaving -= deltaTime;
                    if (runner.Leaving < 0f)
                    {
                        runner.Body.SetActive(false);
                        _runners.RemoveAt(i);
                    }
                }
            }
        }

        private Runner Enter(int slot, Vector3 from)
        {
            if (slot < 0 || slot >= _species.Count)
            {
                return null;
            }

            var species = _species[slot];
            var key = (slot, species, _wild);
            if (!_bodies.TryGetValue(key, out var body))
            {
                body = BaseballPenguin.Build($"Runner{slot}", species, _wild, _materials, _parent);
                _bodies.Add(key, body);
            }

            body.SetActive(true);
            body.Motion.ResetPose();
            body.React(false, false);
            body.PlaceAt(from, Vector3.right, false);
            var runner = new Runner(slot, body);
            _runners.Add(runner);
            return runner;
        }

        private Runner Find(int slot)
        {
            foreach (var runner in _runners)
            {
                if (runner.Slot == slot && runner.Leaving < 0f)
                {
                    return runner;
                }
            }

            return null;
        }

        /// <summary>塁を1つずつ回る道（還るならホームまで。アウトなら次の塁の手前で止まる）</summary>
        private void AddPath(Runner runner, BaseballRunnerMove move)
        {
            var last = move.Scored ? BaseballRules.BaseCount + 1 : move.To;
            for (var b = move.From + 1; b <= last; b++)
            {
                var spot = b > BaseballRules.BaseCount ? HomeSpot : Spot(b);
                if (move.Out && b == last)
                {
                    var from = move.From == 0 ? runner.Body.Root.localPosition : Spot(move.From);
                    spot = Vector3.Lerp(from, spot, OutShare);
                }

                runner.Path.Enqueue(spot);
            }
        }

        private static float PathLength(Runner runner)
        {
            var length = 0f;
            var at = runner.Body.Root.localPosition;
            foreach (var point in runner.Path)
            {
                length += Vector3.Distance(at, point);
                at = point;
            }

            return length;
        }

        /// <summary>前の走りが残っていたら一気に着かせる（次の打席の動きと混ざらないように）</summary>
        private void FinishRuns()
        {
            for (var i = _runners.Count - 1; i >= 0; i--)
            {
                var runner = _runners[i];
                if (runner.Scored || runner.Out)
                {
                    runner.Body.SetActive(false);
                    _runners.RemoveAt(i);
                    continue;
                }

                Vector3? end = null;
                while (runner.Path.Count > 0)
                {
                    end = runner.Path.Dequeue();
                }

                if (end.HasValue)
                {
                    Arrive(runner, end.Value);
                }
            }
        }

        private void Run(Runner runner, float deltaTime)
        {
            var root = runner.Body.Root;
            var target = runner.Path.Peek();
            var to = target - root.localPosition;
            var step = runner.Speed * deltaTime;
            if (to.magnitude > step)
            {
                var direction = to.normalized;
                root.localPosition += direction * step;
                runner.Body.Motion.Velocity = direction * runner.Speed;
                runner.Body.Motion.Facing = direction;
                return;
            }

            runner.Path.Dequeue();
            if (runner.Path.Count == 0)
            {
                Arrive(runner, target);
            }
            else
            {
                root.localPosition = target;
            }
        }

        /// <summary>着いた: 塁ならホームの方を向いて立つ。還った子は喜び、アウトの子は悔しがって、少しして消える</summary>
        private void Arrive(Runner runner, Vector3 at)
        {
            var body = runner.Body;
            body.Root.localPosition = at;
            body.Motion.Velocity = Vector3.zero;
            body.Face(_parent.TransformPoint(HomeSpot));
            if (runner.Scored || runner.Out)
            {
                body.React(runner.Scored, runner.Out);
                runner.Leaving = LeaveSeconds;
            }
        }

        private Vector3 HomeSpot => BaseballSpace.ToWorld(_field.Base(0));

        /// <summary>塁の横の立つ所（ダイヤモンドの真ん中から外へずらす）</summary>
        private Vector3 Spot(int baseNumber)
        {
            var at = _field.Base(baseNumber);
            var center = _field.Base(2) * 0.5f;
            return BaseballSpace.ToWorld(at + (at - center).Normalized * OffBase);
        }

        private sealed class Runner
        {
            public readonly int Slot;
            public readonly BaseballPenguin Body;
            public readonly Queue<Vector3> Path = new Queue<Vector3>();
            public float Speed;
            public bool Scored;
            public bool Out;

            /// <summary>消えるまでの残り秒（まだ消えないなら負）</summary>
            public float Leaving = -1f;

            public Runner(int slot, BaseballPenguin body)
            {
                Slot = slot;
                Body = body;
            }
        }
    }
}
