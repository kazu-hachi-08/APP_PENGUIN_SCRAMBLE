using System.Collections;
using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 結果の舞台: カメラがプレイヤーへ寄り、右側の結果の札を避けて画面の左寄りに映す。
    /// クリアならみんなカメラの方を向いて跳ねて喜び、ソロでは控えの2体も煙から出てきて並ぶ。やられたら倒れた姿をそのまま映す。
    /// Core は止まっているので、ここで作る控えの子は見た目だけ（次の挑戦の Rebuild で消える）
    /// </summary>
    public partial class StageView
    {
        // クリアは寄って顔を見せ、やられたときは少し引いて倒れた様子を見せる
        private const float ResultClearZoom = 0.6f;
        private const float ResultFailZoom = 0.8f;
        private const float ResultMaxZoom = 1.3f;
        // 画面の右側に結果の札が出るので、ペンモンは左寄りに映す（画面の幅のこの割合だけ、カメラを右へずらす）
        private const float ResultScreenShift = 0.2f;
        // ペンモンを映すのに使える横幅（画面の幅に対する割合。残りは結果の札）
        private const float ResultUsableWidth = 0.5f;
        private const float ResultFramePadding = 1.2f;
        // 足元より少し奥を真ん中にして、頭の上まで画面の真ん中あたりに収める（マス）
        private const float ResultLookAhead = 0.3f;
        // カメラの方（-Z）を向き、少しだけ右の札の方へ顔を向ける
        private static readonly Vector3 ResultFacing = new Vector3(0.25f, 0f, -1f);

        // ソロで控えの2体を出す位置（場に出ていた子から。y は奥）。先頭の子が手前のまん中になるよう、少し奥の左右に置く
        private static readonly Vector2[] BenchOffsets = { new Vector2(-1.3f, 0.7f), new Vector2(1.3f, 0.7f) };
        private const float BenchFirstDelay = 0.15f;
        private const float BenchInterval = 0.18f;
        private const float BenchPopSeconds = 0.3f;
        private const float BenchPopStart = 0.4f;

        private readonly List<Vector2> _resultFramed = new List<Vector2>();
        private bool _showingResult;

        /// <param name="localState">自分の3体（ソロの控えを並べるのに使う）。無ければ控えは出さない</param>
        /// <param name="showBench">ソロだけ控えを並べる（協力では人数分の3体が並ぶと狭いので、場に出ていた子だけ）</param>
        public void ShowResultStage(StageSnapshot snapshot, PlayerState localState, bool cleared, bool showBench)
        {
            _showingResult = true;
            _introEndsAt = float.NegativeInfinity;
            _resultFramed.Clear();
            foreach (var unit in snapshot.Units)
            {
                if (unit.Team != Team.Player || !_units.TryGetValue(unit.Id, out var view))
                {
                    continue;
                }

                _resultFramed.Add(unit.Position.ToVector2());
                if (cleared)
                {
                    view.Celebrate(ResultFacing);
                }
                else
                {
                    view.HoldForResult();
                }

                if (cleared && showBench && localState != null && unit.Seat == localState.Seat)
                {
                    LineUpBench(localState, unit);
                }
            }

            FrameResult(cleared ? ResultClearZoom : ResultFailZoom);
        }

        /// <summary>場に出ていた子の左右の奥に、控えの子を順にぽんと出す</summary>
        private void LineUpBench(PlayerState state, UnitState active)
        {
            var slot = 0;
            for (var i = 0; i < state.Members.Count && slot < BenchOffsets.Length; i++)
            {
                if (i == state.ActiveIndex)
                {
                    continue;
                }

                var position = active.Position.ToVector2() + BenchOffsets[slot];
                _resultFramed.Add(position);
                StartCoroutine(PopInBench(state.Members[i].SpeciesNo, position, active.Radius, BenchFirstDelay + BenchInterval * slot));
                slot++;
            }
        }

        private IEnumerator PopInBench(int speciesNo, Vector2 position, float radius, float delay)
        {
            yield return new WaitForSeconds(delay);
            var model = PenguinModelBuilder.Build(PenguinLooks.Get(speciesNo), _materials.Toon, transform);
            var scale = PenguinModelBuilder.ScaleForRadius(radius);
            model.transform.position = position.ToFloor();
            model.SetRealShadows(false);
            var motion = model.gameObject.AddComponent<PenguinMotion>();
            motion.SnapFacing(ResultFacing);
            motion.Cheering = true;
            SmokePuff.Spawn(transform, _materials, position, radius, model.TopHeight * scale);

            for (var age = 0f; age < BenchPopSeconds; age += Time.deltaTime)
            {
                model.transform.localScale = Vector3.one * (scale * ToyEasing.Pop(age / BenchPopSeconds, BenchPopStart));
                yield return null;
            }

            model.transform.localScale = Vector3.one * scale;
        }

        /// <summary>映すペンモンがみんな画面の左寄りに収まる引きで、ゆっくり寄る</summary>
        private void FrameResult(float minZoom)
        {
            if (_resultFramed.Count == 0)
            {
                return;
            }

            var min = _resultFramed[0];
            var max = _resultFramed[0];
            foreach (var point in _resultFramed)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            var view = StageCamera.View;
            var half = (max - min) * 0.5f;
            var need = Mathf.Max((half.x + ResultFramePadding) / (view.x * 0.5f * ResultUsableWidth), (half.y + ResultFramePadding) / (view.y * 0.5f));
            var zoom = Mathf.Clamp(need, minZoom, ResultMaxZoom);
            var center = (min + max) * 0.5f + new Vector2(view.x * zoom * ResultScreenShift, ResultLookAhead);
            _stageCamera.Follow(center, zoom, snap: false, slow: true);
        }
    }
}
