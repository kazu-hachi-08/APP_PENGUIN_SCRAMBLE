using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>コースの仮の見た目: 地面・地面の種類の板・壁・カップと旗</summary>
    public partial class GolfHoleView
    {
        private static readonly Color GroundColor = new Color(0.46f, 0.76f, 0.4f);
        private static readonly Color WaterColor = new Color(0.32f, 0.6f, 0.95f);
        private static readonly Color OutColor = new Color(0.28f, 0.22f, 0.22f);
        private static readonly Color SandColor = new Color(0.96f, 0.86f, 0.56f);
        private static readonly Color SlopeColor = new Color(0.36f, 0.64f, 0.34f);
        private static readonly Color SlopeArrowColor = new Color(0.82f, 0.95f, 0.7f);
        private static readonly Color JumpColor = new Color(0.95f, 0.46f, 0.36f);
        private static readonly Color WallColor = new Color(0.64f, 0.46f, 0.3f);
        private static readonly Color BumperColor = new Color(1f, 0.74f, 0.2f);
        private static readonly Color SoftWallColor = new Color(0.56f, 0.72f, 0.86f);
        private static readonly Color CupColor = new Color(0.1f, 0.1f, 0.12f);
        private static readonly Color PoleColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color FlagColor = new Color(1f, 0.3f, 0.3f);

        // 地面の種類の板は地面より少しずつ上に置く（同じ高さだとちらつく）
        private const float AreaLift = 0.006f;
        private const float WallThickness = 0.14f;
        private const float CupLift = 0.03f;
        private static readonly Vector3 PoleScale = new Vector3(0.05f, 0.75f, 0.05f);
        private static readonly Vector3 FlagScale = new Vector3(0.5f, 0.3f, 0.03f);
        private static readonly Vector3 FlagOffset = new Vector3(0.27f, 1.32f, 0f);

        // 坂の矢印（下る向き）の長さと太さ
        private const float SlopeArrowLength = 1.2f;
        private const float SlopeArrowWidth = 0.12f;

        // 地面の絵の繰り返しの細かさ（1m あたり）
        private const float GroundUvScale = 0.25f;

        private void BuildCourse(GolfHoleDef hole)
        {
            CreateFlat("Ground", hole.Ground, 0f, GroundColor);
            for (var i = 0; i < hole.Areas.Count; i++)
            {
                BuildArea(hole.Areas[i], AreaLift * (i + 1));
            }

            foreach (var wall in hole.Walls)
            {
                BuildWall(wall);
            }

            BuildCup(hole.Cup);
        }

        private void BuildArea(GolfArea area, float lift)
        {
            CreateFlat(area.Kind.ToString(), area.Shape, lift, AreaColor(area.Kind));
            if (area.Kind == GolfAreaKind.Slope)
            {
                BuildSlopeArrow(area, lift);
            }
        }

        private static Color AreaColor(GolfAreaKind kind)
        {
            switch (kind)
            {
                case GolfAreaKind.Water: return WaterColor;
                case GolfAreaKind.OutOfBounds: return OutColor;
                case GolfAreaKind.Sand: return SandColor;
                case GolfAreaKind.Slope: return SlopeColor;
                default: return JumpColor;
            }
        }

        /// <summary>坂の真ん中に、下る向きの矢印（細い板）を置く</summary>
        private void BuildSlopeArrow(GolfArea area, float lift)
        {
            var center = Vec2.Zero;
            foreach (var point in area.Shape)
            {
                center += point;
            }

            center /= area.Shape.Count;
            var arrow = CreatePrimitive(PrimitiveType.Cube, "SlopeArrow", _materials.Floor(Texture2D.whiteTexture, SlopeArrowColor));
            arrow.localPosition = GolfSpace.ToWorld(center, lift + AreaLift);
            arrow.localRotation = Quaternion.LookRotation(GolfSpace.ToWorld(area.Push));
            arrow.localScale = new Vector3(SlopeArrowWidth, AreaLift, SlopeArrowLength);
        }

        private void BuildWall(GolfWall wall)
        {
            var along = GolfSpace.ToWorld(wall.B - wall.A);
            var color = wall.Bounce > 1f ? BumperColor : wall.Bounce < GolfBalance.WallBounce ? SoftWallColor : WallColor;
            var obj = CreatePrimitive(PrimitiveType.Cube, "Wall", _materials.Get(color, 0f));
            obj.localPosition = GolfSpace.ToWorld((wall.A + wall.B) * 0.5f, wall.Height * 0.5f);
            obj.localRotation = Quaternion.LookRotation(along);
            obj.localScale = new Vector3(WallThickness, wall.Height, along.magnitude);
        }

        private void BuildCup(Vec2 cup)
        {
            var hole = CreatePrimitive(PrimitiveType.Cylinder, "Cup", _materials.Floor(Texture2D.whiteTexture, CupColor));
            hole.localPosition = GolfSpace.ToWorld(cup, CupLift);
            hole.localScale = new Vector3(GolfBalance.CupRadius * 2f, 0.005f, GolfBalance.CupRadius * 2f);

            var pole = CreatePrimitive(PrimitiveType.Cylinder, "Pole", _materials.Get(PoleColor, 0f));
            pole.localPosition = GolfSpace.ToWorld(cup, PoleScale.y);
            pole.localScale = PoleScale;

            var flag = CreatePrimitive(PrimitiveType.Cube, "Flag", _materials.Get(FlagColor, 0f));
            flag.localPosition = GolfSpace.ToWorld(cup) + FlagOffset;
            flag.localScale = FlagScale;
        }

        /// <summary>多角形の平らな板（上向き）。地面とその上の種類の板に使う</summary>
        private void CreateFlat(string objectName, IReadOnlyList<Vec2> shape, float height, Color color)
        {
            var vertices = new Vector3[shape.Count];
            var uvs = new Vector2[shape.Count];
            var normals = new Vector3[shape.Count];
            for (var i = 0; i < shape.Count; i++)
            {
                vertices[i] = GolfSpace.ToWorld(shape[i], height);
                uvs[i] = new Vector2(shape[i].X, shape[i].Y) * GroundUvScale;
                normals[i] = Vector3.up;
            }

            // Core の三角形は上から見て左回り。Unity は表から見て右回りが表なので、並びを逆にする
            var indices = GolfGeometry.Triangulate(shape);
            indices.Reverse();
            var mesh = new Mesh { name = objectName, vertices = vertices, uv = uvs, normals = normals, triangles = indices.ToArray() };
            mesh.RecalculateBounds();
            _meshes.Add(mesh);

            var obj = new GameObject(objectName);
            obj.transform.SetParent(_course, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = _materials.Floor(Texture2D.whiteTexture, color);
        }
    }
}
