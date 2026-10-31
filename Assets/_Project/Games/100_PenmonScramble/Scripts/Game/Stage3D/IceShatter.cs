using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 氷の塊が溶けたときに砕けて飛び散るかけら。見た目だけなので物理演算は使わず、1回分をこのコンポーネント1つでまとめて動かす。
    /// かけらは小さいので、半透明ではなく縁の線のある明るい水色にして、乱戦の中でも見えるようにする
    /// </summary>
    public sealed class IceShatter : MonoBehaviour
    {
        private static readonly Color ShardColor = new Color(0.8f, 0.96f, 1f);
        private const float Gloss = 1f;
        private const int Shards = 7;
        // 塊の太さに対する、かけらの大きさ
        private const float ShardSizeMin = 0.22f;
        private const float ShardSizeMax = 0.34f;

        private const float LifeSeconds = 0.55f;
        private const float ShrinkFrom = 0.5f;
        private const float Gravity = 14f;
        private const float SpreadSpeedMin = 1.2f;
        private const float SpreadSpeedMax = 2.6f;
        private const float UpSpeedMin = 2f;
        private const float UpSpeedMax = 4f;
        private const float SpinSpeedMax = 540f;

        private Transform[] _pieces;
        private Vector3[] _velocities;
        private Vector3[] _spins;
        private Vector3[] _sizes;
        private float _age;

        /// <param name="bottom">塊の底の真ん中</param>
        /// <param name="blockSize">塊の大きさ</param>
        public static void Spawn(Transform parent, StageMaterials materials, Vector3 bottom, Vector3 blockSize)
        {
            var shatter = new GameObject("IceShatter").AddComponent<IceShatter>();
            shatter.transform.SetParent(parent, false);
            shatter.transform.position = bottom;
            shatter.Create(materials.Toon.Get(ShardColor, Gloss), blockSize);
        }

        private void Create(Material material, Vector3 blockSize)
        {
            _pieces = new Transform[Shards];
            _velocities = new Vector3[Shards];
            _spins = new Vector3[Shards];
            _sizes = new Vector3[Shards];
            for (var i = 0; i < Shards; i++)
            {
                var outward = ToyEasing.RingDirection(i, Shards, 0.6f);
                var start = outward * (blockSize.x * 0.25f) + Vector3.up * (blockSize.y * Random.Range(0.2f, 0.8f));
                _sizes[i] = Vector3.one * (blockSize.x * Random.Range(ShardSizeMin, ShardSizeMax));
                _pieces[i] = CreatePiece(material, start, _sizes[i]);
                _velocities[i] = outward * Random.Range(SpreadSpeedMin, SpreadSpeedMax) + Vector3.up * Random.Range(UpSpeedMin, UpSpeedMax);
                _spins[i] = Random.insideUnitSphere * SpinSpeedMax;
            }
        }

        private Transform CreatePiece(Material material, Vector3 localPosition, Vector3 size)
        {
            var piece = new GameObject("Shard");
            piece.transform.SetParent(transform, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = size;
            piece.transform.localRotation = Random.rotation;
            var renderer = ToyParts.AddMesh(piece, ToyMeshes.IceChunk, material);
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

            var shrink = ToyEasing.Shrink(t, ShrinkFrom);
            for (var i = 0; i < _pieces.Length; i++)
            {
                _velocities[i] += Vector3.down * (Gravity * dt);
                var position = _pieces[i].position + _velocities[i] * dt;
                // 床に落ちたら止まる（はねると、コインと星の演出とまぎらわしい）
                if (position.y < 0f)
                {
                    position.y = 0f;
                    _velocities[i] = Vector3.zero;
                    _spins[i] = Vector3.zero;
                }

                _pieces[i].position = position;
                _pieces[i].Rotate(_spins[i] * dt, Space.Self);
                _pieces[i].localScale = _sizes[i] * shrink;
            }
        }
    }
}
