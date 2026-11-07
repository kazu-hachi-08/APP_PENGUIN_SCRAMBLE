using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 球: おもちゃの白い球と、盤の上の丸い影（浮いた球は影が小さく薄くなるので、どこへ落ちるかが分かる）。
    /// 転がった道のりの分だけ回す。穴に入った球は穴の中へ沈める。ホームランの球はフェンスで止めず、
    /// 打球の勢いのままフェンスの向こうへ飛んでいく所まで見せる（Core はフェンスを越えた所で結果を決めて止まるので、ここで続きを描く）。
    /// 投げる前は投げる子が持っているので見せない
    /// </summary>
    public sealed class BaseballBallView
    {
        private const float Gloss = 0.6f;
        // 盤に重ねた面（土・線・通り道・穴の札）より上に置く（下だと影が隠れる）
        private const float ShadowLift = 0.07f;
        private const float ShadowSize = 2.2f;
        private const float ShadowShrinkPerHeight = 0.18f;
        private const float ShadowMinScale = 0.35f;
        // 穴に入った球は、黒い穴の中に白い頭が少しだけ見える深さに沈める
        private const float SinkDepth = BaseballBalance.BallRadius * 0.85f;
        private const float FlyAwaySeconds = 2.2f;
        private const float MittSeconds = 0.14f;
        private static readonly Color BallColor = new Color(0.98f, 0.98f, 0.96f);

        private readonly Transform _ball;
        private readonly Transform _shadow;
        private Vector3 _flyVelocity;
        private float _flyTime = -1f;
        private Vector3 _mittFrom;
        private float _mittTime = -1f;

        public BaseballBallView(ToonMaterials materials, Transform parent)
        {
            _ball = Part("Ball", ToyMeshes.Sphere, materials.Get(BallColor, Gloss), parent);
            _ball.localScale = Vector3.one * (BaseballBalance.BallRadius * 2f);
            _shadow = Part("BallShadow", ToyMeshes.FloorQuad, materials.BlobShadow, parent);
            SetVisible(false);
        }

        public Vector3 Position { get; private set; }

        public bool Visible { get; private set; }

        private bool Flying => _flyTime >= 0f;

        public void Sync(BaseballBoard board, BaseballFrame frame, float deltaTime)
        {
            if (Flying)
            {
                TickFlyAway(deltaTime);
                return;
            }

            if (_mittTime >= 0f)
            {
                TickMitt(deltaTime);
                return;
            }

            var shown = board.Phase == BaseballBoardPhase.Pitching
                || board.Phase == BaseballBoardPhase.BallInPlay
                || (board.Phase == BaseballBoardPhase.Done && board.LastHit.HasValue);
            SetVisible(shown);
            if (!shown)
            {
                return;
            }

            var sunk = board.Phase == BaseballBoardPhase.Done && board.Ball.HoleIndex >= 0;
            var target = sunk ? frame.Ball + Vector3.down * SinkDepth : frame.Ball;
            Roll(target - Position);
            Position = target;
            _ball.localPosition = target;
            _shadow.gameObject.SetActive(!sunk);
            PlaceShadow(target);
        }

        /// <summary>ホームランの球をフェンスの向こうへ飛ばし続ける（越えた瞬間の勢いのまま）</summary>
        public void FlyAway(BaseballBallSim sim)
        {
            _flyVelocity = new Vector3(sim.Velocity.X, sim.VerticalSpeed, sim.Velocity.Y);
            _flyTime = 0f;
        }

        /// <summary>
        /// 投げた球をキャッチャーのミットまで運んで消す（Core はホームの少し後ろで1球を決めて止めるので、ミットまでの続きをここで描く）
        /// </summary>
        public void ToMitt()
        {
            _mittFrom = Position;
            _mittTime = 0f;
        }

        /// <summary>次の球の前に隠す（投げる子が持っている）</summary>
        public void ResetPitch()
        {
            _flyTime = -1f;
            _mittTime = -1f;
            SetVisible(false);
        }

        private void TickMitt(float deltaTime)
        {
            _mittTime += deltaTime;
            var t = Mathf.Clamp01(_mittTime / MittSeconds);
            Position = Vector3.Lerp(_mittFrom, BaseballFielders.MittPoint, t);
            _ball.localPosition = Position;
            PlaceShadow(Position);
            if (t >= 1f)
            {
                _mittTime = -1f;
                SetVisible(false);
            }
        }

        private void TickFlyAway(float deltaTime)
        {
            _flyTime += deltaTime;
            _flyVelocity += Vector3.down * (BaseballBalance.Gravity * deltaTime);
            var next = Position + _flyVelocity * deltaTime;
            Roll(next - Position);
            Position = next;
            _ball.localPosition = next;
            _shadow.gameObject.SetActive(false);
            if (_flyTime >= FlyAwaySeconds)
            {
                SetVisible(false);
            }
        }

        /// <summary>動いた分だけ、動く向きに直角な軸で転がす</summary>
        private void Roll(Vector3 moved)
        {
            moved.y = 0f;
            var distance = moved.magnitude;
            if (distance <= 0f || distance > 1f)
            {
                return;
            }

            var axis = Vector3.Cross(Vector3.up, moved / distance);
            _ball.Rotate(axis, distance / BaseballBalance.BallRadius * Mathf.Rad2Deg, Space.World);
        }

        private void PlaceShadow(Vector3 center)
        {
            var height = center.y - BaseballBalance.BallRadius;
            var size = BaseballBalance.BallRadius * ShadowSize * Mathf.Max(ShadowMinScale, 1f - height * ShadowShrinkPerHeight);
            _shadow.localPosition = new Vector3(center.x, ShadowLift, center.z);
            _shadow.localScale = new Vector3(size, 1f, size);
        }

        private void SetVisible(bool visible)
        {
            Visible = visible;
            _ball.gameObject.SetActive(visible);
            _shadow.gameObject.SetActive(visible);
        }

        private static Transform Part(string name, Mesh mesh, Material material, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return obj.transform;
        }
    }
}
