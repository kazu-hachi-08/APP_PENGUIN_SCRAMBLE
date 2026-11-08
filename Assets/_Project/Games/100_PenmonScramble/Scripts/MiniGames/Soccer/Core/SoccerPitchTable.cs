using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ピッチの定義表。どのスタジアムも同じ形（フットサルくらいの広さ）で、スタジアムごとに仕掛けを重ねる。
    /// 仕掛けは赤と青で有利・不利が出ないよう、真ん中を中心に 180° 回して重なる形に置く。
    /// ゴールの前（キーパーの動く所）には置かない（キーパーの読みと止める割合を、どのスタジアムでも同じにするため）
    /// </summary>
    public static class SoccerPitchTable
    {
        // 横 28 × 縦 16 マス・ゴールの口 幅 5 × 高さ 1.6 マス
        private const float HalfLength = 14f;
        private const float HalfWidth = 8f;
        private const float CornerCut = 1.6f;
        private const float GoalHalfWidth = 2.5f;
        private const float GoalBarHeight = 1.6f;
        private const float GoalDepth = 1.2f;

        // 赤（左の陣地）の並び。蹴る側の先頭はボール（真ん中）のすぐ後ろ、残りは少し後ろの上下。
        // 4体目（協力の4人のときの4対4）はゴールの前の真ん中
        private static readonly Vec2[] KickingSpots = { new Vec2(-0.9f, 0f), new Vec2(-5f, 3.5f), new Vec2(-5f, -3.5f), new Vec2(-9f, 0f) };
        private static readonly Vec2[] WaitingSpots = { new Vec2(-3.5f, 0f), new Vec2(-7f, 3.5f), new Vec2(-7f, -3.5f), new Vec2(-10f, 0f) };

        // ---- 氷山: 真ん中の帯と、両陣地の柵ぎわの1枚ずつ（案）----
        private const float IceBandHalf = 6f;
        private static readonly Vec2 IcePatchMin = new Vec2(-11f, 4f);
        private static readonly Vec2 IcePatchMax = new Vec2(-6.5f, HalfWidth);

        // ---- 深海: 両陣地に縦に流れる帯。8 秒ごとに向きが替わる（案）----
        private const float CurrentInner = 5f;
        private const float CurrentOuter = 8.5f;
        private const float CurrentAccel = 9f;
        private const float CurrentMaxSpeed = 4.5f;
        private const float CurrentFlipSeconds = 8f;

        // ---- 嵐の崖: 真ん中の風の帯（3 秒吹いて 2 秒止み、吹くたびに向きが替わる）と、帯の中のバンパーの柱・四隅のバンパー（案）----
        private const float WindHalf = 4.5f;
        private const float WindAccel = 8f;
        private const float WindMaxSpeed = 5f;
        private const float WindBlowSeconds = 3f;
        private const float WindCalmSeconds = 2f;
        private const float PostRadius = 0.65f;
        private static readonly Vec2 PostSpot = new Vec2(0f, 4.6f);

        // ---- 火山の入り江: 6 つの間欠泉。6 秒ごとに 1.5 秒噴き、口ごとにずらす（案）----
        private const float GeyserRadius = 1.25f;
        private const float GeyserPeriod = 6f;
        private const float GeyserActive = 1.5f;
        private static readonly Vec2 OuterGeyser = new Vec2(9f, 3.2f);
        private static readonly Vec2 MiddleGeyser = new Vec2(0f, 5f);

        /// <summary>試しのピッチ（テスト・試合の確認）。スタジアム1（海岸）と同じ仕掛けの無い形</summary>
        public static SoccerPitchDef Standard() => Create("試しのピッチ");

        /// <summary>1 海岸: 仕掛けなし</summary>
        public static SoccerPitchDef Beach() => Create("海岸");

        /// <summary>2 氷山: 氷の床（ボールも子もよく滑る）</summary>
        public static SoccerPitchDef Iceberg() => Create("氷山", grounds: new[]
        {
            new SoccerGroundZone(SoccerGround.Ice, new Vec2(-IceBandHalf, -HalfWidth), new Vec2(IceBandHalf, HalfWidth)),
            new SoccerGroundZone(SoccerGround.Ice, IcePatchMin, IcePatchMax),
            new SoccerGroundZone(SoccerGround.Ice, IcePatchMax * -1f, IcePatchMin * -1f),
        });

        /// <summary>3 深海: 水流の帯（転がるボールを押し流す）。左の帯と右の帯はいつも反対へ流れる</summary>
        public static SoccerPitchDef DeepSea()
        {
            var cycle = new MiniGameCycle(CurrentFlipSeconds, CurrentFlipSeconds);
            return Create("深海", flows: new[]
            {
                new SoccerFlowZone(SoccerFlowKind.Current, new Vec2(-CurrentOuter, -HalfWidth), new Vec2(-CurrentInner, HalfWidth),
                    new Vec2(0f, 1f), CurrentAccel, CurrentMaxSpeed, cycle, reverses: true),
                new SoccerFlowZone(SoccerFlowKind.Current, new Vec2(CurrentInner, -HalfWidth), new Vec2(CurrentOuter, HalfWidth),
                    new Vec2(0f, -1f), CurrentAccel, CurrentMaxSpeed, cycle, reverses: true),
            });
        }

        /// <summary>
        /// 4 嵐の崖: 真ん中の風の帯と、帯の中のバンパーの柱2本（風で流された球が当たって弾ける）・四隅の柵のバンパー（角の球を弾き出す）。
        /// 柱はキックオフの並びとセンターサークルにかからない所に立てる
        /// </summary>
        public static SoccerPitchDef StormCliff()
        {
            var gust = new MiniGameCycle(WindBlowSeconds + WindCalmSeconds, WindBlowSeconds);
            float x = HalfLength, y = HalfWidth, c = CornerCut;
            return Create("嵐の崖",
                flows: new[]
                {
                    new SoccerFlowZone(SoccerFlowKind.Wind, new Vec2(-WindHalf, -HalfWidth), new Vec2(WindHalf, HalfWidth),
                        new Vec2(0f, 1f), WindAccel, WindMaxSpeed, gust, reverses: true),
                },
                posts: new[] { new SoccerPost(PostSpot, PostRadius), new SoccerPost(PostSpot * -1f, PostRadius) },
                bumpers: new[]
                {
                    new SoccerBumper(new Vec2(x - c, -y), new Vec2(x, -y + c)),
                    new SoccerBumper(new Vec2(x, y - c), new Vec2(x - c, y)),
                    new SoccerBumper(new Vec2(-x + c, y), new Vec2(-x, y - c)),
                    new SoccerBumper(new Vec2(-x, -y + c), new Vec2(-x + c, -y)),
                });
        }

        /// <summary>5 火山の入り江: 間欠泉（ボールを上へ跳ね上げる）。180° 回して重なる2つずつは同じ時に噴く</summary>
        public static SoccerPitchDef VolcanoCove()
        {
            SoccerGeyser Geyser(Vec2 at, float offset) => new SoccerGeyser(at, GeyserRadius, new MiniGameCycle(GeyserPeriod, GeyserActive, offset));
            var flipped = new Vec2(OuterGeyser.X, -OuterGeyser.Y);
            return Create("火山の入り江", geysers: new[]
            {
                Geyser(OuterGeyser * -1f, 0f),
                Geyser(OuterGeyser, 0f),
                Geyser(flipped, GeyserPeriod * 0.5f),
                Geyser(flipped * -1f, GeyserPeriod * 0.5f),
                Geyser(MiddleGeyser, GeyserPeriod * 0.25f),
                Geyser(MiddleGeyser * -1f, GeyserPeriod * 0.75f),
            });
        }

        private static SoccerPitchDef Create(
            string name,
            IReadOnlyList<SoccerGroundZone> grounds = null,
            IReadOnlyList<SoccerFlowZone> flows = null,
            IReadOnlyList<SoccerGeyser> geysers = null,
            IReadOnlyList<SoccerBumper> bumpers = null,
            IReadOnlyList<SoccerPost> posts = null)
        {
            return new SoccerPitchDef(
                name,
                HalfLength,
                HalfWidth,
                CornerCut,
                GoalHalfWidth,
                GoalBarHeight,
                GoalDepth,
                KickingSpots,
                WaitingSpots,
                grounds,
                flows,
                geysers,
                bumpers,
                posts);
        }
    }
}
