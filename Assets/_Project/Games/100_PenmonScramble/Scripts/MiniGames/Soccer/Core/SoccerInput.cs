using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// 1体の子への、その歩の入力。ボタンは「押しているか」で渡す（押した・離した瞬間は SoccerWorld が前の歩と比べて決める。
    /// オンラインで歩の途中の瞬間の入力を取りこぼさないように）
    /// </summary>
    public readonly struct SoccerInput
    {
        public static readonly SoccerInput None = default;

        /// <summary>歩く向きと強さ（長さ 1 まで）</summary>
        public readonly Vec2 Move;

        /// <summary>シュートのボタン（押している間ためて、離すと蹴る）</summary>
        public readonly bool Shoot;

        public SoccerInput(Vec2 move, bool shoot)
        {
            Move = move;
            Shoot = shoot;
        }
    }
}
