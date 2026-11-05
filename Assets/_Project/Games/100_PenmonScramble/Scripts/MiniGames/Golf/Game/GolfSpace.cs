using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>Core の地面の座標（X が右・Y が奥）と Unity の3D（Y が上）の行き来</summary>
    public static class GolfSpace
    {
        public static Vector3 ToWorld(Vec2 point, float height = 0f) => new Vector3(point.X, height, point.Y);
    }
}
