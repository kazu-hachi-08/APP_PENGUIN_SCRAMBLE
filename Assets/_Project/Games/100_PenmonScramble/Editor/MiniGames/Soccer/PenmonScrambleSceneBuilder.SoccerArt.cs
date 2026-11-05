using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ミニゲーム画面のサッカーのカードの絵: 芝の上のサッカーボール（白い地に、まん中と縁の黒い五角形）。
    /// 絵はコードで描いて取り込む（ゴルフの旗の絵と同じ描き方）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string SoccerCardPath = UiArtFolder + "CardSoccer.png";

        private static readonly Color SoccerGrassColor = new Color(0.47f, 0.82f, 0.36f);
        private static readonly Color SoccerShadowColor = new Color(0.20f, 0.42f, 0.18f, 0.55f);
        private static readonly Color SoccerBallColor = Color.white;
        private static readonly Color SoccerPatchColor = new Color(0.16f, 0.17f, 0.22f);
        private static readonly Color SoccerRimColor = new Color(0.55f, 0.6f, 0.68f);

        private static readonly Vector2 SoccerBallCenter = new Vector2(0f, 0.04f);
        private const float SoccerBallRadius = 0.62f;
        private const float SoccerRimWidth = 0.04f;
        private const float SoccerCenterPatch = 0.2f;
        private const float SoccerEdgePatch = 0.2f;
        // 縁の五角形の中心はボールの縁より少し内側（半分ほど見切れて、球の丸みに見える）
        private const float SoccerEdgePatchDistance = 0.58f;

        private static void CreateSoccerCardArt()
        {
            WriteUiSprite(SoccerCardPath, DrawIcon(SoccerCardPixel), 0);
        }

        /// <summary>奥から重ねる: 芝の丸 → 影 → ボールの縁 → 白い地 → 五角形（ボールの中だけ）</summary>
        private static Color SoccerCardPixel(Vector2 p, float pixel)
        {
            var color = Color.clear;
            color = Over(color, SoccerGrassColor, IconCoverage(EllipseDistance(p - new Vector2(0f, -0.68f), new Vector2(0.92f, 0.24f)), pixel));
            color = Over(color, SoccerShadowColor, IconCoverage(EllipseDistance(p - new Vector2(0.08f, -0.64f), new Vector2(0.5f, 0.1f)), pixel));

            var local = p - SoccerBallCenter;
            var ball = local.magnitude - SoccerBallRadius;
            color = Over(color, SoccerRimColor, IconCoverage(ball, pixel));
            var inside = ball + SoccerRimWidth;
            color = Over(color, SoccerBallColor, IconCoverage(inside, pixel));

            var patch = PentagonDistance(local, SoccerCenterPatch, Mathf.PI * 0.5f);
            for (var i = 0; i < 5; i++)
            {
                // まん中の五角形の辺の向きに、逆さの五角形を置く
                var angle = Mathf.PI * 0.5f + Mathf.PI / 5f + i * Mathf.PI * 2f / 5f;
                var center = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SoccerEdgePatchDistance;
                patch = Mathf.Min(patch, PentagonDistance(local - center, SoccerEdgePatch, angle));
            }

            return Over(color, SoccerPatchColor, IconCoverage(Mathf.Max(patch, inside), pixel));
        }

        /// <summary>正五角形の辺からのおおよその距離（内側が負）。rotation は頂点の1つの向き</summary>
        private static float PentagonDistance(Vector2 p, float radius, float rotation)
        {
            const float sector = Mathf.PI * 2f / 5f;
            var angle = Mathf.Atan2(p.y, p.x) - rotation;
            // 一番近い辺のまん中からの角度（辺のまん中は頂点から半分ずれた向き）
            var fromEdgeCenter = Mathf.Repeat(angle, sector) - sector * 0.5f;
            return p.magnitude * Mathf.Cos(fromEdgeCenter) - radius * Mathf.Cos(sector * 0.5f);
        }
    }
}
