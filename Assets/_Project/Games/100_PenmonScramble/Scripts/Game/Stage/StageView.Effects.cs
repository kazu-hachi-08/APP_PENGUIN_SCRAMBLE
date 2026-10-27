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
