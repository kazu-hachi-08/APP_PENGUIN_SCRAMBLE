using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>Core の盤の座標（X が右・Y がセンターの奥）と Unity の3D（Y が上）の行き来</summary>
    public static class BaseballSpace
    {
        public static Vector3 ToWorld(Vec2 point, float height = 0f) => new Vector3(point.X, height, point.Y);

        public static Vector3 Direction(Vec2 direction) => new Vector3(direction.X, 0f, direction.Y);

        public static Vec2 ToBoard(Vector3 world) => new Vec2(world.x, world.z);
    }
}
