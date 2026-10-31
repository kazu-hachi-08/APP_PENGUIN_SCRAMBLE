namespace PenmonScramble.Core
{
    /// <summary>
    /// ゲストから届いた入力を、ホストの次の更新まで持っておく。
    /// 「押した」「離した」が同じ更新の間に続けて届いても、押したことを取りこぼさないようにする
    /// </summary>
    public sealed class InputLatch
    {
        private PlayerInput _latest;
        private bool _skill1Pressed;
        private bool _skill2Pressed;
        private bool _specialPressed;
        private bool _switchPressed;

        public void Receive(PlayerInput input)
        {
            _latest = input;
            _skill1Pressed |= input.Skill1;
            _skill2Pressed |= input.Skill2;
            _specialPressed |= input.Special;
            _switchPressed |= input.Switch;
        }

        public PlayerInput Consume()
        {
            var result = new PlayerInput(_latest.Move,
                _latest.Skill1 || _skill1Pressed,
                _latest.Skill2 || _skill2Pressed,
                _latest.Special || _specialPressed,
                _latest.Switch || _switchPressed,
                _latest.InMenu);
            _skill1Pressed = false;
            _skill2Pressed = false;
            _specialPressed = false;
            _switchPressed = false;
            return result;
        }
    }
}
