using System.Collections.Generic;
using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// ゴール: 白い柱2本とバー、網を張る奥の枠（低い柱と上の棒）、揺れる網（SoccerNet）。
    /// 柱は内側の面がゴールの口の端に来るよう、太さの分だけ外へずらす（当たりは Core の口の幅のまま）
    /// </summary>
    public partial class SoccerView
    {
        private const float PostRadius = 0.08f;
        private const float PostGloss = 0.7f;
        // 奥の枠の高さ（バーに対する割合）。網の天井が奥へ向かって下がる
        private const float BackHeightRatio = 0.7f;
        private const float BackBarRadius = 0.05f;

        private static readonly Color PostColor = Color.white;

        private readonly List<SoccerNet> _nets = new List<SoccerNet>();

        private void BuildGoals(SoccerPitchDef pitch)
        {
            foreach (var goal in pitch.Goals)
            {
                BuildGoalFrame(goal);
                var net = new SoccerNet(goal, goal.BarHeight * BackHeightRatio);
                _nets.Add(net);
                AddOwnedMesh("Net", net.Mesh, Materials.Floor(Texture2D.whiteTexture, SoccerNet.Color));
            }
        }

        private void BuildGoalFrame(SoccerGoal goal)
        {
            var post = Materials.Get(PostColor, PostGloss);
            var backX = goal.LineX + goal.Outward * goal.Depth;
            var backHeight = goal.BarHeight * BackHeightRatio;
            var halfWidth = goal.HalfWidth + PostRadius;
            var barY = goal.BarHeight + PostRadius;
            foreach (var side in new[] { -1f, 1f })
            {
                var z = side * halfWidth;
                Bar(new Vector3(goal.LineX, 0f, z), new Vector3(goal.LineX, barY, z), PostRadius, post);
                Bar(new Vector3(backX, 0f, z), new Vector3(backX, backHeight, z), BackBarRadius, post);
                Bar(new Vector3(goal.LineX, barY, z), new Vector3(backX, backHeight, z), BackBarRadius, post);
                Bar(new Vector3(goal.LineX, BackBarRadius, z), new Vector3(backX, BackBarRadius, z), BackBarRadius, post);
            }

            Bar(new Vector3(goal.LineX, barY, -halfWidth), new Vector3(goal.LineX, barY, halfWidth), PostRadius, post);
            Bar(new Vector3(backX, backHeight, -halfWidth), new Vector3(backX, backHeight, halfWidth), BackBarRadius, post);
            Bar(new Vector3(backX, BackBarRadius, -halfWidth), new Vector3(backX, BackBarRadius, halfWidth), BackBarRadius, post);
        }

        /// <summary>a から b への丸い棒（両端は丸いので、太さの分だけ長くして角を埋める）</summary>
        private void Bar(Vector3 a, Vector3 b, float radius, Material material)
        {
            var along = b - a;
            AddPart("Bar", ToyMeshes.Puck, _pitch, (a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, along.normalized),
                new Vector3(radius * 2f, along.magnitude + radius * 2f, radius * 2f), material);
        }

        /// <summary>網に当たった・入った: そのゴールの網を、当たった所を中心に膨らませる</summary>
        private void ShakeNet(int goalTeam, Vector3 at, float speed)
        {
            if (goalTeam >= 0 && goalTeam < _nets.Count)
            {
                _nets[goalTeam].Hit(at, speed);
            }
        }

        private void TickNets(float dt)
        {
            foreach (var net in _nets)
            {
                net.Tick(dt);
            }

            TickBoards(dt);
        }
    }
}
