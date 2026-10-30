using System;
using PenmonScramble.Core;

namespace PenmonScramble.Game
{
    public partial class StageBenchmark
    {
        /// <summary>測る場面1つ分（そのエリアだけの1エリアのステージ）と、その結果</summary>
        public sealed class Scenario
        {
            public Scenario(string name, StageDef stage)
            {
                Name = name;
                Stage = stage;
            }

            public string Name { get; }
            public StageDef Stage { get; }
            public FrameStats Frames { get; } = new FrameStats();
            public CounterStat Batches { get; } = new CounterStat();
            public CounterStat SetPass { get; } = new CounterStat();
            public CounterStat DrawCalls { get; } = new CounterStat();
            public CounterStat Triangles { get; } = new CounterStat();
            public int PeakUnits { get; private set; }

            /// <summary>一番遅いフレームが、測り始めてから何秒目で、そのとき何体いたか（止まった原因を探すため）</summary>
            public float WorstAtSeconds { get; private set; }
            public int WorstAtUnits { get; private set; }
            private float _worst;

            /// <summary>測っている間にガベージコレクションが走った回数（止まりの原因になる）</summary>
            public int GcCollections => _gcStart < 0 ? 0 : GC.CollectionCount(0) - _gcStart;
            private int _gcStart = -1;

            public void SampleUnits(int units) => PeakUnits = Math.Max(PeakUnits, units);

            public void SampleHitch(float seconds, float at, int units)
            {
                if (_gcStart < 0)
                {
                    _gcStart = GC.CollectionCount(0);
                }

                if (seconds > _worst)
                {
                    _worst = seconds;
                    WorstAtSeconds = at;
                    WorstAtUnits = units;
                }
            }

            /// <summary>敵の合計が一番多いエリアと、最後のステージ（シャチ）の大ボスのエリア</summary>
            public static Scenario[] CreateAll()
            {
                var crowd = HeaviestArea(out var crowdStage, out var crowdArea);
                var last = StageTable.Get(StageTable.Count);
                var bossArea = last.Areas[last.Areas.Count - 1];
                return new[]
                {
                    new Scenario($"Crowd (stage {crowdStage.No} area {crowdArea + 1})", new StageDef(crowdStage.No, "BenchCrowd", crowd)),
                    new Scenario($"Big boss (stage {last.No})", new StageDef(last.No, "BenchBoss", bossArea)),
                };
            }

            /// <summary>波の敵の数の合計が一番多いエリア（同じなら先のステージ。ワールドが進むほど敵の見た目も重いので）</summary>
            public static AreaDef HeaviestArea(out StageDef stage, out int areaIndex)
            {
                AreaDef best = null;
                var bestCount = -1;
                stage = null;
                areaIndex = 0;
                for (var no = 1; no <= StageTable.Count; no++)
                {
                    var def = StageTable.Get(no);
                    for (var i = 0; i < def.Areas.Count; i++)
                    {
                        var count = 0;
                        foreach (var wave in def.Areas[i].Waves)
                        {
                            count += wave.Count;
                        }

                        if (count >= bestCount)
                        {
                            best = def.Areas[i];
                            bestCount = count;
                            stage = def;
                            areaIndex = i;
                        }
                    }
                }

                return best;
            }
        }

        /// <summary>描画の回数などの、1フレームごとの値の平均と最大</summary>
        public sealed class CounterStat
        {
            private double _sum;
            private int _count;

            public long Max { get; private set; }
            public long Average => _count > 0 ? (long)(_sum / _count) : 0;
            public bool HasValue => _count > 0;

            public void Add(long value)
            {
                _sum += value;
                _count++;
                Max = Math.Max(Max, value);
            }
        }
    }
}
