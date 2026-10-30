using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>おもちゃらしく弾む動きの曲線</summary>
    public static class ToyEasing
    {
        // はみ出してから戻る回数（半周期の数）。3 で「大きく → 少し小さく → 落ち着く」
        private const float PopWobbles = 3f;

        /// <summary>
        /// ぽんと出る大きさの倍率。t（0〜1）が 0 で start、一度 1 より大きくはみ出してから 1 に落ち着く。
        /// t が 1 以上なら 1
        /// </summary>
        public static float Pop(float t, float start)
        {
            t = Mathf.Clamp01(t);
            var decay = (1f - t) * (1f - t);
            return 1f + (start - 1f) * decay * Mathf.Cos(PopWobbles * Mathf.PI * t);
        }
    }
}
