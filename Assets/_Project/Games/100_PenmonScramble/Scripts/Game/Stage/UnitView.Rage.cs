using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 本気の大ボスの赤い湯気。体の縁から小さな赤い煙が立ちのぼり続ける。吠えた後（HP 半分を切った後）は濃く・速くする。
    /// もぐっている・降りてくる間は体が床の上に無いので出さない
    /// </summary>
    public partial class UnitView
    {
        private static readonly Color SteamColor = new Color(1f, 0.32f, 0.25f);
        private const float SteamInterval = 0.28f;
        private const float EnragedSteamInterval = 0.12f;
        // 煙1つの大きさ（当たりの半径に対する割合）。体を隠さない小ささにする
        private const float SteamPuffRatio = 0.3f;
        // 体の縁から出す（真ん中から出すと体に隠れて見えないため）
        private const float SteamRimRatio = 0.85f;

        private float _steamTimer;

        private void TickRageSteam(float dt)
        {
            if (!IsSerious || IsDefeated || IsSubmerged || IsDroppingIn || _model == null)
            {
                return;
            }

            _steamTimer -= dt;
            if (_steamTimer > 0f)
            {
                return;
            }

            _steamTimer = IsEnraged ? EnragedSteamInterval : SteamInterval;
            var rim = Random.insideUnitCircle.normalized * (_radius * SteamRimRatio);
            var height = _model.TopHeight * _modelScale;
            SmokePuff.Spawn(transform.parent, _materials, _to + rim, _radius * SteamPuffRatio, height, SteamColor);
        }
    }
}
