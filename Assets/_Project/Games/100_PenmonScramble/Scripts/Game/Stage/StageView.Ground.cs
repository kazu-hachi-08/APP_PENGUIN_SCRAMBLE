using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージの床・エリアの出入口・カメラ（§6.1）。床はワールドごとの絵か色（§6.2・StageTheme）を貼った板
    /// </summary>
    public partial class StageView
    {
        // 出入口は床に置いた低い柵。ペンギンを隠さない高さにする
        private const float GateWidth = 0.35f;
        private const float GateHeight = 0.3f;
        private const float GateGloss = 0.3f;
        // 出入口がいまの範囲の内側にあるか（開いているか）を比べるときの余裕
        private const float GateEpsilon = 0.01f;
        // エリアの外に敷く床のはみ出し（マス）。横長のスマホ（21:9 くらい）でも、斜めに見た奥の左右まで埋まるように
        private const float OutsideWidth = 12f;
        // 奥と手前のはみ出し。カメラの上下の余白と揺れの分
        private const float OutsideDepth = 6f;
        // エリアの外の床は少し下げて、エリアの床と同じ高さでちらつかないようにする
        private const float OutsideFloorHeight = -0.02f;

        [SerializeField] private StageCamera _stageCamera;

        private readonly List<(float x, GameObject obj)> _gates = new List<(float x, GameObject obj)>();

        /// <summary>エリアを1枚ずつ敷き、エリアの境目に出入口を置く。1つおきに少し色を変え、ボスエリアは赤っぽくする</summary>
        private void CreateGround(StageSnapshot snapshot)
        {
            _gates.Clear();
            var world = StageTable.WorldOf(snapshot.StageNo);
            var theme = StageTheme.ForWorld(world);
            var art = CreatureArt.Ground(world);
            var texture = art != null ? art.texture : Texture2D.whiteTexture;
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = theme.Outside;
            }

            for (var i = 0; i < snapshot.AreaCount; i++)
            {
                var min = StageLayout.AreaMin(i).ToVector2();
                var max = StageLayout.AreaMax(i).ToVector2();
                var isBossArea = snapshot.AreaCount > 1 && i == snapshot.AreaCount - 1;
                var color = theme.AreaColor(i, isBossArea, art != null);
                CreateOutside(i, snapshot.AreaCount, texture, min, max, theme.OutsideColor(art != null));
                CreateFloor($"Area{i}", _materials.Toon.Floor(texture, color), min, max, 0f);
                if (i < snapshot.AreaCount - 1)
                {
                    _gates.Add((max.x, CreateGate(i, max.x, min.y, max.y, theme.Gate)));
                }
            }
        }

        /// <summary>
        /// エリアより一回り大きい床を暗くして下に敷く。横長の画面ではエリアの外まで映るので、
        /// カメラの背景色が見えて不自然にならないようにする。重ならないよう、左右へはみ出すのは最初と最後のエリアだけ
        /// </summary>
        private void CreateOutside(int index, int areaCount, Texture texture, Vector2 min, Vector2 max, Color color)
        {
            var outsideMin = new Vector2(index == 0 ? min.x - OutsideWidth : min.x, min.y - OutsideDepth);
            var outsideMax = new Vector2(index == areaCount - 1 ? max.x + OutsideWidth : max.x, max.y + OutsideDepth);
            CreateFloor($"Outside{index}", _materials.Toon.Floor(texture, color), outsideMin, outsideMax, OutsideFloorHeight);
        }

        /// <summary>絵を min〜max（床の上の長方形）いっぱいに引き伸ばして貼った板</summary>
        private void CreateFloor(string objName, Material material, Vector2 min, Vector2 max, float height)
        {
            var floor = new GameObject(objName);
            floor.transform.SetParent(transform, false);
            floor.transform.position = ((min + max) * 0.5f).ToFloor(height);
            var size = max - min;
            floor.transform.localScale = new Vector3(size.x, 1f, size.y);
            floor.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.FloorQuad;
            floor.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>エリアの境目に、奥から手前まで横たわる低い柵（角の丸い円柱を細長くつぶす）</summary>
        private GameObject CreateGate(int index, float x, float minY, float maxY, Color color)
        {
            var gate = new GameObject($"Gate{index}");
            gate.transform.SetParent(transform, false);
            gate.transform.position = new Vector3(x, GateHeight * 0.5f, (minY + maxY) * 0.5f);
            gate.transform.localScale = new Vector3(GateWidth, GateHeight, maxY - minY);
            gate.AddComponent<MeshFilter>().sharedMesh = ToyMeshes.Puck;
            gate.AddComponent<MeshRenderer>().sharedMaterial = _materials.Toon.Get(color, GateGloss);
            return gate;
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
