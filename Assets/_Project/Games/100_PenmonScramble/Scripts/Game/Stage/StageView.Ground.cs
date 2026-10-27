using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージの地面・エリアの出入口・カメラ（§6.1）
    /// </summary>
    public partial class StageView
    {
        private const float GateWidth = 0.35f;
        // 出入口がいまの範囲の内側にあるか（開いているか）を比べるときの余裕
        private const float GateEpsilon = 0.01f;

        [SerializeField] private Color _areaAltColor = new Color(0.84f, 0.91f, 0.96f);
        [SerializeField] private Color _bossAreaColor = new Color(0.98f, 0.88f, 0.88f);
        [SerializeField] private Color _gateColor = new Color(0.40f, 0.32f, 0.28f);
        [SerializeField] private StageCamera _stageCamera;

        private readonly List<(float x, GameObject obj)> _gates = new List<(float x, GameObject obj)>();

        /// <summary>エリアを1枚ずつ色を変えて敷き、エリアの境目に出入口を置く。ボスエリアは赤っぽくする</summary>
        private void CreateGround(StageSnapshot snapshot)
        {
            _gates.Clear();
            for (var i = 0; i < snapshot.AreaCount; i++)
            {
                var min = StageLayout.AreaMin(i).ToVector2();
                var max = StageLayout.AreaMax(i).ToVector2();
                var isBossArea = snapshot.AreaCount > 1 && i == snapshot.AreaCount - 1;
                var color = isBossArea ? _bossAreaColor : i % 2 == 0 ? _areaColor : _areaAltColor;
                CreateRect($"Area{i}", (min + max) * 0.5f, max - min, color, SortingDepth.Background);
                if (i < snapshot.AreaCount - 1)
                {
                    var gate = CreateRect($"Gate{i}", new Vector2(max.x, (min.y + max.y) * 0.5f), new Vector2(GateWidth, max.y - min.y), _gateColor, SortingDepth.Ground);
                    _gates.Add((max.x, gate.gameObject));
                }
            }
        }

        private SpriteRenderer CreateRect(string objName, Vector2 center, Vector2 size, Color color, int order)
        {
            var rect = new GameObject(objName).AddComponent<SpriteRenderer>();
            rect.transform.SetParent(transform, false);
            rect.sprite = PlaceholderSprites.Square;
            rect.color = color;
            rect.sortingOrder = order;
            rect.transform.position = center;
            rect.transform.localScale = new Vector3(size.x, size.y, 1f);
            return rect;
        }

        /// <summary>動ける範囲の内側にある出入口は開いている（消す）。通ってきた出入口・先の出入口は閉じている</summary>
        private void UpdateGates(StageSnapshot snapshot)
        {
            foreach (var (x, obj) in _gates)
            {
                var isOpen = snapshot.AreaMin.X + GateEpsilon < x && x < snapshot.AreaMax.X - GateEpsilon;
                obj.SetActive(!isOpen);
            }
        }

        /// <summary>
        /// 動ける範囲が画面より広い（出入口が開いている）間は、元気なプレイヤーの真ん中を追う。
        /// 閉じているときは範囲がちょうど1エリアなので、エリアの真ん中に止まる
        /// </summary>
        private void FollowCamera(StageSnapshot snapshot, bool snap)
        {
            var half = Balance.AreaWidth * 0.5f;
            var minX = snapshot.AreaMin.X + half;
            var maxX = Mathf.Max(minX, snapshot.AreaMax.X - half);
            var sum = 0f;
            var count = 0;
            foreach (var unit in snapshot.Units)
            {
                if (unit.Team == Team.Player && !unit.Has(UnitFlags.Down))
                {
                    sum += unit.Position.X;
                    count++;
                }
            }

            var x = count > 0 ? sum / count : minX;
            _stageCamera.Follow(Mathf.Clamp(x, minX, maxX), snap);
        }
    }
}
