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
        // InputManager の Action1〜4 のうち、技1・技2 に使う番号
        private const int Skill1Action = 0;
        private const int Skill2Action = 1;

        private UnityEngine.Vector2 _move;
        private bool _skill1Held;
        private bool _skill2Held;
        private bool _skill1Pressed;
        private bool _skill2Pressed;

        public void Sample(InputManager input)
        {
            _move = input.MoveVector;
            _skill1Held = input.IsActionHeld(Skill1Action);
            _skill2Held = input.IsActionHeld(Skill2Action);
            _skill1Pressed |= input.IsActionDown(Skill1Action);
            _skill2Pressed |= input.IsActionDown(Skill2Action);
        }

        public PlayerInput Consume()
        {
            var result = new PlayerInput(_move.ToVec2(), _skill1Held || _skill1Pressed, _skill2Held || _skill2Pressed);
            _skill1Pressed = false;
            _skill2Pressed = false;
            return result;
        }
    }
}
