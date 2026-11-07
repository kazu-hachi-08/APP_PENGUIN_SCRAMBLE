using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 魔球（投げる側）と必殺打（打つ側）。各子1試合1回ずつ（残りは BaseballGame が持つ）。
    /// 人は投げる前に「魔球」「必殺打」の札（Space / L1・画面の札）で ON にしておく。ON の魔球は次に投げる球で使い、
    /// 必殺打はその打席の間 ON のまま（当てるまで使った扱いにしない）。CPU は投げる前に、相手に見えないまま決める。
    /// 投げた瞬間に、出した側のカットイン（上が投げる子・下が打つ子）を出す
    /// </summary>
    public partial class BaseballMatch
    {
        private const string MagicHeading = "の魔球！";
        private const string HitHeading = "の必殺打！";

        [Header("魔球・必殺打のカットイン")]
        [Tooltip("投げる子の魔球（画面の上の帯）")]
        [SerializeField] private SpecialCutIn _pitchCutIn;
        [Tooltip("打つ子の必殺打（画面の下の帯）")]
        [SerializeField] private SpecialCutIn _hitCutIn;

        private System.Random _specialRandom;
        private bool _magicOn;
        private bool _specialOn;
        private BaseballPitchMagic _cpuMagic;

        private int PitcherSpecies => _teams[_game.FieldingTeam][_game.CurrentPitcher];
        private int BatterSpecies => _teams[_game.BattingTeam][_game.CurrentBatter];

        /// <summary>CPU が投げる1球を組み立てる（魔球を出すかも、ここで人に見えないまま決める）</summary>
        private void PlanCpuPitch()
        {
            var field = _board.Field;
            if (_ai.WantsMagic(_game))
            {
                var special = BaseballSpecial.Of(PitcherSpecies);
                _cpuMagic = _ai.RollMagic(special);
                _plan = _ai.PlanMagicPitch(field, special);
                return;
            }

            _cpuMagic = BaseballPitchMagic.None;
            _plan = _ai.PlanPitch(field, _game.Count);
        }

        /// <summary>この球の魔球（人が ON にしていた・CPU が決めていた）。人の ON は投げたら戻す</summary>
        private BaseballPitchMagic TakeMagic()
        {
            if (HumanBats)
            {
                var magic = _cpuMagic;
                _cpuMagic = BaseballPitchMagic.None;
                return magic;
            }

            var on = _magicOn && _game.CanUseMagic;
            _magicOn = false;
            return on ? BaseballPitchMagic.Roll(BaseballSpecial.Of(PitcherSpecies), _specialRandom) : BaseballPitchMagic.None;
        }

        /// <summary>この球の必殺打（人が ON にしている・CPU がこの球で決めた）</summary>
        private BaseballSpecial TakeHitSpecial()
        {
            var on = HumanBats ? _specialOn && _game.CanUseSpecialHit : _ai.WantsSpecialHit(_game);
            return on ? BaseballSpecial.Of(BatterSpecies) : BaseballSpecial.None;
        }

        /// <summary>
        /// 札を切り替えられる間: 投げる番は投げるまで（構え・振りかぶり）。打つ番は投げるまでと、打席が続く間の結果を見せている間
        /// （CPU の構えは短いので、前の球の結果を見ている間にも押せるように）
        /// </summary>
        private bool CanToggleSpecial
        {
            get
            {
                if (_game == null || _game.IsOver || IsMenuOpen)
                {
                    return false;
                }

                var beforeThrow = _stage == Stage.Ready || _stage == Stage.WindUp;
                if (!HumanBats)
                {
                    return beforeThrow && _game.CanUseMagic;
                }

                var betweenPitches = _stage == Stage.Result && !_game.IsPlateStart && !_game.IsHalfOver;
                return (beforeThrow || betweenPitches) && _game.CanUseSpecialHit;
            }
        }

        private void ToggleSpecial()
        {
            if (!CanToggleSpecial)
            {
                return;
            }

            if (HumanBats)
            {
                _specialOn = !_specialOn;
            }
            else
            {
                _magicOn = !_magicOn;
            }

            GameSounds.Play(GameSound.Click);
            RefreshSpecialToggle();
        }

        /// <summary>札の出し入れと ON / OFF の見た目（残っていない子・切り替えられない間は隠す）</summary>
        private void RefreshSpecialToggle()
        {
            if (_game == null)
            {
                return;
            }

            var on = HumanBats ? _specialOn : _magicOn;
            var species = HumanBats ? BatterSpecies : PitcherSpecies;
            _hud.ShowSpecialToggle(CanToggleSpecial, HumanBats, on, BaseballSpecial.Of(species));
        }

        /// <summary>投げた瞬間のカットイン（出した側だけ。両方なら上下に2本）</summary>
        private void PlayCutIns(BaseballSpecial magic, BaseballSpecial hit)
        {
            var seconds = BaseballBalance.SpecialCutInSeconds;
            if (magic.IsActive)
            {
                _pitchCutIn.Play(PitcherSpecies, magic.Element, MagicHeading, BaseballSpecialNames.Magic(magic.Element), seconds);
            }

            if (hit.IsActive)
            {
                _hitCutIn.Play(BatterSpecies, hit.Element, HitHeading, BaseballSpecialNames.Hit(hit.Element), seconds);
            }

            BaseballSounds.Play(BaseballSound.Special);
        }

        /// <summary>必殺打で当てていたら使った扱いにする（次の打者へ進む前に。人の ON も戻す）</summary>
        private void SpendSpecialHit()
        {
            if (!_board.SpecialHitUsed)
            {
                return;
            }

            _game.UseSpecialHit();
            if (HumanBats)
            {
                _specialOn = false;
            }
        }

        /// <summary>人が追い風ヒットで打ち返したら、スマホでは打球を曲げるスティックを出す（打席の終わりで隠す）</summary>
        private void OnSpecialEvent(BaseballEvent e)
        {
            if (e.Kind == BaseballEventKind.SpecialHit && HumanBats && _board.Ball.Special.Is(PenmonScramble.Core.Element.Wind))
            {
                _hud.SetSteerStick(true);
            }
        }

        /// <summary>打席が替わったら、人の必殺打の ON を戻す（ON はその打席の子のもの）</summary>
        private void ResetSpecialToggles()
        {
            _specialOn = false;
            _magicOn = false;
            _cpuMagic = BaseballPitchMagic.None;
            _pitchCutIn.Hide();
            _hitCutIn.Hide();
        }
    }
}
