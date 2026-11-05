using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// おもちゃの木のクラブ（パター）。持ち手（グリップ）を原点に、柄が下（-Y）へ伸び、先に横長の頭が付く。
    /// 頭の帯は打つ子の属性の色（どの子で打つかを、クラブでも見分けられるように）。寸法はペンギンの背の高さ約1.2に対する値
    /// </summary>
    public static class GolfClub
    {
        public const float Length = 0.52f;

        private const float ShaftWidth = 0.045f;
        private const float GripWidth = 0.07f;
        private const float GripLength = 0.16f;
        private static readonly Vector3 HeadSize = new Vector3(0.24f, 0.085f, 0.11f);
        private static readonly Vector3 BandSize = new Vector3(0.08f, 0.092f, 0.118f);

        private static readonly Color ShaftColor = new Color(0.80f, 0.60f, 0.38f);
        private static readonly Color GripColor = new Color(0.30f, 0.22f, 0.20f);
        private static readonly Color HeadColor = new Color(0.92f, 0.76f, 0.52f);
        private const float WoodGloss = 0.35f;

        /// <summary>頭の横の半分の長さ（球に当てる面から頭の中心まで）</summary>
        public static float HeadHalfWidth => HeadSize.x * 0.5f;

        /// <summary>組み立てて、頭の中心（Head）を返す。根元が持ち手</summary>
        public static Transform Build(ToonMaterials materials, Transform grip, Color bandColor)
        {
            Add("Shaft", ToyMeshes.Puck, grip, Vector3.down * (Length * 0.5f), new Vector3(ShaftWidth, Length, ShaftWidth), materials.Get(ShaftColor, WoodGloss));
            Add("Grip", ToyMeshes.Puck, grip, Vector3.down * (GripLength * 0.4f), new Vector3(GripWidth, GripLength, GripWidth), materials.Get(GripColor, WoodGloss));
            var head = Add("Head", ToyMeshes.Puck, grip, Vector3.down * Length, HeadSize, materials.Get(HeadColor, WoodGloss));
            Add("Band", ToyMeshes.Puck, head.parent, head.localPosition, BandSize, materials.Get(bandColor, WoodGloss));
            return head;
        }

        private static Transform Add(string name, Mesh mesh, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            return obj.transform;
        }
    }
}
