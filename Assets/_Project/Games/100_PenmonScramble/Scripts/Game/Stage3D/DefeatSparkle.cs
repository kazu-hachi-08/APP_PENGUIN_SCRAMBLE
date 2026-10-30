using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 敵を倒したときに飛び散るコインと星（§9）。上へはじけて弧を描いて落ち、床で1回はねて小さくなって消える。
    /// 見た目だけなので物理演算は使わず、1回分のかけらをこのコンポーネント1つでまとめて動かす
    /// </summary>
    public sealed class DefeatSparkle : MonoBehaviour
    {
        private static readonly Color CoinColor = new Color(1f, 0.78f, 0.15f);
        private static readonly Color StarColor = new Color(1f, 0.95f, 0.45f);
        private static readonly Vector3 CoinSize = new Vector3(0.24f, 0.06f, 0.24f);
        private const float StarSize = 0.3f;
        private const float Gloss = 1f;

        private const float LifeSeconds = 0.9f;
        // 終わりのこの割合から小さくしていく
        private const float ShrinkFrom = 0.65f;
        private const float Gravity = 16f;
        private const float BounceRestitution = 0.35f;
        private const float SpreadSpeedMin = 1.2f;
        private const float SpreadSpeedMax = 2.6f;
        private const float UpSpeedMin = 4.5f;
        private const float UpSpeedMax = 6.5f;
        private const float SpinSpeedMax = 720f;
        // 倒れた子の胸のあたりから出す
        private const float StartHeight = 0.5f;

        private Transform[] _pieces;
        private Vector3[] _velocities;
        private Vector3[] _spins;
        private Vector3[] _sizes;
        private float _age;

        /// <param name="coins">コインの数。星はその半分（少なくとも1つ）</param>
        public static void Spawn(Transform parent, StageMaterials materials, Vector2 position, int coins, float spread)
        {
            var sparkle = new GameObject("DefeatSparkle").AddComponent<DefeatSparkle>();
            sparkle.transform.SetParent(parent, false);
            sparkle.transform.position = position.ToFloor(StartHeight);
            var stars = Mathf.Max(1, coins / 2);
            sparkle.Create(materials, coins, stars, spread);
        }

        private void Create(StageMaterials materials, int coins, int stars, float spread)
        {
            var count = coins + stars;
            _pieces = new Transform[count];
            _velocities = new Vector3[count];
            _spins = new Vector3[count];
            _sizes = new Vector3[count];
            var coin = materials.Toon.Get(CoinColor, Gloss);
            var star = materials.Toon.Get(StarColor, Gloss);
            for (var i = 0; i < count; i++)
            {
                var isCoin = i < coins;
                _sizes[i] = isCoin ? CoinSize : Vector3.one * StarSize;
                _pieces[i] = CreatePiece(isCoin ? ToyMeshes.Puck : ToyMeshes.Star, isCoin ? coin : star, _sizes[i]);
                // 全部が同じ向きへ飛ばないよう、ぐるりと等間隔に散らしてから少しずらす（見た目だけなので Core の乱数とは関係ない）
                var angle = (i + Random.value * 0.6f) / count * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (Random.Range(SpreadSpeedMin, SpreadSpeedMax) * spread);
                _velocities[i] = outward + Vector3.up * Random.Range(UpSpeedMin, UpSpeedMax);
                _spins[i] = Random.insideUnitSphere * SpinSpeedMax;
            }
        }

        private Transform CreatePiece(Mesh mesh, Material material, Vector3 size)
        {
            var piece = new GameObject("Piece");
            piece.transform.SetParent(transform, false);
            piece.transform.localScale = size;
            piece.transform.localRotation = Random.rotation;
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return piece.transform;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            _age += dt;
            var t = _age / LifeSeconds;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            var shrink = t < ShrinkFrom ? 1f : 1f - (t - ShrinkFrom) / (1f - ShrinkFrom);
            for (var i = 0; i < _pieces.Length; i++)
            {
                Move(i, dt);
                _pieces[i].localScale = _sizes[i] * shrink;
            }
        }

        private void Move(int i, float dt)
        {
            var piece = _pieces[i];
            _velocities[i] += Vector3.down * (Gravity * dt);
            var position = piece.position + _velocities[i] * dt;
            // 床（高さ0）より下へは行かず、はね返る。かけらの大きさの半分だけ浮かせて、めり込んで見えないようにする
            var floor = _sizes[i].x * 0.5f;
            if (position.y < floor && _velocities[i].y < 0f)
            {
                position.y = floor;
                _velocities[i] = new Vector3(_velocities[i].x * BounceRestitution, -_velocities[i].y * BounceRestitution, _velocities[i].z * BounceRestitution);
            }

            piece.position = position;
            piece.Rotate(_spins[i] * dt, Space.Self);
        }
    }
}
