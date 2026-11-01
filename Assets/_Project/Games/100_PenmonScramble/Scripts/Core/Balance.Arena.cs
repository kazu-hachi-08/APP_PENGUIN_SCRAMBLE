namespace PenmonScramble.Core
{
    /// <summary>闘技場の連戦（戦いの合間）・巨大ペンモン・大ボスの本気の調整値</summary>
    public static partial class Balance
    {
        // 戦いの合間。闘技場で勝って次の闘技場があるとき（ボスラッシュ）
        /// <summary>1戦勝つごとに、元気な子の HP をそれぞれ最大 HP のこの割合だけ回復する</summary>
        public const float RoundClearHealRatio = 0.3f;
        /// <summary>ダウンしていた人は、先頭の子をこの割合の HP で起こす</summary>
        public const float RoundClearReviveRatio = 0.3f;

        // 巨大ペンモン。ステージのボス（BossStats）をさらに大きく・硬くする
        /// <summary>ステージのボスの HP に、さらに掛ける倍率</summary>
        public const float GiantHpScale = 1.6f;
        /// <summary>体の半径 ＝ ペンギンの半径 × これ（ステージのボスは BossRadiusScale）</summary>
        public const float GiantRadiusScale = 3f;
        /// <summary>闘技場の大技を出し終えてから、次の大技の予告までの秒数。この間はふつうの技1・技2で戦う</summary>
        public const float GiantBigAttackInterval = 6f;
        /// <summary>出てきてから最初の大技までの秒数。2体組の2体目は、1体目の大技の後に順番が回る</summary>
        public const float GiantFirstBigAttackDelay = 3f;
        /// <summary>竜巻が引き寄せる範囲 ＝ 竜巻の円の半径 × これ。円のすぐ外にいても吸い込まれそうに感じさせる</summary>
        public const float VortexReachRatio = 1.6f;

        // 大ボスの本気（ボスラッシュの3戦目）
        /// <summary>本気の大ボスが HP のこの割合を切ると、吠えて攻撃が激しくなる</summary>
        public const float RageHpRatio = 0.5f;
        /// <summary>吠えたときに全体を止める長さ（更新の回数）。0.5秒</summary>
        public const int RageRoarTicks = 15;
        /// <summary>吠えた後の、予告の長さの倍率</summary>
        public const float RageTelegraphScale = 0.8f;
        /// <summary>吠えた後の、攻撃と攻撃の休みの倍率</summary>
        public const float RageRestScale = 0.7f;
        /// <summary>本気のシャチの2本の波の間（マス）。帯の間に立てばどちらにも当たらない広さ</summary>
        public const float RageWaveGap = 1.6f;
    }
}
