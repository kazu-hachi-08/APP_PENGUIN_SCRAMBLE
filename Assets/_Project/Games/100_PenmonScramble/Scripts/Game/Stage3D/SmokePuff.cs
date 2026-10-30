using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 交代・ダウン・起き上がりの「ぽん」という煙（§9）。白い玉がぱっとふくらみ、外へ広がりながらしぼんで消える。
    /// 煙が一瞬ペンギンを隠すので、組み直した瞬間の入れ替わりが目立たない。見た目だけなので物理演算は使わない
    /// </summary>
    public sealed class SmokePuff : MonoBehaviour
    {
        private static readonly Color SmokeColor = new Color(0.97f, 0.97f, 1f);
        private const float Gloss = 0f;
        private const int Puffs = 9;
        private const float LifeSeconds = 0.42f;

        // 大きさ・位置・速さは、ユニットの当たりの半径に対する倍率（大きいボスほど大きな煙になる）
        private const float PuffSizeMin = 0.9f;
        private const float PuffSizeMax = 1.4f;
        private const float StartSpread = 0.5f;
        private const float SpreadSpeed = 3.2f;
        private const float RiseSpeed = 1.2f;
        // 速さが毎秒この割合ずつ落ちる（ふわっと止まる）
        private const float Drag = 5f;

        private Transform[] _puffs;
        private Vector3[] _velocities;
        private float[] _sizes;
        private float _age;

        /// <param name="height">ペンギンの背の高さ。煙を足元から頭まで散らして体全体を隠す</param>
        public static void Spawn(Transform parent, StageMaterials materials, Vector2 position, float radius, float height)
        {
            var puff = new GameObject("SmokePuff").AddComponent<SmokePuff>();
            puff.transform.SetParent(parent, false);
            puff.transform.position = position.ToFloor();
            puff.Create(materials.Toon.Get(SmokeColor, Gloss), radius, height);
        }

        private void Create(Material material, float radius, float height)
        {
            _puffs = new Transform[Puffs];
            _velocities = new Vector3[Puffs];
            _sizes = new float[Puffs];
            for (var i = 0; i < Puffs; i++)
            {
                // ぐるりと等間隔に散らしてから少しずらし、高さは下から上へ順に（見た目だけなので Core の乱数とは関係ない）
                var angle = (i + Random.value * 0.7f) / Puffs * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var y = height * (0.15f + 0.75f * ((i * 0.618f) % 1f));
                var start = outward * (radius * StartSpread) + Vector3.up * y;
                _sizes[i] = radius * Random.Range(PuffSizeMin, PuffSizeMax);
                _puffs[i] = CreatePuff(material, start);
                _velocities[i] = (outward * SpreadSpeed + Vector3.up * RiseSpeed) * radius;
            }

            Apply(0f);
        }

        private Transform CreatePuff(Material material, Vector3 localPosition)
        {
            var obj = new GameObject("Puff");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPosition;
            obj.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.Sphere;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return obj.transform;
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

            var keep = Mathf.Max(0f, 1f - Drag * dt);
            for (var i = 0; i < _puffs.Length; i++)
            {
                _puffs[i].localPosition += _velocities[i] * dt;
                _velocities[i] *= keep;
            }

            Apply(t);
        }

        /// <summary>はじめの 1/4 で一気にふくらみ、残りでしぼむ（sin(π√t) は t = 0.25 で 1）</summary>
        private void Apply(float t)
        {
            var swell = Mathf.Sin(Mathf.PI * Mathf.Sqrt(t));
            for (var i = 0; i < _puffs.Length; i++)
            {
                _puffs[i].localScale = Vector3.one * (_sizes[i] * swell);
            }
        }
    }
}
