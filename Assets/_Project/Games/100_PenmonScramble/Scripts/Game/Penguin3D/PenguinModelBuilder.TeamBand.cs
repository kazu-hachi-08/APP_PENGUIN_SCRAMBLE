using UnityEngine;
using UnityEngine.Rendering;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ミニゲームのチーム分けのはちまき。種類の飾り（Ornament）とは別に、組み立てた後から頭に巻く。
    /// 色を変える部品（ToyModel.SetTint）には登録しない: 野生の子を暗くしても、凍っても、チームの色がはっきり残るように
    /// </summary>
    public static partial class PenguinModelBuilder
    {
        // 種類のはちまき（HeadbandHeight）と同じ高さで、少し外・少し太く巻く（はちまき・ゴーグルを付けた種類は、その上からチームの色で覆うため）
        private const float TeamBandOutset = 0.025f;
        private const float TeamBandThickness = 0.085f;
        private const float TeamBandTilt = -14f;
        private static readonly Vector3 TeamBandTailSize = new Vector3(0.09f, 0.26f, 0.05f);

        /// <summary>チームの色のはちまきを頭に巻く（後ろで結んだ端を2本垂らす）。look は model を組み立てたときのもの</summary>
        public static void AddTeamHeadband(PenguinModel model, PenguinLook look, ToonMaterials materials, Color color)
        {
            var s = look.HeadSize;
            var material = materials.Get(color, ClothGloss);
            var height = HeadbandHeight;
            var radius = Mathf.Sqrt(HeadRadius * HeadRadius - height * height) + TeamBandOutset;
            var center = (HeadCenter + Vector3.up * height) * s;
            var bandScale = new Vector3(radius / TorusCenterRadius, TeamBandThickness / TorusTubeDiameter, radius / TorusCenterRadius) * s;
            AddTeamPart("TeamBand", ToyMeshes.Torus, model.Head, center, Quaternion.Euler(TeamBandTilt, 0f, 0f), bandScale, material);

            for (var side = -1f; side <= 1f; side += 2f)
            {
                var tail = new Vector3(0.05f * side, HeadCenter.y + height, -radius - 0.06f) * s;
                AddTeamPart("TeamBandTail", ToyMeshes.Drop, model.Head, tail, Point(0.4f * side, -0.5f, -1f), TeamBandTailSize * s, material);
            }
        }

        private static void AddTeamPart(string objName, Mesh mesh, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            var obj = new GameObject(objName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            // ミニゲームは足元の丸い影なので、本物の影は落とさない
            ToyParts.AddMesh(obj, mesh, material).shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
