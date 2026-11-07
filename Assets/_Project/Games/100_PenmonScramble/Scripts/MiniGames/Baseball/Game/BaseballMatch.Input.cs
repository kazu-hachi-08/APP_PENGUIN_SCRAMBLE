using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Input;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 入力: 打つ人は 振る（J / × / 右の大ボタン。押した時刻を歩の中まで渡す）と バント（K / ○ / 右の中ボタンを押している間）。
    /// 投げる人は 構えの間に球種（J ストレート / K カーブ / L チェンジアップ。× ○ △・画面の札でも）を選んで投げ、
    /// 投げた後は ←→ / 左スティック / スマホのスティックで曲げる。打席の始めは ↑↓（画面の札）で投げる子を替えられる。
    /// 魔球・必殺打の ON / OFF は Space / L1（画面の札）。追い風ヒットで打った後は ←→ / スティックで打球を曲げる。
    /// 相手の側は CPU（BaseballAi）
    /// </summary>
    public partial class BaseballMatch
    {
        // 球種のキー（InputManager の Action1〜3 = J/K/L・×/○/△）
        private const int PitchActionCount = 3;
        private const int SwingAction = 0;
        private const int BuntAction = 1;
        // 魔球・必殺打の ON / OFF（Action4 = Space / L1）
        private const int SpecialAction = 3;

        // 投げる子を替える ↑↓ を、倒したと見なす深さ（倒したままでは1つずつしか替わらない）
        private const float PitcherStickThreshold = 0.6f;

        private BaseballPitchType? _chosenPitch;
        private bool _swingQueued;
        private float _swingTime;
        private bool _pitcherStickHeld;

        private static InputManager Input => InputManager.HasInstance ? InputManager.Instance : null;

        /// <summary>魔球・必殺打の切り替えと、投げる人が構えの間に押した球種・投げる子の切り替えを読む</summary>
        private void ReadPitchKeys()
        {
            var input = Input;
            if (input == null || _stage == Stage.Over)
            {
                return;
            }

            if (input.IsActionDown(SpecialAction))
            {
                ToggleSpecial();
            }

            if (HumanBats || _stage != Stage.Ready)
            {
                return;
            }

            ReadPitcherStick(input);
            for (var i = 0; i < PitchActionCount; i++)
            {
                if (input.IsActionDown(i))
                {
                    _chosenPitch = (BaseballPitchType)i;
                    return;
                }
            }
        }

        /// <summary>↑↓ で投げる子を1つずつ替える（打席の始めだけ。下が次の子）</summary>
        private void ReadPitcherStick(InputManager input)
        {
            var y = input.MoveVector.y;
            if (System.Math.Abs(y) < PitcherStickThreshold)
            {
                _pitcherStickHeld = false;
                return;
            }

            if (_pitcherStickHeld || !_game.IsPlateStart)
            {
                return;
            }

            _pitcherStickHeld = true;
            var count = _teams[_humanTeam].Count;
            var step = y < 0f ? 1 : count - 1;
            ChoosePitcher((_game.CurrentPitcher + step) % count);
        }

        private void OnPitchCardChosen(BaseballPitchType type)
        {
            if (!HumanBats && _stage == Stage.Ready && !IsMenuOpen)
            {
                _chosenPitch = type;
            }
        }

        private void OnPitcherCardChosen(int slot) => ChoosePitcher(slot);

        /// <summary>
        /// 振るボタンを押した瞬間を、次に進める歩の始めからの秒で取っておく。画面に描いているのは前の歩と今の歩の間（補間）なので、
        /// 押した瞬間に見えていた時刻は「次の歩の始め − (1歩 − 余り)」。負の時刻で渡し、見えていた球に対して振ったことにする
        /// </summary>
        private void ReadSwing()
        {
            var input = Input;
            if (input == null || _stage == Stage.Over || !HumanBats)
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
            return HumanBats ? HumanBatsInput() : HumanPitchesInput();
        }

        /// <summary>人が打つ: 振る・バントは人、曲げるのは CPU の組み立て。追い風ヒットで打った後は、人のスティックで打球を曲げる</summary>
        private BaseballInput HumanBatsInput()
        {
            var input = Input;
            var bunt = input != null && input.IsActionHeld(BuntAction);
            var bend = _board.Phase == BaseballBoardPhase.BallInPlay
                ? input != null ? input.MoveVector.x : 0f
                : BaseballAi.Bend(_board.Pitch, _plan, _board.Field);
            var result = new BaseballInput(bend, _swingQueued, bunt, _swingTime);
            _swingQueued = false;
            return result;
        }

        /// <summary>人が投げる: 曲げるのは人（左右の倒し具合）、振る・バント・追い風ヒットの打球を曲げるのは CPU</summary>
        private BaseballInput HumanPitchesInput()
        {
            var input = Input;
            var bend = _board.Phase == BaseballBoardPhase.BallInPlay
                ? BaseballAi.Steer(_board)
                : input != null ? input.MoveVector.x : 0f;
            var bat = _ai.BatInput(_board);
            return new BaseballInput(bend, bat.Swing, bat.Bunt, bat.SwingTime);
        }
    }
}
