using System;
using System.Collections.Generic;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 1球の間の、1人ぶんの入力の記録（投げてからの歩ごと）。投げる人は曲げる入力、打つ人は振る（歩の中の時刻つき）・バントの構えと、
    /// 曲げる入力の欄に「投げた球が来ている間は打席の中の左右・打った後は追い風ヒットの打球を曲げる入力」。オンラインではこれを送り合い、どの端末でも同じ入力で同じ1球を計算する（BaseballPitchSession）。
    /// 送る値に丸めてから記録する（自分の端末も丸めた値で計算しないと、ホストと道のりがずれるため）。
    /// まだ届いていない歩は、最後に届いた値が続くと見なす（振るは「振っていない」）
    /// </summary>
    public sealed class BaseballRoleLog
    {
        /// <summary>1球で記録するいちばん多い歩（打球が止まるまでの上限より長く。壊れた値で配列を作りすぎないように）</summary>
        public const int MaxSteps = 30 * 16;

        private const float BendScale = 127f;

        private readonly List<sbyte> _bend = new List<sbyte>();
        private readonly List<bool> _bunt = new List<bool>();

        /// <summary>記録した歩の数（0〜Progress−1 の歩の入力がわかっている）</summary>
        public int Progress => _bend.Count;

        /// <summary>振った歩（振っていなければ −1）</summary>
        public int SwingStep { get; private set; } = -1;

        /// <summary>振った時刻（丸めた値。BaseballInput.SwingTime を 1歩で −127〜127 に）</summary>
        public sbyte SwingTime { get; private set; }

        public IReadOnlyList<sbyte> Bends => _bend;

        public IReadOnlyList<bool> Bunts => _bunt;

        public void Clear()
        {
            _bend.Clear();
            _bunt.Clear();
            SwingStep = -1;
            SwingTime = 0;
        }

        /// <summary>次の歩（Progress 番目）の入力を足す。振るは1球に1回だけ（2回目は無視）</summary>
        public void Record(float bend, bool bunt, bool swing, float swingTime)
        {
            if (Progress >= MaxSteps)
            {
                return;
            }

            if (swing && SwingStep < 0)
            {
                SwingStep = Progress;
                SwingTime = QuantizeSwingTime(swingTime);
            }

            _bend.Add(QuantizeBend(bend));
            _bunt.Add(bunt);
        }

        /// <summary>
        /// この人の入力はもう届かない（抜けた）: 最後の値が続くとして、記録を上限まで埋める（結果を決めるのに、この人を待たないように）
        /// </summary>
        public void Seal()
        {
            var bend = _bend.Count > 0 ? _bend[_bend.Count - 1] : (sbyte)0;
            var bunt = _bunt.Count > 0 && _bunt[_bunt.Count - 1];
            while (_bend.Count < MaxSteps)
            {
                _bend.Add(bend);
                _bunt.Add(bunt);
            }
        }

        /// <summary>その歩の入力（記録より先の歩は、最後の値が続くと見なす）</summary>
        public float BendAt(int step) => DequantizeBend(At(_bend, step, (sbyte)0));

        public bool BuntAt(int step) => At(_bunt, step, false);

        public bool SwingAt(int step) => step == SwingStep;

        public float SwingTimeAt(int step) => step == SwingStep ? DequantizeSwingTime(SwingTime) : 0f;

        /// <summary>届いた記録で置き換える（いま持っているより先まで記録されているときだけ。古い記録が後から届いても戻さない）</summary>
        public bool Merge(BaseballRoleLog other)
        {
            if (other == null || other.Progress <= Progress)
            {
                return false;
            }

            CopyFrom(other);
            return true;
        }

        public void CopyFrom(BaseballRoleLog other)
        {
            _bend.Clear();
            _bend.AddRange(other._bend);
            _bunt.Clear();
            _bunt.AddRange(other._bunt);
            SwingStep = other.SwingStep;
            SwingTime = other.SwingTime;
        }

        /// <summary>通信で読んだ記録を組み立てる（BaseballCodec から）</summary>
        public static BaseballRoleLog FromParts(IReadOnlyList<sbyte> bends, IReadOnlyList<bool> bunts, int swingStep, sbyte swingTime)
        {
            var log = new BaseballRoleLog();
            var count = Math.Min(Math.Min(bends.Count, bunts.Count), MaxSteps);
            for (var i = 0; i < count; i++)
            {
                log._bend.Add(bends[i]);
                log._bunt.Add(bunts[i]);
            }

            log.SwingStep = swingStep >= 0 && swingStep < count ? swingStep : -1;
            log.SwingTime = log.SwingStep >= 0 ? swingTime : (sbyte)0;
            return log;
        }

        public static sbyte QuantizeBend(float bend) => (sbyte)MathF.Round(Math.Clamp(bend, -1f, 1f) * BendScale);

        public static float DequantizeBend(sbyte value) => value / BendScale;

        public static sbyte QuantizeSwingTime(float seconds)
        {
            var t = Math.Clamp(seconds / BaseballRules.StepSeconds, -1f, 1f);
            return (sbyte)MathF.Round(t * BendScale);
        }

        public static float DequantizeSwingTime(sbyte value) => value / BendScale * BaseballRules.StepSeconds;

        private static T At<T>(List<T> list, int step, T empty)
        {
            if (list.Count == 0 || step < 0)
            {
                return empty;
            }

            return list[Math.Min(step, list.Count - 1)];
        }
    }
}
