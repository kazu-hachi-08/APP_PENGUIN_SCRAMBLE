using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 技・必殺技の演出（範囲がはじける見た目・画面の揺れ・カットイン）
    /// </summary>
    public partial class StageView
    {
        private const float SwingAlpha = 0.45f;
        private const float SwingSeconds = 0.12f;
        private const float RingSeconds = 0.25f;
        private const float BlastAlpha = 0.6f;
        private const float BlastSeconds = 0.3f;

        // 揺れの強さ（マス）と長さ（秒）
        private const float SpecialUsedShake = 0.15f;
        private const float SpecialHitShake = 0.25f;
        private const float SpecialHitShakeSeconds = 0.3f;
        private const float BigBlastShake = 0.08f;
        private const float BigBlastShakeSeconds = 0.15f;
        // これより大きい落下は、必殺技でなくても少し揺らす
        private const float BigBlastRadius = 1.5f;

        // 合体技（§4.6）。必殺技ほどではないが、ふつうの技より大きく見せる
        private static readonly Color ComboColor = new Color(1f, 0.9f, 0.35f);
        private const float ComboRingRadius = 3f;
        private const float ComboRingSeconds = 0.4f;
        private const float ComboFlashRadius = 1.6f;
        private const float ComboFlashSeconds = 0.25f;
        private const float ComboShake = 0.18f;
        private const float ComboShakeSeconds = 0.25f;

        private void PlayEvents(StageSnapshot snapshot)
        {
            foreach (var used in snapshot.SkillsUsed)
            {
                if (_units.TryGetValue(used.UnitId, out var view))
                {
                    view.Squash();
                }
            }

            foreach (var special in snapshot.SpecialsUsed)
            {
                OnSpecialUsed(special);
            }

            foreach (var burst in snapshot.Bursts)
            {
                OnBurst(burst);
            }

            foreach (var combo in snapshot.Combos)
            {
                OnCombo(combo);
            }
        }

        /// <summary>「合体！○○」の文字は HUD（StageHud.Progress）で出す</summary>
        private void OnCombo(ComboEvent e)
        {
            var position = e.Position.ToVector2();
            BurstFx.Spawn(transform, PlaceholderSprites.Circle, WithAlpha(Color.white, BlastAlpha), position, ComboFlashRadius * 0.3f, ComboFlashRadius, ComboFlashSeconds, SortingDepth.Popup - 1);
            BurstFx.Spawn(transform, PlaceholderSprites.Ring, ComboColor, position, ComboRingRadius * 0.2f, ComboRingRadius, ComboRingSeconds, SortingDepth.Popup - 1);
            _cameraShake.Shake(ComboShake, ComboShakeSeconds);
        }

        private void OnSpecialUsed(SpecialUsedEvent e)
        {
            _cutIn.Play(e);
            _cameraShake.Shake(SpecialUsedShake, Balance.SpecialCutInTicks * Balance.TickSeconds);
        }

        private void OnBurst(BurstEvent e)
        {
            var color = PenguinPalette.ElementColor(e.Element);
            var position = e.Position.ToVector2();
            switch (e.Shape)
            {
                case BurstShape.Swing:
                    BurstFx.Spawn(transform, PlaceholderSprites.Circle, WithAlpha(color, SwingAlpha), position, e.Radius * 0.6f, e.Radius, SwingSeconds, SortingDepth.Ground);
                    break;
                case BurstShape.Ring:
                    BurstFx.Spawn(transform, PlaceholderSprites.Ring, color, position, e.Radius * 0.3f, e.Radius, RingSeconds, SortingDepth.Popup - 3);
                    break;
                default:
                    BurstFx.Spawn(transform, PlaceholderSprites.Circle, WithAlpha(color, BlastAlpha), position, e.Radius * 0.5f, e.Radius * 1.1f, BlastSeconds, SortingDepth.Popup - 3);
                    break;
            }

            if (e.IsSpecial)
            {
                _cameraShake.Shake(SpecialHitShake, SpecialHitShakeSeconds);
            }
            else if (e.Shape == BurstShape.Blast && e.Radius >= BigBlastRadius)
            {
                _cameraShake.Shake(BigBlastShake, BigBlastShakeSeconds);
            }
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
