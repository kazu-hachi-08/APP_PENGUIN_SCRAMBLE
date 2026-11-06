using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// 1体の子への、その歩の入力（PK戦では、蹴る子と守る側の決まった子への入力として SoccerShootout が読む）。ボタンは「押しているか」で渡す（押した・離した瞬間は SoccerWorld が前の歩と比べて決める。
    /// オンラインで歩の途中の瞬間の入力を取りこぼさないように）
    /// </summary>
    public readonly struct SoccerInput
    {
        public static readonly SoccerInput None = default;

        /// <summary>歩く向きと強さ（長さ 1 まで）</summary>
        public readonly Vec2 Move;

        /// <summary>シュートのボタン（持っている: 押している間ためて、離すと蹴る。持っていない: 押すとスライディング）</summary>
        public readonly bool Shoot;

        /// <summary>パスのボタン（持っている: 押すとパス。持っていない: 押すとダッシュ）</summary>
        public readonly bool Pass;

        /// <summary>必殺シュートのボタン（持っていて、チームのゲージが満タンなら押した瞬間に出す）</summary>
        public readonly bool Special;

        public SoccerInput(Vec2 move, bool shoot, bool pass = false, bool special = false)
        {
            Move = move;
            Shoot = shoot;
            Pass = pass;
            Special = special;
        }
    }
}
