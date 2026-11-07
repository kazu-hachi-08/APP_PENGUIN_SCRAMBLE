using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>Core のピッチの座標（X が横・Y が縦）と Unity の3D（Y が上）の行き来</summary>
    public static class SoccerSpace
    {
        public static Vector3 ToWorld(Vec2 point, float height = 0f) => new Vector3(point.X, height, point.Y);

        public static Vector3 Direction(Vec2 direction) => new Vector3(direction.X, 0f, direction.Y);
    }
}
