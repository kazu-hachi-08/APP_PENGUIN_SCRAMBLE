using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Input;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 入力: 打つ人は 打席の中を左右へ動く（←→ / 左スティック / スマホのスティック。投げた球が来ている間）と
    /// 振る（J / × / 右の大ボタン。押した時刻を歩の中まで渡す）と バント（K / ○ / 右の中ボタンを押している間）。
    /// 投げる人は 構えの間に球種（J ストレート / K カーブ / L チェンジアップ。× ○ △・画面の札でも）を選んで投げ、
    /// 投げた後は ←→ / 左スティック / スマホのスティックで曲げる。打席の始めは ↑↓（画面の札）で投げる子を替えられる。
    /// 魔球・必殺打の ON / OFF は Space / L1（画面の札）。追い風ヒットで打った後は ←→ / スティックで打球を曲げる。
    /// 1歩ごとに、この端末の人の番の入力と、CPU が打つ番の入力（ひとり・ホストだけ）を1球の記録に書く。ほかの人の入力は届いた記録を使う
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
            if (input == null || _game == null || _stage == BaseballStage.Over)
            {
                return;
            }

            if (input.IsActionDown(SpecialAction))
            {
                ToggleSpecial();
            }

            if (!LocalPitches || _stage != BaseballStage.Ready)
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

        /// <summary>↑↓ で投げる子を1つずつ替える（打席の始めだけ。下が次の子。自分の子だけを順に回る）</summary>
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
            var count = _teams[_game.FieldingTeam].Count;
            var step = y < 0f ? 1 : count - 1;
            var slot = _game.CurrentPitcher;
            for (var i = 0; i < count; i++)
            {
                slot = (slot + step) % count;
                if (_plan.CanPitch(_localSeat, _game.FieldingTeam, slot))
                {
                    ChoosePitcher(slot);
                    return;
                }
            }
        }

        private void OnPitchCardChosen(BaseballPitchType type)
        {
            if (LocalPitches && _stage == BaseballStage.Ready && !IsMenuOpen)
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
            if (input == null || _game == null || _stage == BaseballStage.Over || !LocalBats)
            {
                return;
            }

            if (_stage != BaseballStage.Live || Board.Phase != BaseballBoardPhase.Pitching)
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

        /// <summary>この歩の入力を記録に書く（この端末の人の番と、ひとり・ホストでは CPU が打つ番）。計算は記録だけを見る</summary>
        private void RecordInputs()
        {
            // 記録はこの歩の分まで（届いた記録に置き換わっていなければ、ちょうどこの歩の番になっている）
            var step = _session.Step;
            if (LocalPitches)
            {
                var input = Input;
                var bend = input != null && !IsMenuOpen ? input.MoveVector.x : 0f;
                while (_session.PitcherLog.Progress <= step && _session.PitcherLog.Progress < BaseballRoleLog.MaxSteps)
                {
                    _session.PitcherLog.Record(bend, false, false, 0f);
                }
            }

            if (_session.BatterLog.Progress > step)
            {
                return;
            }

            if (LocalBats)
            {
                RecordLocalBat(step);
            }
            else if (CpuBats && !IsGuest)
            {
                var bat = _ai.BatInput(Board);
                _session.BatterLog.Record(bat.Move, bat.Bunt, bat.Swing, bat.SwingTime);
            }
        }

        /// <summary>左右・振る・バントは人。←→ / スティックは、投げた球が来ている間は打席の中の左右、追い風ヒットで打った後は打球を曲げる</summary>
        private void RecordLocalBat(int step)
        {
            var input = Input;
            var usable = input != null && !IsMenuOpen;
            var bunt = usable && input.IsActionHeld(BuntAction);
            var steer = usable ? input.MoveVector.x : 0f;
            var log = _session.BatterLog;
            while (log.Progress < step && log.Progress < BaseballRoleLog.MaxSteps)
            {
                log.Record(steer, bunt, false, 0f);
            }

            log.Record(steer, bunt, _swingQueued, _swingTime);
            _swingQueued = false;
        }
    }
}
