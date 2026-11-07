using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Input;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 入力: 打つ人は 振る（J / × / 右の大ボタン。押した時刻を歩の中まで渡す）と バント（K / ○ / 右の中ボタンを押している間）。
    /// 投げる人は 構えの間に球種（J ストレート / K カーブ / L チェンジアップ。× ○ △・画面の札でも）を選んで投げ、
    /// 投げた後は ←→ / 左スティック / スマホのスティックで曲げる。相手の側は CPU（BaseballAi）
    /// </summary>
    public partial class BaseballTestPlay
    {
        // 球種のキー（InputManager の Action1〜3 = J/K/L・×/○/△）
        private const int PitchActionCount = 3;
        private const int SwingAction = 0;
        private const int BuntAction = 1;

        private BaseballPitchType? _chosenPitch;
        private bool _swingQueued;
        private float _swingTime;

        private static InputManager Input => InputManager.HasInstance ? InputManager.Instance : null;

        /// <summary>投げる人が構えの間に押した球種を取っておく（次の歩の構えで投げ始める）</summary>
        private void ReadPitchKeys()
        {
            var input = Input;
            if (_side != BaseballTestSide.Pitch || _stage != Stage.Ready || input == null)
            {
                return;
            }

            for (var i = 0; i < PitchActionCount; i++)
            {
                if (input.IsActionDown(i))
                {
                    _chosenPitch = (BaseballPitchType)i;
                    return;
                }
            }
        }

        private void OnPitchCardChosen(BaseballPitchType type)
        {
            if (_side == BaseballTestSide.Pitch && _stage == Stage.Ready)
            {
                _chosenPitch = type;
            }
        }

        /// <summary>
        /// 振るボタンを押した瞬間を、次に進める歩の始めからの秒で取っておく。画面に描いているのは前の歩と今の歩の間（補間）なので、
        /// 押した瞬間に見えていた時刻は「次の歩の始め − (1歩 − 余り)」。負の時刻で渡し、見えていた球に対して振ったことにする
        /// </summary>
        private void ReadSwing()
        {
            var input = Input;
            if (_side != BaseballTestSide.Bat || input == null)
            {
                return;
            }

            if (_stage != Stage.Live || _board.Phase != BaseballBoardPhase.Pitching)
            {
                // 投げる前に押した分は持ち越さない（構えのうちに押しておけば当たる、にならないように）
                _swingQueued = false;
                return;
            }

            if (!_swingQueued && input.IsActionDown(SwingAction))
            {
                _swingQueued = true;
                _swingTime = _accumulator - BaseballRules.StepSeconds;
            }
        }

        private BaseballInput TakeInput()
        {
            return _side == BaseballTestSide.Bat ? HumanBats() : HumanPitches();
        }

        /// <summary>人が打つ: 振る・バントは人、曲げるのは CPU の組み立て</summary>
        private BaseballInput HumanBats()
        {
            var input = Input;
            var bunt = input != null && input.IsActionHeld(BuntAction);
            var bend = BaseballAi.Bend(_board.Pitch, _plan, _board.Field);
            var result = new BaseballInput(bend, _swingQueued, bunt, _swingTime);
            _swingQueued = false;
            return result;
        }

        /// <summary>人が投げる: 曲げるのは人（左右の倒し具合）、振るのは CPU</summary>
        private BaseballInput HumanPitches()
        {
            var input = Input;
            var bend = input != null ? input.MoveVector.x : 0f;
            var swing = _ai.TrySwing(_board, out var swingTime);
            return new BaseballInput(bend, swing, false, swingTime);
        }
    }
}
