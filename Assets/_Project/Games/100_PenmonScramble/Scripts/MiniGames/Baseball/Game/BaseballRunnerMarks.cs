using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 走者の目印: 走者が立っている塁を囲む光る輪、走者の足元の輪、走った跡の小さな丸（少しして縮んで消える）。
    /// 色は自分のチームが黄・相手のチームが赤（守備の子・相手の暗い色の子にまぎれず、どの塁に誰がいるかが盤だけで分かるように）。
    /// 輪と丸は最初に作っておいて使い回す
    /// </summary>
    public sealed class BaseballRunnerMarks
    {
        public static readonly Color MineColor = new Color(1f, 0.84f, 0.2f);
        public static readonly Color RivalColor = new Color(1f, 0.36f, 0.32f);
        private const float Gloss = 0.5f;

        // 塁の輪は白い台の角の外を回る大きさ。盤の線・札・穴より上に浮かせる
        private const float BaseRingMargin = 0.28f;
        private const float BaseRingLift = 0.06f;
        private const float BaseRingFlatten = 0.6f;
        // 塁の輪はゆっくり膨らんだり縮んだりして、止まっている盤の上で目に入るようにする
        private const float PulseSpeed = 4.5f;
        private const float PulseAmount = 0.07f;

        // 足元の輪は子（半径 0.26）の体のまわりに見える大きさ
        private const float FootRingDiameter = 0.78f;
        private const float FootRingFlatten = 0.5f;
        private const float FootRingLift = 0.01f;

        private const float TrailSpacing = 0.24f;
        private const float TrailSize = 0.2f;
        private const float TrailSeconds = 0.6f;
        // 4人が同時に走っても、1人あたり塁2つ分ほどの跡が並べられる数
        private const int TrailMarks = 64;

        private readonly Transform[] _baseRings = new Transform[BaseballRules.BaseCount + 1];
        private readonly Renderer[] _baseRenderers = new Renderer[BaseballRules.BaseCount + 1];
        private readonly float[] _baseSizes = new float[BaseballRules.BaseCount + 1];
        private readonly List<TrailMark> _trail = new List<TrailMark>();
        private readonly ToonMaterials _materials;
        private readonly Transform _figures;
        private float _clock;
        private int _nextTrail;

        /// <param name="board">盤（塁の輪を置く）</param>
        /// <param name="figures">子を立たせる所（足元の輪と跡を置く）</param>
        public BaseballRunnerMarks(BaseballFieldDef field, ToonMaterials materials, Transform board, Transform figures)
        {
            _materials = materials;
            _figures = figures;
            BuildBaseRings(field, board);
            BuildTrail();
        }

        /// <summary>そのチームの目印の色</summary>
        public Material TeamMaterial(bool mine) => _materials.Get(mine ? MineColor : RivalColor, Gloss);

        /// <summary>子の足元の輪を作る（子の体の下に付けるので、出てくるときに体といっしょにぽんと膨らむ）</summary>
        public Renderer BuildFootRing(BaseballPenguin body)
        {
            var obj = new GameObject("RunnerRing");
            obj.transform.SetParent(body.Root, false);
            var scale = FootRingDiameter / Mathf.Max(0.0001f, body.Root.localScale.x);
            obj.transform.localScale = new Vector3(scale, scale * FootRingFlatten, scale);
            obj.transform.localPosition = Vector3.up * (FootRingLift / Mathf.Max(0.0001f, body.Root.localScale.x));
            return AddRenderer(obj, ToyMeshes.Torus);
        }

        /// <summary>塁ごとに、立っている走者がいるか（いれば光る輪をその色で出す）</summary>
        public void ShowBases(bool?[] standingMine, float deltaTime)
        {
            _clock += deltaTime;
            var pulse = 1f + Mathf.Sin(_clock * PulseSpeed) * PulseAmount;
            for (var b = 1; b <= BaseballRules.BaseCount; b++)
            {
                var ring = _baseRings[b];
                var shown = standingMine[b].HasValue;
                ring.gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                _baseRenderers[b].sharedMaterial = TeamMaterial(standingMine[b].Value);
                var size = _baseSizes[b] * pulse;
                ring.localScale = new Vector3(size, size * BaseRingFlatten, size);
            }

            TickTrail(deltaTime);
        }

        /// <summary>走っている子の跡を1つ置く（前の跡から TrailSpacing 進んだときだけ）</summary>
        public void MarkTrail(ref Vector3? last, Vector3 at, bool mine)
        {
            var ground = new Vector3(at.x, FootRingLift, at.z);
            if (last.HasValue && (ground - last.Value).sqrMagnitude < TrailSpacing * TrailSpacing)
            {
                return;
            }

            last = ground;
            var mark = _trail[_nextTrail];
            _nextTrail = (_nextTrail + 1) % _trail.Count;
            mark.Renderer.sharedMaterial = _materials.Floor(Texture2D.whiteTexture, mine ? MineColor : RivalColor);
            mark.Transform.localPosition = ground;
            mark.Age = 0f;
            mark.Transform.gameObject.SetActive(true);
        }

        public void HideAll()
        {
            for (var b = 1; b <= BaseballRules.BaseCount; b++)
            {
                _baseRings[b].gameObject.SetActive(false);
            }

            foreach (var mark in _trail)
            {
                mark.Transform.gameObject.SetActive(false);
            }
        }

        private void BuildBaseRings(BaseballFieldDef field, Transform board)
        {
            for (var b = 1; b <= BaseballRules.BaseCount; b++)
            {
                var obj = new GameObject($"BaseRing{b}");
                obj.transform.SetParent(board, false);
                obj.transform.localPosition = BaseballSpace.ToWorld(field.Base(b), BaseRingLift);
                var bag = b == 2 ? BaseballView.SecondBaseSize : BaseballView.BaseSize;
                // 台は45°傾けた正方形で、真ん中から角まで bag の半分
                _baseSizes[b] = (bag * 0.5f + BaseRingMargin) * 2f;
                _baseRenderers[b] = AddRenderer(obj, ToyMeshes.Torus);
                _baseRings[b] = obj.transform;
                obj.SetActive(false);
            }
        }

        private void BuildTrail()
        {
            var root = new GameObject("RunnerTrail").transform;
            root.SetParent(_figures, false);
            for (var i = 0; i < TrailMarks; i++)
            {
                var obj = new GameObject("Trail");
                obj.transform.SetParent(root, false);
                var renderer = AddRenderer(obj, ToyMeshes.FloorDisc);
                obj.SetActive(false);
                _trail.Add(new TrailMark(obj.transform, renderer));
            }
        }

        /// <summary>跡は置いてから縮んで消える（線が残り続けて盤が散らからないように）</summary>
        private void TickTrail(float deltaTime)
        {
            foreach (var mark in _trail)
            {
                if (!mark.Transform.gameObject.activeSelf)
                {
                    continue;
                }

                mark.Age += deltaTime;
                var left = 1f - mark.Age / TrailSeconds;
                if (left <= 0f)
                {
                    mark.Transform.gameObject.SetActive(false);
                    continue;
                }

                var size = TrailSize * left;
                mark.Transform.localScale = new Vector3(size, 1f, size);
            }
        }

        private static Renderer AddRenderer(GameObject obj, Mesh mesh)
        {
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }

        private sealed class TrailMark
        {
            public readonly Transform Transform;
            public readonly Renderer Renderer;
            public float Age;

            public TrailMark(Transform transform, Renderer renderer)
            {
                Transform = transform;
                Renderer = renderer;
            }
        }
    }
}
