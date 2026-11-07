using System.Collections.Generic;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// アイスロードの跡: 凍った道を転がる球の通った所に、薄い氷の丸を並べる（どこまで凍っているか・アウトの穴を渡ったのが見えるように）。
    /// 丸は最初に作っておいて使い回す（1球ごとに作って消さない）
    /// </summary>
    public sealed class BaseballIceTrail
    {
        private static readonly Color IceColor = new Color(0.78f, 0.95f, 1f);
        private const float Gloss = 0.8f;
        // 盤に重ねた面（土・線・穴の札）より上、球の影より下
        private const float Lift = 0.055f;
        private const float Spacing = 0.22f;
        private const float Size = 0.42f;
        // 凍る道の長さ（★3 で 9 マス）を並べきれる数
        private const int MaxMarks = 48;

        private readonly List<Transform> _marks = new List<Transform>();
        private int _used;
        private Vector3? _last;

        public BaseballIceTrail(ToonMaterials materials, Transform parent)
        {
            var root = new GameObject("IceTrail").transform;
            root.SetParent(parent, false);
            var material = materials.Get(IceColor, Gloss);
            for (var i = 0; i < MaxMarks; i++)
            {
                var obj = new GameObject("Ice");
                obj.transform.SetParent(root, false);
                obj.transform.localScale = new Vector3(Size, 1f, Size);
                obj.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.FloorDisc;
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                obj.SetActive(false);
                _marks.Add(obj.transform);
            }
        }

        /// <summary>球の今の位置。前の丸から Spacing 進んだら、次の丸を置く</summary>
        public void Mark(Vector3 ball)
        {
            var ground = new Vector3(ball.x, Lift, ball.z);
            if (_last.HasValue && (ground - _last.Value).sqrMagnitude < Spacing * Spacing)
            {
                return;
            }

            if (_used >= _marks.Count)
            {
                return;
            }

            var mark = _marks[_used++];
            mark.localPosition = ground;
            mark.gameObject.SetActive(true);
            _last = ground;
        }

        public void Clear()
        {
            for (var i = 0; i < _used; i++)
            {
                _marks[i].gameObject.SetActive(false);
            }

            _used = 0;
            _last = null;
        }
    }
}
