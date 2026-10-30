using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// Core の Vec2 と Unity の座標の変換。Core は床の上の2D（x が右、y が奥）なので、3D では床の (x, 高さ, y) に置く
    /// </summary>
    public static class CoreConversions
    {
        public static Vector2 ToVector2(this Vec2 v) => new Vector2(v.X, v.Y);
        public static Vec2 ToVec2(this Vector2 v) => new Vec2(v.x, v.y);

        /// <summary>Core の位置を、床から height だけ上の3Dの位置にする</summary>
        public static Vector3 ToFloor(this Vec2 v, float height = 0f) => new Vector3(v.X, height, v.Y);

        /// <summary>床の上の2D（Vector2）を3Dにする。補間した位置など、Core の値を Vector2 で持っているとき用</summary>
        public static Vector3 ToFloor(this Vector2 v, float height = 0f) => new Vector3(v.x, height, v.y);
    }
}
