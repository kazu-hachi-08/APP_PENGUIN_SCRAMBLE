namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 往復するパワーゲージ。0 → 1 → 0 … を一定の速さで行き来し、端でも止まらない
    /// （端で一瞬止まると、そこを待って 100% を狙うだけになるため。強さはいつも「止める間合い」で決める）
    /// </summary>
    public sealed class GolfPowerGauge
    {
        private float _seconds;

        /// <summary>動いている（もう一度押すと止まる）</summary>
        public bool Running { get; private set; }

        /// <summary>いまの強さ（0〜1）。止めた後は止めたときの値のまま</summary>
        public float Power { get; private set; }

        /// <summary>0 から動かし始める</summary>
        public void Start()
        {
            Running = true;
            _seconds = 0f;
            Power = 0f;
        }

        public void Tick(float seconds)
        {
            if (!Running)
            {
                return;
            }

            _seconds += seconds;
            Power = PowerAt(_seconds);
        }

        /// <summary>止めて、そのときの強さを返す</summary>
        public float Stop()
        {
            Running = false;
            return Power;
        }

        /// <summary>止めて 0 に戻す（番が替わった・打った後）</summary>
        public void Reset()
        {
            Running = false;
            _seconds = 0f;
            Power = 0f;
        }

        /// <summary>動かし始めてから seconds 秒後の強さ。前半の半周で 0 → 1、後半で 1 → 0（三角の波）</summary>
        public static float PowerAt(float seconds)
        {
            var cycle = GolfRules.GaugeCycleSeconds;
            var half = cycle * 0.5f;
            var t = seconds % cycle;
            if (t < 0f)
            {
                t += cycle;
            }

            return t <= half ? t / half : 2f - t / half;
        }
    }
}
