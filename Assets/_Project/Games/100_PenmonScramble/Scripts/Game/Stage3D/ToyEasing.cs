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

        /// <summary>
        /// 終わりの方で小さくする倍率。t が shrinkFrom までは 1、そこから 0 へ直線的に縮む
        /// </summary>
        public static float Shrink(float t, float shrinkFrom)
        {
            return t < shrinkFrom ? 1f : 1f - (t - shrinkFrom) / (1f - shrinkFrom);
        }

        /// <summary>
        /// count 個を、ぐるりと等間隔に散らした水平方向の単位ベクトル。index 番目の角度を jitter の分だけずらし、
        /// 全部が同じ向きへ飛ばないようにする（見た目だけなので Core の乱数とは関係ない）
        /// </summary>
        public static Vector3 RingDirection(int index, int count, float jitter)
        {
            var angle = (index + Random.value * jitter) / count * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}
