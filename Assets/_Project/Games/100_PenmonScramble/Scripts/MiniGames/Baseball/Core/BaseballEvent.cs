using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    public enum BaseballEventKind
    {
        /// <summary>投げた（Value は球種の番号）</summary>
        Pitch,
        /// <summary>曲げられる区間が終わった（ここから先はまっすぐ来る）</summary>
        BendEnd,
        /// <summary>バットを振り始めた</summary>
        Swing,
        /// <summary>バントの構えを始めた</summary>
        BuntSquare,
        /// <summary>投げた球がホームの線を越えた（Flag はゾーンの中か）</summary>
        CrossPlate,
        /// <summary>投げた球がミットに収まった（打たなかった・空振り）</summary>
        Catch,
        /// <summary>バットに当たった（Value は強さ 0〜1、Flag は浮いたか。バントも）</summary>
        Contact,
        /// <summary>浮いた打球が地面に着いた（Value は落ちる速さ）</summary>
        Land,
        /// <summary>打球がフェンスに当たった（Value はフェンスに向かう速さ）</summary>
        FenceHit,
        /// <summary>打球が穴の縁で曲がって通り過ぎた（Index は穴の番号）</summary>
        LipOut,
        /// <summary>打球が穴に入った（Index は穴の番号、Result はその穴の結果）</summary>
        HoleIn,
        /// <summary>打球が穴に入らずに止まった（Result は止まった場所の札）</summary>
        Stop,
        /// <summary>浮いた打球がフェンスを越えた（Result は HomeRun）</summary>
        HomeRun,
        /// <summary>打球がファウルになった</summary>
        Foul,
        /// <summary>フリーズボールが凍って止まった（Value は止まる秒）</summary>
        MagicFreeze,
        /// <summary>フリーズボールがまた動き出した</summary>
        MagicThaw,
        /// <summary>消える魔球が消えた</summary>
        MagicVanish,
        /// <summary>消える魔球が現れた</summary>
        MagicAppear,
        /// <summary>旋風ボールが逆へ曲がり出した</summary>
        MagicReverse,
        /// <summary>必殺打で打ち返した（Index は属性の番号）</summary>
        SpecialHit,
        /// <summary>水切りヒットが盤の上で跳ねた</summary>
        Skip,
        /// <summary>間欠泉が打球を跳ね上げた（Index は間欠泉の番号）</summary>
        GeyserLaunch,
    }

    /// <summary>盤の上で起きたこと。表示と音へはこれをイベントで渡す（Core は表示を知らない）</summary>
    public readonly struct BaseballEvent
    {
        public BaseballEvent(BaseballEventKind kind, Vec2 position, float value = 0f, bool flag = false, int index = -1, BaseballHitResult result = BaseballHitResult.Single)
        {
            Kind = kind;
            Position = position;
            Value = value;
            Flag = flag;
            Index = index;
            Result = result;
        }

        public BaseballEventKind Kind { get; }
        public Vec2 Position { get; }
        public float Value { get; }
        public bool Flag { get; }
        public int Index { get; }
        public BaseballHitResult Result { get; }

        public override string ToString() => $"{Kind} {Position} v={Value:0.##} flag={Flag} i={Index} {Result}";
    }
}
