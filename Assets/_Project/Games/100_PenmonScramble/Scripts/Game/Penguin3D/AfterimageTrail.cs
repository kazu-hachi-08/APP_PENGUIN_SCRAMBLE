using System.Collections.Generic;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 突進の残像（§9）。出している間、少しおきにペンギン・大ボスの今の形を写して置き、薄くしながら消す。
    /// 写しは体全体（ToyModel.Figure）を複製したものを使い回し、部品の位置・向き・大きさだけを写す
    /// （毎回作ると、突進のたびに部品の数だけ物が増えて重いため）
    /// </summary>
    public sealed class AfterimageTrail : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private const float IntervalSeconds = 0.04f;
        // 前の写しからこれだけ動いていなければ置かない（止まったまま重なると、本体が白くかすんで見えるため）
        private const float MinSpacing = 0.2f;
        private const float LifeSeconds = 0.2f;
        private const float StartAlpha = 0.45f;
        // 元の色そのままだと黒い背中が影のように見えるので、少し白っぽくして「速さの跡」に見せる
        private const float Whiten = 0.3f;
        // 間隔と長さから、同時に出る写しの数は (長さ / 間隔) + 1 まで
        private const int MaxGhosts = 6;

        private readonly List<Ghost> _ghosts = new List<Ghost>();
        private MaterialPropertyBlock _block;
        private ToyModel _model;
        private Transform[] _sourceNodes;
        private Renderer[] _sourceRenderers;
        private Material _material;
        private Transform _stage;
        private float _timer;
        private Vector3 _lastSpawn;
        private bool _hasLastSpawn;

        /// <summary>残像を出している（突進中）</summary>
        public bool Emitting { get; set; }

        /// <summary>いま見えている写しの数</summary>
        public int VisibleCount
        {
            get
            {
                var count = 0;
                foreach (var ghost in _ghosts)
                {
                    count += ghost.Root.activeSelf ? 1 : 0;
                }

                return count;
            }
        }

        /// <param name="stage">写しを置く場所。ユニットと一緒に動かないよう、ユニットの外に置く</param>
        public void Bind(ToyModel model, Material ghostMaterial, Transform stage)
        {
            Clear();
            _model = model;
            _material = ghostMaterial;
            _stage = stage;
            _sourceNodes = model.Figure.GetComponentsInChildren<Transform>(true);
            _sourceRenderers = model.Figure.GetComponentsInChildren<MeshRenderer>(true);
            _hasLastSpawn = false;
            _block ??= new MaterialPropertyBlock();
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>dt 秒進める（テストからも呼ぶ）</summary>
        public void Tick(float dt)
        {
            if (Emitting && _model != null)
            {
                _timer -= dt;
                if (_timer <= 0f && HasMovedFromLastSpawn())
                {
                    _timer = IntervalSeconds;
                    Spawn();
                }
            }
            else
            {
                // 次に出し始めた瞬間に1つ目を置く
                _timer = 0f;
                _hasLastSpawn = false;
            }

            foreach (var ghost in _ghosts)
            {
                Fade(ghost, dt);
            }
        }

        private bool HasMovedFromLastSpawn()
        {
            return !_hasLastSpawn || (_model.Figure.position - _lastSpawn).sqrMagnitude >= MinSpacing * MinSpacing;
        }

        private void Spawn()
        {
            _lastSpawn = _model.Figure.position;
            _hasLastSpawn = true;
            var ghost = FreeGhost();
            ghost.Age = 0f;
            ghost.Root.SetActive(true);
            var figure = _model.Figure;
            ghost.Root.transform.SetPositionAndRotation(figure.position, figure.rotation);
            ghost.Root.transform.localScale = figure.lossyScale;
            // 0 番は Figure 自身（上で置いた）。部品は同じ並びで複製してあるので、順に写せばよい
            for (var i = 1; i < _sourceNodes.Length; i++)
            {
                var source = _sourceNodes[i];
                var copy = ghost.Nodes[i];
                copy.localPosition = source.localPosition;
                copy.localRotation = source.localRotation;
                copy.localScale = source.localScale;
            }

            for (var i = 0; i < _sourceRenderers.Length; i++)
            {
                var color = _sourceRenderers[i].sharedMaterial.GetColor(BaseColorId);
                ghost.Colors[i] = Color.Lerp(color, Color.white, Whiten);
            }

            Fade(ghost, 0f);
        }

        /// <summary>消えている写しを使う。全部見えていれば、いちばん古いものを使い回す</summary>
        private Ghost FreeGhost()
        {
            Ghost oldest = null;
            foreach (var ghost in _ghosts)
            {
                if (!ghost.Root.activeSelf)
                {
                    return ghost;
                }

                if (oldest == null || ghost.Age > oldest.Age)
                {
                    oldest = ghost;
                }
            }

            if (_ghosts.Count < MaxGhosts || oldest == null)
            {
                var created = CreateGhost();
                _ghosts.Add(created);
                return created;
            }

            return oldest;
        }

        private Ghost CreateGhost()
        {
            var root = Instantiate(_model.Figure.gameObject, _stage);
            root.name = "Afterimage";
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return new Ghost(root, root.GetComponentsInChildren<Transform>(true), renderers);
        }

        private void Fade(Ghost ghost, float dt)
        {
            if (!ghost.Root.activeSelf)
            {
                return;
            }

            ghost.Age += dt;
            if (ghost.Age >= LifeSeconds)
            {
                ghost.Root.SetActive(false);
                return;
            }

            var alpha = StartAlpha * (1f - ghost.Age / LifeSeconds);
            for (var i = 0; i < ghost.Renderers.Length; i++)
            {
                var color = ghost.Colors[i];
                color.a = alpha;
                _block.SetColor(BaseColorId, color);
                ghost.Renderers[i].SetPropertyBlock(_block);
            }
        }

        /// <summary>写しを全部消す（組み直したとき・ユニットが消えたとき）</summary>
        private void Clear()
        {
            foreach (var ghost in _ghosts)
            {
                if (ghost.Root == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(ghost.Root);
                }
                else
                {
                    DestroyImmediate(ghost.Root);
                }
            }

            _ghosts.Clear();
        }

        private void OnDestroy() => Clear();

        private sealed class Ghost
        {
            public readonly GameObject Root;
            public readonly Transform[] Nodes;
            public readonly Renderer[] Renderers;
            public readonly Color[] Colors;
            public float Age;

            public Ghost(GameObject root, Transform[] nodes, Renderer[] renderers)
            {
                Root = root;
                Nodes = nodes;
                Renderers = renderers;
                Colors = new Color[renderers.Length];
            }
        }
    }
}
