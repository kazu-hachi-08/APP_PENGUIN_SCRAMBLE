using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 塁の上の走者（おもちゃのコマのように塁の横に立つ）。打席の結果が決まってから、Core の走者の動き（BaseballRunnerMove）どおりに
    /// 塁を回って走る（走っている間に何かを決めることは無い）。ホームへ還った子は「+1」を出して喜んで消え、ゲッツーでアウトになった子は「アウト」を出して悔しがって消える。
    /// 立っている塁には光る輪、子の足元には輪、走った跡には丸を、チームの色で出す（BaseballRunnerMarks）。
    /// 体は「種類・CPU か」ごとに作って取っておき、塁に出た1人ずつに空いている体を貸す（攻守が替わるたびに作り直さない）。
    /// 編成が3体までなので、塁にいる子がもう一度打席に立って塁に出ることがある。体を打順で決めると2人が1つの体を取り合って
    /// 片方が消えるので、走者は「何塁にいるか」で探し、体は1人に1つずつ貸す
    /// </summary>
    public sealed class BaseballRunners
    {
        private const float RunSpeed = 3.2f;
        private const float WalkSpeed = 1.4f;
        // ホームランで4つ回っても、次の打者が出てくるまでに走り終える長さ
        private const float MaxRunSeconds = 1.8f;
        // 塁の真上ではなく、ダイヤモンドの外側へこれだけずらして立つ（塁と重ならない・二塁のゲッツーの穴を隠さないように）
        private const float OffBase = 0.35f;
        // アウトになった子は、次の塁までのこの割合だけ走って止まる
        private const float OutShare = 0.6f;
        // 還った・アウトになった子が消えるまでの秒（「+1」「アウト」を読める長さ）
        private const float LeaveSeconds = 1f;

        private readonly Dictionary<(int Species, bool Wild), List<Body>> _bodies = new Dictionary<(int, bool), List<Body>>();
        private readonly List<Runner> _runners = new List<Runner>();
        private readonly bool?[] _standing = new bool?[BaseballRules.BaseCount + 1];
        private readonly BaseballFieldDef _field;
        private readonly ToonMaterials _materials;
        private readonly Transform _parent;
        private readonly BaseballRunnerMarks _marks;
        private IReadOnlyList<int> _species = new int[0];
        private bool _wild;
        private bool _mine;
        private int _scoredInPlay;

        /// <param name="board">盤（塁の光る輪を置く）</param>
        /// <param name="parent">子を立たせる所</param>
        public BaseballRunners(BaseballFieldDef field, ToonMaterials materials, Transform board, Transform parent)
        {
            _field = field;
            _materials = materials;
            _parent = parent;
            _marks = new BaseballRunnerMarks(field, materials, board, parent);
        }

        /// <summary>走者がホームへ還った（引数はこの打席で何人目か。0 から）</summary>
        public event System.Action<int> Scored;

        /// <summary>ダイヤモンドの真ん中（走者を見せるときにカメラが見る所）</summary>
        public Vector3 DiamondCenter => _parent.TransformPoint(BaseballSpace.ToWorld(_field.Base(2) * 0.5f));

        /// <summary>打つ側のチームを替える（攻守交代・試合の始め）。塁の上の子は消す</summary>
        /// <param name="mine">この端末の人のチーム（目印を黄にする。相手は赤）</param>
        public void Use(IReadOnlyList<int> species, bool wild, bool mine)
        {
            Clear();
            _species = species;
            _wild = wild;
            _mine = mine;
        }

        public void Clear()
        {
            foreach (var runner in _runners)
            {
                runner.Body.Penguin.SetActive(false);
            }

            _runners.Clear();
            _marks.HideAll();
        }

        /// <summary>
        /// 打席の結果の走者の動きを始める（打者は打席の子のいた所から走り出す。四球は歩く）。
        /// 塁にいた走者は startDelay 秒待ってから走る（カメラが打球からダイヤモンドへ戻ってから走るのが見えるように）
        /// </summary>
        public void Apply(IReadOnlyList<BaseballRunnerMove> moves, Vector3 batterFrom, bool walk, float startDelay)
        {
            FinishRuns();
            _scoredInPlay = 0;
            foreach (var move in moves)
            {
                var runner = move.From == 0 ? Enter(move.Runner, batterFrom) : FindOn(move.From);
                if (runner == null)
                {
                    continue;
                }

                runner.Path.Clear();
                AddPath(runner, move);
                runner.Scored = move.Scored;
                runner.Out = move.Out;
                runner.Target = move.To;
                runner.Wait = move.From == 0 ? 0f : startDelay;
                runner.Speed = Mathf.Max(walk ? WalkSpeed : RunSpeed, PathLength(runner) / MaxRunSeconds);
            }
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < _standing.Length; i++)
            {
                _standing[i] = null;
            }

            for (var i = _runners.Count - 1; i >= 0; i--)
            {
                var runner = _runners[i];
                runner.Body.Penguin.Tick(deltaTime);
                if (!TickRunner(runner, deltaTime))
                {
                    runner.Body.Penguin.SetActive(false);
                    _runners.RemoveAt(i);
                    continue;
                }

                if (runner.At > 0 && runner.Leaving < 0f)
                {
                    _standing[runner.At] = _mine;
                }
            }

            _marks.ShowBases(_standing, deltaTime);
        }

        /// <summary>1人ぶん進める（待つ・走る・消えるのを待つ）。消えたら false</summary>
        private bool TickRunner(Runner runner, float deltaTime)
        {
            if (runner.Path.Count > 0)
            {
                if (runner.Wait > 0f)
                {
                    runner.Wait -= deltaTime;
                    return true;
                }

                runner.At = 0;
                Run(runner, deltaTime);
                return true;
            }

            if (runner.Leaving >= 0f)
            {
                runner.Leaving -= deltaTime;
                return runner.Leaving >= 0f;
            }

            return true;
        }

        private Runner Enter(int slot, Vector3 from)
        {
            if (slot < 0 || slot >= _species.Count)
            {
                return null;
            }

            var body = FreeBody(_species[slot]);
            body.Ring.sharedMaterial = _marks.TeamMaterial(_mine);
            body.Penguin.SetActive(true);
            body.Penguin.Motion.ResetPose();
            body.Penguin.React(false, false);
            body.Penguin.PlaceAt(from, Vector3.right, false);
            var runner = new Runner(body);
            _runners.Add(runner);
            return runner;
        }

        /// <summary>その種類の体で、いま誰も使っていない物（無ければ作る）</summary>
        private Body FreeBody(int species)
        {
            var key = (species, _wild);
            if (!_bodies.TryGetValue(key, out var bodies))
            {
                bodies = new List<Body>();
                _bodies.Add(key, bodies);
            }

            foreach (var body in bodies)
            {
                if (!InUse(body))
                {
                    return body;
                }
            }

            var penguin = BaseballPenguin.Build($"Runner{species}_{bodies.Count}", species, _wild, _materials, _parent);
            var built = new Body(penguin, _marks.BuildFootRing(penguin));
            bodies.Add(built);
            return built;
        }

        private bool InUse(Body body)
        {
            foreach (var runner in _runners)
            {
                if (runner.Body == body)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>その塁（1〜3）に立っている走者（還った・アウトで消えていく子は除く）</summary>
        private Runner FindOn(int baseNumber)
        {
            foreach (var runner in _runners)
            {
                if (runner.Target == baseNumber && runner.Leaving < 0f && !runner.Scored && !runner.Out)
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
                    var from = move.From == 0 ? runner.Body.Penguin.Root.localPosition : Spot(move.From);
                    spot = Vector3.Lerp(from, spot, OutShare);
                }

                runner.Path.Enqueue(spot);
            }
        }

        private static float PathLength(Runner runner)
        {
            var length = 0f;
            var at = runner.Body.Penguin.Root.localPosition;
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
                    runner.Body.Penguin.SetActive(false);
                    _runners.RemoveAt(i);
                    continue;
                }

                Vector3? end = null;
                while (runner.Path.Count > 0)
                {
                    end = runner.Path.Dequeue();
                }

                runner.Wait = 0f;
                if (end.HasValue)
                {
                    Arrive(runner, end.Value, false);
                }
            }
        }

        private void Run(Runner runner, float deltaTime)
        {
            var root = runner.Body.Penguin.Root;
            var target = runner.Path.Peek();
            var to = target - root.localPosition;
            var step = runner.Speed * deltaTime;
            _marks.MarkTrail(ref runner.LastTrail, root.localPosition, _mine);
            if (to.magnitude > step)
            {
                var direction = to.normalized;
                root.localPosition += direction * step;
                runner.Body.Penguin.Motion.Velocity = direction * runner.Speed;
                runner.Body.Penguin.Motion.Facing = direction;
                return;
            }

            runner.Path.Dequeue();
            if (runner.Path.Count == 0)
            {
                Arrive(runner, target, true);
            }
            else
            {
                root.localPosition = target;
            }
        }

        /// <summary>
        /// 着いた: 塁ならホームの方を向いて立つ（その塁が光る）。還った子は「+1」を出して喜び、アウトの子は「アウト」を出して悔しがって、少しして消える。
        /// shown が false（次の打席が始まって一気に着かせた）なら字も音も出さない
        /// </summary>
        private void Arrive(Runner runner, Vector3 at, bool shown)
        {
            var body = runner.Body.Penguin;
            body.Root.localPosition = at;
            body.Motion.Velocity = Vector3.zero;
            body.Face(_parent.TransformPoint(HomeSpot));
            runner.LastTrail = null;
            if (!runner.Scored && !runner.Out)
            {
                runner.At = runner.Target;
                return;
            }

            body.React(runner.Scored, runner.Out);
            runner.Leaving = LeaveSeconds;
            if (!shown)
            {
                return;
            }

            if (runner.Scored)
            {
                BaseballRunnerPopup.Spawn(_parent, at, "+1", BaseballRunnerPopup.ScoreColor);
                Scored?.Invoke(_scoredInPlay++);
            }
            else
            {
                BaseballRunnerPopup.Spawn(_parent, at, "アウト", BaseballRunnerPopup.OutColor);
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

        /// <summary>取っておく体（子と足元の輪）</summary>
        private sealed class Body
        {
            public readonly BaseballPenguin Penguin;
            public readonly Renderer Ring;

            public Body(BaseballPenguin penguin, Renderer ring)
            {
                Penguin = penguin;
                Ring = ring;
            }
        }

        private sealed class Runner
        {
            public readonly Body Body;
            public readonly Queue<Vector3> Path = new Queue<Vector3>();
            public float Speed;
            public bool Scored;
            public bool Out;
            public int Target;

            /// <summary>立っている塁（1〜3。走っている・塁に着く前なら 0）</summary>
            public int At;

            /// <summary>走り出すまでの残り秒</summary>
            public float Wait;

            /// <summary>最後に跡を置いた所</summary>
            public Vector3? LastTrail;

            /// <summary>消えるまでの残り秒（まだ消えないなら負）</summary>
            public float Leaving = -1f;

            public Runner(Body body)
            {
                Body = body;
            }
        }
    }
}
