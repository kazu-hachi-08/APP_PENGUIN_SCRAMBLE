using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>Core の Vec2 と Unity の Vector2 の変換</summary>
    public static class CoreConversions
    {
        public static Vector2 ToVector2(this Vec2 v) => new Vector2(v.X, v.Y);
        public static Vec2 ToVec2(this Vector2 v) => new Vec2(v.x, v.y);
    }
}
