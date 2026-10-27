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

        public void Receive(PlayerInput input)
        {
            _latest = input;
            _skill1Pressed |= input.Skill1;
            _skill2Pressed |= input.Skill2;
        }

        public PlayerInput Consume()
        {
            var result = new PlayerInput(_latest.Move, _latest.Skill1 || _skill1Pressed, _latest.Skill2 || _skill2Pressed);
            _skill1Pressed = false;
            _skill2Pressed = false;
            return result;
        }
    }
}
