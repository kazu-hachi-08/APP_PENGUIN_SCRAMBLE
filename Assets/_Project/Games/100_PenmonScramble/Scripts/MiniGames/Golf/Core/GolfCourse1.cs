using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// コース1「海岸」（砂浜と流氷）。砂地（止まりやすい）・流氷の低い壁（水切りで越えられる）・波打ち際の池。
    /// パーは 2・3・4（合計 9）。座標は上から見た地面の上（X が右・Y が奥、m）
    /// </summary>
    public static class GolfCourse1
    {
        public static readonly IReadOnlyList<GolfHoleDef> Holes = new[]
        {
            FirstBeach(),
            Shoreline(),
            FloeInlet(),
        };

        /// <summary>
        /// ホール1「はじまりの浜」パー 2: まっすぐな浜の通り。まん中の流氷をよけて（または壁で返して）カップの手前まで、2打目で入れる
        /// </summary>
        private static GolfHoleDef FirstBeach()
        {
            var ground = MiniGameGeometry.Rect(0f, 0f, 6f, 16f);
            var walls = GolfHoleDef.WallsAround(ground);
            // まん中の流氷（三角の低い壁。とがった方をティーへ向ける）
            walls.AddRange(GolfHoleDef.WallsAround(new[] { new Vec2(3f, 6.6f), new Vec2(4.1f, 8.5f), new Vec2(1.9f, 8.5f) }, GolfBalance.WallBounce, GolfBalance.LowWallHeight));

            var areas = new List<GolfArea>
            {
                // カップの右手前の砂地。強すぎた球を止める
                GolfArea.Sand(MiniGameGeometry.Rect(3.9f, 10.8f, 5.7f, 13.2f)),
                // カップの左奥の砂地
                GolfArea.Sand(MiniGameGeometry.Rect(0.3f, 13.4f, 2.1f, 15.6f)),
            };

            return new GolfHoleDef("はじまりの浜", 2, new Vec2(3f, 1.6f), new Vec2(3f, 13.6f), ground, walls, areas);
        }

        /// <summary>
        /// ホール2「波打ち際」パー 3: 右に海が入り込んだ広い浜。まん中の砂州を越えるか、左から回るか。カップの右は海
        /// </summary>
        private static GolfHoleDef Shoreline()
        {
            var ground = MiniGameGeometry.Rect(0f, 0f, 12f, 16f);
            var walls = GolfHoleDef.WallsAround(ground);
            // 左の奥の流氷（低い壁）。左から回る道を少し狭くする
            walls.Add(GolfWall.Low(new Vec2(0f, 11f), new Vec2(3.2f, 11f)));

            var areas = new List<GolfArea>
            {
                // 右の海（波打ち際がうねる）
                GolfArea.Water(new[]
                {
                    new Vec2(8.6f, 0f), new Vec2(12f, 0f), new Vec2(12f, 16f), new Vec2(9.6f, 16f),
                    new Vec2(10.4f, 10.5f), new Vec2(8.2f, 5.5f),
                }),
                // まん中の砂州
                GolfArea.Sand(MiniGameGeometry.Rect(2.4f, 7f, 6.8f, 8.8f)),
            };

            return new GolfHoleDef("波打ち際", 3, new Vec2(3f, 1.6f), new Vec2(7f, 13.6f), ground, walls, areas);
        }

        /// <summary>
        /// ホール3「流氷の入り江」パー 4: 流氷の低い壁で3本の道に分かれた入り江を、つづら折りに上っていく。
        /// 角はよく弾むバンパー。水切りなら流氷を越えて近道できる。まん中の道に小さな池
        /// </summary>
        private static GolfHoleDef FloeInlet()
        {
            var ground = MiniGameGeometry.Rect(0f, 0f, 16f, 14f);
            var walls = GolfHoleDef.WallsAround(ground);
            // 流氷の壁: 手前の道とまん中の道（右が開いている）、まん中の道と奥の道（左が開いている）
            walls.Add(GolfWall.Low(new Vec2(0f, 4.7f), new Vec2(11f, 4.7f)));
            walls.Add(GolfWall.Low(new Vec2(5f, 9.3f), new Vec2(16f, 9.3f)));
            // 曲がり角のバンパー（斜め 45°）。角に当てると次の道へ向きが変わる
            walls.Add(new GolfWall(new Vec2(13.6f, 0f), new Vec2(16f, 2.4f), GolfBalance.BumperBounce));
            walls.Add(new GolfWall(new Vec2(13.6f, 9.3f), new Vec2(16f, 6.9f), GolfBalance.BumperBounce));
            walls.Add(new GolfWall(new Vec2(0f, 6.9f), new Vec2(2.2f, 4.7f), GolfBalance.BumperBounce));
            walls.Add(new GolfWall(new Vec2(0f, 11.6f), new Vec2(2.4f, 14f), GolfBalance.BumperBounce));

            var areas = new List<GolfArea>
            {
                // まん中の道の小さな池
                GolfArea.Water(MiniGameGeometry.Rect(7f, 6.2f, 9f, 7.8f)),
                // カップの手前の砂地
                GolfArea.Sand(MiniGameGeometry.Rect(9.4f, 10.2f, 11.2f, 13.2f)),
            };

            return new GolfHoleDef("流氷の入り江", 4, new Vec2(1.8f, 2.3f), new Vec2(13.6f, 11.8f), ground, walls, areas);
        }
    }
}
