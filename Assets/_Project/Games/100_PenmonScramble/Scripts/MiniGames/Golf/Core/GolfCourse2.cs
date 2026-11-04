using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// コース2「氷山」（氷の洞くつ）。氷の床（よく滑る）・氷の坂（押し戻される）・つららの壁（高い壁）・雪だまり（砂地と同じく止まりやすい）。
    /// パーは 2・3・4（合計 9）。座標は上から見た地面の上（X が右・Y が奥、m）
    /// </summary>
    public static class GolfCourse2
    {
        public static readonly IReadOnlyList<GolfHoleDef> Holes = new[]
        {
            IceSlide(),
            IceSlope(),
            IcicleCave(),
        };

        /// <summary>
        /// ホール1「氷のすべり台」パー 2: まっすぐな通りのまん中が氷の床。弱く打っても滑っていくので、強さを控えめに。
        /// 氷の上に互い違いのつらら。カップのまわりの雪だまりで止める
        /// </summary>
        private static GolfHoleDef IceSlide()
        {
            var ground = GolfGeometry.Rect(0f, 0f, 6f, 17f);
            var walls = GolfHoleDef.WallsAround(ground);
            // 互い違いのつらら（高い壁）
            walls.Add(new GolfWall(new Vec2(0f, 6.4f), new Vec2(2.6f, 6.4f)));
            walls.Add(new GolfWall(new Vec2(3.4f, 9.6f), new Vec2(6f, 9.6f)));

            var areas = new List<GolfArea>
            {
                GolfArea.Ice(GolfGeometry.Rect(0f, 3.4f, 6f, 12.2f)),
                // カップの奥と左右の雪だまり
                GolfArea.Sand(GolfGeometry.Rect(0f, 15.2f, 6f, 17f)),
                GolfArea.Sand(GolfGeometry.Rect(0f, 12.6f, 1.6f, 15.2f)),
                GolfArea.Sand(GolfGeometry.Rect(4.4f, 12.6f, 6f, 15.2f)),
            };

            return new GolfHoleDef("氷のすべり台", 2, new Vec2(3f, 1.6f), new Vec2(3f, 14f), ground, walls, areas);
        }

        /// <summary>
        /// ホール2「氷の坂」パー 3: 奥の段へ上る、凍った坂。弱いと滑り落ちてくる。坂の上の段は左右に分かれ、
        /// カップは右の段の奥。坂のまん中のつららの間を抜けるか、端を上る
        /// </summary>
        private static GolfHoleDef IceSlope()
        {
            var ground = GolfGeometry.Rect(0f, 0f, 12f, 17f);
            var walls = GolfHoleDef.WallsAround(ground);
            // 坂のまん中のつらら（短い高い壁が2本）
            walls.Add(new GolfWall(new Vec2(3.2f, 7.6f), new Vec2(5.2f, 7.6f)));
            walls.Add(new GolfWall(new Vec2(6.8f, 7.6f), new Vec2(8.8f, 7.6f)));
            // 上の段を左右に分ける氷の壁（左の段は行き止まり）
            walls.Add(new GolfWall(new Vec2(6f, 12.4f), new Vec2(6f, 17f)));

            var areas = new List<GolfArea>
            {
                // 凍った坂（手前へ下る。氷なので止まれず、押し戻される）
                GolfArea.Slope(GolfGeometry.Rect(0f, 5f, 12f, 10.4f), new Vec2(0f, -1.3f)),
                GolfArea.Ice(GolfGeometry.Rect(0f, 5f, 12f, 10.4f)),
                // 上の段の雪だまり（坂を上りきった球を止める）
                GolfArea.Sand(GolfGeometry.Rect(0.4f, 10.8f, 5.6f, 12.2f)),
                GolfArea.Sand(GolfGeometry.Rect(6.4f, 10.8f, 8.6f, 12.4f)),
            };

            return new GolfHoleDef("氷の坂", 3, new Vec2(4f, 1.8f), new Vec2(9.4f, 14.6f), ground, walls, areas);
        }

        /// <summary>
        /// ホール3「つららの洞くつ」パー 4: コの字に曲がった洞くつ。右の通りは氷の床で、奥の角の雪だまりで曲がる。
        /// 内側の角はよく弾むつららのバンパー。奥の通りは割れ目（池）をよけて左のカップへ
        /// </summary>
        private static GolfHoleDef IcicleCave()
        {
            // コの字: 手前の通り（左→右）・右の通り（手前→奥）・奥の通り（右→左）
            var ground = new[]
            {
                new Vec2(0f, 0f), new Vec2(16f, 0f), new Vec2(16f, 16f), new Vec2(0f, 16f),
                new Vec2(0f, 11f), new Vec2(11f, 11f), new Vec2(11f, 5f), new Vec2(0f, 5f),
            };
            var walls = GolfHoleDef.WallsAround(ground);
            // 内側の角のつららのバンパー（斜め）
            walls.Add(new GolfWall(new Vec2(11f, 4.2f), new Vec2(11.8f, 5f), GolfBalance.BumperBounce));
            walls.Add(new GolfWall(new Vec2(11f, 11.8f), new Vec2(11.8f, 11f), GolfBalance.BumperBounce));
            // 外側の角の斜めの壁（角に当てると次の通りへ向きが変わる）
            walls.Add(new GolfWall(new Vec2(14f, 0f), new Vec2(16f, 2f)));
            walls.Add(new GolfWall(new Vec2(16f, 14f), new Vec2(14f, 16f)));
            // 奥の通りのつらら
            walls.Add(new GolfWall(new Vec2(6.5f, 16f), new Vec2(6.5f, 14.2f)));

            var areas = new List<GolfArea>
            {
                // 右の通りの氷の床
                GolfArea.Ice(new[] { new Vec2(11f, 3f), new Vec2(16f, 3f), new Vec2(16f, 13f), new Vec2(11f, 13f) }),
                // 奥の通りの氷の割れ目
                GolfArea.Water(GolfGeometry.Rect(7.6f, 11f, 9.4f, 13.2f)),
                // 奥の右の角の雪だまり
                GolfArea.Sand(GolfGeometry.Rect(13f, 13.4f, 16f, 16f)),
                // カップの奥の雪だまり
                GolfArea.Sand(GolfGeometry.Rect(0f, 11f, 1.6f, 16f)),
            };

            return new GolfHoleDef("つららの洞くつ", 4, new Vec2(1.8f, 2.5f), new Vec2(3.2f, 13.5f), ground, walls, areas);
        }
    }
}
