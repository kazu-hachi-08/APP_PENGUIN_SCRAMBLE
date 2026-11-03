using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 開発用の試しのホール（Tools/PenmonScramble/Golf から開く）。球の計算の仕組みを一通り確かめられるよう、
    /// 地面の種類・壁の種類を全部1つずつ置く。本番のコースは GolfCourseTable（フェーズ 4 から）
    /// </summary>
    public static class GolfTestHoles
    {
        public static readonly GolfHoleDef TestHole = CreateTestHole();

        // L 字の地面: 手前の左の通路を奥へ上り、右へ曲がった先にカップ
        private static GolfHoleDef CreateTestHole()
        {
            var ground = new[]
            {
                new Vec2(0f, 0f), new Vec2(8f, 0f), new Vec2(8f, 14f),
                new Vec2(16f, 14f), new Vec2(16f, 22f), new Vec2(0f, 22f),
            };

            var walls = GolfHoleDef.WallsAround(ground);
            // 左奥の角の斜めのバンパー（よく弾む）。左の通路から当てて右へ曲げる
            walls.Add(new GolfWall(new Vec2(0f, 18.5f), new Vec2(3.5f, 22f), GolfBalance.BumperBounce));
            // 通路の途中の低い壁。手前のジャンプ台で越えるか、横を回る
            walls.Add(GolfWall.Low(new Vec2(2f, 9.6f), new Vec2(6f, 9.6f)));
            // カップの手前の柔らかい壁（跳ね返りが弱い）
            walls.Add(new GolfWall(new Vec2(10f, 15.5f), new Vec2(10f, 17f), GolfBalance.SoftWallBounce));

            var areas = new List<GolfArea>
            {
                // 右の壁ぎわの池
                GolfArea.Water(GolfGeometry.Rect(5.4f, 3.6f, 8f, 6.6f)),
                // 低い壁の手前のジャンプ台
                GolfArea.Jump(GolfGeometry.Rect(2.6f, 7.8f, 5.4f, 8.7f), 3.2f),
                // 曲がり角の手前の砂地
                GolfArea.Sand(GolfGeometry.Rect(0.4f, 12f, 4f, 15.5f)),
                // カップの奥の坂（左＝カップの方へ下る）。行き過ぎた球が戻ってくる
                GolfArea.Slope(GolfGeometry.Rect(13.4f, 14f, 16f, 22f), new Vec2(-2.4f, 0f)),
            };

            return new GolfHoleDef("試しのホール", 3, new Vec2(4f, 1.6f), new Vec2(12.6f, 18f), ground, walls, areas);
        }
    }
}
