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

        /// <summary>
        /// 寿命の時計を deltaSeconds 進めて、寿命に対する経過の割合を返す（1 以上で寿命切れ）。
        /// 消す処理は呼び出し側に任せる（クラスごとに後始末が違うため）
        /// </summary>
        public static float Advance(ref float age, float deltaSeconds, float lifeSeconds)
        {
            age += deltaSeconds;
            return age / lifeSeconds;
        }

        /// <summary>
        /// ぽんと出る拡大を model の大きさへ反映する。加算と終わりの判定は呼び出し側が行う
        /// </summary>
        public static void ApplyPopScale(Transform model, float baseScale, float popAge, float popSeconds, float popStart)
        {
            model.localScale = Vector3.one * (baseScale * Pop(popAge / popSeconds, popStart));
        }

        /// <summary>
        /// 0〜1 をなめらかに往復する点滅の値。time は呼び出し側の時計（Time.unscaledTime など）をそのまま渡す。
        /// 0.5 + 0.5 * sin で、暗い方が 0・明るい方が 1 になる
        /// </summary>
        public static float Pulse01(float time, float perSecond)
        {
            return 0.5f + 0.5f * Mathf.Sin(time * perSecond * Mathf.PI * 2f);
        }
    }
}
