using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// ピッチを囲む低い柵（ボード）: 角の丸い板を寝かせた本体と、上の色の帯。色はワールドの柵の色（本編の闘技場・ゴルフの柵と同じ）。
    /// 内側の面が当たりの線に来るよう、板の厚みの半分だけ外へずらして置く。ゴールの口・網は .Goals.cs
    /// </summary>
    public partial class SoccerView
    {
        private const float BoardHeight = 0.55f;
        private const float BoardThickness = 0.24f;
        private const float BoardGloss = 0.35f;
        private const float BoardCapHeight = 0.08f;
        private const float BoardCapWiden = 1.3f;
        // ボールが当たった板がぽよんと弾む長さと膨らむ割合
        private const float BoardPopSeconds = 0.2f;
        private const float BoardPopScale = 0.25f;
        private const float BoardPopReach = 1.2f;

        private readonly System.Collections.Generic.List<BoardPart> _boards = new System.Collections.Generic.List<BoardPart>();

        private void BuildBoards(SoccerPitchDef pitch, StageTheme theme)
        {
            _boards.Clear();
            foreach (var wall in pitch.Walls)
            {
                if (wall.Kind == SoccerWallKind.Board)
                {
                    _boards.Add(BuildBoard(wall, theme));
                }
            }
        }

        private BoardPart BuildBoard(SoccerWall wall, StageTheme theme)
        {
            var edge = wall.B - wall.A;
            // 外周は左回りなので、辺の右手が外
            var outward = new Vec2(edge.Y, -edge.X).Normalized;
            var along = SoccerSpace.Direction(edge);
            var root = new GameObject("Board").transform;
            root.SetParent(_pitch, false);
            root.localPosition = SoccerSpace.ToWorld((wall.A + wall.B) * 0.5f + outward * (BoardThickness * 0.5f));
            root.localRotation = Quaternion.LookRotation(along);

            // 両端は丸いので、厚みだけ長くして角を埋める
            var length = along.magnitude + BoardThickness;
            // 板はマットの縁の外（マットより低い外の地面の上）に立つので、外の地面まで下ろす
            var bottom = -(TurfDepth + OutsideDrop);
            var height = BoardHeight - bottom;
            AddPart("Body", ToyMeshes.Puck, root, Vector3.up * (bottom + height * 0.5f), Quaternion.identity,
                new Vector3(BoardThickness, height, length), Materials.Get(theme.FencePost, BoardGloss));
            AddPart("Cap", ToyMeshes.Puck, root, Vector3.up * BoardHeight, Quaternion.identity,
                new Vector3(BoardThickness * BoardCapWiden, BoardCapHeight, length + BoardThickness * (BoardCapWiden - 1f)), Materials.Get(theme.FenceRail, BoardGloss));
            return new BoardPart(wall, root);
        }

        /// <summary>ボールが強く当たった所の板を弾ませる</summary>
        private void PopBoardNear(Vec2 point)
        {
            foreach (var part in _boards)
            {
                var closest = MiniGames.Core.MiniGameGeometry.ClosestOnSegment(part.Wall.A, part.Wall.B, point);
                if (Vec2.Distance(closest, point) <= BoardPopReach)
                {
                    part.PopLeft = BoardPopSeconds;
                }
            }
        }

        private void TickBoards(float dt)
        {
            foreach (var part in _boards)
            {
                if (part.PopLeft <= 0f)
                {
                    continue;
                }

                part.PopLeft = Mathf.Max(0f, part.PopLeft - dt);
                var swell = Mathf.Sin((1f - part.PopLeft / BoardPopSeconds) * Mathf.PI) * BoardPopScale;
                part.Root.localScale = new Vector3(1f + swell, 1f + swell * 0.5f, 1f);
            }
        }

        private sealed class BoardPart
        {
            public readonly SoccerWall Wall;
            public readonly Transform Root;
            public float PopLeft;

            public BoardPart(SoccerWall wall, Transform root)
            {
                Wall = wall;
                Root = root;
            }
        }
    }
}
