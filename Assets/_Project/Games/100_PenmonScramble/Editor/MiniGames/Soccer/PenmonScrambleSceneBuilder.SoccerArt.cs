using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// サッカーの絵。コードで描いて取り込む（ゴルフの旗の絵と同じ描き方）:
    /// ミニゲーム画面のカード（芝の上のサッカーボール。白い地に、まん中と縁の黒い五角形）と、
    /// スマホのボタンのアイコン（どれも白で描いて、ボタンの縁取りで面から浮かせる）:
    /// シュート＝勢いの線を引いて飛ぶボール、スライディング＝低く伸ばした足とボール、パス＝ボールから伸びる矢印、ダッシュ＝2つの山形
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string SoccerCardPath = UiArtFolder + "CardSoccer.png";
        internal const string SoccerShootIconPath = UiArtFolder + "SoccerShootIcon.png";
        internal const string SoccerSlideIconPath = UiArtFolder + "SoccerSlideIcon.png";
        internal const string SoccerPassIconPath = UiArtFolder + "SoccerPassIcon.png";
        internal const string SoccerDashIconPath = UiArtFolder + "SoccerDashIcon.png";

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

        // シュートのアイコン: 右上へ飛ぶボールと、その後ろの3本の勢いの線
        private static readonly Vector2 ShootBallCenter = new Vector2(0.28f, 0.12f);
        private const float ShootBallRadius = 0.5f;
        private const float ShootPatch = 0.17f;
        private const float ShootLineHalfWidth = 0.065f;
        private static readonly Vector2[] ShootLines =
        {
            new Vector2(-0.88f, 0.36f), new Vector2(-0.3f, 0.36f),
            new Vector2(-0.95f, 0.02f), new Vector2(-0.3f, 0.02f),
            new Vector2(-0.82f, -0.32f), new Vector2(-0.34f, -0.32f),
        };

        // スライディング: 右下のボールへ、左上から低く伸ばした足（太い線）と、その後ろの2本の勢いの線
        private static readonly Vector2 SlideBallCenter = new Vector2(0.48f, -0.38f);
        private const float SlideBallRadius = 0.3f;
        private const float SlidePatch = 0.1f;
        private static readonly Vector2 SlideLegFrom = new Vector2(-0.62f, 0.32f);
        private static readonly Vector2 SlideLegTo = new Vector2(0.08f, -0.4f);
        private const float SlideLegHalfWidth = 0.13f;
        private static readonly Vector2[] SlideLines =
        {
            new Vector2(-0.92f, -0.22f), new Vector2(-0.5f, -0.22f),
            new Vector2(-0.95f, -0.55f), new Vector2(-0.4f, -0.55f),
        };

        // パス: 左のボールから右へ伸びる矢印
        private static readonly Vector2 PassBallCenter = new Vector2(-0.52f, 0f);
        private const float PassBallRadius = 0.32f;
        private const float PassPatch = 0.11f;
        private static readonly Vector2 PassShaftFrom = new Vector2(-0.08f, 0f);
        private static readonly Vector2 PassTip = new Vector2(0.86f, 0f);
        private static readonly Vector2 PassHeadBack = new Vector2(0.5f, 0.34f);
        private const float PassLineHalfWidth = 0.09f;

        // ダッシュ: 右向きの山形を2つ
        private static readonly Vector2[] DashChevrons =
        {
            new Vector2(-0.62f, 0.52f), new Vector2(-0.12f, 0f),
            new Vector2(0.02f, 0.52f), new Vector2(0.52f, 0f),
        };
        private const float DashLineHalfWidth = 0.13f;

        private static void CreateSoccerArt()
        {
            WriteUiSprite(SoccerCardPath, DrawIcon(SoccerCardPixel), 0);
            WriteUiSprite(SoccerShootIconPath, DrawIcon(SoccerShootPixel), 0);
            WriteUiSprite(SoccerSlideIconPath, DrawIcon(SoccerSlidePixel), 0);
            WriteUiSprite(SoccerPassIconPath, DrawIcon(SoccerPassPixel), 0);
            WriteUiSprite(SoccerDashIconPath, DrawIcon(SoccerDashPixel), 0);
        }

        /// <summary>まん中の五角形だけを抜いた白いボール（小さいアイコン用。縁の五角形まで描くと潰れる）</summary>
        private static float IconBall(Vector2 p, Vector2 center, float radius, float patch)
        {
            var local = p - center;
            return Mathf.Max(local.magnitude - radius, -PentagonDistance(local, patch, Mathf.PI * 0.5f));
        }

        private static Color SoccerSlidePixel(Vector2 p, float pixel)
        {
            var shape = Mathf.Min(IconBall(p, SlideBallCenter, SlideBallRadius, SlidePatch), SegmentDistance(p, SlideLegFrom, SlideLegTo) - SlideLegHalfWidth);
            for (var i = 0; i < SlideLines.Length; i += 2)
            {
                shape = Mathf.Min(shape, SegmentDistance(p, SlideLines[i], SlideLines[i + 1]) - ShootLineHalfWidth);
            }

            return new Color(1f, 1f, 1f, IconCoverage(shape, pixel));
        }

        private static Color SoccerPassPixel(Vector2 p, float pixel)
        {
            var headLow = new Vector2(PassHeadBack.x, -PassHeadBack.y);
            var arrow = Mathf.Min(
                SegmentDistance(p, PassShaftFrom, PassTip),
                Mathf.Min(SegmentDistance(p, PassTip, PassHeadBack), SegmentDistance(p, PassTip, headLow))) - PassLineHalfWidth;
            var shape = Mathf.Min(IconBall(p, PassBallCenter, PassBallRadius, PassPatch), arrow);
            return new Color(1f, 1f, 1f, IconCoverage(shape, pixel));
        }

        private static Color SoccerDashPixel(Vector2 p, float pixel)
        {
            var shape = float.MaxValue;
            for (var i = 0; i < DashChevrons.Length; i += 2)
            {
                var top = DashChevrons[i];
                var tip = DashChevrons[i + 1];
                var bottom = new Vector2(top.x, -top.y);
                shape = Mathf.Min(shape, Mathf.Min(SegmentDistance(p, top, tip), SegmentDistance(p, tip, bottom)) - DashLineHalfWidth);
            }

            return new Color(1f, 1f, 1f, IconCoverage(shape, pixel));
        }

        /// <summary>白いボール（まん中と縁の五角形は抜いて、ボタンの色を透かす）と勢いの線</summary>
        private static Color SoccerShootPixel(Vector2 p, float pixel)
        {
            var local = p - ShootBallCenter;
            var ball = local.magnitude - ShootBallRadius;
            var patch = PentagonDistance(local, ShootPatch, Mathf.PI * 0.5f);
            for (var i = 0; i < 5; i++)
            {
                var angle = Mathf.PI * 0.5f + Mathf.PI / 5f + i * Mathf.PI * 2f / 5f;
                var center = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ShootBallRadius * 0.95f);
                patch = Mathf.Min(patch, PentagonDistance(local - center, ShootPatch, angle));
            }

            var lines = float.MaxValue;
            for (var i = 0; i < ShootLines.Length; i += 2)
            {
                lines = Mathf.Min(lines, SegmentDistance(p, ShootLines[i], ShootLines[i + 1]) - ShootLineHalfWidth);
            }

            var shape = Mathf.Min(Mathf.Max(ball, -patch), lines);
            return new Color(1f, 1f, 1f, IconCoverage(shape, pixel));
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
