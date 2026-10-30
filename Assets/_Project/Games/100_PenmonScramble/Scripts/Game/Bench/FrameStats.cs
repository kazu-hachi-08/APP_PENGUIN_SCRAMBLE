using System;
using System.Collections.Generic;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 1フレームごとの時間を集めて、平均の fps と「カクつき」の目安を出す。
    /// 平均だけだと、ときどき長く止まるフレームが埋もれるので、遅い方から 1% のフレームの fps（1% Low）と一番遅いフレームも出す
    /// </summary>
    public sealed class FrameStats
    {
        // 遅い方から数えてこの割合のフレームの時間を「1% Low」とする
        private const float LowPercentile = 0.99f;

        private readonly List<float> _seconds = new List<float>();
        private double _total;

        public int Frames => _seconds.Count;

        public float AverageFps => _total > 0 ? (float)(_seconds.Count / _total) : 0f;

        /// <summary>遅い方から 1% のフレームの fps</summary>
        public float LowFps
        {
            get
            {
                if (_seconds.Count == 0)
                {
                    return 0f;
                }

                var sorted = new List<float>(_seconds);
                sorted.Sort();
                var index = Math.Min(sorted.Count - 1, (int)(sorted.Count * LowPercentile));
                return 1f / sorted[index];
            }
        }

        public float WorstMs
        {
            get
            {
                var worst = 0f;
                foreach (var s in _seconds)
                {
                    worst = Math.Max(worst, s);
                }

                return worst * 1000f;
            }
        }

        public void Add(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _seconds.Add(seconds);
            _total += seconds;
        }
    }
}
