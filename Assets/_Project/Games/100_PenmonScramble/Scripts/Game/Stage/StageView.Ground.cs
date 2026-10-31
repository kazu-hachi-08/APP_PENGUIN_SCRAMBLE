using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージの床とエリアの出入口。床はワールドごとの地面の絵を貼った板。
    /// 大ボスの闘技場は、楕円に切った床（闘技場の絵）と、楕円に沿った柵（ArenaFence）
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
        // 闘技場の外の床は、前のエリアの外の床と重なる（引いて全体を見せたときに左の奥まで埋めるため）ので、さらに下げる
        private const float ArenaOutsideFloorHeight = -0.03f;
        // 闘技場への通り道の床。前のエリアと闘技場の床の下に隠れ、楕円の左端のすき間だけ見えるよう、少し下げる
        private const float DoorwayFloorHeight = -0.01f;
        // 技の実演の床は、最初のワールドの地面（どのステージにも属さない場なので、いちばん見慣れた絵にする）
        public const int PreviewWorld = 1;

        private readonly List<(float x, GameObject obj)> _gates = new List<(float x, GameObject obj)>();
        // 闘技場の柵の、入口にかかる柱と、入口（楕円の左端）の横の位置。null なら闘技場の無いステージ
        private GameObject _arenaDoor;
        private float _arenaDoorX;

        /// <summary>エリアを1枚ずつ敷き、エリアの境目に出入口を置く。1つおきに少し色を変え、ボスエリアは赤っぽくする</summary>
        private void CreateGround(StageSnapshot snapshot)
        {
            _gates.Clear();
            _arenaDoor = null;
            if (_preview)
            {
                CreatePreviewGround(snapshot);
                return;
            }

            var world = StageTable.WorldOf(snapshot.StageNo);
            var theme = StageTheme.ForWorld(world);
            var art = CreatureArt.Ground(world);
            var texture = art != null ? art.texture : Texture2D.whiteTexture;
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = theme.Background;
            }

            var stage = StageTable.Get(snapshot.StageNo);
            for (var i = 0; i < snapshot.AreaCount; i++)
            {
                var area = StageLayout.Area(stage, i);
                var min = area.Min.ToVector2();
                var max = area.Max.ToVector2();
                if (area.IsEllipse)
                {
                    CreateArena(i, area, world, theme, texture);
                    continue;
                }

                var isBossArea = snapshot.AreaCount > 1 && i == snapshot.AreaCount - 1;
                CreateOutside(i, snapshot.AreaCount, texture, min, max);
                CreateFloor($"Area{i}", _materials.Toon.Floor(texture, StageTheme.AreaTintOf(i, isBossArea)), min, max, 0f);
                if (i < snapshot.AreaCount - 1)
                {
                    _gates.Add((max.x, CreateGate(i, max.x, min.y, max.y, theme.Gate)));
                }
            }
        }

        /// <summary>
        /// 闘技場: 楕円を囲む四角より広く暗い床を敷き、その上に楕円の床と柵を置く。
        /// 闘技場の絵が無ければ、ワールドの地面をボスエリアの色にして敷く
        /// </summary>
        private void CreateArena(int index, AreaShape arena, int world, StageTheme theme, Texture worldTexture)
        {
            var min = arena.Min.ToVector2();
            var max = arena.Max.ToVector2();
            var outside = new Vector2(OutsideWidth, OutsideDepth);
            CreateFloor($"Outside{index}", _materials.Toon.Floor(worldTexture, StageTheme.OutsideTint), min - outside, max + outside, ArenaOutsideFloorHeight);

            var art = CreatureArt.ArenaGround(world);
            var floor = art != null
                ? _materials.Toon.Floor(art.texture, Color.white)
                : _materials.Toon.Floor(worldTexture, StageTheme.AreaTintOf(index, true));
            var size = max - min;
            var disc = ToyParts.AddMesh(new GameObject($"Area{index}"), ToyMeshes.FloorDisc, floor);
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = arena.Center.ToFloor();
            disc.transform.localScale = new Vector3(size.x, 1f, size.y);

            var doorway = StageLayout.Doorway(arena);
            CreateFloor("Doorway", floor, doorway.Min.ToVector2(), doorway.Max.ToVector2(), DoorwayFloorHeight);
            _arenaDoor = ArenaFence.Build(transform, arena, _materials.Toon, theme);
            _arenaDoorX = min.x;
        }

        /// <summary>技の実演: 動ける範囲（エリア1枚分）にワールド1の地面を敷き、外を暗い床で囲む。出入口は無い</summary>
        private void CreatePreviewGround(StageSnapshot snapshot)
        {
            var art = CreatureArt.Ground(PreviewWorld);
            var texture = art != null ? art.texture : Texture2D.whiteTexture;
            var min = snapshot.AreaMin.ToVector2();
            var max = snapshot.AreaMax.ToVector2();
            var outside = new Vector2(OutsideWidth, OutsideDepth);
            CreateFloor("Outside", _materials.Toon.Floor(texture, StageTheme.OutsideTint), min - outside, max + outside, OutsideFloorHeight);
            CreateFloor("Area", _materials.Toon.Floor(texture, StageTheme.AreaTintOf(0, false)), min, max, 0f);
        }

        /// <summary>
        /// エリアより一回り大きい床を暗くして下に敷く。横長の画面ではエリアの外まで映るので、
        /// カメラの背景色が見えて不自然にならないようにする。重ならないよう、左右へはみ出すのは最初と最後のエリアだけ
        /// </summary>
        private void CreateOutside(int index, int areaCount, Texture texture, Vector2 min, Vector2 max)
        {
            var outsideMin = new Vector2(index == 0 ? min.x - OutsideWidth : min.x, min.y - OutsideDepth);
            var outsideMax = new Vector2(index == areaCount - 1 ? max.x + OutsideWidth : max.x, max.y + OutsideDepth);
            CreateFloor($"Outside{index}", _materials.Toon.Floor(texture, StageTheme.OutsideTint), outsideMin, outsideMax, OutsideFloorHeight);
        }

        /// <summary>絵を min〜max（床の上の長方形）いっぱいに引き伸ばして貼った板</summary>
        private void CreateFloor(string objName, Material material, Vector2 min, Vector2 max, float height)
        {
            // StageView は原点・無回転の根のオブジェクトなので、ローカルの位置は床の位置と同じ
            var size = max - min;
            ToyParts.FloorPlate(objName, transform, ((min + max) * 0.5f).ToFloor(height), new Vector3(size.x, 1f, size.y), material);
        }

        /// <summary>エリアの境目に、奥から手前まで横たわる低い柵（角の丸い円柱を細長くつぶす）</summary>
        private GameObject CreateGate(int index, float x, float minY, float maxY, Color color)
        {
            var gate = new GameObject($"Gate{index}");
            gate.transform.SetParent(transform, false);
            gate.transform.position = new Vector3(x, GateHeight * 0.5f, (minY + maxY) * 0.5f);
            gate.transform.localScale = new Vector3(GateWidth, GateHeight, maxY - minY);
            ToyParts.AddMesh(gate, ToyMeshes.Puck, _materials.Toon.Get(color, GateGloss));
            return gate;
        }

        /// <summary>動ける範囲の内側にある出入口は開いている（消す）。通ってきた出入口・先の出入口は閉じている</summary>
        private void UpdateGates(StageSnapshot snapshot)
        {
            foreach (var (x, obj) in _gates)
            {
                obj.SetActive(!IsGateOpenAt(snapshot, x));
            }

            if (_arenaDoor != null)
            {
                _arenaDoor.SetActive(!IsGateOpenAt(snapshot, _arenaDoorX));
            }
        }

        private static bool IsGateOpenAt(StageSnapshot snapshot, float x) => snapshot.AreaMin.X + GateEpsilon < x && x < snapshot.AreaMax.X - GateEpsilon;
    }
}
