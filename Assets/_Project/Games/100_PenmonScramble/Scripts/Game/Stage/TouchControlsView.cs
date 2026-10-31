using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// スマホのボタンに、いま場に出ている子の様子を出す（§3）: 技1・技2は技の形のアイコンを属性の色で、待ちの残りは暗い扇で。
    /// 必殺技は縁のリングにゲージ、交代は次に出る子の顔と交代の待ち。出し入れは TouchControlsVisibility
    /// </summary>
    public class TouchControlsView : MonoBehaviour
    {
        private const int NoSpecies = -1;

        [SerializeField] private TouchControlsVisibility _visibility;
        [SerializeField] private TouchSkillButton _skill1;
        [SerializeField] private TouchSkillButton _skill2;
        [SerializeField] private TouchSpecialButton _special;
        [SerializeField] private TouchSkillButton _switch;

        private int _shownSpecies = NoSpecies;
        private int _shownNext = NoSpecies;

        public void SetShown(bool shown)
        {
            _visibility.SetShown(shown);
        }

        /// <param name="fallen">3体とも倒れて場から消えた。ボタンは出せないものとして見せる</param>
        public void Show(PlayerState state, bool fallen)
        {
            var active = state.Members[state.ActiveIndex];
            ShowSkills(active.SpeciesNo);
            _skill1.SetCooldown(fallen ? 1f : Cooldown(state, 0));
            _skill2.SetCooldown(fallen ? 1f : Cooldown(state, 1));
            _special.SetGauge(fallen ? 0f : state.SpecialGauge);
            ShowSwitch(state, fallen);
        }

        private static float Cooldown(PlayerState state, int slot)
        {
            return slot < state.SkillCooldowns.Count ? state.SkillCooldowns[slot] : 0f;
        }

        /// <summary>交代した時だけ描き直す（毎フレーム同じ絵を入れ直さないように）</summary>
        private void ShowSkills(int speciesNo)
        {
            if (speciesNo == _shownSpecies)
            {
                return;
            }

            _shownSpecies = speciesNo;
            var species = SpeciesTable.Get(speciesNo);
            if (species == null)
            {
                return;
            }

            var color = PenguinPalette.ElementColor(species.Element);
            _skill1.SetColor(color);
            _skill2.SetColor(color);
            _skill1.SetIcon(SkillShapeIcons.Get(species.Skill1.Shape));
            _skill2.SetIcon(SkillShapeIcons.Get(species.Skill2.Shape));
        }

        private void ShowSwitch(PlayerState state, bool fallen)
        {
            var next = fallen ? -1 : state.NextAliveIndex();
            _switch.SetUsable(next >= 0);
            _switch.SetCooldown(next >= 0 ? state.SwitchCooldown / Balance.SwitchCooldownSeconds : 0f);
            var nextSpecies = next >= 0 ? state.Members[next].SpeciesNo : NoSpecies;
            if (nextSpecies == _shownNext)
            {
                return;
            }

            _shownNext = nextSpecies;
            if (nextSpecies != NoSpecies)
            {
                _switch.SetFace(nextSpecies);
            }
        }
    }
}
