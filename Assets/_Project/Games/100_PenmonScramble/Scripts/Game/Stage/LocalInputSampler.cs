using PenmonScramble.Common.Input;
using PenmonScramble.Core;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 毎フレームの入力を、Core の更新（1/30秒）1回分の PlayerInput にまとめる。
    /// 更新の間に押して離した短いタップも、次の更新で「押した」として届けるため
    /// </summary>
    public sealed class LocalInputSampler
    {
        // InputManager の Action1〜4 = 技1・技2・必殺技・交代（J/K/L/Space、南/東/北/LB）
        private const int Skill1Action = 0;
        private const int Skill2Action = 1;
        private const int SpecialAction = 2;
        private const int SwitchAction = 3;
        private const int ActionCount = 4;

        private readonly bool[] _held = new bool[ActionCount];
        private readonly bool[] _pressed = new bool[ActionCount];
        private UnityEngine.Vector2 _move;

        /// <summary>メニューを開いている間は、押したものを貯めずに「メニュー中」の入力を出す（閉じた瞬間に技が出ないように）</summary>
        public bool InMenu { get; set; }

        public void Sample(InputManager input)
        {
            if (InMenu)
            {
                _move = UnityEngine.Vector2.zero;
                System.Array.Clear(_held, 0, ActionCount);
                System.Array.Clear(_pressed, 0, ActionCount);
                return;
            }

            _move = input.MoveVector;
            for (var i = 0; i < ActionCount; i++)
            {
                _held[i] = input.IsActionHeld(i);
                _pressed[i] |= input.IsActionDown(i);
            }
        }

        public PlayerInput Consume()
        {
            if (InMenu)
            {
                return PlayerInput.MenuOpen;
            }

            var result = new PlayerInput(_move.ToVec2(), Get(Skill1Action), Get(Skill2Action), Get(SpecialAction), Get(SwitchAction));
            System.Array.Clear(_pressed, 0, ActionCount);
            return result;
        }

        private bool Get(int action) => _held[action] || _pressed[action];
    }
}
