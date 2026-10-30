using System;
using Unity.Profiling;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 描画の回数（Batches・SetPass・Draw Calls・三角形の数）をフレームごとに読む。
    /// 配る版と同じ設定のビルドでも読める値だけを使う（読めない環境では「-」になる）
    /// </summary>
    public sealed class BenchCounters : IDisposable
    {
        private ProfilerRecorder _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        private ProfilerRecorder _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        private ProfilerRecorder _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        private ProfilerRecorder _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");

        public void Sample(StageBenchmark.Scenario scenario)
        {
            Add(_batches, scenario.Batches);
            Add(_setPass, scenario.SetPass);
            Add(_drawCalls, scenario.DrawCalls);
            Add(_triangles, scenario.Triangles);
        }

        public void Dispose()
        {
            _batches.Dispose();
            _setPass.Dispose();
            _drawCalls.Dispose();
            _triangles.Dispose();
        }

        private static void Add(ProfilerRecorder recorder, StageBenchmark.CounterStat stat)
        {
            // 計測を始めた直後は値がまだ無い（0）ので数えない
            if (recorder.Valid && recorder.LastValue > 0)
            {
                stat.Add(recorder.LastValue);
            }
        }
    }
}
