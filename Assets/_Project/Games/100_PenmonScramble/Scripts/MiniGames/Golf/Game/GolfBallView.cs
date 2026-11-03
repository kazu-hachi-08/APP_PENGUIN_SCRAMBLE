using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 1人の球の見た目: 白い球に席の色の帯（オンラインで誰の球か見分ける）。進んだ分だけ転がって帯が回り、
    /// 足元に丸い影（浮くと小さく）。カップに入ると穴へ沈む。コースから外れた球は隠す
    /// </summary>
    public sealed class GolfBallView
    {
        private const float BallGloss = 0.9f;
        // 帯は球より少しだけ大きい輪を、縦に厚くして巻く
        private const float BandWiden = 1.04f;
        private const float BandThickness = 3f;
        private const float ShadowSize = 2.2f;
        // 地面の種類の板（池・砂地）より上に出す（下に隠れないように）
        private const float ShadowLift = 0.04f;
        private const float ShadowFadeHeight = 1.2f;
        private const float ShadowMinScale = 0.45f;
        // カップに入ってから沈みきるまでの秒と、沈む深さ（球の直径より深く。地面の台に隠れて見えなくなる）
        private const float SinkSeconds = 0.25f;
        private const float SinkDepth = GolfBalance.BallRadius * 2.4f;

        private readonly Transform _root;
        private readonly Transform _spin;
        private readonly Transform _shadow;
        private Vector3 _lastGround;
        private bool _placed;
        private float _sink;

        public GolfBallView(ToonMaterials materials, Transform parent, Color seatColor)
        {
            _root = new GameObject("Ball").transform;
            _root.SetParent(parent, false);
            _spin = new GameObject("Spin").transform;
            _spin.SetParent(_root, false);
            var diameter = GolfBalance.BallRadius * 2f;
            Add("Shell", ToyMeshes.Sphere, _spin, Vector3.one * diameter, materials.Get(Color.white, BallGloss));
            Add("Band", ToyMeshes.Torus, _spin, new Vector3(diameter * BandWiden, diameter * BandThickness, diameter * BandWiden), materials.Get(seatColor, BallGloss));

            _shadow = new GameObject("Shadow").transform;
            _shadow.SetParent(parent, false);
            var shadow = Add("Blob", ToyMeshes.FloorQuad, _shadow, new Vector3(diameter * ShadowSize, 1f, diameter * ShadowSize), materials.BlobShadow);
            shadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadow.receiveShadows = false;
        }

        public void Sync(GolfBall ball, GolfSurface surface, float deltaTime)
        {
            _root.gameObject.SetActive(ball.Active);
            _shadow.gameObject.SetActive(ball.Active && !ball.InCup);
            if (!ball.Active)
            {
                _placed = false;
                return;
            }

            var groundHeight = surface.HeightAt(ball.Position);
            var ground = GolfSpace.ToWorld(ball.Position, groundHeight);
            Roll(ground);
            _sink = ball.InCup ? Mathf.Min(1f, _sink + deltaTime / SinkSeconds) : 0f;
            var sink = _sink * _sink * SinkDepth;
            _root.localPosition = ground + Vector3.up * (GolfBalance.BallRadius + ball.Height - sink);

            var shadowScale = Mathf.Lerp(1f, ShadowMinScale, Mathf.Clamp01(ball.Height / ShadowFadeHeight));
            _shadow.localPosition = ground + Vector3.up * ShadowLift;
            _shadow.localScale = Vector3.one * shadowScale;
        }

        /// <summary>地面を進んだ分だけ、進む向きと直角の軸で回す（滑らず転がって見えるように）。置き直し（池の戻し）は回さない</summary>
        private void Roll(Vector3 ground)
        {
            var moved = ground - _lastGround;
            moved.y = 0f;
            _lastGround = ground;
            if (!_placed)
            {
                _placed = true;
                return;
            }

            var distance = moved.magnitude;
            if (distance <= 0f || distance > GolfBalance.BallRadius * 4f)
            {
                return;
            }

            var axis = Vector3.Cross(Vector3.up, moved / distance);
            var degrees = distance / GolfBalance.BallRadius * Mathf.Rad2Deg;
            _spin.localRotation = Quaternion.AngleAxis(degrees, axis) * _spin.localRotation;
        }

        private static MeshRenderer Add(string name, Mesh mesh, Transform parent, Vector3 scale, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }
    }
}
